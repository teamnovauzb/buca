"""Footage-led revision. No image generation, invented scenery or added particles.
Every pictorial frame is an existing Unity recording or the supplied platform UI.
Both aspect ratios have independent framing; originals and game assets are untouched.
"""
from pathlib import Path
from functools import lru_cache
import argparse,json,math,subprocess,wave
import cv2,numpy as np
from PIL import Image,ImageDraw,ImageFilter,ImageFont
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'output/buca-luxodd-attract';OUT=SOURCE/'revision-5';OUT.mkdir(exist_ok=True)
FF=imageio_ffmpeg.get_ffmpeg_exe();FPS=30;DURATION=48;TOTAL=1440
DESK=Path('/Users/nurxayot/Desktop');CREAM='#fff6df';GOLD='#ebc56d';GREEN='#54e9a1'
PREV=SOURCE/'revision-4';CAPTURE=json.loads((PREV/'level30/capture.json').read_text())
assert CAPTURE['goal_verified'] and CAPTURE['level']==30
RATE=(CAPTURE['win_frame']/FPS)/9;HOLD=13+3.6/RATE;RELEASE=13+6/RATE
cv2.setNumThreads(2)
def ease(x):
 x=max(0,min(1,x));return x*x*x*(x*(x*6-15)+10)
def out(x):return 1-(1-max(0,min(1,x)))**3
@lru_cache(maxsize=64)
def font(n):return ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf',round(n))
def load(path):return Image.open(path).convert('RGB')
def blendrect(a,b,p):return tuple(x+(y-x)*p for x,y in zip(a,b))
def journey_source(t):
 # Preserve every source event while shortening the empty closed-gate/curtain pause.
 return float(np.interp(t,[29,35.3,36.1,37,43],[10,15.28,16.64,19.35,21.7]))

def crop_resize(art,rect,size):
 # PIL floating crop avoids integer stepping during subpixel camera pushes.
 return art.transform(size,Image.Transform.EXTENT,rect,Image.Resampling.BICUBIC)
def fill_rect(rect,size):
 x0,y0,x1,y1=rect;w=x1-x0;h=y1-y0;ratio=size[0]/size[1]
 if w/h>ratio:w=h*ratio
 else:h=w/ratio
 cx=(x0+x1)/2;cy=(y0+y1)/2;return (cx-w/2,cy-h/2,cx+w/2,cy+h/2)
