"""Original strong gusting stereo wind and dry foliage rustle, crossfaded loop."""
import math, random, struct, wave
from pathlib import Path
rate=24000
channels=[]
for channel in range(2):
    rng=random.Random(8841+channel); low=body=soft=0.; samples=[]
    for i in range(rate*34):
        t=i/rate; n=rng.uniform(-1,1)
        low+=.008*(n-low); body+=.065*(n-body); soft+=.28*(n-soft)
        gust=.22+.78*(.5+.5*math.sin(t*1.05+channel*.10))**2
        leaves=(.5+.5*math.sin(t*5.7+channel))*(.5+.5*math.sin(t*11.3+.8))
        samples.append(low*gust*2.8+(body-low)*gust*.85+(soft-body)*(.08+.22*leaves)*gust)
    count=rate*2
    for i in range(count):
        w=.5-.5*math.cos(math.pi*i/(count-1))
        samples[-count+i]=samples[-count+i]*(1-w)+samples[i]*w
    channels.append(samples[count:])
gain=.78/max(abs(v) for c in channels for v in c)
path=Path('Assets/Audio/Ambience/ForestWind.wav')
with wave.open(str(path),'wb') as out:
    out.setparams((2,2,rate,0,'NONE','not compressed'))
    out.writeframes(b''.join(struct.pack('<hh',round(a*gain*32767),round(b*gain*32767)) for a,b in zip(*channels)))
path.with_suffix('.source.txt').write_text('Original synthesized strong rolling wind gusts and leaf rustling. 32 seconds, 24 kHz, 16-bit PCM; two-second loop crossfade. No third-party recordings. Generator: Tools/Audio/bake_forest_wind.py\n')
print(path)
