"""Original procedural splash: filtered noise, droplets and water plops."""
import math, random, wave, struct
from pathlib import Path
rng=random.Random(31);rate=44100;duration=1.9;low=0;data=[]
for i in range(int(rate*duration)):
 t=i/rate;noise=rng.uniform(-1,1);low=.86*low+.14*noise
 envelope=(1-math.exp(-t*45))*math.exp(-t*3.6)
 value=(.5*low+.13*noise)*envelope
 for j in range(10):
  dt=t-(.12+j*.105)
  if 0<dt<.16:value+=.055*math.sin(2*math.pi*(720-j*32)*dt*math.exp(-dt*3))*math.exp(-dt*32)
 data.append(struct.pack('<h',int(max(-1,min(1,value))*30000)))
p=Path('Assets/Audio/HarborWhale/Splash.wav');p.parent.mkdir(parents=True,exist_ok=True)
with wave.open(str(p),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(b''.join(data))
