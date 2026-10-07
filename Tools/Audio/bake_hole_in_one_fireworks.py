import math, random, wave, struct
from pathlib import Path
rate=44100
seconds=2.1
samples=[[0.0,0.0] for _ in range(int(rate*seconds))]
rng=random.Random(721)
# Two soft launch swishes followed by rounded bass impacts and sparkling tails.
for launch,burst,pan in [(0,.55,-.32),(.18,.73,.32)]:
    low=0.0
    crackles=[(burst+.065+rng.random()*.64, .014+rng.random()*.018, rng.uniform(.018,.065)) for _ in range(12)]
    for i in range(int(launch*rate),len(samples)):
        t=i/rate
        noise=rng.uniform(-1,1)
        low += .16*(noise-low)
        value=0.0
        u=t-launch
        if 0<=u<burst-launch:
            x=u/(burst-launch)
            value += .045*noise*(math.sin(math.pi*x)**2)*(.25+.75*x)
        u=t-burst
        if 0<=u<1.15:
            attack=min(1,u/.005)
            # Falling-pitch body + filtered transient; intentionally no harsh gunshot crack.
            phase=2*math.pi*(58*u+36*.05*(1-math.exp(-u/.05)))
            body=.40*math.sin(phase)*math.exp(-u*9)+.095*math.sin(2*phase)*math.exp(-u*15)
            air=.20*low*math.exp(-u*12)+.045*noise*math.exp(-u*6)
            tail=.022*math.sin(2*math.pi*1480*u)*math.exp(-u*5)
            value += attack*(body+air+tail)
        for start,length,amp in crackles:
            c=t-start
            if 0<=c<length:value+=amp*noise*(math.sin(math.pi*c/length)**2)
        samples[i][0]+=value*math.sqrt((1-pan)/2)
        samples[i][1]+=value*math.sqrt((1+pan)/2)
peak=max(abs(v) for pair in samples for v in pair)
gain=.72/max(peak,1e-6)
output=Path('Assets/Audio/Celebration/HoleInOneTwinFireworks.wav')
with wave.open(str(output),'wb') as f:
    f.setnchannels(2);f.setsampwidth(2);f.setframerate(rate)
    f.writeframes(b''.join(struct.pack('<hh',*(round(max(-1,min(1,v*gain))*32767) for v in pair)) for pair in samples))
print(f'{output}: {seconds}s stereo, peaks at -2.85 dBFS, two impacts at 0.55s and 0.73s')
