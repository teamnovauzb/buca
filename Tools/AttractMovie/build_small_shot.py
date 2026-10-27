"""BUCA: Small Shot, Big Adventure. Original 32-second loop; existing captured gameplay.
Run with numpy, Pillow and imageio-ffmpeg. --preview writes a storyboard only.
No project scenes or existing films are modified.
"""
from pathlib import Path
from functools import lru_cache
import math, subprocess, wave, sys, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'output/buca-small-shot';OUT.mkdir(exist_ok=True)
SRC=ROOT/'output/buca-luxodd-attract'
W,H=1920,1080;FPS=30;DURATION=32
FF=imageio_ffmpeg.get_ffmpeg_exe()
CREAM='#fff2d1';GOLD='#f4c35e';TEAL='#65d5c5';INK='#102c30'
def ease(x):
 x=max(0,min(1,x));return x*x*(3-2*x)
@lru_cache(None)
def font(s):return ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf',s)
def txt(im,s,xy,size,color=CREAM):
 ImageDraw.Draw(im).text(xy,s,font=font(size),fill=color,anchor='mm',stroke_width=0)
def frame_source(kind,t):
 path=SRC/('revision-4/level30/frames' if kind=='game' else 'worlds/frames')/f'{min(359 if kind=="game" else 869,max(0,round(t*30))):04d}.jpg'
 return Image.open(path).convert('RGB')
def crop(im,rect):return im.transform((W,H),Image.Transform.EXTENT,rect,Image.Resampling.BICUBIC)
def mixrect(a,b,p):return tuple(x+(y-x)*p for x,y in zip(a,b))
# Subtle printed-paper gradient, with no random animation flicker.
y,x=np.mgrid[0:H,0:W];v=np.clip(1-np.sqrt(((x-960)/1250)**2+((y-480)/900)**2),0,1)
base=np.zeros((H,W,3),np.uint8)
for c,(a,b) in enumerate([(9,22),(27,55),(31,57)]):base[:,:,c]=a+(b-a)*v
BG=Image.fromarray(base)
def puck(im,cx,cy,r=40):
 d=ImageDraw.Draw(im);d.ellipse((cx-r,cy-r+9,cx+r,cy+r+9),fill='#856127');d.ellipse((cx-r,cy-r,cx+r,cy+r),fill=GOLD);d.ellipse((cx-r+8,cy-r+8,cx+r-8,cy+r-8),outline=CREAM,width=3)
def graphic(t,end=False):
 im=BG.copy();d=ImageDraw.Draw(im)
 # A single looping orbit becomes the visual identity of the film.
 cx,cy=960,540;r=394
 d.ellipse((cx-r,cy-r,cx+r,cy+r),outline='#36504b',width=2)
 d.arc((cx-r,cy-r,cx+r,cy+r),-80,-20,fill=TEAL,width=5)
 angle=-math.pi/2+t*math.pi*.5
 puck(im,cx+r*math.cos(angle),cy+r*math.sin(angle),28)
 if end:
  txt(im,'BUCA',(960,385),184)
  txt(im,'SMALL SHOT. BIG ADVENTURE.',(960,557),47,GOLD)
  txt(im,'YOUR TURN',(960,704),65,TEAL)
  txt(im,'AIM  /  HOLD  /  RELEASE',(960,800),24)
 else:
  txt(im,'ONE LITTLE PUCK.',(960,416),78)
  txt(im,'A WORLD OF POSSIBILITY.',(960,532),58,GOLD)
  txt(im,'BUCA',(960,685),43,TEAL)
 return im

def caption(im,small,big):
 layer=Image.new('RGBA',(W,H));d=ImageDraw.Draw(layer)
 for yy in range(780,H):d.line((0,yy,W,yy),fill=(5,20,23,round(215*(yy-780)/300)))
 im=Image.alpha_composite(im.convert('RGBA'),layer).convert('RGB')
 txt(im,small,(960,904),24,GOLD);txt(im,big,(960,975),55)
 return im

def scene(t):
 if t<3.5:
  im=graphic(t)
  if t>2.65:
   p=ease((t-2.65)/.85);art=frame_source('game',0)
   mask=Image.new('L',(W,H));d=ImageDraw.Draw(mask);r=1200*p;d.ellipse((960-r,540-r,960+r,540+r),fill=255);im.paste(art,(0,0),mask)
  return im
 if t<6:
  u=t-3.5;im=frame_source('game',u*1.55)
  return caption(im,'01 / FIND YOUR ANGLE','A little precision.')
 if t<8:
  u=t-6;im=frame_source('game',3.875+u*1.0625)
  return caption(im,'02 / FEEL THE MOMENT','A little anticipation.')
 if t<11:
  # Continuous, normal-speed launch and real physics through the verified win.
  return frame_source('game',6+t-8)
 if t<13.5:
  im=frame_source('game',9+(t-11)*.75)
  return im
 if t<16:
  u=t-13.5;im=frame_source('world',5.5+u)
  return caption(im,'ONE SHOT CAN START SOMETHING','Much bigger.')
 if t<24:
  u=t-16;src=float(np.interp(u,[0,3.4,4.0,4.5,8],[10,15.3,16.6,19.3,22.8]));im=frame_source('world',src)
  rect=mixrect((920,160,1920,722),(1000,250,1920,767.5),ease(u/8))
  im=crop(im,rect)
  return caption(im,'THE ADVENTURE KEEPS MOVING','Next stop: wonder.')
 if t<25.5:
  im=frame_source('world',22.8+(t-24));return caption(im,'WELCOME TO WORLD 2','Play past the ordinary.')
 if t<30.6:return graphic(t-25.5,True)
 # Return to the exact opening state for cabinet looping.
 return Image.blend(graphic(t-25.5,True),graphic(0),ease((t-30.6)/1.3666667))
