"""Original shy-rabbit reward Foley and soft celesta; no external samples."""
from pathlib import Path
import math, random, struct, wave
rate=48000
length=5.1
samples=[0.0]*round(rate*length)
rng=random.Random(51024)
def tone(start,duration,freq,level,decay):
    for n in range(round(duration*rate)):
        t=n/rate
        env=min(1,t/.006)*math.exp(-t/decay)*min(1,(duration-t)/.04)
        samples[round(start*rate)+n]+=level*env*(math.sin(2*math.pi*freq*t)+.18*math.sin(2*math.pi*freq*2.003*t))
for start in [.32,.48,.65]:
    tone(start,.16,185,.16,.035);tone(start,.10,470,.045,.022)
# A quiet cloth-and-hinge whisper follows the lid, with no cartoon boing.
low=0.0
for n in range(round(.7*rate)):
    t=n/rate
    low+=.06*(rng.uniform(-1,1)-low)
    samples[round(.86*rate)+n]+=.11*low*math.sin(math.pi*t/.7)**2
for start,freq,level in [(1.18,784,.07),(1.72,988,.075),(2.72,659.25,.105),(2.85,830.61,.09),(3.02,987.77,.08),(3.22,1318.51,.065)]:
    tone(start,1.6,freq,level,.40)
# Small stereo room tail, kept quieter than the dry sound.
peak=max(abs(s) for s in samples)
gain=.62/max(peak,.62)
path=Path('Assets/Audio/UI/RabbitGiftReveal.wav')
with wave.open(str(path),'wb') as out:
    out.setparams((2,2,rate,0,'NONE','not compressed'))
    frames=[]
    for i,v in enumerate(samples):
        a=(v+(samples[i-1392]*.11 if i>=1392 else 0))*gain
        b=(v+(samples[i-1776]*.10 if i>=1776 else 0))*gain
        frames.append(struct.pack('<hh',round(a*32767),round(b*32767)))
    out.writeframes(b''.join(frames))
path.with_suffix('.source.txt').write_text('Original procedural wooden taps, cloth/hinge whisper, and soft celesta greeting. Stereo 48 kHz PCM. Generator: Tools/Audio/bake_rabbit_gift.py. No external samples.\n')
print(path)
