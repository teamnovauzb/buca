"""Original seamless train wheel/chuff loop; no external samples."""
import math, random, wave, struct
from pathlib import Path
rate=24000
rng=random.Random(31030)
samples=[]
low=0.0
for i in range(rate*2):
    t=i/rate
    phase=t%.5
    noise=rng.uniform(-1,1)
    low=.88*low+.12*noise
    chuff=math.exp(-phase*19)*(1-math.exp(-phase*180))
    clack=0
    for offset in (0,.085):
        age=phase-offset
        if age>=0: clack+=math.exp(-age*65)*(math.sin(2*math.pi*145*age)*.24+noise*.12)
    value=.9*low*chuff+clack+.035*math.sin(2*math.pi*48*t)
    samples.append(struct.pack('<h',int(max(-1,min(1,value))*27000)))
p=Path('Assets/Audio/Celebration/StationMovingTrain.wav')
with wave.open(str(p),'wb') as w:
    w.setparams((1,2,rate,0,'NONE','not compressed'));w.writeframes(b''.join(samples))
print(p)
