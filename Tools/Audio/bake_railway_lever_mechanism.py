"""Original short lever Foley: release, pivot scrape, detent clunk and settling rattle.
No whistle, steam, speech, chime, or external samples. All audio is baked offline.
"""
from pathlib import Path
import array, math, random, struct, wave
RATE=48000
SECONDS=.68
N=round(RATE*SECONDS)
LOCK=.375  # The handle reaches its left stop at .38 seconds.
rng=random.Random(50318)
noise=[rng.uniform(-1,1) for _ in range(N)]
excite=[0.0]*N
# Short, non-periodic excitation into heavily damped body resonances.
for at,amp,decay in [(.007,.32,.003),(LOCK,1,.006),(.404,.20,.003),(.437,.09,.002)]:
    begin=int(at*RATE)
    for i in range(begin,min(N,begin+int(.055*RATE))):
        x=(i-begin)/RATE
        excite[i]+=amp*math.exp(-x/decay)*noise[i]
body=[0.0]*N
for freq,decay,amp in [(136,.028,.40),(283,.019,.27),(617,.014,.16),(1273,.008,.08),(2531,.005,.035)]:
    radius=math.exp(-1/(RATE*decay))
    a=2*radius*math.cos(2*math.pi*freq/RATE);b=radius*radius
    y1=y2=0.
    gain=math.sqrt(1-radius)*amp
    for i in range(N):
        y=excite[i]+a*y1-b*y2;y2=y1;y1=y
        body[i]+=y*gain
raw=[];low=mid=grit=0.
for i in range(N):
    t=i/RATE;low+=.025*(noise[i]-low);mid+=.18*(noise[i]-mid)
    u=min(1,max(0,(t-.045)/.295))
    travel=math.sin(math.pi*u)**1.4 if .045<t<.34 else 0.
    # Uneven friction is tied to the moving portion, not a continuous hiss.
    friction=(mid-low)*travel*(.11+.04*math.sin(t*59)+.025*math.sin(t*137))
    tick=excite[i]*.19
    raw.append(body[i]+friction+tick)
peak=max(map(abs,raw));gain=.75/peak
channels=[]
for channel in range(2):
    out=[]
    for i,value in enumerate(raw):
        # Tiny box reflection; keep the mechanism firmly centered.
        delay=int(RATE*(.012 if channel==0 else .015))
        reflected=raw[i-delay]*.075 if i>=delay else 0
        fade=min(1,i/(RATE*.002),(N-1-i)/(RATE*.045))
        out.append((value+reflected)*gain*max(0,fade))
    channels.append(out)
peak=max(abs(x) for c in channels for x in c)
if peak>.79:
    channels=[[x*.79/peak for x in c] for c in channels]
path=Path('Assets/Audio/UI/RailwayLeverMechanism.wav')
with wave.open(str(path),'wb') as f:
    f.setparams((2,2,RATE,0,'NONE','not compressed'))
    f.writeframes(b''.join(struct.pack('<hh',round(a*32767),round(b*32767)) for a,b in zip(*channels)))
path.with_suffix('.source.txt').write_text('Original offline procedural mechanical lever Foley. Release click, short irregular pivot scrape, damped metal/wood detent clunk at 0.375 seconds, and settling rattle. No recordings, speech, train whistle, steam or musical chime. Stereo 48 kHz PCM, 0.68 seconds. Generator: Tools/Audio/bake_railway_lever_mechanism.py.\n')
print(path)
print('peak dBFS:',round(20*math.log10(max(abs(x) for c in channels for x in c)),2))
