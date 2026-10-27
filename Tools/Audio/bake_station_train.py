"""Original toy steam-engine Foley. Saved clips; no synthesis in the WebGL player."""
import math, random, struct, wave
from pathlib import Path
RATE=22050
folder=Path('Assets/Audio/Celebration')
def render(name,duration,kind):
    rng=random.Random(30120);out=[];smooth=0
    for i in range(int(RATE*duration)):
        t=i/RATE;n=rng.uniform(-1,1);smooth=.86*smooth+.14*n
        if kind=='idle':
            phase=(t%1.2)/1.2
            hiss=math.sin(math.pi*min(1,phase/.65))**2 if phase<.65 else .03
            v=smooth*(.12+.6*hiss)+.013*math.sin(2*math.pi*75*t)
        elif kind=='whistle':
            v=0
            for start,length in [(.08,.43),(.62,.72)]:
                a=t-start
                if 0<a<length:
                    env=math.sin(math.pi*a/length)**.7
                    freq=440*(1+.004*math.sin(t*28))
                    v+=.17*env*(math.sin(2*math.pi*freq*t)+.24*math.sin(2*math.pi*freq*1.5*t)+.12*math.sin(2*math.pi*freq*2*t))
            v+=smooth*.018*math.sin(math.pi*t/duration)**2
        else:
            fade=min(1,t/.12,max(0,(duration-t)/.8))
            wheel=(t*(1.1+.30*t))%1
            v=smooth*(.13+.58*math.exp(-wheel*13))*fade
            for start,length in [(.35,.55),(1.02,.8)]:
                a=t-start
                if 0<a<length:v+=.18*math.sin(math.pi*a/length)**.6*(math.sin(2*math.pi*440*t)+.25*math.sin(2*math.pi*660*t))
            if t<1.9:
                a=max(0,t-.12);v+=.075*math.exp(-a*3)*(math.sin(2*math.pi*784*a)+.14*math.sin(2*math.pi*1580*a))
        out.append(max(-.8,min(.8,v)))
    with wave.open(str(folder/name),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(RATE)
        f.writeframes(b''.join(struct.pack('<h',round(v*32767)) for v in out))
    print(name,round(max(map(abs,out)),3))
render('StationIdleSteam.wav',4.8,'idle')
render('StationWhistle.wav',1.65,'whistle')
render('StationDeparture.wav',7.5,'departure')
