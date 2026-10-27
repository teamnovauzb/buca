"""Original, pre-rendered station bell / steam-train arrival. No runtime synthesis."""
import math, random, wave, struct
from pathlib import Path
rate=44100
samples=[[0.,0.] for _ in range(rate*4)]
rng=random.Random(31040)
def add(t,v,pan=0):
    i=int(t*rate)
    if 0<=i<len(samples):
        samples[i][0]+=v*math.sqrt((1-pan)/2)
        samples[i][1]+=v*math.sqrt((1+pan)/2)
# A soft pass-by sweep and slowing wheels.
for k in range(9):
    start=.08+k*.12+k*k*.012
    for j in range(int(.12*rate)):
        t=j/rate
        add(start+t,rng.uniform(-1,1)*.045*math.sin(math.pi*t/.12)**2*(1-k/12),-.7+k*.13)
# Two gentle harmonized whistle notes.
for start,freq in [(0.3,523.25),(.78,659.25)]:
    for j in range(int(.60*rate)):
        t=j/rate
        env=math.sin(math.pi*t/.60)**2
        v=.1*env*(math.sin(2*math.pi*freq*t+.035*math.sin(2*math.pi*5*t))+.12*math.sin(4*math.pi*freq*t))
        add(start+t,v,-.2)
# Clear station bell with a short stereo tail.
for start,freq in [(1.25,783.99),(1.56,1046.5)]:
    for j in range(int(2*rate)):
        t=j/rate
        v=.12*min(1,t/.008)*math.exp(-t*3)*(math.sin(2*math.pi*freq*t)+.2*math.sin(2*math.pi*freq*2.01*t))
        add(start+t,v,.22);add(start+t+.11,v*.18,-.3)
peak=max(abs(v) for p in samples for v in p)
gain=.63/max(peak,1e-5)
p=Path('Assets/Audio/Celebration/AllAboardArrival.wav')
with wave.open(str(p),'wb') as f:
    f.setnchannels(2);f.setsampwidth(2);f.setframerate(rate)
    f.writeframes(b''.join(struct.pack('<hh',*(round(v*gain*32767) for v in pair)) for pair in samples))
print(p)
