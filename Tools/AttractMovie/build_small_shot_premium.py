"""Second edition: animated rounded typography and original upbeat 120 BPM score."""
import build_small_shot as b
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
from functools import lru_cache
import numpy as np, math, subprocess,wave,json,sys
b.OUT=b.ROOT/'output/buca-small-shot-premium';b.OUT.mkdir(exist_ok=True)
@lru_cache(None)
def f(size):return ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf',size)
def spring(t):
 t=max(0,t);return 1-math.exp(-9*t)*math.cos(10*t)
def label(im,s,x,y,size,color=b.CREAM,t=1,delay=0):
 u=t-delay
 if u<=0:return
 p=spring(u);alpha=min(1,u/.18)
 layer=Image.new('RGBA',im.size);d=ImageDraw.Draw(layer)
 yy=y+48*(1-p);font=f(size)
 d.text((x,yy+4),s,font=font,fill=(0,0,0,round(60*alpha)),anchor='mm')
 rgb=tuple(int(color[k:k+2],16) for k in (1,3,5))
 d.text((x,yy),s,font=font,fill=rgb+(round(255*alpha),),anchor='mm')
 im.paste(layer,(0,0),layer)
def words(im,s,y,size,color,t,delay=0):
 items=s.split();widths=[f(size).getlength(w) for w in items];gap=f(size).getlength(' ');x=(1920-sum(widths)-gap*(len(items)-1))/2
 for i,(word,w) in enumerate(zip(items,widths)):
  label(im,word,x+w/2,y,size,color,t,delay+i*.075);x+=w+gap

def graphic(t,end=False):
 im=b.BG.copy();d=ImageDraw.Draw(im)
 # Off-center concentric arcs and floating confetti establish a playful UI system.
 for r in [440,480,550]:d.ellipse((960-r,540-r,960+r,540+r),outline='#284848',width=2)
 for i in range(12):
  a=i*2.399+t*.17;rr=620+65*math.sin(i*2);x=960+math.cos(a)*rr;y=540+math.sin(a)*rr*.66
  col=[b.GOLD,b.TEAL,'#eb967f'][i%3]
  if i%2:d.rounded_rectangle((x-6,y-17,x+6,y+17),radius=6,fill=col)
  else:d.ellipse((x-7,y-7,x+7,y+7),fill=col)
 angle=-math.pi/2+t*.9;b.puck(im,960+505*math.cos(angle),540+425*math.sin(angle),27)
 if end:
  label(im,'Make your next shot',(960),280,36,b.TEAL,t)
  # Individual letters settle into a large wordmark.
  widths=[f(205).getlength(c) for c in 'BUCA'];x=(1920-sum(widths)-18*3)/2
  for i,(c,w) in enumerate(zip('BUCA',widths)):
   label(im,c,x+w/2,435,205,b.CREAM,t,.12+i*.08);x+=w+18
  words(im,'Small shot. Big adventure.',610,57,b.GOLD,t,.42)
  p=b.ease((t-.7)/.4);ww=360*p
  if ww>5:d.rounded_rectangle((960-ww/2,718,960+ww/2,810),radius=46,fill=b.TEAL)
  label(im,'Your turn',960,761,38,b.INK,t,.85)
  label(im,'AIM     •     HOLD     •     RELEASE',960,879,23,b.CREAM,t,1.05)
 else:
  label(im,'B U C A   /   A LITTLE MOMENT OF PLAY',960,274,25,b.TEAL,t,.08)
  words(im,'Small shot.',435,128,b.CREAM,t,.18)
  words(im,'Big adventure.',586,128,b.GOLD,t,.38)
  label(im,'Ready for something wonderful?',960,764,32,b.CREAM,t,.68)
 return im
