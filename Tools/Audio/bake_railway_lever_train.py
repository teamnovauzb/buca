"""Mix the game's original train whistle/steam with the lever's mechanical latch.
All inputs are project-generated Foley; no external recordings. Run from project root.
"""
from pathlib import Path
import array, math, struct, wave
RATE=48000
DURATION=1.72

def load(path):
    with wave.open(str(path)) as w:
        assert w.getsampwidth()==2
        return array.array('h',w.readframes(w.getnframes())),w.getframerate(),w.getnchannels()

def sample(source,t,channel):
    data,rate,channels=source
    if t<0:return 0.
    pos=t*rate;i=int(pos);fraction=pos-i
    if (i+1)*channels>=len(data):return 0.
    c=min(channel,channels-1)
    return (data[i*channels+c]*(1-fraction)+data[(i+1)*channels+c]*fraction)/32768.

latch=load(Path('Assets/Audio/UI/RailwayLeverLatch.wav'))
whistle=load(Path('Assets/Audio/Celebration/StationWhistle.wav'))
steam=load(Path('Assets/Audio/Celebration/StationIdleSteam.wav'))
channels=[]
for channel in range(2):
    track=[]
    for i in range(round(DURATION*RATE)):
        t=i/RATE
        # Immediate physical lever click; familiar two-note train whistle follows.
        value=.60*sample(latch,t,channel)
        value+=2.15*sample(whistle,t-.025-channel*.002,channel)
        pressure=math.sin(math.pi*min(1,max(0,(t-.045)/.9)))**2 if .045<t<.945 else 0
        value+=1.05*pressure*sample(steam,t+.16+channel*.004,channel)
        value*=min(1,t/.002,max(0,(DURATION-t)/.06))
        track.append(value)
    channels.append(track)
peak=max(abs(v) for track in channels for v in track);gain=.79/peak
path=Path('Assets/Audio/UI/RailwayLeverTrainPull.wav')
with wave.open(str(path),'wb') as w:
    w.setparams((2,2,RATE,0,'NONE','not compressed'))
    w.writeframes(b''.join(struct.pack('<hh',round(a*gain*32767),round(b*gain*32767)) for a,b in zip(*channels)))
path.with_suffix('.source.txt').write_text('Original project Foley mix: RailwayLeverLatch.wav (Tools/Audio/bake_menu_lever.py), StationWhistle.wav and StationIdleSteam.wav (Tools/Audio/bake_station_train.py). Immediate mechanical lever action, two-note train whistle and steam release. Stereo 48 kHz PCM; 1.72 seconds; peak -2.05 dBFS. Rebuild with Tools/Audio/bake_railway_lever_train.py. No external recordings.\n')
print(path)
print('duration',DURATION,'peak dBFS',round(20*math.log10(.79),2))
