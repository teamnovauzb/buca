"""Pre-render the results fanfare; no sound synthesis runs inside the game."""
import math, random, wave, struct
from pathlib import Path
rate=44100
duration=4.3
samples=[[0.,0.] for _ in range(int(rate*duration))]
rng=random.Random(404)
def add(index,value,pan):
    if 0<=index<len(samples):
        samples[index][0]+=value*math.sqrt((1-pan)/2)
        samples[index][1]+=value*math.sqrt((1+pan)/2)
for burst,pan in [(.32,-.52),(.64,.52)]:
    low=0.
    for i in range(int((burst-.28)*rate),min(len(samples),int((burst+1.4)*rate))):
        t=i/rate-burst
        noise=rng.uniform(-1,1);low+=.11*(noise-low)
        if t<0:
            x=(t+.28)/.28
            value=.032*noise*math.sin(math.pi*x)**2*x
        else:
            phase=2*math.pi*(62*t+42*.045*(1-math.exp(-t/.045)))
            value=(.35*math.sin(phase)*math.exp(-8*t)+.1*math.sin(phase*2)*math.exp(-13*t)+.24*low*math.exp(-8*t)+.035*noise*math.exp(-5*t))*min(1,t/.006)
        add(i,value,pan)
    for _ in range(18):
        start=burst+.04+rng.random()*.84
        length=.012+rng.random()*.024
        for j in range(int(length*rate)):
            add(int(start*rate)+j,.035*rng.uniform(-1,1)*math.sin(math.pi*j/(length*rate))**2,pan+rng.uniform(-.12,.12))
# Rising marimba/bell figure, then a warm major-sixth chord at score completion.
notes=[(.87,523.25,.12,-.3),(1.05,659.25,.115,-.1),(1.23,783.99,.12,.12),(1.42,1046.5,.11,.3),
       (1.8,523.25,.10,-.22),(1.8,659.25,.087,-.08),(1.8,783.99,.080,.08),(1.8,1046.5,.07,.22),(1.8,1318.51,.035,.36)]
for start,freq,amp,pan in notes:
    for j in range(int(2.3*rate)):
        t=j/rate
        value=amp*min(1,t/.005)*(math.sin(2*math.pi*freq*t)*math.exp(-t*2.7)+.26*math.sin(2*math.pi*freq*2.006*t)*math.exp(-t*5.5)+.09*math.sin(2*math.pi*freq*3.97*t)*math.exp(-t*9))
        add(int(start*rate)+j,value,pan)
        add(int((start+.095)*rate)+j,value*.13,-pan)
        add(int((start+.173)*rate)+j,value*.08,pan)
peak=max(abs(v) for pair in samples for v in pair)
gain=.71/max(peak,1e-6)
path=Path('Assets/Audio/Celebration/ResultsFireworksFanfare.wav')
with wave.open(str(path),'wb') as f:
    f.setnchannels(2);f.setsampwidth(2);f.setframerate(rate)
    f.writeframes(b''.join(struct.pack('<hh',*(round(v*gain*32767) for v in pair)) for pair in samples))
print(f'{path}: {duration}s stereo, peak -3 dBFS; bursts at .32/.64s, final chime at 1.8s')
