"""45-second BUCA attractor: actual gameplay and current isolated Unity captures."""
from pathlib import Path
from functools import lru_cache
import argparse, math, subprocess, json
import numpy as np
import cv2
from PIL import Image,ImageDraw,ImageFont
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'output/buca-attractor-2026'; FF=imageio_ffmpeg.get_ffmpeg_exe()
FPS=30; DURATION=45; cv2.setNumThreads(2)
@lru_cache(None)
def font(s):return ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf',int(s))
def ease(x): x=max(0,min(1,x));return x*x*(3-2*x)
def crop(a,r,size):return a.transform(size,Image.Transform.EXTENT,r,Image.Resampling.BICUBIC)
def fit(a,size):
 w,h=size;sw,sh=a.size; scale=max(w/sw,h/sh);rw=w/scale;rh=h/scale
 return crop(a,((sw-rw)/2,(sh-rh)/2,(sw+rw)/2,(sh+rh)/2),size)
def source(kind,t):
 if kind=='game':p=ROOT/'output/buca-luxodd-attract/revision-4/level30/frames';n=359
 elif kind=='night':p=ROOT/'output/buca-luxodd-attract/gameplay/world2/frames';n=179
 else:p=OUT/'capture'/kind;n={'party':359,'reveal':659,'whale':215,'journey':239}[kind]
 return Image.open(p/f'{min(n,max(0,int(t*30))):04d}.jpg').convert('RGB')