class Movie:
 def __init__(self,vertical=False):
  self.v=vertical;self.w,self.h=(1080,1920) if vertical else (1920,1080);self.size=(self.w,self.h);self.cache={}
  self.platforms=[load(SOURCE/'revision-2'/f'platform-{x}.png') for x in ['tower','parabox','buca']]
  # Same approved removal of the ownership badge from the actual supplied listing.
  a=self.platforms[2];a.paste(a.crop((861,878,975,922)),(738,878))
  # A neutral veil is used only behind typography, never as invented game imagery.
  self.bottom_veil=self.veil_image(False);self.top_veil=self.veil_image(True)
 def veil_image(self,top):
  n=420 if self.v else 240;yy=np.arange(n)/n;a=np.zeros((n,self.w,4),np.uint8)
  a[:,:,:3]=[4,12,12];a[:,:,3]=np.round((1-yy if top else yy)[:,None]**1.7*170).astype(np.uint8)
  return Image.fromarray(a)
 def text(self,im,text,x,y,size=38,color=CREAM,alpha=1,anchor='mm',tracking=0):
  if alpha<=0:return
  alpha=min(1,alpha);f=font(size);d=ImageDraw.Draw(im);box=d.textbbox((0,0),text,font=f)
  w=round(f.getlength(text)+tracking*max(0,len(text)-1)+24);h=box[3]-box[1]+30
  layer=Image.new('RGBA',(w,h));ld=ImageDraw.Draw(layer);pos=12-box[0]
  c=tuple(int(color[i:i+2],16) for i in (1,3,5))
  for ch in text:
   ld.text((pos+1,15-box[1]+2),ch,font=f,fill=(0,0,0,round(150*alpha)))
   ld.text((pos,15-box[1]),ch,font=f,fill=c+(round(255*alpha),));pos+=f.getlength(ch)+tracking
  xx=x-w/2 if anchor=='mm' else x
  im.paste(layer,(round(xx),round(y-h/2)),layer)
 def caption(self,im,headline,sub,t,at=0,top=False):
  p=out((t-at)/.42);y=(170 if self.v else 89) if top else self.h-(170 if self.v else 97)
  art=self.top_veil if top else self.bottom_veil;im.paste(art,(0,0 if top else self.h-art.height),art)
  self.text(im,headline,self.w/2,y+18*(1-p),42 if self.v else 35,CREAM,p,tracking=1.4)
  if sub:self.text(im,sub,self.w/2,y+(64 if self.v else 47)+18*(1-p),27 if self.v else 24,GOLD,out((t-at-.1)/.4),tracking=.8)
 def world(self,seconds):return load(SOURCE/'worlds/frames'/f'{min(869,max(0,round(seconds*30))):04d}.jpg')
 def game(self,seconds):return load(PREV/'level30/frames'/f'{min(359,max(0,round(seconds*30))):04d}.jpg')
 def cover(self,art,rect=None):return crop_resize(art,fill_rect(rect or (0,0,*art.size),self.size),self.size)
 def preserve(self,art,rect,fill=.55):
  # Portrait keeps the whole board and controls. Soft extensions are the SAME frame.
  bg=self.cover(art,rect).resize((135,240)).filter(ImageFilter.GaussianBlur(12)).resize(self.size,Image.Resampling.BICUBIC)
  bg=Image.blend(bg,Image.new('RGB',self.size,'#05110f'),fill)
  x0,y0,x1,y1=rect;scale=self.w/(x1-x0);height=round((y1-y0)*scale)
  pic=crop_resize(art,rect,(self.w,height));im=bg;im.paste(pic,(0,(self.h-height)//2))
  # Slight feather only on extension joins; no card border or decorative frame.
  for upper in [True,False]:
   n=38;y=(self.h-height)//2 if upper else (self.h+height)//2-n
   patch=bg.crop((0,y,self.w,y+n));a=np.tile(np.linspace(255,0,n)[:,None] if upper else np.linspace(0,255,n)[:,None],(1,self.w)).astype('uint8')
   im.paste(patch,(0,y),Image.fromarray(a))
  return im
 def platform(self,index,zoom=0):
  art=self.platforms[index]
  if self.v:
   return self.cover(art,blendrect((570,0,1350,1063),(674,55,1228,1040),zoom))
  return self.cover(art,blendrect((0,0,1952,1063),(580,190,1350,775),zoom))
 def scene(self,t):
  if t<4:
   im=self.platform(0)
   if t>=1.2:
    u=t-1.2;im=Image.blend(im.filter(ImageFilter.GaussianBlur(5*ease(u/.5))),Image.new('RGB',self.size,'#071811'),.60*ease(u/.4))
    lines=['WHAT KIND OF','GAME IS BUCA?'] if not self.v else ['WHAT KIND','OF GAME','IS BUCA?']
    sz=103 if not self.v else 112;step=151 if not self.v else 159;start=self.h/2-(len(lines)-1)*step/2
    for i,s in enumerate(lines):
     p=out((u-i*.085)/.5);self.text(im,s,self.w/2,start+i*step+46*(1-p),sz,GREEN if i==len(lines)-1 else CREAM,p)
   if t<.24:im=Image.blend(Image.new('RGB',self.size,'#081713'),im,ease(t/.24))
   return im
  if t<7:
   u=t-4
   if u<.85:idx=0;prev=0;p=0
   elif u<1.7:idx=1;prev=0;p=ease((u-.85)/.38)
   else:idx=2;prev=1;p=ease((u-1.7)/.4)
   # The carousel is depicted with its supplied actual screens, with brief editorial cuts.
   zoom=.12*ease((u-2.2)/.8) if idx==2 else 0
   im=self.platform(idx,zoom)
   if p<1:im=Image.blend(self.platform(prev),im,p)
   return im
  if t<10:
   u=t-7;art=self.world(.5+u)
   if self.v:im=self.cover(art,blendrect((450,0,1057.5,1080),(745,160,1251.25,1060),ease(u/3)))
   else:im=self.cover(art,blendrect((0,0,1920,1080),(115,80,1810,1033),ease(u/3)*.6))
   self.caption(im,'WORLD 1','30 LEVELS',u,top=self.v);return im
  if t<11.4:
   u=t-10;art=self.game(1+u*.4)
   rect=blendrect((515,568,956,1009),(540,594,947,1002),ease(u/1.4))
   im=self.cover(art,rect) if self.v else self.cover(art,(370,575,1290,1092))
   self.caption(im,'JOYSTICK','PULL TO AIM',u,top=True);return im
  if t<13:
   u=t-11.4;art=self.game(4+u*.7)
   rect=(770,590,1165,1080) if self.v else (570,592,1435,1079)
   im=self.cover(art,rect);self.caption(im,'BUTTON 1','HOLD, THEN RELEASE',u,top=True);return im
  if t<22:
   u=t-13;src=u*RATE;art=self.game(src)
   im=self.preserve(art,(595,0,1355,1080),.46) if self.v else self.cover(art)
   label='AIM' if t<HOLD else 'HOLD' if t<RELEASE else 'RELEASE'
   at=13 if t<HOLD else HOLD if t<RELEASE else RELEASE
   # Give the real HUD room. Instructions stay in the portrait extension / landscape floor margin.
   self.caption(im,'LEVEL 30',label,t,at,top=self.v)
   if self.v and t<HOLD:self.text(im,'BUTTON 3 · UNDO',540,1740,27,CREAM,.85,tracking=.8)
   return im
  if t<24:
   art=self.game(CAPTURE['win_frame']/FPS+(t-22)*.85)
   return self.preserve(art,(420,45,1500,1080),.27) if self.v else self.cover(art)
  if t<28:
   art=self.world(5.25+t-24)
   return self.preserve(art,(275,40,1645,1040),.34) if self.v else self.cover(art)
  if t<29:
   art=self.world(10+(t-28)*.1)
   if self.v:return self.cover(art,blendrect((835,0,1442.5,1080),(1285,135,1910,1000),ease(t-28)))
   return self.cover(art,blendrect((0,0,1920,1080),(1060,180,1920,665),ease((t-28)/1.0)))
  if t<43:return self.journey(t)
  return self.endcard(t)
 def journey(self,t):
  src=journey_source(t);art=self.world(src)
  if self.v:
   # Camera stays on the actual train, gate and their immediate station surroundings.
   rect=blendrect((1285,135,1910,1000),(1285,230,1910,1080),ease((src-18.3)/3.4)*.65)
   im=self.cover(art,rect)
  else:
   # A closer editorial pan follows the locomotive. Bonus level buttons stay outside every crop.
   start=(1285,218,1910,570);end=(1285,333,1910,685)
   if src<16.7:
    # In the first world retain a wider view of the departure, then push toward the closed gate.
    wide=(1017,208,1910,710);close=(1285,210,1910,562)
    rect=blendrect(wide,close,ease((src-10)/5.5))
   else:rect=blendrect(start,end,ease((src-18.5)/3.2))
   im=self.cover(art,rect)
  if src<16.6:self.caption(im,'LEAVING WORLD 1','',t,29,top=False)
  elif src>19.6:self.caption(im,'WORLD 2','NIGHT CITY',src,19.6,top=self.v)
  return im
 def endcard(self,t):
  u=t-43
  # The real destination is the backdrop. No generated city, trains, scenery, or logo artwork.
  raw=self.world(21.7)
  rect=(1285,230,1910,1050) if self.v else (1285,320,1910,672)
  im=self.cover(raw,rect).resize((self.w//4,self.h//4)).filter(ImageFilter.GaussianBlur(8)).resize(self.size,Image.Resampling.BICUBIC)
  im=Image.blend(im,Image.new('RGB',self.size,'#06130f'),.83)
  p=out(u/.5);self.text(im,'COMING SOON TO',self.w/2,self.h/2-(92 if self.v else 106)+24*(1-p),43 if self.v else 49,CREAM,p,tracking=5)
  q=out((u-.18)/.65);self.text(im,'LUXODD',self.w/2,self.h/2+(35 if self.v else 38)+26*(1-q),150 if self.v else 178,GREEN,q,tracking=3)
  return im
 def frame(self,t):
  im=self.scene(t)
  for cut in [4,7,10,11.4,13,24,28,29,43]:
   duration=.24 if cut!=43 else .65
   if cut<=t<cut+duration:
    if cut not in self.cache:self.cache[cut]=self.scene(cut-1/FPS)
    im=Image.blend(self.cache[cut],im,ease((t-cut)/duration));break
  return im
 def render(self,preview=False):
  name='vertical' if self.v else 'horizontal'
  times=[.7,2.6,6.6,8.4,10.8,12.2,14.4,18,20.6,21.8,22.8,25.3,27,28.6,30.8,34,35.8,38,40.4,42.6,45.2,47.95]
  if preview:
   for t in times:self.frame(t).save(OUT/f'preview-{name}-{t:05.2f}.jpg',quality=95)
   w,h=(480,270) if not self.v else (270,480);sheet=Image.new('RGB',(w*4,(h+27)*math.ceil(len(times)/4)),(12,22,19));d=ImageDraw.Draw(sheet)
   for i,t in enumerate(times):
    x=i%4*w;y=i//4*(h+27);sheet.paste(load(OUT/f'preview-{name}-{t:05.2f}.jpg').resize((w,h)),(x,y));d.text((x+8,y+h+7),str(t),fill='white')
   sheet.save(OUT/f'preview-{name}-contact.jpg',quality=94);return
  target=OUT/f'BUCA-Luxodd-{name}-v5-silent.mp4'
  p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s',f'{self.w}x{self.h}','-r','30','-i','-','-an','-c:v','libx264','-preset','fast','-crf','17','-pix_fmt','yuv420p','-threads','4','-movflags','+faststart',str(target)],stdin=subprocess.PIPE)
  try:
   for i in range(TOTAL):
    p.stdin.write(np.asarray(self.frame(i/FPS)).tobytes())
    if i%120==0:print(name,i,'/',TOTAL,flush=True)
  finally:p.stdin.close()
  if p.wait():raise RuntimeError('Encoder failed')
  print(target,flush=True)
def audio():
 rate=48000;n=rate*DURATION
 def decode(path):
  r=subprocess.run([FF,'-v','error','-i',str(path),'-vn','-f','f32le','-ac','2','-ar',str(rate),'-'],capture_output=True,check=True)
  return np.frombuffer(r.stdout,np.float32).reshape(-1,2).copy()
 music=decode(DESK/'AQNl5Jj2JgBuRrPLN_sFVZYLobNtRTt9ZIbjxkCxY5r_AUr40pEIpHoyAOhe3oea.mp4')[:n]*.72
 fx=np.zeros((n,2),np.float32);voice=fx.copy();duck=np.ones(n,np.float32);log=[]
 def place(bus,a,at,gain):
  start=round(at*rate);end=min(n,start+len(a));bus[start:end]+=a[:end-start]*gain
 def cue(path,at,gain):
  a=decode(ROOT/path);place(fx,a,at,gain);log.append({'asset':path,'seconds':at,'gain':gain})
 # Only the two earlier requested narration lines are reused. No new AI voice is generated.
 for path,at in [(SOURCE/'revision-2/voice-question.mp3',1.38),(SOURCE/'revision-3/voice-coming-soon.mp3',43.4)]:
  a=decode(path);a*=.44/max(.001,float(abs(a).max()));place(voice,a,at,1)
  duck[max(0,int((at-.12)*rate)):min(n,int((at+len(a)/rate+.18)*rate))]=.34
 coarse=cv2.GaussianBlur(duck[::240].reshape(1,-1),(0,0),sigmaX=8).ravel();duck=np.interp(np.arange(n),np.arange(len(coarse))*240,coarse);music*=duck[:,None]
 # All edit accents come from audio clips already present in the game.
 for at in [1.2,4.85,5.7,10,11.4,13,HOLD]:cue('Assets/Audio/UI/NavTick.ogg',at,.14)
 for at in [7,10,11.4,13,43.15]:cue('Assets/Audio/UI/PanelOpen.ogg',at,.16)
 cue('Assets/Audio/Player/PuckLaunch.ogg',RELEASE,.40)
 cue('Assets/Audio/Player/HoleSink.ogg',22,.38)
 cue('Assets/Audio/Celebration/HoleInOneTwinFireworks.wav',22,.26)
 cue('Assets/Audio/Celebration/HappyYeah.wav',22.27,.35)
 cue('Assets/Audio/Celebration/World30Victory.wav',24,.23)
 cue('Assets/Audio/Celebration/HappyYeah.wav',24.45,.22)
 cue('Assets/Audio/Celebration/StationWhistle.wav',29.65,.19)
 train=decode(ROOT/'Assets/Audio/Celebration/StationMovingTrain.wav')
 for at,duration in [(29.95,5.25),(37.15,5.6)]:
  count=round(duration*rate);a=np.tile(train,(math.ceil(count/len(train)),1))[:count].copy();a[:9600]*=np.linspace(0,1,9600)[:,None];a[-16800:]*=np.linspace(1,0,16800)[:,None];place(fx,a,at,.23)
 cue('Assets/Audio/UI/PanelClose.ogg',35.3,.24)
 cue('Assets/Audio/UI/PanelOpen.ogg',37.05,.18)
 cue('Assets/Audio/Level/StarReveal_1.ogg',43.65,.18)
 music[-36000:]*=np.linspace(1,0,36000)[:,None];fx[-24000:]*=np.linspace(1,0,24000)[:,None]
 mix=music+fx+voice;mix[:2000]*=np.linspace(0,1,2000)[:,None];peak=float(abs(mix).max());mix*=min(1,.90/peak)
 with wave.open(str(OUT/'soundtrack-v5.wav'),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate);w.writeframes((mix*32767).astype('<i2').tobytes())
 (OUT/'audio-report.json').write_text(json.dumps({'impact_seconds':22,'music':'User reference at original timing','new_synthesized_audio':False,'reused_narration':['What kind of game is Buca?','Coming soon to Luxodd.'],'game_audio_cues':log,'peak':float(abs(mix).max()),'train_movement_intervals':[[29.95,35.2],[37.15,42.75]]},indent=2));print('Audio ready',flush=True)
def mux():
 for name in ['horizontal','vertical']:
  target=OUT/f'BUCA-Luxodd-{name}-v5.mp4'
  subprocess.run([FF,'-y','-v','error','-i',str(OUT/f'BUCA-Luxodd-{name}-v5-silent.mp4'),'-i',str(OUT/'soundtrack-v5.wav'),'-map','0:v:0','-map','1:a:0','-c:v','copy','-c:a','aac','-b:a','224k','-t','48','-movflags','+faststart',str(target)],check=True);print(target,flush=True)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('action',choices=['preview','horizontal','vertical','audio','mux']);args=p.parse_args()
 if args.action=='preview':Movie().render(True);Movie(True).render(True)
 elif args.action=='audio':audio()
 elif args.action=='mux':mux()
 else:Movie(args.action=='vertical').render()
