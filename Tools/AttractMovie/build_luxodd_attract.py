"""BUCA / Luxodd motion edit, with separately composed 16:9 and 9:16 masters.
Uses supplied cover art/cabinet photo, saved Unity captures, and supplied reference audio.
All compositing is video finishing; no game assets or platform listings are changed.
"""
from pathlib import Path
import argparse, math, subprocess, json, wave
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'output/buca-luxodd-attract'
DESK=Path('/Users/nurxayot/Desktop')
FF=imageio_ffmpeg.get_ffmpeg_exe()
FPS=30; TOTAL=1750; DURATION=TOTAL/FPS
FONT='/System/Library/Fonts/Supplemental/Arial Bold.ttf'
ROUND='/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf'
REG='/System/Library/Fonts/Supplemental/Arial.ttf'
CREAM='#fff2d2'; GREEN='#54e9a1'; GOLD='#edc76b'; MUTED='#a7c3b8'
cv2.setNumThreads(2)

def ease(x):
 x=max(0,min(1,x));return x*x*(3-2*x)
def out(x):return 1-(1-max(0,min(1,x)))**3
def font(n,bold=True):return ImageFont.truetype(FONT if bold else REG,int(n))
def rgb(path):return Image.open(path).convert('RGB')
def crop_scaled(path,rect,base):
 im=rgb(path);s=im.width/base;return im.crop(tuple(round(v*s) for v in rect))

