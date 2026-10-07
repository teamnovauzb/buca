"""Original 40-second cheerful instrumental for the mall-film preview (no sampled songs)."""
from pathlib import Path
import numpy as np, wave
root=Path(__file__).resolve().parents[2]
sr=44100; dur=40; out=np.zeros((sr*dur,2)); rng=np.random.default_rng(17)
beat=60/126

def add(x,at,pan=0,vol=1):
 i=int(at*sr);n=min(len(x),len(out)-i)
 if n<=0:return
 out[i:i+n,0]+=x[:n]*vol*np.sqrt((1-pan)/2)
 out[i:i+n,1]+=x[:n]*vol*np.sqrt((1+pan)/2)
def tone(midi,at,kind='bell',vol=.1,pan=0,d=.5):
 t=np.arange(int(sr*d))/sr;f=440*2**((midi-69)/12)
 if kind=='bell':x=(np.sin(2*np.pi*f*t)+.23*np.sin(2*np.pi*2.01*f*t)+.07*np.sin(2*np.pi*3.97*f*t))*np.exp(-t*7)*(1-np.exp(-t*200))
 elif kind=='bass':x=(np.sin(2*np.pi*f*t)+.3*np.sin(2*np.pi*2*f*t))*np.exp(-t*6)*(1-np.exp(-t*160))
 else:x=sum(np.sin(2*np.pi*f*k*t)*np.exp(-t*(3+k*2))/k**1.6 for k in range(1,6))*(1-np.exp(-t*250))
 add(x,at,pan,vol)
chords=[[60,64,67],[57,60,64],[53,57,60],[55,59,62]]
phrase=[72,76,79,76,74,72,76,79,81,79,76,72,74,76,74,71,72,79,84,83,81,79,76,74,77,76,74,72,71,74,79,76]
for k in range(80):
 st=k*beat+.15
 intro=.65 if st<6 else 1
 chord=chords[(k//4)%4]
 for j,m in enumerate(chord):tone(m,st+j*.014,'pluck',.053*intro,-.35,.5)
 if k%2==0:tone(chord[0]-24,st,'bass',.16*intro,0,.7)
 if k%2==0 or k%8 in [3,7]:tone(phrase[k%32],st,'bell',.13*intro,.22,.65)
 if st>6:
  t=np.arange(int(.19*sr))/sr
  kick=np.sin(2*np.pi*(48*t+22*.03*(1-np.exp(-t/.03))))*np.exp(-t*26)
  if k%2==0:add(kick,st,0,.11)
  else:
   clap=rng.normal(size=len(t))*np.exp(-t*40)*(1-np.exp(-t*600));clap=np.diff(clap,prepend=clap[0])
   add(clap,st,.1,.028)
  tt=np.arange(int(.065*sr))/sr;hat=rng.normal(size=len(tt))*np.exp(-tt*75)
  add(hat,st+beat/2,-.25,.014)
# A brief lift for the winning reaction, followed by the closing cadence.
for j,m in enumerate([72,76,79,84,88]):tone(m,28+j*.105,'bell',.14,0,1.1)
for m in [60,64,67,72]:tone(m,38.2,'pluck',.08,0,1.5)
out*=np.minimum(np.arange(len(out))/int(sr*.2),1)[:,None]
out[-int(sr*1.2):]*=np.linspace(1,0,int(sr*1.2))[:,None]
peak=np.max(np.abs(out));out*=.87/max(peak,.87)
p=root/'output/buca-mall-story/fun-music.wav'
with wave.open(str(p),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes((out*32767).astype('<i2').tobytes())
print('40 seconds, original instrumental; peak',round(float(np.max(np.abs(out))),3))