current_t=0
starts={'01 / FIND YOUR ANGLE':3.5,'02 / FEEL THE MOMENT':6,'ONE SHOT CAN START SOMETHING':13.5,'THE ADVENTURE KEEPS MOVING':16,'WELCOME TO WORLD 2':24}
copy={'01 / FIND YOUR ANGLE':('FIND YOUR ANGLE','Line it up.'),'02 / FEEL THE MOMENT':('FEEL THE MOMENT','Hold. Then let go.'),'ONE SHOT CAN START SOMETHING':('OH, THAT FEELING','One shot. Pure joy.'),'THE ADVENTURE KEEPS MOVING':('ALL ABOARD','Next stop: wonder.'),'WELCOME TO WORLD 2':('HELLO, NIGHT CITY','More magic awaits.')}
def caption(im,small,big):
 u=current_t-starts[small];p=b.ease(u/.35)
 layer=Image.new('RGBA',im.size);d=ImageDraw.Draw(layer)
 # Dark translucent, fine-bordered UI panel keeps text clear over moving footage.
 yy=868+round(34*(1-spring(max(0,u))))
 d.rounded_rectangle((530,yy,1390,1030+yy-868),radius=32,fill=(9,30,33,round(228*p)),outline=(118,210,193,round(90*p)),width=2)
 d.rounded_rectangle((561,yy+32,567,yy+130),radius=3,fill=(244,195,94,round(255*p)))
 im.paste(layer,(0,0),layer)
 a,c=copy[small];label(im,a,960,yy+39,22,b.TEAL,u,.08);words(im,c,yy+102,57,b.CREAM,u,.13)
 return im
b.graphic=graphic;b.caption=caption
original_scene=b.scene
def scene(t):
 global current_t
 current_t=t
 if t>=30.6:return Image.blend(graphic(t-25.5,True),graphic(0),b.ease((t-30.6)/1.3666667))
 return original_scene(t)
b.scene=scene

