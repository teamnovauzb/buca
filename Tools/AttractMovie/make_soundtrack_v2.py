"""Original toy-mallet score and timed effects for the 26-second BUCA movie."""
from pathlib import Path
import wave
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
sr=44100; duration=12; mix=np.zeros((sr*duration,2),np.float64)
def add(sound,start,pan=0):
    at=int(start*sr); n=min(len(sound),len(mix)-at)
    if n<=0:return
    if sound.ndim==1:sound=np.column_stack((sound*np.sqrt((1-pan)/2),sound*np.sqrt((1+pan)/2)))
    mix[at:at+n]+=sound[:n]
def note(midi,start,vol=.13,length=.6,pan=0):
    t=np.arange(int(sr*length))/sr; f=440*2**((midi-69)/12)
    env=(1-np.exp(-t*220))*np.exp(-t*7)
    s=(np.sin(2*np.pi*f*t)+.3*np.sin(2*np.pi*f*2.01*t)*np.exp(-t*6)+.12*np.sin(2*np.pi*f*3.98*t)*np.exp(-t*15))*env*vol
    add(s,start,pan)
# A light original C-major melody with gentle bass; no speech or lyrics.
melody=[72,76,79,76,74,77,81,79,76,79,84,83,81,79,76,74]
for i in range(28):
    st=i*.40+.05
    duck=.45 if 6.3<st<8.6 else 1
    note(melody[i%16],st,.14*duck,.7,(-.3 if i%2==0 else .3))
    if i%2==0:note([48,53,55,48][(i//4)%4],st,.11*duck,.8)
# Three bright anticipation notes and a soft launch swoosh.
for i in range(3):note(72+i*4,2.25+i*.27,.20,.6)
rng=np.random.default_rng(4)
t=np.arange(int(sr*.4))/sr
add(rng.normal(0,1,len(t))*.035*np.sin(np.pi*t/.4)**2,3.2)
for i,m in enumerate([72,76,79,84]):note(m,6.25+i*.10,.24,1.3,0)
# Reuse the game's licensed unison children cheer, exactly once at the goal.
with wave.open(str(ROOT/'Assets/Audio/Celebration/HappyYeah.wav'),'rb') as w:
    assert w.getsampwidth()==2 and w.getframerate()==sr
    cheer=np.frombuffer(w.readframes(w.getnframes()),'<i2').astype(float)/32768
    cheer=cheer.reshape(-1,w.getnchannels())
    if cheer.shape[1]==1:cheer=np.repeat(cheer,2,axis=1)
add(cheer*.65,6.5)
# End on a friendly major chord and a fade for repeat playback.
for m in [60,64,67,72]:note(m,10.5,.11,1.4)
mix[:int(.15*sr)]*=np.linspace(0,1,int(.15*sr))[:,None]
mix[-sr:]*=np.linspace(1,0,sr)[:,None]
mix*=.89/max(.89,np.abs(mix).max())
out=ROOT/'output/attract-v2/soundtrack.wav'
with wave.open(str(out),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes((mix*32767).astype('<i2').tobytes())
print(out, 'peak',round(float(abs(mix).max()),3))
