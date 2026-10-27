"""Second Luxodd edit: brand first, World 1 only, musical impact at 22 seconds.
Existing scene captures are read-only. Original delivery remains available.
"""
from build_luxodd_attract import Movie as Base, ROOT, OUT as SOURCE, DESK, FF, font, ease, out, CREAM, GREEN, GOLD, MUTED
from pathlib import Path
import argparse, math, json, subprocess, wave
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops
OUT=SOURCE/'revision-2';OUT.mkdir(exist_ok=True)
FPS=30;DURATION=48;TOTAL=FPS*DURATION;IMPACT=22.0

class Movie(Base):
 def __init__(self,vertical=False):
  super().__init__(vertical);self.text_cache={};self.art_cache={}
  rng=np.random.default_rng(37)
  self.platforms=[Image.open(OUT/f'platform-{name}.png').convert('RGB') for name in ['tower','parabox','buca']]
  self.particles=[(rng.uniform(0,math.tau),rng.uniform(310,1200),rng.uniform(.6,1.4),rng.uniform(5,13),int(rng.integers(0,4)),rng.uniform(0,math.tau)) for _ in range(235)]
 def letters(self,text,n,color):
  key=(text,n,color)
  if key in self.text_cache:return self.text_cache[key]
  f=font(n);b=f.getbbox(text);w=b[2]-b[0]+40;h=b[3]-b[1]+44
  mask=Image.new('L',(w,h));ImageDraw.Draw(mask).text((20-b[0],15-b[1]),text,font=f,fill=255,stroke_width=0)
  layer=Image.new('RGBA',(w,h));d=ImageDraw.Draw(layer)
  # The shallow extrusion and soft specular edge give the letters physical depth.
  for k in range(9,0,-1):d.text((20-b[0]+k*.22,15-b[1]+k),text,font=f,fill=(5,32,28,255))
  c=tuple(int(color[i:i+2],16) for i in (1,3,5));grad=np.zeros((h,w,4),np.uint8)
  for y in range(h):
   blend=.10+.11*math.cos((y/h)*math.pi*2)
   grad[y,:,:3]=[min(255,round(v+(255-v)*blend)) for v in c]
  grad[:,:,3]=np.asarray(mask);layer=Image.alpha_composite(layer,Image.fromarray(grad))
  self.text_cache[key]=layer;return layer
 def title(self,im,text,cx,cy,n,local,color=CREAM,delay=0,mode='rise'):
  u=local-delay
  if u<0:return
  p=min(1,u/.75);alpha=out(p);art=self.letters(text,n,color).copy()
  if mode=='slam':scale=1+.65*math.exp(-9*p)*math.cos(8*p)
  else:scale=.94+.06*out(p)+.022*math.sin(p*math.pi)*math.exp(-p*3)
  y=cy+(1-out(p))*78
  if p<.75:art=art.filter(ImageFilter.GaussianBlur((1-p/.75)*5))
  art=art.resize((max(1,round(art.width*scale)),max(1,round(art.height*scale))),Image.Resampling.BICUBIC)
  if alpha<1:art.putalpha(art.getchannel('A').point(lambda a:round(a*alpha)))
  # A highlight passes through once after the text settles.
  sweep=(u-1.05)/.85
  if 0<sweep<1:
   sheen=Image.new('RGBA',art.size);d=ImageDraw.Draw(sheen);x=int((-150+(art.width+300)*sweep));d.polygon([(x-45,0),(x+20,0),(x+110,art.height),(x+45,art.height)],fill=(255,255,242,100))
   sheen.putalpha(ImageChops.multiply(sheen.getchannel('A'),art.getchannel('A')));art=Image.alpha_composite(art,sheen)
  im.paste(art,(round(cx-art.width/2),round(y-art.height/2)),art)
 def small(self,im,text,y,local,color=MUTED,n=30):
  self.txt(im,text,self.w/2,y+20*(1-out(local/.6)),n,color,alpha=out(local/.5))
 def glow(self,im,cx,cy,r,amount=.5):
  key=(round(r),round(amount,2))
  if key not in self.art_cache:
   rr=int(r);yy,xx=np.mgrid[-rr:rr,-rr:rr];a=np.exp(-(xx*xx+yy*yy)/(rr*.42)**2)*125*amount
   arr=np.zeros((rr*2,rr*2,4),np.uint8);arr[:,:,:3]=[71,230,169];arr[:,:,3]=a.astype(np.uint8);self.art_cache[key]=Image.fromarray(arr)
  art=self.art_cache[key];im.paste(art,(round(cx-art.width/2),round(cy-art.height/2)),art)
 def burst(self,im,local,cx=None,cy=None,strength=1):
  if local<0 or local>4.6:return
  cx=self.w/2 if cx is None else cx;cy=self.h*.52 if cy is None else cy
  layer=Image.new('RGBA',self.size);d=ImageDraw.Draw(layer);scale=min(self.w/1080,self.h/1080)
  # The shockwave expands once, rather than flashing repeatedly.
  if local<1.25:
   rad=(60+local*1100)*scale;alpha=int(220*(1-local/1.25));d.ellipse((cx-rad,cy-rad,cx+rad,cy+rad),outline=(255,221,137,alpha),width=max(1,int(9*(1-local/1.3))))
  # Gold radial streaks and two staggered fireworks make the musical drop expansive.
  if local<.8:
   for j in range(44):
    theta=j*math.tau/44+.07;inner=(80+local*560)*scale;outer=inner+(180+90*math.sin(j*7))*scale*(1-local/.8)
    col=(255,220,123,int(180*(1-local/.8)))
    d.line((cx+math.cos(theta)*inner,cy+math.sin(theta)*inner,cx+math.cos(theta)*outer,cy+math.sin(theta)*outer),fill=col,width=3)
  for bx,by,delay in [(self.w*.16,self.h*.24,.30),(self.w*.84,self.h*.28,.54)]:
   age=local-delay
   if 0<age<1.55:
    for j in range(46):
     theta=j*math.tau/46;dist=(35+250*(1-math.exp(-age*2)))*scale;tail=26*math.exp(-age)*scale;fall=80*age*age;alpha=round(230*(1-age/1.55))
     d.line((bx+math.cos(theta)*(dist-tail),by+math.sin(theta)*(dist-tail)+fall,bx+math.cos(theta)*dist,by+math.sin(theta)*dist+fall),fill=(255,220,123,alpha),width=3)
  colors=[(248,202,93),(86,232,162),(255,239,202),(218,155,52)]
  for angle,speed,life,size,k,rot in self.particles:
   age=local;life*=3.6
   if age>life:continue
   distance=speed*(1-math.exp(-age*.65))/.65*scale
   x=cx+math.cos(angle)*distance;y=cy+math.sin(angle)*distance+125*age*age*scale
   opacity=round(255*min(1,(life-age)/.75)*strength);col=colors[k]+(max(0,opacity),)
   r=size*scale*(.7+.3*math.sin(age*5+rot));theta=rot+age*2.3
   dx=math.cos(theta)*r;dy=math.sin(theta)*r
   if k==2:d.line((x-dx*2,y-dy*2,x+dx*2,y+dy*2),fill=col,width=max(1,int(r*.45)))
   else:d.polygon([(x-dx-dy*.5,y-dy+dx*.5),(x+dx-dy*.5,y+dy+dx*.5),(x+dx+dy*.5,y+dy-dx*.5),(x-dx+dy*.5,y-dy-dx*.5)],fill=col)
  im.paste(layer,(0,0),layer)
 def cabinet_cut(self,t,live=False):
  factor=self.cab.width/502;source=np.array(self.cab);hh,ww=source.shape[:2]
  screen=Image.new('RGB',(960,540),(7,26,23))
  if live:
   pic=self.video(SOURCE/'gameplay/real-gameplay.mp4',min(7,max(0,t)))
   screen=pic.resize((960,540),Image.Resampling.LANCZOS)
  else:
   d=ImageDraw.Draw(screen);d.text((480,158),'LUXODD',font=font(105),fill=GREEN,anchor='mm');d.text((480,274),'ARCADE',font=font(60),fill=CREAM,anchor='mm')
   d.rounded_rectangle((290,355,670,361),radius=3,fill=GOLD)
  quad=np.float32([(109,85),(331,93),(343,222),(127,226)])*factor;H=cv2.getPerspectiveTransform(np.float32([(0,0),(959,0),(959,539),(0,539)]),quad)
  src=cv2.warpPerspective(np.asarray(screen),H,(ww,hh));mask=np.zeros((hh,ww),np.uint8);cv2.fillConvexPoly(mask,np.int32(quad),255);source[mask>0]=src[mask>0]
  cut=Image.fromarray(source).convert('RGBA');a=Image.new('L',(ww,hh));points=[(77,66),(342,65),(363,212),(429,282),(421,359),(273,379),(80,338),(75,277),(82,215)]
  ImageDraw.Draw(a).polygon([(round(x*factor),round(y*factor)) for x,y in points],fill=255);cut.putalpha(a)
  return cut.crop((int(70*factor),int(60*factor),int(435*factor),int(385*factor)))
 def put_cabinet(self,im,t,cx,cy,w,live=False):
  art=self.cabinet_cut(t,live);ratio=w/art.width;art=art.resize((round(art.width*ratio),round(art.height*ratio)),Image.Resampling.LANCZOS)
  im.paste(art,(round(cx-art.width/2),round(cy-art.height/2)),art)
 def game(self,im,local,label=None,winning=False):
  art=self.video(SOURCE/'gameplay/real-gameplay.mp4',local)
  if self.v:
   self.shot(im,art,(22,525,1036,985),crop=(260,0,1640,1080),radius=30)
   if label:self.title(im,label,540,1640,55,local,color=GOLD)
  else:self.shot(im,art,(36,192,1848,856),radius=28)
 def victory(self,t):
  # Sample only World 1 victory frames; never sample the second-world map.
  a=self.world(6.2+min(t,3.6)).copy();d=ImageDraw.Draw(a)
  # Replace the onward-travel subtitle with a World 1 invitation in this edit.
  d.rounded_rectangle((440,692,1480,810),radius=13,fill='#704a1b',outline='#ffde84',width=4)
  d.rounded_rectangle((452,703,1468,797),radius=8,fill='#f1e3b2',outline='#c4a552',width=3)
  d.text((960,752),'EVERY LEVEL. EVERY LITTLE VICTORY.',font=font(38),fill='#263d36',anchor='mm')
  return a
 def platform(self,im,position,zoom=1):
  left=max(0,min(2,int(position)));right=min(2,left+1);f=ease(position-left)
  art=Image.blend(self.platforms[left],self.platforms[right],f) if right!=left else self.platforms[left]
  if self.v:
   self.shot(im,art,(22,570,1036,1070),crop=(430,0,1510,1063),radius=24)
  else:
   self.shot(im,art,(0,0,1920,1080),radius=0)
 def frame(self,t):
  im=self.base.copy();self.atmosphere(im,t*1.3)
  if t<3.1:
   self.platform(im,0)
   if self.v:
    self.title(im,'LUXODD',540,275,147,t,GREEN);self.title(im,'ARCADE',540,426,96,t,CREAM,.12)
    self.small(im,'CHOOSE YOUR NEXT GAME.',1758,t-.6,GOLD,34)
   else:
    self.title(im,'LUXODD ARCADE',960,169,88,t,GREEN)
  elif t<6.6:
   u=t-3.1;self.platform(im,0);im=Image.blend(im.filter(ImageFilter.GaussianBlur(8)),self.base,.72)
   self.brand(im);ys=[680,870,1060] if self.v else [335,518,701];n=140 if self.v else 146
   for i,text in enumerate(['WHAT KIND','OF GAME','IS BUCA?']):self.title(im,text,self.w/2,ys[i],n,u,GREEN if i==2 else CREAM,i*.23)
   d=ImageDraw.Draw(im);p=out((u-.8)/.8);y=1240 if self.v else 863;d.rounded_rectangle((self.w/2-230*p,y,self.w/2+230*p,y+6),radius=3,fill=GOLD)
  elif t<11:
   u=t-6.6;position=0 if u<.45 else ease((u-.45)/.75) if u<1.65 else 1+ease((u-1.65)/.9)
   self.platform(im,position)
   if self.v:
    self.brand(im);self.title(im,'LET’S FIND',540,263,95,u);self.title(im,'BUCA.',540,394,124,u,GREEN,.12)
    if u>2.7:self.title(im,'THERE IT IS.',540,1758,59,u-2.7,GOLD,mode='slam')
   else:
    self.title(im,'Let’s find BUCA.',960,170,79,u,GREEN)
    if u>2.7:self.title(im,'THERE IT IS.',1580,807,40,u-2.7,GOLD,mode='slam')
   if u>2.7:
    age=u-2.7
    # A brief focus ring marks the actual BUCA listing, without inventing a website button.
    layer=Image.new('RGBA',self.size);d=ImageDraw.Draw(layer)
    if age<.6:
     cx,cy=(550,1100) if self.v else (958,550);r=65+age*120;d.ellipse((cx-r,cy-r,cx+r,cy+r),outline=(255,215,103,round(220*(1-age/.6))),width=4);im.paste(layer,(0,0),layer)
  elif t<16.4:
   u=t-11;art=self.world(min(u,4.8));self.brand(im,'WORLD 1')
   if self.v:
    self.title(im,'30 LEVELS.',540,298,104,u,GOLD);self.title(im,'Find your shot.',540,422,74,u,GREEN,.18)
    self.shot(im,art,(25,630,1030,782),crop=(180,105,1775,1080));self.small(im,'AIM. BOUNCE. SINK.',1638,u-.45,GOLD,43)
   else:
    self.shot(im,art,(42,226,1836,815));self.title(im,'30 levels. Find your perfect shot.',960,135,66,u)
  elif t<22:
   u=t-16.4;self.brand(im,'HOW TO PLAY');label='AIM.' if u<3 else 'HOLD.' if u<3.9 else 'RELEASE.';age=u if u<3 else u-3 if u<3.9 else u-3.9
   self.game(im,u)
   self.title(im,label,self.w/2,340 if self.v else 107,129 if self.v else 94,age,GREEN,mode='slam')
   if self.v:self.small(im,'Joystick to aim. Button 1 to shoot.',1690,u-.4,MUTED,34)
   # The anticipation is brief: a subtle push and tightening light before the drop.
   if t>21.45:
    p=(t-21.45)/.55;scale=1+.023*p;sz=(round(self.w*scale),round(self.h*scale));zoom=im.resize(sz,Image.Resampling.BICUBIC);im=zoom.crop(((sz[0]-self.w)//2,(sz[1]-self.h)//2,(sz[0]+self.w)//2,(sz[1]+self.h)//2))
  elif t<26:
   u=t-22;art=self.video(SOURCE/'gameplay/real-gameplay.mp4',5.6+min(u*.38,1.45))
   if self.v:
    self.title(im,'ONE SHOT.',540,284,115,u,GOLD,mode='slam');self.title(im,'BIG FEELING.',540,420,104,u,GREEN,.17,mode='slam')
    self.shot(im,art,(0,550,1080,1020),crop=(300,0,1620,1080),radius=0)
    self.small(im,'THAT’S BUCA.',1690,u-.28,CREAM,55)
   else:
    self.shot(im,art,(0,0,1920,1080),radius=0)
    self.title(im,'ONE SHOT. BIG FEELING.',960,100,90,u,GOLD,mode='slam')
   self.burst(im,u)
   # A short white-gold exposure hit and decaying camera kick are tied to 22.00 s.
   if u<.17:im=Image.blend(im,Image.new('RGB',self.size,'#fff2cf'),.84*(1-u/.17))
   if u<.75:
    scale=1+.09*math.exp(-u*6);sw,sh=round(self.w*scale),round(self.h*scale);im=im.resize((sw,sh),Image.Resampling.BICUBIC);dx=int(8*math.sin(u*53)*math.exp(-u*5));dy=int(6*math.sin(u*67)*math.exp(-u*5));cx=(sw-self.w)//2+dx;cy=(sh-self.h)//2+dy;im=im.crop((cx,cy,cx+self.w,cy+self.h))
  elif t<31:
   u=t-26;self.brand(im,'THE ADVENTURE');self.title(im,'FIVE CHAPTERS.',self.w/2,305 if self.v else 126,83 if self.v else 76,u,GOLD)
   a=self.world(min(u,4.9));self.shot(im,a,(25,610,1030,790) if self.v else (42,230,1836,810),crop=(190,280,1550,1080) if self.v else None)
   if self.v:self.title(im,'One satisfying',540,1550,67,u-.3);self.title(im,'challenge after another.',540,1638,60,u-.6,GREEN)
  elif t<35:
   u=t-31;self.brand(im,'FEEL THE CONTROLS')
   art=self.video(SOURCE/'gameplay/real-gameplay.mp4',.6+u*.72)
   if self.v:
    self.title(im,'YOUR MOVE.',540,320,105,u,GREEN);self.shot(im,art,(30,610,1020,840),crop=(375,310,1530,1080))
    self.small(im,'AIM  /  SHOOT  /  UNDO',1630,u-.4,GOLD,45)
   else:
    self.title(im,'YOUR MOVE.',960,130,87,u,GREEN);self.shot(im,art,(42,260,1836,775),crop=(220,290,1695,1080));
  elif t<40:
   u=t-35;art=self.victory(u)
   if self.v:
    self.title(im,'MAKE IT',540,270,107,u);self.title(im,'A BIG WIN.',540,392,105,u,GOLD,.14)
    self.shot(im,art,(15,645,1050,805),crop=(220,0,1700,1060))
    self.small(im,'EVERY LEVEL COUNTS.',1650,u-.4,GREEN,44)
   else:self.shot(im,art,(0,0,1920,1080),radius=0)
   self.burst(im,u*.82,strength=.65)
  elif t<43:
   u=t-40;self.brand(im);self.glow(im,self.w*.55,self.h*.62,650,.6)
   if self.v:
    self.title(im,'BUILT FOR',540,310,101,u);self.title(im,'THE ARCADE.',540,440,98,u,GREEN,.14)
    self.put_cabinet(im,u,540,1060,880*(1+.02*u),True);self.small(im,'PICK BUCA. TAKE YOUR SHOT.',1650,u-.4,GOLD,35)
   else:
    self.title(im,'BUILT FOR',520,385,109,u);self.title(im,'THE ARCADE.',520,525,104,u,GREEN,.14)
    self.put_cabinet(im,u,1410,610,730*(1+.02*u),True)
  else:
   u=t-43;self.brand(im)
   if self.v:
    self.card(im,self.icon,540,857,795,795,1+.012*math.sin(u),-1,True)
    self.title(im,'YOUR TURN.',540,1420,119,u,GREEN,mode='slam');self.small(im,'Find BUCA on',1550,u-.2,CREAM,45);self.pill(im,'LUXODD ARCADE',540,1650,44)
   else:
    self.card(im,self.icon,536,590,740,740,1+.012*math.sin(u),-1,True)
    self.title(im,'YOUR',1390,385,150,u);self.title(im,'TURN.',1390,560,150,u,GREEN,.16,mode='slam')
    self.txt(im,'Find BUCA on',1390,760,44,CREAM,alpha=out((u-.35)/.5));self.pill(im,'LUXODD ARCADE',1390,862,44)
  # Short directional light wipes support the kinetic edit; the drop uses its own flash.
  for cut in [3.1,6.6,11,16.4,26,31,35,40,43]:
   delta=t-cut
   if 0<=delta<.22:
    p=delta/.22;layer=Image.new('RGBA',self.size);d=ImageDraw.Draw(layer);x=int(-self.w*.25+p*self.w*1.65);d.polygon([(x-130,0),(x+65,0),(x+210,self.h),(x+15,self.h)],fill=(91,241,174,round(50*math.sin(p*math.pi))));im.paste(layer,(0,0),layer)
  if t>DURATION-.5:im=Image.blend(im,self.base,ease((t-(DURATION-.5))/.5))
  return im
 def render(self,preview=False):
  name='vertical' if self.v else 'horizontal'
  if preview:
   for t in [.8,2,4.7,9.9,13,18,20.5,21.9,22,22.2,22.6,23.6,27,32,36.5,41.5,45]:self.frame(t).save(OUT/f'preview-{name}-{t:04.1f}.jpg',quality=93)
   return
  dst=OUT/f'BUCA-Luxodd-{name}-v2-silent.mp4'
  p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s',f'{self.w}x{self.h}','-r',str(FPS),'-i','-','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-threads','4','-movflags','+faststart',str(dst)],stdin=subprocess.PIPE)
  try:
   for i in range(TOTAL):
    p.stdin.write(np.asarray(self.frame(i/FPS)).tobytes())
    if i%90==0:print(name,i,'/',TOTAL,flush=True)
  finally:p.stdin.close()
  if p.wait():raise RuntimeError('Encode failed')
  print(dst,flush=True)

def audio():
 rate=48000;n=int(DURATION*rate);rng=np.random.default_rng(27)
 def decode(path):
  p=subprocess.run([FF,'-v','error','-i',str(path),'-vn','-f','f32le','-ac','2','-ar',str(rate),'-'],capture_output=True,check=True)
  return np.frombuffer(p.stdout,np.float32).reshape(-1,2).copy()
 raw=decode(DESK/'AQNl5Jj2JgBuRrPLN_sFVZYLobNtRTt9ZIbjxkCxY5r_AUr40pEIpHoyAOhe3oea.mp4')[:n];music=raw*.83;voice=np.zeros((n,2),np.float32);fx=voice.copy();duck=np.ones(n,np.float32)
 def place(bus,a,at,gain=1):
  start=round(at*rate);end=min(n,start+len(a));
  if a.ndim==1:a=np.repeat(a[:,None],2,axis=1)
  if end>start:bus[start:end]+=a[:end-start]*gain
 def cue(path,at,gain=.4):place(fx,decode(ROOT/path),at,gain)
 for name,at in [('brand',.12),('question',3.35),('turn',43.35)]:
  a=decode(OUT/f'voice-{name}.mp3');peak=max(.01,float(abs(a).max()));a*=.48/peak;place(voice,a,at)
  start=max(0,int((at-.16)*rate));stop=min(n,int((at+len(a)/rate+.22)*rate));duck[start:stop]=np.minimum(duck[start:stop],.32)
 # Smooth automation keeps narration intelligible without losing the original beat.
 coarse=duck[::240];coarse=cv2.GaussianBlur(coarse.reshape(1,-1),(0,0),sigmaX=9).ravel();duck=np.interp(np.arange(n),np.arange(len(coarse))*240,coarse);music*=duck[:,None]
 def whoosh(at,dur=.45,gain=.11):
  m=int(dur*rate);tt=np.arange(m)/rate;noise=rng.standard_normal(m).astype(np.float32);lp=np.convolve(noise,np.ones(9)/9,'same');env=np.sin(np.pi*tt/dur)**2
  sound=lp*env*gain;pan=np.linspace(0,1,m);place(fx,np.stack([sound*np.sqrt(1-pan),sound*np.sqrt(pan)],axis=1),at)
 def hit(at,gain=.4,heavy=False):
  dur=2.2 if heavy else .38;tt=np.arange(int(rate*dur))/rate
  phase=2*np.pi*(42*tt+42*.035*(1-np.exp(-tt/.035))) if heavy else 2*np.pi*(95*tt+75*.018*(1-np.exp(-tt/.018)))
  body=np.sin(phase)*np.exp(-tt*(2.9 if heavy else 15));noise=rng.standard_normal(len(tt))*np.exp(-tt*30)*.24
  place(fx,(body+noise)*gain,at)
 def sparkle(at):
  for j,f in enumerate([1046.5,1318.5,1568,2093]):
   tt=np.arange(int(.65*rate))/rate;a=np.sin(2*np.pi*f*tt)*np.exp(-tt*8)*.045;place(fx,a,at+j*.07)
 for at in [0,3.1,3.33,3.56,6.6,9.55,11,16.4,19.4,20.3,26,31,35,40,43]:whoosh(max(0,at-.12),.38,.09);hit(at,.085)
 whoosh(20.8,1.2,.16)
 hit(IMPACT,.63,True);sparkle(IMPACT+.12)
 cue('Assets/Audio/Celebration/HoleInOneTwinFireworks.wav',22,.22)
 cue('Assets/Audio/Celebration/HappyYeah.wav',22.25,.38)
 cue('Assets/Audio/Player/PuckLaunch.ogg',20.3,.35)
 cue('Assets/Audio/Player/HoleSink.ogg',21.8,.34)
 cue('Assets/Audio/UI/ButtonClick.ogg',10.02,.30)
 cue('Assets/Audio/Celebration/World30Victory.wav',35,.19)
 cue('Assets/Audio/Celebration/HappyYeah.wav',35.45,.25)
 hit(35,.27,True);sparkle(35.2);sparkle(43.4)
 # Briefly make room for the impact transient, without moving the song by one frame.
 start=int(22*rate);count=int(.13*rate);music[start:start+count]*=np.linspace(.62,1,count)[:,None]
 mix=music+voice+fx;mix[:2400]*=np.linspace(0,1,2400)[:,None];mix[-24000:]*=np.linspace(1,0,24000)[:,None]
 before=float(abs(mix).max());mix*=min(1,.94/max(.001,before))
 with wave.open(str(OUT/'soundtrack-v2.wav'),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate);w.writeframes((np.clip(mix,-1,1)*32767).astype('<i2').tobytes())
 (OUT/'audio-report.json').write_text(json.dumps({'duration':DURATION,'impact_seconds':IMPACT,'impact_frame':round(IMPACT*FPS),'timing_basis':'User specified 22 seconds; lyric recognition found phrase starting around 21.74 seconds. Original music timeline preserved.','pre_normalization_peak':before,'peak':float(abs(mix).max()),'narration':'New synthetic en-US-GuyNeural voice; not an imitation of the reference singer.','music':'Exact user-supplied reference soundtrack','effects':'Original synthesized whooshes and bass hits; existing game cues and children cheering.'},indent=2));print('Audio ready',flush=True)

def mux():
 for name in ['horizontal','vertical']:
  target=OUT/f'BUCA-Luxodd-{name}-v2.mp4'
  subprocess.run([FF,'-y','-v','error','-i',str(OUT/f'BUCA-Luxodd-{name}-v2-silent.mp4'),'-i',str(OUT/'soundtrack-v2.wav'),'-map','0:v:0','-map','1:a:0','-c:v','copy','-c:a','aac','-b:a','192k','-t',str(DURATION),'-movflags','+faststart',str(target)],check=True);print(target,flush=True)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('action',choices=['preview','horizontal','vertical','audio','mux']);a=p.parse_args()
 if a.action=='audio':audio()
 elif a.action=='mux':mux()
 elif a.action=='preview':Movie().render(True);Movie(True).render(True)
 else:Movie(a.action=='vertical').render()