def frame(t):
 im=scene(t)
 for cut in [3.5,6,13.5,16,24,25.5]:
  if cut<=t<cut+.25:im=Image.blend(scene(cut-1/FPS),im,ease((t-cut)/.25))
 return im

def audio():
 sr=48000;n=sr*DURATION;mix=np.zeros((n,2),np.float64);rng=np.random.default_rng(2026)
 def add(a,at,amp=1,pan=0):
  st=round(at*sr);end=min(n,st+len(a))
  if end>st:
   a=a[:end-st]*amp;mix[st:end,0]+=a*math.sqrt((1-pan)/2);mix[st:end,1]+=a*math.sqrt((1+pan)/2)
 def note(midi,at,dur,amp=.14,pan=0):
  tt=np.arange(round(sr*dur))/sr;f=440*2**((midi-69)/12)
  a=(np.sin(2*np.pi*f*tt)+.32*np.sin(2*np.pi*f*2*tt)+.13*np.sin(2*np.pi*f*3*tt))*np.exp(-tt*5/dur)*np.minimum(tt/.008,1)
  a*=np.minimum((dur-tt)/.025,1);add(a,at,amp,pan)
 # Original clockwork marimba theme in D major, 120 BPM.
 chords=[[50,57,62,66],[47,54,59,62],[43,50,55,59],[45,52,57,61]]
 melody=[74,78,81,78,76,74,69,73,74,78,83,81,78,76,73,69]
 for beat in range(60):
  at=beat*.5;ch=chords[(beat//4)%4];gain=.65 if at<3.5 else 1
  if 8<at<9.4:gain=.22
  note(ch[0],at,.46,.17*gain)
  note(ch[1+(beat%3)],at+.25,.7,.1*gain,(-1)**beat*.45)
  note(melody[beat%16],at,.65,.115*gain,math.sin(beat)*.25)
  if at>=3.5:
   tt=np.arange(4800)/sr;hit=np.sin(2*np.pi*(75*tt-190*tt**2))*np.exp(-tt*42);add(hit,at,.13*gain)
   noise=rng.normal(0,1,2200)*np.exp(-np.arange(2200)/400);add(noise,at+.25,.025*gain,.3)
 for i,m in enumerate([62,66,69,74,78,81]):note(m,25.5+i*.09,2.5,.15)
 def fx(path,at,gain):
  p=subprocess.run([FF,'-v','error','-i',str(ROOT/path),'-f','f32le','-ar',str(sr),'-ac','1','-'],capture_output=True,check=True)
  add(np.frombuffer(p.stdout,np.float32),at,gain)
 fx('Assets/Audio/Player/PuckLaunch.ogg',8,.5)
 fx('Assets/Audio/Player/HoleSink.ogg',9.4,.55)
 fx('Assets/Audio/Celebration/HoleInOneTwinFireworks.wav',9.4,.22)
 fx('Assets/Audio/Celebration/StationWhistle.wav',16.3,.24)
 mix[:2400]*=np.linspace(0,1,2400)[:,None];mix[-int(sr*1.5):]*=np.linspace(1,0,int(sr*1.5))[:,None]
 mix*=.88/max(.88,abs(mix).max())
 with wave.open(str(OUT/'original-score.wav'),'wb') as f:
  f.setnchannels(2);f.setsampwidth(2);f.setframerate(sr);f.writeframes((mix*32767).astype('<i2').tobytes())
 return float(abs(mix).max())
def preview():
 times=[0,2,3.4,4.5,7,8.6,9.6,12,14.5,17,20.5,23,24.5,27,30,31.966]
 sheet=Image.new('RGB',(1920,4*297),INK);d=ImageDraw.Draw(sheet)
 for i,t in enumerate(times):
  im=frame(t);im.save(OUT/f'frame-{t:05.2f}.jpg',quality=93)
  xx=i%4*480;yy=i//4*297;sheet.paste(im.resize((480,270)),(xx,yy));d.text((xx+12,yy+274),f'{t:.2f}s',fill=CREAM,font=font(16))
 sheet.save(OUT/'storyboard.jpg',quality=94)
if __name__=='__main__':
 preview()
 if '--preview' in sys.argv:sys.exit()
 peak=audio();target=OUT/'BUCA-Small-Shot-Big-Adventure.mp4'
 p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','1920x1080','-r','30','-i','-','-i',str(OUT/'original-score.wav'),'-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4','-c:a','aac','-b:a','192k','-movflags','+faststart','-shortest',str(target)],stdin=subprocess.PIPE)
 for i in range(FPS*DURATION):
  p.stdin.write(frame(i/FPS).tobytes())
  if i%90==0:print(f'Rendered {i}/{FPS*DURATION}',flush=True)
 p.stdin.close();assert p.wait()==0
 (OUT/'production.json').write_text(json.dumps({'title':'Small Shot. Big Adventure.','duration':32,'resolution':[W,H],'fps':FPS,'music':'Original procedurally composed marimba score','footage':'Existing project Unity captures; Level 30 win frame 222','audio_peak':peak,'integration':'Standalone film; no automatic playback changes'},indent=2))
 print(target,flush=True)
