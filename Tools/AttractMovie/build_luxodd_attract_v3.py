"""Luxodd coming-soon trailer: tutorial, World 1 and a World 2 station teaser.
Keep the 22-second musical impact. World 2 level buttons are never shown.
"""
from build_luxodd_attract_v2 import Movie as Previous, SOURCE, OUT as PREVIOUS, ROOT, DESK, FF, font, ease, out, CREAM, GREEN, GOLD, MUTED
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter,ImageChops
import numpy as np
import cv2,math,json,subprocess,argparse,wave
OUT=SOURCE/'revision-3';OUT.mkdir(exist_ok=True)
FPS=30;DURATION=48;TOTAL=1440;IMPACT=22.0
# Every moving headline/subtitle has an entrance cue. The two aspect ratios share the timing.
TEXT_CUES=[0,.12,.6,3.1,3.33,3.56,6.6,6.72,9.3,11,11.18,11.45,16.4,16.8,19.4,20.3,22,22.17,22.28,26,26.3,26.6,31,31.25,32.3,33.55,35,35.14,35.4,39,39.16,39.45,43,43.28]
CUTS=[3.1,6.6,11,16.4,26,31,35,39,43]

class Movie(Previous):
 def __init__(self,vertical=False):
  super().__init__(vertical);self.current_time=0;self.cut_cache={};self.logo_cache={}
  # Local video artwork only: replace the BUCA ownership badge with its clean panel.
  self.platforms[2]=self.platforms[2].copy()
  self.platforms[2].paste(self.platforms[2].crop((861,878,975,922)),(738,878))
  a=np.asarray(self.platforms[0].crop((20,0,449,400))).astype(float)
  mask=np.clip((a[:,:,1]-np.maximum(a[:,:,0],a[:,:,2]))*3.5,0,255)
  rgba=np.zeros((*mask.shape,4),np.uint8);rgba[:,:,:3]=[76,207,151];rgba[:,:,3]=mask.astype(np.uint8)
  self.clover=Image.fromarray(rgba)
 def title(self,im,text,cx,cy,n,local,color=CREAM,delay=0,mode='rise'):
  u=local-delay
  if u<0:return
  art=self.letters(text,n,color).copy();f=font(n);words=text.split(' ');space=f.getlength(' ')
  # Reveal whole words on a short stagger, retaining a common baseline and crisp final glyphs.
  total=sum(f.getlength(w) for w in words)+space*(len(words)-1);cursor=20
  if u<.52+(len(words)-1)*.055:
   for j,word in enumerate(words):
    age=u-j*.055
    if age<0:cursor+=f.getlength(word)+space;continue
    p=min(1,age/.42);alpha=out(min(1,age/.18));w=f.getlength(word)
    x0=max(0,int(cursor-3));x1=min(art.width,int(cursor+w+10));piece=art.crop((x0,0,x1,art.height))
    shift=42*(1-out(p));scale=1+.12*(1-p)**3 if mode=='slam' else .985+.015*out(p)
    if age<.13:piece=piece.filter(ImageFilter.GaussianBlur(1.6*(1-age/.13)))
    if scale!=1:piece=piece.resize((max(1,round(piece.width*scale)),max(1,round(piece.height*scale))),Image.Resampling.BICUBIC)
    piece.putalpha(piece.getchannel('A').point(lambda a:round(a*alpha)))
    x=cx-art.width/2+(x0+x1)/2-piece.width/2;y=cy-piece.height/2+shift
    im.paste(piece,(round(x),round(y)),piece);cursor+=w+space
  else:
   sweep=(u-.72)/.62
   if 0<sweep<1:
    sheen=Image.new('RGBA',art.size);d=ImageDraw.Draw(sheen);x=round(-100+(art.width+180)*sweep)
    d.polygon([(x-25,0),(x+20,0),(x+65,art.height),(x+20,art.height)],fill=(255,255,243,75))
    sheen.putalpha(ImageChops.multiply(sheen.getchannel('A'),art.getchannel('A')));art=Image.alpha_composite(art,sheen)
   im.paste(art,(round(cx-art.width/2),round(cy-art.height/2)),art)
 def small(self,im,text,y,local,color=MUTED,n=30):
  if local<0:return
  p=out(local/.34);self.txt(im,text,self.w/2,y+16*(1-p),n,color,alpha=p)
 def tracked(self,im,text,cx,cy,n,spacing,u,color=GOLD):
  if u<0:return
  f=font(n);width=sum(f.getlength(c) for c in text)+spacing*(len(text)-1);x=cx-width/2
  for j,c in enumerate(text):
   age=u-j*.025;w=f.getlength(c)
   if age>=0:
    p=out(age/.40);self.txt(im,c,x+w/2,cy+25*(1-p),n,color,alpha=p)
   x+=w+spacing
 def endcard(self,t):
  u=t-43;im=self.base.copy();self.atmosphere(im,t*.5)
  cx=self.w/2;logo_y=985 if self.v else 576
  # Exact requested ending: COMING SOON TO LUXODD. No secondary call to action.
  sz=900 if self.v else 880;mark=self.clover.resize((sz,round(sz*self.clover.height/self.clover.width)),Image.Resampling.LANCZOS)
  mark.putalpha(mark.getchannel('A').point(lambda a:round(a*.10)));im.paste(mark,(round(cx-mark.width/2),round(logo_y-mark.height/2)),mark)
  self.glow(im,cx,logo_y,700,.7)
  d=ImageDraw.Draw(im);margin=54 if self.v else 85
  d.rounded_rectangle((margin,margin,self.w-margin,self.h-margin),radius=34,outline='#254c3d',width=2)
  for yy in [margin,self.h-margin]:
   x=margin+(self.w-2*margin)*((u*.10+.1)%1)
   d.line((x-48,yy,x+48,yy),fill='#bcad71',width=2)
  self.tracked(im,'COMING SOON TO',cx,logo_y-(191 if self.v else 170),42 if self.v else 52,7 if self.v else 9,u,CREAM)
  self.title(im,'LUXODD',cx,logo_y,156 if self.v else 205,u-.28,GREEN)
  line_y=logo_y+(151 if self.v else 167);p=out((u-.65)/.7);d.rounded_rectangle((cx-132*p,line_y,cx+132*p,line_y+4),radius=2,fill=GOLD)
  if 2.9<u<3.65:
   a=math.sin((u-2.9)/.75*math.pi);layer=Image.new('RGBA',self.size);ld=ImageDraw.Draw(layer)
   x=cx-132+264*(u-2.9)/.75;ld.ellipse((x-8,line_y-8,x+8,line_y+8),fill=(255,245,196,int(220*a)));im.paste(layer,(0,0),layer)
  return im
 def tutorial(self,t):
  u=t-31;im=self.base.copy();self.atmosphere(im,t);self.brand(im,'QUICK TUTORIAL')
  art=self.video(SOURCE/'gameplay/real-gameplay.mp4',.6+min(u,2.3)*.72)
  self.title(im,'MAKE YOUR MOVE.',self.w/2,302 if self.v else 165,87 if self.v else 94,u,GREEN)
  if self.v:
   box=(30,590,1020,840);crop=(375,310,1530,1080)
  else:
   box=(42,330,1836,708);crop=(220,290,1695,1080)
  self.shot(im,art,box,crop=crop)
  phase=0 if u<1.3 else 1 if u<2.55 else 2;age=u if phase==0 else u-1.3 if phase==1 else u-2.55
  labels=[('JOYSTICK','PULL TO AIM'),('BUTTON 1','HOLD, THEN RELEASE'),('BUTTON 3','UNDO YOUR LAST SHOT')]
  main,sub=labels[phase]
  self.title(im,main,self.w/2,1588 if self.v else 258,59 if self.v else 47,age,GOLD)
  if self.v:self.small(im,sub,1690,age-.12,CREAM,39)
  else:self.txt(im,sub,960,304+12*(1-out((age-.12)/.3)),29,CREAM,alpha=out((age-.12)/.3))
  # A restrained gold outline connects each instruction with the real control.
  x,y,w,h=box;cw,ch=crop[2]-crop[0],crop[3]-crop[1];s=max(w/cw,h/ch)
  source_x=[700,960,1215][phase];source_y=815
  cx=x+(source_x-crop[0])*s-(cw*s-w)/2;cy=y+(source_y-crop[1])*s-(ch*s-h)/2
  r=(99 if self.v else 111)+8*math.sin(min(1,age/.5)*math.pi)
  layer=Image.new('RGBA',self.size);d=ImageDraw.Draw(layer);a=round(235*out(age/.2))
  d.ellipse((cx-r,cy-r*.70,cx+r,cy+r*.70),outline=(237,199,107,a),width=4)
  im.paste(layer,(0,0),layer)
  # The three dots read as tutorial steps, never as cabinet button numbers.
  d=ImageDraw.Draw(im)
  if self.v:
   for j in range(3):d.rounded_rectangle((480+j*45,1770,505+j*45,1775),radius=2,fill=GOLD if j==phase else '#345448')
  return im
 def night_city(self,t):
  u=t-39;im=self.base.copy();self.atmosphere(im,t*.5);self.brand(im,'THE NEXT WORLD')
  # These bounds contain only the city, gate, locomotive, clock and platform.
  # Stop before frame 660, when the large welcome ticket covers the camera.
  idx=570+round(min(1,u/3.999)*80)
  art=Image.open(SOURCE/'worlds/frames'/f'{idx:04d}.jpg').convert('RGB').crop((1285,45,1910,950))
  if self.v:
   self.title(im,'WORLD 2',540,254,114,u,GOLD);self.title(im,'NIGHT CITY',540,393,86,u-.16,GREEN)
   self.shot(im,art,(121,570,838,1214),radius=30)
   self.small(im,'THE ADVENTURE CONTINUES.',1840,u-.45,CREAM,30)
  else:
   self.glow(im,1290,550,730,.32)
   self.shot(im,art,(1143,111,608,880),radius=30)
   self.title(im,'WORLD 2',551,390,142,u,GOLD)
   self.title(im,'NIGHT CITY',551,551,99,u-.16,GREEN)
   self.txt(im,'THE ADVENTURE CONTINUES.',551,694+20*(1-out((u-.45)/.4)),32,CREAM,alpha=out((u-.45)/.4))
  return im
 def scene(self,t):
  self.current_time=t
  if t>=43:return self.endcard(t)
  if t>=39:return self.night_city(t)
  if 31<=t<35:return self.tutorial(t)
  im=super().frame(t)
  if 22<=t<26 and not self.v:
   # Give the gold headline contrast against the warm room without obscuring the win.
   # Recompose only its top strip: the scene remains full-frame and the impact stays at 22 s.
   u=t-22
   if u>.18:
    strip=im.crop((0,0,self.w,182));dark=Image.new('RGB',strip.size,'#08251e')
    strip=Image.blend(strip,dark,.60)
    im.paste(strip,(0,0));self.title(im,'ONE SHOT. BIG FEELING.',960,100,90,u,GOLD,mode='slam')
  return im
 def frame(self,t):
  incoming=self.scene(t)
  for cut in CUTS:
   length=.34 if cut!=43 else .55
   if cut<=t<cut+length:
    if cut not in self.cut_cache:self.cut_cache[cut]=self.scene(cut-1/FPS)
    # A short eased dissolve replaces the abrupt cuts. The 22 s impact remains a hard cut.
    incoming=Image.blend(self.cut_cache[cut],incoming,ease((t-cut)/length));break
  self.current_time=t;return incoming
 def render(self,preview=False):
  name='vertical' if self.v else 'horizontal'
  if preview:
   for t in [2,3.55,4.8,10.3,13,18.5,22.6,31.9,32.9,34.3,37,39.8,41.5,42.9,43.6,44.6,46.5,47.95]:self.frame(t).save(OUT/f'preview-{name}-{t:05.2f}.jpg',quality=93)
   return
  target=OUT/f'BUCA-Luxodd-{name}-v3-silent.mp4'
  p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s',f'{self.w}x{self.h}','-r','30','-i','-','-an','-c:v','libx264','-preset','fast','-crf','17','-pix_fmt','yuv420p','-threads','4','-movflags','+faststart',str(target)],stdin=subprocess.PIPE)
  try:
   for i in range(TOTAL):
    p.stdin.write(np.asarray(self.frame(i/FPS)).tobytes())
    if i%120==0:print(name,i,'/',TOTAL,flush=True)
  finally:p.stdin.close()
  if p.wait():raise RuntimeError('Video encoder failed')
  print(target,flush=True)

