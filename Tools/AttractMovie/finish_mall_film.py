"""Assemble the 40-second motion commercial, original music, game audio and titles."""
from __future__ import annotations
import argparse
import json
from pathlib import Path
import subprocess
import wave

import cv2
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/"output/buca-mall-film"
FFMPEG="/private/tmp/buca-local-video/bin/ffmpeg"
SR=48000
N=40*SR


def decode_audio(path):
    result=subprocess.run([FFMPEG,"-hide_banner","-loglevel","error","-i",str(path),
        "-vn","-f","f32le","-ac","2","-ar",str(SR),"-"],capture_output=True,check=True)
    return np.frombuffer(result.stdout,dtype="<f4").reshape(-1,2).copy()


def mix_audio():
    rng=np.random.default_rng(839)
    music=decode_audio(ROOT/"output/buca-mall-story/fun-music.wav")[:N]
    mix=np.zeros((N,2),np.float32)
    music_gain=np.full(N,1.25,np.float32)
    # Make space for the shared cheer without burying the happy music.
    for start,end,gain in [(20.3,21.1,.90),(25.55,26.25,.85),(28.7,32.3,.72)]:
        music_gain[int(start*SR):int(end*SR)]=gain
    # Smooth the gain envelope over 30 ms.
    kernel=np.ones(1441,dtype=np.float32)/1441
    music_gain=np.convolve(music_gain,kernel,mode="same")
    mix[:len(music)]+=music*music_gain[:len(music),None]

    def add(sound,time,gain=1,pan=0):
        start=int(time*SR)
        if start>=N:
            return
        if sound.ndim==1:
            sound=np.stack([sound*np.sqrt((1-pan)/2),sound*np.sqrt((1+pan)/2)],axis=1)
        length=min(N-start,len(sound))
        mix[start:start+length]+=sound[:length]*gain

    # Quiet ventilation / spacious indoor room tone, with no invented speech.
    noise=rng.normal(0,1,(N,2)).astype(np.float32)
    for channel in range(2):
        noise[:,channel]=np.convolve(noise[:,channel],np.ones(81)/81,mode="same")
    mix+=noise*.012
    # Restrained footsteps for the opening tracking shot.
    for j,time in enumerate(np.arange(.25,5.85,.59)):
        t=np.arange(int(.17*SR))/SR
        step=rng.normal(0,1,len(t))*np.exp(-t*55)
        step=np.convolve(step,np.ones(11)/11,mode="same")
        step+=np.sin(2*np.pi*92*t)*np.exp(-t*44)*.12
        add(step,time,.034,(-.15 if j%2 else .15))

    def cue(relative,time,gain,maximum=None):
        sound=decode_audio(ROOT/relative)
        if maximum:
            sound=sound[:int(maximum*SR)]
        add(sound,time,gain)

    cue("Assets/Audio/UI/ButtonClick.ogg",14.82,.23)
    cue("Assets/Audio/UI/ButtonClick.ogg",19.8333,.18)
    cue("Assets/Audio/UI/ButtonClick.ogg",20.5,.17)
    cue("Assets/Audio/Player/PuckLaunch.ogg",20.5,.32)
    cue("Assets/Audio/Player/HoleSink.ogg",25.8,.52)
    cue("Assets/Audio/Music/WinJingle.wav",28.65,.24,3.8)
    cue("Assets/Audio/Celebration/HappyYeah.wav",29.45,.48)

    # The matching performance audio was checked for unintended speech and
    # trimmed to its brief laugh; keep its timing aligned with the boy.
    laugh=ART/"audio/celebration-laugh.wav"
    if laugh.exists():
        add(decode_audio(laugh),30,.35)
    fade_in=np.minimum(np.arange(N)/(SR*.2),1)
    fade_out=np.minimum((N-1-np.arange(N))/(SR*.8),1)
    mix*=np.minimum(fade_in,fade_out)[:,None]
    peak=float(np.max(np.abs(mix)))
    if peak>.86:
        mix*=.86/peak
    target=ART/"audio/film-mix.wav"
    target.parent.mkdir(exist_ok=True)
    with wave.open(str(target),"wb") as out:
        out.setnchannels(2);out.setsampwidth(2);out.setframerate(SR)
        out.writeframes((np.clip(mix,-1,1)*32767).astype("<i2").tobytes())
    print(f"Balanced 40-second stereo mix saved: {target}",flush=True)
    return target