def soundtrack():
 sr=48000;n=sr*32;rng=np.random.default_rng(71);mix=np.zeros((n,2))
 def put(sig,at,vol=1,pan=0):
  st=round(at*sr)
  if st>=n:return
  a=sig[:n-st]*vol;mix[st:st+len(a),0]+=a*math.sqrt((1-pan)/2);mix[st:st+len(a),1]+=a*math.sqrt((1+pan)/2)
 def tone(m,at,dur,vol=.1,kind='keys',pan=0):
  tt=np.arange(round(dur*sr))/sr;hz=440*2**((m-69)/12)
  if kind=='bass':
   sig=np.sin(2*np.pi*hz*tt)+.26*np.sin(4*np.pi*hz*tt)+.1*np.sin(6*np.pi*hz*tt);env=np.minimum(tt/.005,1)*np.exp(-tt*3)
  elif kind=='bell':
   sig=np.sin(2*np.pi*hz*tt+1.3*np.sin(2*np.pi*hz*2*tt)*np.exp(-tt*12));env=np.minimum(tt/.003,1)*np.exp(-tt*6)
  elif kind=='lead':
   sig=sum(np.sin(2*np.pi*hz*k*tt+.012*np.sin(2*np.pi*5*tt))/k**1.7 for k in range(1,7));env=np.minimum(tt/.018,1)*np.exp(-tt*4)
  else:
   sig=np.sin(2*np.pi*hz*tt+1.6*np.exp(-tt*8)*np.sin(2*np.pi*hz*tt))+.12*np.sin(2*np.pi*hz*3*tt);env=np.minimum(tt/.005,1)*np.exp(-tt*3.5)
  sig*=env*np.minimum((dur-tt)/.04,1);put(sig,at,vol,pan)
  if kind in ['bell','keys']:put(sig,at+.1875,vol*.18,-pan);put(sig,at+.375,vol*.08,pan)
 def drum(at,kind,vol=1):
  dur=.3 if kind in ['kick','clap'] else .08;tt=np.arange(round(dur*sr))/sr;noise=rng.normal(size=len(tt))
  if kind=='kick':sig=np.sin(2*np.pi*(48*tt+3*(1-np.exp(-tt*30))))*np.exp(-tt*19)+noise*np.exp(-tt*170)*.1
  elif kind=='clap':
   noise=np.concatenate(([0],np.diff(noise)));env=sum(np.exp(-np.maximum(0,tt-o)*37)*(tt>=o) for o in [0,.011,.024]);sig=noise*env*.17
  else:sig=np.concatenate(([0],np.diff(noise)))*np.exp(-tt*75)*.12
  put(sig,at,vol,.2 if kind=='hat' else 0)
 chords=[[50,57,61,66],[47,54,57,62],[43,50,57,59],[45,52,55,61]]
 melody=[(0,78),(.75,81),(1.25,83),(1.75,81),(2.5,78),(3.25,76),(3.75,74),(4.5,78),(5.25,81),(5.75,86),(6.5,83),(7.25,81),(7.75,78)]
 for bar in range(16):
  at=bar*2;ch=chords[bar%4];g=.8 if bar==0 else 1
  if bar==15:g=.3
  for off in [0,.75,1.5]:
   for j,m in enumerate(ch[1:]):tone(m+12,at+off,.58,.072*g,'keys',(j-1)*.4)
  for off,m in [(0,ch[0]-12),(.75,ch[0]-12),(1,ch[0]),(1.75,ch[0]-10)]:tone(m,at+off,.3,.27*g,'bass')
  for off in [0,1,1.75]:drum(at+off,'kick',.47*g)
  for off in [.5,1.5]:drum(at+off,'clap',.45*g)
  for i in range(8):drum(at+i*.25+(.025 if i%2 else 0),'hat',(.44 if i%2 else .7)*g)
 for phrase in range(4):
  for i,(at,m) in enumerate(melody):tone(m,phrase*8+at,.55,.15 if phrase else .12,'bell' if phrase%2==0 else 'lead',.18*math.sin(i))
 # A rising sparkle and chord accent mark the win and the title reveal.
 for at in [9.4,25.5]:
  for j,m in enumerate([74,78,81,86]):tone(m,at+j*.07,1.3,.17,'bell',(j-1.5)*.22)
 # Sidechain-style space for effects, with a restrained mix under the music.
 def fx(path,at,vol):
  p=subprocess.run([b.FF,'-v','error','-i',str(b.ROOT/path),'-f','f32le','-ar',str(sr),'-ac','1','-'],capture_output=True,check=True);put(np.frombuffer(p.stdout,np.float32),at,vol)
 for path,at,g in [('Assets/Audio/Player/PuckLaunch.ogg',8,.32),('Assets/Audio/Player/HoleSink.ogg',9.4,.4),('Assets/Audio/Celebration/HappyYeah.wav',9.6,.16),('Assets/Audio/Celebration/StationWhistle.wav',16.3,.16)]:fx(path,at,g)
 mix=np.tanh(mix*1.25);mix*=.88/max(.88,float(abs(mix).max()))
 mix[:960]*=np.linspace(0,1,960)[:,None];mix[-48000:]*=np.linspace(1,0,48000)[:,None]
 raw=b.OUT/'score-mix.wav'
 with wave.open(str(raw),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes((mix*32767).astype('<i2').tobytes())
 target=b.OUT/'premium-fun-score.wav'
 subprocess.run([b.FF,'-y','-v','error','-i',str(raw),'-af','loudnorm=I=-16:TP=-1.5:LRA=9','-ar','48000',str(target)],check=True)
 return target
if __name__=='__main__':
 b.preview()
 if '--preview' in sys.argv:sys.exit()
 score=soundtrack();target=b.OUT/'BUCA-Premium-Fun.mp4'
 p=subprocess.Popen([b.FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','1920x1080','-r','30','-i','-','-i',str(score),'-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4','-c:a','aac','-b:a','256k','-movflags','+faststart','-shortest',str(target)],stdin=subprocess.PIPE)
 for i in range(960):
  p.stdin.write(b.frame(i/30).tobytes())
  if i%120==0:print(f'{i}/960',flush=True)
 p.stdin.close();assert p.wait()==0
 print(target,flush=True)