def audio():
 rate=48000;n=rate*DURATION;rng=np.random.default_rng(308)
 def decode(path):
  r=subprocess.run([FF,'-v','error','-i',str(path),'-vn','-f','f32le','-ac','2','-ar',str(rate),'-'],capture_output=True,check=True)
  return np.frombuffer(r.stdout,np.float32).reshape(-1,2).copy()
 source=decode(DESK/'AQNl5Jj2JgBuRrPLN_sFVZYLobNtRTt9ZIbjxkCxY5r_AUr40pEIpHoyAOhe3oea.mp4')[:n]
 music=source*.76;fx=np.zeros((n,2),np.float32);voice=fx.copy();duck=np.ones(n,np.float32)
 def place(bus,a,at,gain=1):
  start=round(at*rate);offset=max(0,-start);start=max(0,start)
  if a.ndim==1:a=np.repeat(a[:,None],2,axis=1)
  a=a[offset:];end=min(n,start+len(a))
  if end>start:bus[start:end]+=a[:end-start]*gain
 def cue(rel,at,gain):place(fx,decode(ROOT/rel),at,gain)
 for name,at in [('brand',.12),('question',3.35),('turn',43.35)]:
  a=decode(OUT/'voice-coming-soon.mp3' if name=='turn' else PREVIOUS/f'voice-{name}.mp3');a*=.48/max(.001,float(abs(a).max()));place(voice,a,at)
  start=max(0,int((at-.15)*rate));stop=min(n,int((at+len(a)/rate+.14)*rate));duck[start:stop]=.33
 coarse=cv2.GaussianBlur(duck[::240].reshape(1,-1),(0,0),sigmaX=8).ravel();duck=np.interp(np.arange(n),np.arange(len(coarse))*240,coarse);music*=duck[:,None]
 def swish(at,gain=.14,dur=.32,pan=1):
  m=round(dur*rate);tt=np.arange(m)/rate;noise=rng.standard_normal(m)
  filtered=np.convolve(noise,np.ones(11)/11,'same');envelope=np.sin(np.pi*tt/dur)**2
  tone=np.sin(2*np.pi*(340*tt+1100*tt*tt))*np.exp(-tt*6)*.07
  a=(filtered*envelope+tone)*gain;lr=np.linspace(.2,.8,m) if pan>0 else np.linspace(.8,.2,m)
  place(fx,np.stack([a*np.sqrt(1-lr),a*np.sqrt(lr)],axis=1),at)
 def tap(at,gain=.085,pitch=175):
  tt=np.arange(int(.16*rate))/rate;env=(1-np.exp(-tt*1300))*np.exp(-tt*44)
  a=(np.sin(2*np.pi*pitch*tt)+.22*np.sin(2*np.pi*pitch*2.71*tt))*env
  a+=rng.standard_normal(len(tt))*.17*np.exp(-tt*130);place(fx,a*gain,at)
 def bass(at,gain=.42):
  tt=np.arange(int(2.0*rate))/rate;phase=2*np.pi*(42*tt+47*.038*(1-np.exp(-tt/.038)))
  a=np.sin(phase)*np.exp(-tt*3)+rng.standard_normal(len(tt))*.16*np.exp(-tt*35);place(fx,a*gain,at)
 def bell(at,freq,gain=.075,dur=.95):
  tt=np.arange(int(dur*rate))/rate;attack=1-np.exp(-tt*500)
  a=(np.sin(2*np.pi*freq*tt)+.17*np.sin(2*np.pi*freq*2.01*tt))*attack*np.exp(-tt*6/dur);place(fx,a*gain,at)
 for i,at in enumerate(TEXT_CUES):
  # Clearly audible, short text sounds: soft air on entry, tactile stop as the letters settle.
  gain=.16 if at in [0,3.1,6.6,11,16.4,19.4,20.3,26,31,39,43] else .10
  swish(max(0,at-.035),gain,.29,1 if i%2 else -1);tap(at+.18,.105 if gain>.10 else .065,155+(i%4)*23)
 for i,at in enumerate([7.05,8.25,9.3]):swish(at,.17,.38,1);bell(at+.28,784+i*196,.075)
 swish(20.85,.20,1.15);bass(22,.63)
 for at,f in [(22.12,1046.5),(22.20,1318.5),(22.28,1568)]:bell(at,f,.065)
 cue('Assets/Audio/Celebration/HoleInOneTwinFireworks.wav',22,.22)
 cue('Assets/Audio/Celebration/HappyYeah.wav',22.25,.35)
 cue('Assets/Audio/Player/PuckLaunch.ogg',20.3,.35);cue('Assets/Audio/Player/HoleSink.ogg',21.8,.33)
 cue('Assets/Audio/Celebration/World30Victory.wav',35,.18);cue('Assets/Audio/Celebration/HappyYeah.wav',35.45,.24);bass(35,.28)
 # Train foley follows only the moving locomotive, fading before it parks.
 train=decode(ROOT/'Assets/Audio/Celebration/StationMovingTrain.wav')
 count=int(3.05*rate);train=np.tile(train,(math.ceil(count/len(train)),1))[:count].copy()
 train[:9600]*=np.linspace(0,1,9600)[:,None];train[-16800:]*=np.linspace(1,0,16800)[:,None]
 place(fx,train,39.3,.15);bell(39.35,659.25,.055);bell(39.57,987.77,.055)
 # Warm three-note identity, landing as the final Luxodd title settles and glints.
 bass(43,.16)
 for at,f in [(43.35,523.25),(43.58,659.25),(43.85,783.99),(46.02,1046.5)]:bell(at,f,.09,1.45)
 music[int(22*rate):int(22.12*rate)]*=np.linspace(.60,1,int(.12*rate))[:,None]
 # The narration finishes naturally; only music/effects fade while the final brand stays visible.
 music[-33600:]*=np.linspace(1,0,33600)[:,None];fx[-18000:]*=np.linspace(1,0,18000)[:,None]
 mix=music+voice+fx;mix[:1800]*=np.linspace(0,1,1800)[:,None];peak=float(abs(mix).max());mix*=min(1,.92/max(.001,peak))
 with wave.open(str(OUT/'soundtrack-v3.wav'),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate);w.writeframes((mix*32767).astype('<i2').tobytes())
 (OUT/'audio-report.json').write_text(json.dumps({'duration_seconds':48,'impact_seconds':22,'text_cue_seconds':TEXT_CUES,'source_music_timing':'Original supplied music remains at its original time position.','narration':'Synthetic en-US-GuyNeural; new closing line: Coming soon to Luxodd.','text_effects':'Original stereo air swishes, tactile taps and short pitched accents.','train_effect_seconds':[39.3,42.35],'end_signature_seconds':[43.35,43.58,43.85,46.02],'pre_normalization_peak':peak,'peak':float(abs(mix).max())},indent=2));print('Audio ready',flush=True)

def mux():
 for name in ['horizontal','vertical']:
  target=OUT/f'BUCA-Luxodd-{name}-v3.mp4'
  subprocess.run([FF,'-y','-v','error','-i',str(OUT/f'BUCA-Luxodd-{name}-v3-silent.mp4'),'-i',str(OUT/'soundtrack-v3.wav'),'-map','0:v:0','-map','1:a:0','-c:v','copy','-c:a','aac','-b:a','224k','-t',str(DURATION),'-movflags','+faststart',str(target)],check=True);print(target,flush=True)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('action',choices=['preview','horizontal','vertical','audio','mux']);a=p.parse_args()
 if a.action=='audio':audio()
 elif a.action=='mux':mux()
 elif a.action=='preview':Movie().render(True);Movie(True).render(True)
 else:Movie(a.action=='vertical').render()