def finish(height):
    from composite_mall_film import SCENES
    folder=ART/f"composited-{height}p"
    missing=[folder/f"{name}.mp4" for name in SCENES if not (folder/f"{name}.mp4").exists()]
    if missing:
        raise RuntimeError("Cannot deliver an incomplete film. Missing: "+", ".join(map(str,missing)))
    concat=ART/f"edit-{height}p.txt"
    concat.write_text("".join(f"file '{folder/name}.mp4'\n" for name in SCENES))
    audio=ART/"audio/film-mix.wav"
    if not audio.exists():
        audio=mix_audio()
    scale=height/1080
    font="/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf"
    # Keep the final title above the cabinet, away from the boy's face and LCD.
    x=round(1430*scale);y=round(57*scale)
    alpha="if(lt(t,36),0,if(lt(t,36.3),(t-36)/0.3,1))"
    title=(f"drawtext=fontfile='{font}':text='BUCA':fontcolor=white:"
           f"fontsize={round(84*scale)}:x={x}-tw/2:y={y}:"
           f"borderw={round(2*scale)}:bordercolor=0x132b3b@0.9:"
           f"shadowcolor=0x132b3b@0.75:shadowx=0:shadowy={round(3*scale)}:alpha='{alpha}',"
           f"drawtext=fontfile='{font}':text='IT’S YOUR TURN!':fontcolor=white:"
           f"fontsize={round(33*scale)}:x={x}-tw/2:y={y+round(94*scale)}:"
           f"borderw={round(1*scale)}:bordercolor=0x132b3b@0.9:"
           f"shadowcolor=0x132b3b@0.8:shadowx=0:shadowy={round(2*scale)}:alpha='{alpha}'")
    suffix="4K-Upscaled" if height==2160 else f"{height}p-Preview"
    target=ART/f"BUCA-Mall-Commercial-40s-{suffix}.mp4"
    command=[FFMPEG,"-y","-hide_banner","-loglevel","warning","-f","concat","-safe","0",
        "-i",str(concat),"-i",str(audio),"-map","0:v:0","-map","1:a:0","-t","40",
        "-vf",title,"-af","loudnorm=I=-16:TP=-1:LRA=8","-c:v","libx264",
        "-preset","medium","-crf","17","-pix_fmt","yuv420p","-threads","3",
        "-c:a","aac","-b:a","256k","-ar","48000",
        "-metadata","title=BUCA — IT’S YOUR TURN!",
        "-metadata","comment=AI-generated mall and human scenes; actual BUCA gameplay. 4K edition is upscaled.",
        "-movflags","+faststart",str(target)]
    subprocess.run(command,check=True)
    # Decode the complete delivery rather than checking only its header.
    subprocess.run([FFMPEG,"-v","error","-i",str(target),"-f","null","-"],check=True)
    cap=cv2.VideoCapture(str(target))
    frames=int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    frame_rate=cap.get(cv2.CAP_PROP_FPS)
    dimensions=[int(cap.get(cv2.CAP_PROP_FRAME_WIDTH)),int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))]
    cap.release()
    if frames!=960 or abs(frame_rate-24)>.001 or dimensions!=[height*16//9,height]:
        raise RuntimeError(f"Unexpected output geometry/timing: {frames}, {frame_rate}, {dimensions}")
    report={"file":str(target),"duration_seconds":40,"frame_rate":24,
            "resolution":dimensions,"frame_count":frames,"full_decode":"passed",
            "visual_review":"pending",
            "human_scenes":"AI generated locally, native 1024 x 576",
            "screen_content":"Actual BUCA capture and supplied game artwork",
            "integrated_into_game":False}
    (ART/f"delivery-check-{height}p.json").write_text(json.dumps(report,indent=2)+"\n")
    print(f"40-second commercial assembled and fully decoded: {target}",flush=True)


if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--height",type=int,choices=[1080,2160],default=1080)
    parser.add_argument("--audio-only",action="store_true")
    args=parser.parse_args()
    if args.audio_only:
        mix_audio()
    else:
        finish(args.height)