class Movie:
 def __init__(self,v):self.v=v;self.size=(1080,1920) if v else (1920,1080);self.w,self.h=self.size
 def text(self,im,s,y,size=70,color='#fff7df',x=None):
  d=ImageDraw.Draw(im);f=font(size)
  while d.textlength(s,font=f)>self.w-100:size-=2;f=font(size)
  d.text((x or self.w/2,y),s,font=f,anchor='mm',fill=color,stroke_width=2,stroke_fill='#063540')
 def bg(self,t):
  im=Image.new('RGB',self.size,'#073b49');d=ImageDraw.Draw(im)
  for i,c in enumerate(['#0b5961','#10515d','#0b626a']):
   x=int(self.w*(.12+i*.4)+55*math.sin(t*.6+i));y=int(self.h*(.15+i*.36));r=int(self.w*.40)
   d.ellipse((x-r,y-r,x+r,y+r),fill=c)
  return im
 def panel(self,im,a,r,y,h):
  ratio=(r[2]-r[0])/(r[3]-r[1]);pw=min(self.w-64,round(h*ratio));ph=round(pw/ratio);y+=(h-ph)//2;pic=crop(a,r,(pw,ph));mask=Image.new('L',pic.size);ImageDraw.Draw(mask).rounded_rectangle((0,0,pic.width-1,pic.height-1),radius=35,fill=255)
  im.paste(pic,((self.w-pic.width)//2,y),mask)
 def caption(self,im,title,sub,t):
  if self.v:
   self.text(im,title,190+24*(1-ease(t/.4)),76,'#ffdc79');self.text(im,sub,285,35)
  else:
   layer=Image.new('RGBA',self.size);d=ImageDraw.Draw(layer);d.rounded_rectangle((40,32,1880,152),30,fill=(3,37,47,225));im.paste(layer,(0,0),layer)
   self.text(im,title,80,55,'#ffdc79');self.text(im,sub,125,24)
 def brand(self,im,y,t,size):
  colors=['#4ab6ff','#ff6d71','#ffd35d','#6ce2a0'];step=size*.8
  for i,ch in enumerate('BUCA'):self.text(im,ch,y-12*math.sin(t*math.pi*4-i*.65),size,colors[i],self.w/2+(i-1.5)*step)
 def scene(self,t):
  if t<3 or t>=42:
   end=t>=42;u=t-42 if end else t;im=self.bg(t);a=source('party',4+u*.5)
   if self.v:
    self.brand(im,360,u,230);self.text(im,'BIG FUN. LITTLE SHOTS.',555,48)
    self.panel(im,a,(110,380,1800,980),700,600)
    self.text(im,'YOUR TURN!' if end else 'LET’S PLAY!',1470,100,'#ffdc79');self.text(im,'AIM • SHOOT • CELEBRATE',1600,39)
   else:
    self.panel(im,a,(70,370,1840,1000),390,620);self.brand(im,160,u,200);self.text(im,'YOUR TURN!  •  PRESS PLAY' if end else 'BIG FUN. LITTLE SHOTS.',310,58,'#ffdc79')
   if end:
    y=self.h-110
    ImageDraw.Draw(im).rounded_rectangle((35,y-22,self.w-35,y+58),18,fill='#073b49')
    self.text(im,'Music: Funkorama — Kevin MacLeod (incompetech.com)',y,22)
    self.text(im,'CC BY 4.0 • creativecommons.org/licenses/by/4.0/',y+32,20)
   return im
  if t<13:
   u=t-3;a=source('game',1+u);title='READY. AIM. GO!' if u<5 else 'MAKE YOUR SHOT!' if u<6.4 else 'YEEEY! YOU DID IT!';sub='Pull to aim. Hold, then release.' if u<6.4 else 'Small shot. Big celebration.'
   if self.v:
    im=self.bg(t);self.panel(im,a,(420,0,1500,1080),410,1080);self.text(im,'ONE SHOT CAN CHANGE EVERYTHING',1640,35)
   else:im=a
  elif t<16:
   u=t-13;a=source('journey',u*2);title='A WORLD OF SURPRISES';sub='50 levels • 2 worlds • 5 animal friends'
   if self.v:
    im=self.bg(t);self.panel(im,a,(116,65,1804,1015),390,610);self.panel(im,a,(180,255,1030,840),1050,630)
   else:im=crop(a,(116,65,1804,1015),self.size)
  elif t<25:
   u=t-16;a=source('party',u+.45);title='MEET YOUR CREW!';sub='Rabbit • Fox • Bear • Panda • Penguin'
   if self.v:
    im=self.bg(t);cx=700+650*ease(u/8);self.panel(im,a,(cx-480,330,cx+480,1010),400,850);self.panel(im,a,(70,350,1840,1000),1310,355);self.text(im,'ALL ABOARD!',1750,65,'#ffdc79')
   else:im=a
  elif t<32:
   u=t-25;a=source('reveal',u*22/7);title='NEXT STOP: WORLD 2';sub='A new train. A whole new adventure.'
   if self.v:
    im=self.bg(t);self.panel(im,a,(0,0,1920,1080),385,600);self.panel(im,a,(930,180,1900,900),1040,710)
   else:im=a
  elif t<37:
   u=t-32;a=source('whale',u*1.35);title='MAKE A SPLASH!';sub='Meet your new harbor friend'
   if self.v:
    im=self.bg(t);self.panel(im,a,(1040,170,1920,940),390,1180);self.text(im,'HELLO, NEW FRIEND!',1700,51,'#ffdc79')
   else:im=fit(a.crop((520,190,1920,978)),self.size)
  else:
   u=t-37;a=source('night',u+0.3);title='KEEP THE ADVENTURE GOING';sub='Welcome to the night city'
   if self.v:
    im=self.bg(t);self.panel(im,a,(440,0,1460,1080),400,1140);self.text(im,'WHAT WILL YOU DISCOVER?',1670,45,'#ffdc79')
   else:im=a
  self.caption(im,title,sub,u);return im
 def frame(self,t):
  im=self.scene(t)
  for cut in [3,13,16,25,32,37,42]:
   if cut<=t<cut+.16:im=Image.blend(self.scene(cut-1/30),im,ease((t-cut)/.16));break
  return im
 def render(self,preview):
  name='Vertical' if self.v else 'Horizontal'
  if preview:
   times=[1,5,9.7,14.5,18.5,22,27,31.5,33.5,36,39,43.5];tw,th=(216,384) if self.v else (384,216)
   sheet=Image.new('RGB',(tw*4,(th+24)*3));d=ImageDraw.Draw(sheet)
   for i,t in enumerate(times):
    a=self.frame(t);x=i%4*tw;y=i//4*(th+24);sheet.paste(a.resize((tw,th)),(x,y));d.text((x+5,y+th+3),str(t),fill='white')
   sheet.save(OUT/f'Storyboard-{name}.jpg',quality=95);return
  silent=OUT/f'{name}-silent.mp4';p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s',f'{self.w}x{self.h}','-r','30','-i','-','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4',str(silent)],stdin=subprocess.PIPE)
  for i in range(1350):
   p.stdin.write(np.asarray(self.frame(i/30)).tobytes())
   if i%150==0:print(name,i,flush=True)
  p.stdin.close();assert p.wait()==0
  subprocess.run([FF,'-y','-v','error','-i',str(silent),'-i',str(OUT/'soundtrack.wav'),'-c:v','copy','-c:a','aac','-b:a','256k','-t','45','-movflags','+faststart',str(OUT/f'BUCA-Attractor-{name}-1080p.mp4')],check=True)
def audio():
 rate=48000;n=45*rate
 def decode(path,extra=[]):
  b=subprocess.check_output([FF,'-v','error',*extra,'-i',str(path),'-f','f32le','-ar',str(rate),'-ac','2','-']);return np.frombuffer(b,np.float32).reshape(-1,2).copy()
 b=subprocess.check_output([FF,'-v','error','-ss',str(32*60/101),'-i',str(ROOT/'output/buca-reel/assets/Funkorama.mp3'),'-af','atempo=1.188118812','-t','45','-f','f32le','-ar',str(rate),'-ac','2','-']);mix=np.frombuffer(b,np.float32).reshape(-1,2).copy()[:n]*.65
 if len(mix)<n:mix=np.pad(mix,((0,n-len(mix)),(0,0)))
 cues=[('Player/PuckLaunch.ogg',8,.65),('Player/HoleSink.ogg',9.4,.7),('Celebration/HappyYeah.wav',9.5,.5),('Celebration/StationWhistle.wav',16,.28),('Celebration/StationMovingTrain.wav',16.1,.24),('Celebration/HappyYeah.wav',18.2,.4),('Celebration/World2Welcome.wav',29.5,.25),('HarborWhale/Splash.wav',32,.35),('HarborWhale/Splash.wav',34.8,.45)]
 for at in [0,3,13,16,25,32,37,42]:cues.append(('UI/PanelOpen.ogg',at,.2))
 for path,at,gain in cues:
  a=decode(ROOT/'Assets/Audio'/path);a*=gain/max(.1,float(abs(a).max()));start=int(at*rate);length=min(len(a),n-start);mix[start:start+length]+=a[:length]
 mix[:2400]*=np.linspace(0,1,2400)[:,None];mix[-12000:]*=np.linspace(1,0,12000)[:,None]
 p=subprocess.run([FF,'-y','-v','error','-f','f32le','-ar','48000','-ac','2','-i','-','-af','loudnorm=I=-14:TP=-1.5:LRA=9','-ar','48000',str(OUT/'soundtrack.wav')],input=mix.astype(np.float32).tobytes(),check=True)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--vertical',action='store_true');p.add_argument('--preview',action='store_true');p.add_argument('--audio',action='store_true');a=p.parse_args()
 if a.audio:audio()
 else:Movie(a.vertical).render(a.preview)
