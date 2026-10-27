"""Original arrival chimes, punchy party-popper bursts and confetti shimmer."""
import math, random, wave, struct
from pathlib import Path
rate=32000
rng=random.Random(30)
frames=[]
low=0
for i in range(int(rate*6.3)):
    t=i/rate
    n=rng.uniform(-1,1);low=.94*low+.06*n
    v=0
    # Sharp 'paaaq' attack, warm body and short crackling tail with the ticket pop.
    for start,gain in [(.08,.72),(.52,.30),(.92,.22)]:
        a=t-start
        if 0<=a<.65:
            attack=1-math.exp(-a*1400)
            crack=n*math.exp(-a*28)
            body=math.sin(2*math.pi*(155*a-65*a*a))*math.exp(-a*13)
            tail=(n-low)*math.exp(-a*8)*(.5+.5*math.sin(a*137)**2)
            v+=gain*attack*(.65*crack+.45*body+.12*tail)
    for start,gain in [(0,.25),(.52,.15),(.92,.12)]:
        a=t-start
        if a>=0:
            v+=gain*(math.sin(2*math.pi*(62*a-13*a*a))*math.exp(-a*6)+low*math.exp(-a*9))*(1-math.exp(-a*200))
    for j,f in enumerate([659.25,783.99,987.77,1318.5,1568,1975.5]):
        a=t-(.20+j*.14)
        if a>=0:
            v+=(math.sin(2*math.pi*f*a)+.25*math.sin(2*math.pi*f*2*a))*.085*math.exp(-a*1.8)*(1-math.exp(-a*100))
    for f in [523.25,659.25,783.99]:
        a=t-1.25
        if a>=0:v+=.055*math.sin(2*math.pi*f*a)*math.exp(-a*.75)*(1-math.exp(-a*4))
    if t<2:v+=n*.06*math.exp(-t*.8)*(0.5+0.5*math.sin(t*43)**2)
    fade=min(1,max(0,(6.3-t)/.45));v=math.tanh(v*1.1)*fade
    pan=.08*math.sin(t*3)
    frames.append(struct.pack('<hh',int(v*(1-pan)*28000),int(v*(1+pan)*28000)))
p=Path('Assets/Audio/Celebration/World2Welcome.wav')
with wave.open(str(p),'wb') as out:
    out.setparams((2,2,rate,0,'NONE','not compressed'));out.writeframes(b''.join(frames))
print(p)
