"""Original tactile wooden latch and restrained stereo menu-return sweep. No samples."""
from pathlib import Path
import math, random, wave, struct
R=48000
folder=Path('Assets/Audio/UI');folder.mkdir(parents=True,exist_ok=True)
def bake(name,seconds,synth):
    channels=[]
    for ch in range(2):
        rng=random.Random(760+ch);lo=0.;out=[]
        for i in range(round(seconds*R)):
            t=i/R;n=rng.uniform(-1,1);lo+=.045*(n-lo)
            value=synth(t,n,lo,ch)
            value*=min(1,t/.002,(seconds-t)/.025)
            out.append(value)
        channels.append(out)
    peak=max(abs(x) for c in channels for x in c);gain=.63/max(.001,peak)
    path=folder/(name+'.wav')
    with wave.open(str(path),'wb') as f:
        f.setparams((2,2,R,0,'NONE','not compressed'))
        f.writeframes(b''.join(struct.pack('<hh',round(l*gain*32767),round(r*gain*32767)) for l,r in zip(*channels)))
    print(name, 'duration',seconds,'peak dBFS',round(20*math.log10(.63),2))
def impact(t,at,freq,decay):
    x=t-at
    if x<0:return 0
    return (1-math.exp(-x*1400))*math.exp(-x*decay)*(math.sin(2*math.pi*freq*x)+.25*math.sin(2*math.pi*freq*2.76*x))
def lever(t,n,lo,ch):
    latch=.42*impact(t,.008,225,55)+.65*impact(t,.345,172,30)+.11*impact(t,.357,1280,60)
    friction=lo*.34*math.sin(math.pi*min(1,t/.38))**2 if t<.38 else 0
    detent=n*.035*math.exp(-abs(t-.345)*150)
    return latch+friction+detent
def sweep(t,n,lo,ch):
    env=math.sin(math.pi*min(1,t/.72))**2
    air=lo*env*.7*(.92+.08*math.sin(t*5+ch*.5))
    resolve=.075*impact(t,.29+ch*.003,523.25,9)+.045*impact(t,.31+ch*.003,783.99,10)
    return air+resolve
bake('RailwayLeverLatch',.62,lever)
bake('RailwayMenuReturn',.95,sweep)
(folder/'RailwayMenuSounds.source.txt').write_text('Original procedural audio by Tools/Audio/bake_menu_lever.py. Stereo 48 kHz 16-bit PCM. Peak -4 dBFS. Wooden latch with subdued brass detent; soft low-passed transition sweep and gentle resolution. No third-party recordings.\n')