class Movie:
 def __init__(self,vertical=False):
  self.v=vertical;self.w,self.h=(1080,1920) if vertical else (1920,1080)
  self.size=(self.w,self.h);self.cache={};self.sources={}
  self.icon=rgb(ROOT/'output/branding/BUCA-premium-icon-v2.png')
  self.tower=crop_scaled(DESK/'Screenshot 2026-10-11 at 20.09.56.png',(787,487,1143,995),1952)
  self.para=crop_scaled(DESK/'Screenshot 2026-10-11 at 20.10.02.png',(790,489,1140,995),1952)
  self.cab=rgb(DESK/'Screenshot 2026-10-11 at 20.07.33.png')
  self.base=self.background()
 def background(self):
  yy,xx=np.mgrid[0:self.h,0:self.w].astype(np.float32);xx/=self.w;yy/=self.h
  g=np.exp(-((xx-.68)**2/.18+(yy-.43)**2/.22));gold=np.exp(-((xx-.15)**2/.5+(yy-1.05)**2/.22))
  a=np.zeros((self.h,self.w,3),np.float32)+[5,18,22];a+=g[:,:,None]*[3,36,27]+gold[:,:,None]*[10,9,0]
  return Image.fromarray(np.clip(a,0,255).astype(np.uint8))
 def txt(self,im,text,x,y,n,color=CREAM,anchor='mm',alpha=1,bold=True):
  if alpha<=0:return
  layer=Image.new('RGBA',self.size);d=ImageDraw.Draw(layer);f=font(n,bold)
  d.text((x+1,y+3),text,font=f,fill=(0,0,0,round(125*alpha)),anchor=anchor)
  c=tuple(int(color[i:i+2],16) for i in (1,3,5));d.text((x,y),text,font=f,fill=c+(round(255*alpha),),anchor=anchor)
  im.paste(layer,(0,0),layer)
 def pill(self,im,text,cx,cy,n=26,color=GREEN):
  d=ImageDraw.Draw(im);f=font(n);box=d.textbbox((0,0),text,font=f);w=box[2]+44;h=n+28
  d.rounded_rectangle((cx-w/2,cy-h/2,cx+w/2,cy+h/2),radius=h/2,fill=color)
  self.txt(im,text,cx,cy,n,'#08221e')
 def brand(self,im,phase=None):
  m=64 if self.v else 76
  self.txt(im,'LUXODD',m,m,30 if self.v else 27,GREEN,'lm')
  self.txt(im,'ARCADE',m+157,m,22 if self.v else 20,CREAM,'lm')
  if phase:self.txt(im,phase,self.w-m,m,22,MUTED,'rm')
 def atmosphere(self,im,t):
  d=ImageDraw.Draw(im)
  # Slow, restrained orbit lines and glints remain behind the content.
  cx=self.w*.8;cy=self.h*.54
  for i in range(3):
   r=self.w*(.30+i*.09);d.arc((cx-r,cy-r,cx+r,cy+r),-70+t*3+i*16,30+t*3+i*16,fill=(21,69,57),width=2)
  for i in range(15):
   x=(i*.618033+t*.012)%1*self.w;y=(i*.38197-t*.009)%1*self.h
   r=1 if i%3 else 2;d.ellipse((x-r,y-r,x+r,y+r),fill=(62,109,88))
 def card(self,im,art,cx,cy,w,h,scale=1,angle=0,selected=False,opacity=1):
  w=max(1,round(w*scale));h=max(1,round(h*scale));src=np.asarray(art)
  # Covers fill their card just as they do in the supplied Luxodd carousel.
  ratio=max(w/src.shape[1],h/src.shape[0]);rw=round(src.shape[1]*ratio);rh=round(src.shape[0]*ratio)
  a=cv2.resize(src,(rw,rh),interpolation=cv2.INTER_LANCZOS4);a=a[(rh-h)//2:(rh-h)//2+h,(rw-w)//2:(rw-w)//2+w]
  patch=Image.fromarray(a).convert('RGBA');mask=Image.new('L',(w,h));ImageDraw.Draw(mask).rounded_rectangle((0,0,w-1,h-1),radius=max(16,w*.038),fill=255);patch.putalpha(mask)
  d=ImageDraw.Draw(patch);d.rounded_rectangle((2,2,w-3,h-3),radius=max(16,w*.038),outline=GOLD if selected else '#376259',width=4 if selected else 2)
  if angle:patch=patch.rotate(angle,Image.Resampling.BICUBIC,expand=True)
  if opacity<1:patch.putalpha(patch.getchannel('A').point(lambda v:int(v*opacity)))
  x=round(cx-patch.width/2);y=round(cy-patch.height/2)
  shadow=Image.new('RGBA',(patch.width+80,patch.height+80));sm=Image.new('L',shadow.size);sm.paste(patch.getchannel('A'),(40,40));sm=sm.filter(ImageFilter.GaussianBlur(20));shadow.putalpha(sm.point(lambda v:round(v*.60)));im.paste(shadow,(x-40,y-16),shadow)
  im.paste(patch,(x,y),patch)
 def video(self,path,t):
  path=str(path)
  if path not in self.sources:self.sources[path]=cv2.VideoCapture(path)
  c=self.sources[path];idx=max(0,min(int(t*c.get(cv2.CAP_PROP_FPS)),int(c.get(cv2.CAP_PROP_FRAME_COUNT))-1))
  c.set(cv2.CAP_PROP_POS_FRAMES,idx);ok,a=c.read()
  if not ok:raise RuntimeError('Unreadable movie '+path+' '+str(t))
  return Image.fromarray(cv2.cvtColor(a,cv2.COLOR_BGR2RGB))
 def world(self,t):
  idx=min(869,max(0,round(t*30)));return rgb(OUT/'worlds/frames'/f'{idx:04d}.jpg')
 def shot(self,im,art,box,crop=None,radius=25):
  x,y,w,h=map(round,box)
  if crop:art=art.crop(crop)
  a=np.asarray(art);s=max(w/a.shape[1],h/a.shape[0]);rw,rh=round(a.shape[1]*s),round(a.shape[0]*s)
  a=cv2.resize(a,(rw,rh),interpolation=cv2.INTER_LANCZOS4);a=a[(rh-h)//2:(rh-h)//2+h,(rw-w)//2:(rw-w)//2+w]
  patch=Image.fromarray(a);mask=Image.new('L',(w,h));ImageDraw.Draw(mask).rounded_rectangle((0,0,w,h),radius=radius,fill=255);im.paste(patch,(x,y),mask)
  ImageDraw.Draw(im).rounded_rectangle((x,y,x+w,y+h),radius=radius,outline='#698179',width=2)
 def selector(self,im,t,header=True):
  # Move through Tower Rush and Parabox, landing on the current BUCA cover.
  pos=0 if t<.8 else ease((t-.8)/1.2) if t<2.4 else 1+ease((t-2.4)/1.35)
  cy=self.h*.51;w,h=(560,796) if self.v else (396,564);step=650 if self.v else 560
  for i in sorted(range(3),key=lambda i:-abs(i-pos)):
   distance=i-pos;s=1-.22*min(1,abs(distance));cx=self.w/2+distance*step
   if cx+w/2<0 or cx-w/2>self.w:continue
   art=[self.tower,self.para,self.icon][i]
   self.card(im,art,cx,cy,w,h,s,0,abs(distance)<.15,1-.35*min(1,abs(distance)))
  if header:
   self.brand(im,'CHOOSE YOUR GAME')
   if self.v:self.txt(im,'Find BUCA.',self.w/2,285,86)
   else:self.txt(im,'Find your next challenge.',self.w/2,176,68)
   selected=round(pos);label=['TOWER RUSH','PARABOX','BUCA'][selected]
   self.txt(im,label,self.w/2,cy+h/2+67,58 if self.v else 45)
   self.pill(im,'SELECT BUCA' if pos>1.85 else 'BROWSE GAMES',self.w/2,cy+h/2+154,30)
   if self.v:self.txt(im,'One arcade. Your next favourite.',self.w/2,1655,31,MUTED)
 def cabinet(self,im,t):
  # Use the supplied exact cabinet photograph. Composite into its LCD only.
  factor=self.cab.width/502;source=np.array(self.cab);hh,ww=source.shape[:2]
  tmp=Image.new('RGB',(960,540),(7,26,23));m=MovieScreen(self);m.render(tmp,t)
  quad=np.float32([(109,85),(331,93),(343,222),(127,226)])*factor
  H=cv2.getPerspectiveTransform(np.float32([(0,0),(959,0),(959,539),(0,539)]),quad)
  screen=cv2.warpPerspective(np.asarray(tmp),H,(ww,hh));mask=np.zeros((hh,ww),np.uint8);cv2.fillConvexPoly(mask,np.int32(quad),255)
  source[mask>0]=screen[mask>0]
  cut=Image.fromarray(source).convert('RGBA');a=Image.new('L',(ww,hh));p=[(77,66),(342,65),(363,212),(429,282),(421,359),(273,379),(80,338),(75,277),(82,215)]
  ImageDraw.Draw(a).polygon([(round(x*factor),round(y*factor)) for x,y in p],fill=255);cut.putalpha(a)
  maxw=940 if self.v else 870;scale=(1+.045*ease(t/4))*maxw/ww;cut=cut.resize((round(ww*scale),round(hh*scale)),Image.Resampling.LANCZOS)
  cx=self.w/2 if self.v else 1330;cy=1010 if self.v else 560
  im.paste(cut,(round(cx-cut.width/2),round(cy-cut.height/2)),cut)
  self.brand(im)
  if self.v:
   self.txt(im,'Meet your',540,320,80);self.txt(im,'next obsession.',540,414,80)
   self.pill(im,'LUXODD ARCADE',540,1510,42)
   self.txt(im,'Real controls. A satisfying challenge.',540,1600,31,MUTED)
  else:
   self.txt(im,'Meet your',112,398,88,anchor='lm');self.txt(im,'next obsession.',112,500,88,anchor='lm')
   self.pill(im,'LUXODD ARCADE',428,645,32)
   self.txt(im,'Real controls. A satisfying challenge.',112,730,30,MUTED,'lm')
 def frame(self,t):
  im=self.base.copy();self.atmosphere(im,t)
  if t<4:
   self.brand(im)
   if self.v:
    self.card(im,self.icon,540,1110,750,750,1+.012*math.sin(t),-2+math.sin(t)*1)
    words=[('WHAT KIND',280),('OF GAME',380),('IS BUCA?',480)]
    for j,(s,y) in enumerate(words):self.txt(im,s,540,y+60*(1-out((t-j*.18)/.5)),94,GREEN if j==2 else CREAM,alpha=out((t-j*.18)/.4))
    self.txt(im,'AIM. BOUNCE. SINK.',540,1640,46,GOLD,alpha=ease(t-1.8))
   else:
    self.card(im,self.icon,1400,570,710,710,1+.012*math.sin(t),-2+math.sin(t))
    for j,(s,y) in enumerate([('WHAT KIND',340),('OF GAME',455),('IS BUCA?',570)]):self.txt(im,s,120,y+60*(1-out((t-j*.18)/.5)),102,GREEN if j==2 else CREAM,'lm',out((t-j*.18)/.4))
    self.txt(im,'AIM. BOUNCE. SINK.',120,755,43,GOLD,'lm',ease(t-1.8))
  elif t<8:self.cabinet(im,t-4)
  elif t<14:self.selector(im,t-8)
  elif t<19:
   art=self.world(t-14)
   if self.v:
    self.brand(im,'WORLD 1');self.txt(im,'Small steps.',540,282,88);self.txt(im,'Big worlds.',540,383,88,GREEN)
    self.shot(im,art,(34,574,1012,720),crop=(135,0,1810,1080))
    self.txt(im,'30',540,1470,168,GOLD);self.txt(im,'LEVELS TO MASTER',540,1590,39)
   else:
    self.shot(im,art,(48,240,1824,792));self.brand(im,'WORLD 1');self.txt(im,'30 levels. One big adventure.',960,162,68)
  elif t<29:
   local=t-19;art=self.video(OUT/'gameplay/real-gameplay.mp4',local)
   label='AIM WITH THE JOYSTICK' if local<3 else 'HOLD. RELEASE.' if local<4.1 else 'MAKE IT COUNT.' if local<6 else 'THAT WINNING FEELING.'
   if self.v:
    self.brand(im,'HOW TO PLAY' if local<6 else 'HOLE IN ONE');self.txt(im,'Line it up.' if local<6 else 'One shot.',540,275,91);self.txt(im,'Let it fly.' if local<6 else 'Big smiles.',540,377,91,GREEN)
    # The whole board and all three controls remain visible.
    self.shot(im,art,(32,532,1016,963),crop=(260,0,1640,1080))
    self.txt(im,label,540,1600,41,GOLD)
    self.txt(im,'Joystick to aim  •  Button 1 to shoot',540,1694,31,MUTED)
   else:
    self.shot(im,art,(44,204,1832,833));self.brand(im,'HOW TO PLAY');self.txt(im,label,960,142,65)
  elif t<34:
   art=self.world(5+t-29)
   if self.v:
    self.brand(im,'WORLD 1 COMPLETE');self.txt(im,'You earned it.',540,311,86,GOLD)
    self.shot(im,art,(24,530,1032,720),crop=(185,0,1735,1080))
    self.txt(im,'30 levels complete.',540,1405,61);self.txt(im,'The next world is waiting.',540,1513,44,GREEN)
   else:self.shot(im,art,(0,0,1920,1080),radius=0)
  elif t<46:
   local=t-34;art=self.world(10+local)
   if self.v:
    self.brand(im,'THE NEXT CHAPTER');self.txt(im,'All aboard.',540,305,105,GOLD)
    # Follow the right-side train throughout the departure and arrival.
    self.shot(im,art,(28,532,1024,1000),crop=(740,0,1846,1080))
    self.txt(im,'WORLD 1  →  WORLD 2',540,1638,46)
   else:
    self.shot(im,art,(0,0,1920,1080),radius=0)
    if local<2.5:self.pill(im,'ALL ABOARD FOR WORLD 2',960,83,30)
  elif t<50:
   art=self.world(22+t-46)
   if self.v:
    self.brand(im,'WORLD 2');self.txt(im,'A new world.',540,300,93,GOLD)
    self.shot(im,art,(20,573,1040,740),crop=(185,0,1735,1080))
    self.txt(im,'20 BONUS LEVELS',540,1500,57);self.txt(im,'Welcome to Night City.',540,1600,43,GREEN)
   else:self.shot(im,art,(0,0,1920,1080),radius=0)
  elif t<54:
   art=self.video(OUT/'gameplay/world2-gameplay.mp4',t-50)
   if self.v:
    self.brand(im,'NIGHT CITY');self.txt(im,'Keep the',540,268,91);self.txt(im,'adventure going.',540,375,83,GREEN)
    self.shot(im,art,(30,530,1020,1020),crop=(340,0,1550,1080))
    self.txt(im,'50 LEVELS. 2 WORLDS.',540,1680,50,GOLD)
   else:
    self.shot(im,art,(44,221,1832,817));self.brand(im,'NIGHT CITY');self.txt(im,'50 levels. 2 worlds. Your adventure.',960,149,64)
  else:
   self.brand(im)
   if self.v:
    self.card(im,self.icon,540,840,790,790,1+.012*math.sin(t),0,True)
    self.txt(im,'YOUR TURN.',540,1380,113,GREEN);self.txt(im,'Find BUCA on',540,1540,46);self.pill(im,'LUXODD ARCADE',540,1645,46)
   else:
    self.card(im,self.icon,540,585,765,765,1+.012*math.sin(t),-1,True)
    self.txt(im,'YOUR',1390,385,135);self.txt(im,'TURN.',1390,533,135,GREEN)
    self.txt(im,'Find BUCA on',1390,715,44);self.pill(im,'LUXODD ARCADE',1390,809,43)
  # A brief dip softens cuts without hiding key text or controls.
  for c in [4,8,14,19,29,34,46,50,54]:
   dt=t-c
   if abs(dt)<.10:
    a=.35*(1-abs(dt)/.10);im=Image.blend(im,self.base,a)
  if t<.22:im=Image.blend(self.base,im,ease(t/.22))
  if t>58.10:im=Image.blend(im,self.base,ease((t-58.1)/.23))
  return im
 def render(self,preview=False):
  name='vertical' if self.v else 'horizontal'
  if preview:
   for t in [2,6,9,11,13,16,22,25,31,36,43,48,52,56]:
    self.frame(t).save(OUT/f'preview-{name}-{t:02d}.jpg',quality=92)
   return
  target=OUT/f'BUCA-Luxodd-{name}-silent.mp4'
  cmd=[FF,'-y','-loglevel','error','-f','rawvideo','-pix_fmt','rgb24','-s',f'{self.w}x{self.h}','-r','30','-i','-','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4','-movflags','+faststart',str(target)]
  p=subprocess.Popen(cmd,stdin=subprocess.PIPE)
  try:
   for i in range(TOTAL):
    p.stdin.write(np.asarray(self.frame(i/FPS)).tobytes())
    if i%90==0:
     (OUT/f'progress-{name}.txt').write_text(f'{i} / {TOTAL}');print(name,i,'/',TOTAL,flush=True)
  finally:p.stdin.close()
  if p.wait():raise RuntimeError('Encoder failed')
  print(target,flush=True)

class MovieScreen:
 def __init__(self,m):self.m=m
 def render(self,im,t):
  d=ImageDraw.Draw(im);d.text((480,48),'LUXODD ARCADE',font=font(33),fill=GREEN,anchor='mm')
  # An exact cover from the selected game appears on the photographed LCD.
  art=self.m.icon.resize((350,350),Image.Resampling.LANCZOS);im.paste(art,(305,110))
  d.rounded_rectangle((303,108,657,462),radius=12,outline=GOLD,width=3)
  d.text((480,504),'BUCA',font=font(35),fill=CREAM,anchor='mm')

def audio():
 ref=DESK/'AQNl5Jj2JgBuRrPLN_sFVZYLobNtRTt9ZIbjxkCxY5r_AUr40pEIpHoyAOhe3oea.mp4'
 def decode(path):
  data=subprocess.run([FF,'-v','error','-i',str(path),'-vn','-f','f32le','-ac','2','-ar','48000','-'],capture_output=True,check=True).stdout
  return np.frombuffer(data,np.float32).reshape(-1,2).copy()
 n=round(DURATION*48000);track=decode(ref);mix=np.zeros((n,2),np.float32);mix[:min(n,len(track))]=track[:n]*.85
 def add(rel,at,gain,length=None):
  a=decode(ROOT/rel);a=a[:round(length*48000)] if length else a;start=round(at*48000);length=min(len(a),n-start)
  if length>0:mix[start:start+length]+=a[:length]*gain
 # Preserve the supplied vocal/music track, with restrained game cues.
 add('Assets/Audio/UI/ButtonClick.ogg',12,.24)
 add('Assets/Audio/Player/PuckLaunch.ogg',22.9,.23)
 add('Assets/Audio/Player/HoleSink.ogg',24.4,.30)
 add('Assets/Audio/Celebration/World30Victory.wav',29,.18)
 add('Assets/Audio/Celebration/HappyYeah.wav',29.8,.22)
 add('Assets/Audio/Celebration/StationDeparture.wav',34,.12,5.2)
 for at in [35,37,42.8,44]:add('Assets/Audio/Celebration/StationMovingTrain.wav',at,.10,1.7)
 add('Assets/Audio/Celebration/World2Welcome.wav',46,.20)
 add('Assets/Audio/Celebration/HappyYeah.wav',46.4,.19)
 mix*=np.minimum(np.arange(n)/4800,1)[:,None];mix[-16000:]*=np.linspace(1,0,16000)[:,None]
 peak=float(abs(mix).max());mix*=min(1,.93/max(peak,.001))
 with wave.open(str(OUT/'soundtrack.wav'),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(48000);w.writeframes((mix*32767).astype('<i2').tobytes())
 (OUT/'audio-report.json').write_text(json.dumps({'duration':DURATION,'pre_limiter_peak':peak,'source':'User-supplied reference video soundtrack, with existing game sound effects. No cloned narrator.'},indent=2))

def mux():
 for name in ['horizontal','vertical']:
  src=OUT/f'BUCA-Luxodd-{name}-silent.mp4';dst=OUT/f'BUCA-Luxodd-{name}.mp4'
  subprocess.run([FF,'-y','-v','error','-i',str(src),'-i',str(OUT/'soundtrack.wav'),'-map','0:v:0','-map','1:a:0','-c:v','copy','-c:a','aac','-b:a','192k','-t',str(DURATION),'-movflags','+faststart',str(dst)],check=True)
  print(dst,flush=True)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('action',choices=['preview','horizontal','vertical','audio','mux']);a=p.parse_args()
 if a.action=='audio':audio()
 elif a.action=='mux':mux()
 elif a.action=='preview':Movie().render(True);Movie(True).render(True)
 else:Movie(a.action=='vertical').render()
