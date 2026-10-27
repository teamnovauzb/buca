"""One continuous story: Level 30 win -> World 1 complete -> departure -> World 2.
Uses real gameplay and the unbroken saved train animation. No World 2 levels.
The removed 'Luxodd Arcade' overlays and opening voice are not used.
"""
from build_luxodd_attract_v3 import Movie as Previous
from build_luxodd_attract_v2 import SOURCE, ROOT, DESK, FF, font, ease, out, CREAM, GREEN, GOLD, MUTED
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import cv2,numpy as np,math,json,subprocess,argparse,wave
OUT=SOURCE/'revision-4';OUT.mkdir(exist_ok=True)
FPS=30;DURATION=48;TOTAL=1440
CAPTURE=json.loads((OUT/'level30/capture.json').read_text())
assert CAPTURE['goal_verified'] and CAPTURE['level']==30
WIN_SOURCE=CAPTURE['win_frame']/FPS
PLAY_RATE=WIN_SOURCE/9
HOLD_AT=13+3.6/PLAY_RATE
RELEASE_AT=13+6/PLAY_RATE
TEXT_CUES=[3.1,3.33,3.56,6.6,6.72,9.3,11,11.15,13,13.15,13.3,HOLD_AT,RELEASE_AT,22,24,24.15,28,29,29.15,39.2,39.35,43,43.28]
CUTS=[3.1,6.6,11,13,24,28,29,43]
class Movie(Previous):
 def brand(self,im,phase=None):
  # User removed the text and spoken phrase Luxodd Arcade from this edit.
  if phase:self.txt(im,phase,self.w-(64 if self.v else 76),64 if self.v else 58,22,MUTED,'rm')
 def level_frame(self,seconds):
  idx=max(0,min(359,round(seconds*30)))
  return Image.open(OUT/'level30/frames'/f'{idx:04d}.jpg').convert('RGB')
 def gameplay(self,t):
  u=t-13;src=u*PLAY_RATE;im=self.base.copy();self.atmosphere(im,t*.5)
  art=self.level_frame(src)
  self.title(im,'LEVEL 30',self.w/2,235 if self.v else 105,116 if self.v else 89,u,GOLD)
  if self.v:self.title(im,'THE FINAL CHALLENGE',540,374,56,u-.15,CREAM)
  if src<3.6:label='JOYSTICK · AIM';age=u;control=0
  elif src<6:label='BUTTON 1 · HOLD';age=t-HOLD_AT;control=1
  else:label='RELEASE TO SHOOT';age=t-RELEASE_AT;control=-1
  self.title(im,label,self.w/2,1660 if self.v else 192,49 if self.v else 40,age,GREEN)
  if self.v:
   box=(22,540,1036,1010);crop=(260,0,1640,1080)
   self.small(im,'BUTTON 3 · UNDO',1772,u-.3,MUTED,30)
  else:box=(40,255,1840,790);crop=None
  self.shot(im,art,box,crop=crop,radius=28)
  # Highlight the actual control only while its input is demonstrated.
  if control>=0:
   x,y,w,h=box;rect=crop or (0,0,1920,1080);cw,ch=rect[2]-rect[0],rect[3]-rect[1];s=max(w/cw,h/ch)
   sx=[700,960][control];sy=815;cx=x+(sx-rect[0])*s-(cw*s-w)/2;cy=y+(sy-rect[1])*s-(ch*s-h)/2
   r=75 if self.v else 78;layer=Image.new('RGBA',self.size);d=ImageDraw.Draw(layer)
   d.ellipse((cx-r,cy-r*.65,cx+r,cy+r*.65),outline=(237,199,107,round(190*out(age/.2))),width=3);im.paste(layer,(0,0),layer)
  return im
 def victory(self,t):
  u=t-22;im=self.base.copy();art=self.level_frame(WIN_SOURCE+u*.85)
  if self.v:
   self.title(im,'LEVEL 30',540,266,112,u,GOLD,mode='slam')
   self.title(im,'COMPLETE!',540,407,110,u-.15,GREEN,mode='slam')
   self.shot(im,art,(0,560,1080,1030),crop=(260,0,1640,1080),radius=0)
   self.small(im,'ONE LAST SHOT. ONE BIG WIN.',1722,u-.25,CREAM,35)
  else:
   self.shot(im,art,(0,0,1920,1080),radius=0)
   band=Image.new('RGB',(1920,180),'#08251e');im.paste(Image.blend(im.crop((0,0,1920,180)),band,.68),(0,0))
   self.title(im,'LEVEL 30 COMPLETE!',960,103,85,u,GOLD,mode='slam')
  self.burst(im,u)
  if u<.17:im=Image.blend(im,Image.new('RGB',self.size,'#fff2cf'),.82*(1-u/.17))
  return im
 def world_complete(self,t):
  u=t-24;im=self.base.copy();art=self.world(5.25+min(u,4.6))
  if self.v:
   self.title(im,'WORLD 1',540,252,111,u,GOLD)
   self.title(im,'COMPLETE',540,391,99,u-.15,CREAM)
   self.shot(im,art,(20,635,1040,800),crop=(185,0,1735,1080))
   self.small(im,'NEXT STOP: WORLD 2',1645,u-.3,GREEN,47)
  else:self.shot(im,art,(0,0,1920,1080),radius=0)
  self.burst(im,u*.85,strength=.55)
  return im
 def journey(self,t):
  u=t-29;src=10+u*(11.7/14);im=self.base.copy();self.atmosphere(im,t*.5)
  raw=self.world(src);art=raw.crop((1285,45,1910,950))
  arriving=src>=18.0
  heading='WELCOME TO' if arriving else 'LEAVING'
  world='WORLD 2' if arriving else 'WORLD 1'
  local=u-(8/(11.7/14)) if arriving else u
  # Matching camera framing retains screen direction from departure through arrival.
  if self.v:
   self.title(im,heading,540,246,73,local,CREAM)
   self.title(im,world,540,377,111,local-.15,GREEN if arriving else GOLD)
   self.shot(im,art,(125,554,830,1202),radius=28)
   self.small(im,'NIGHT CITY' if arriving else '30 LEVELS COMPLETE',1830,local-.35,GOLD,35)
  else:
   self.shot(im,art,(1143,111,608,880),radius=30)
   self.title(im,heading,551,371,84,local,CREAM)
   self.title(im,world,551,520,132,local-.15,GREEN if arriving else GOLD)
   self.txt(im,'NIGHT CITY' if arriving else '30 LEVELS COMPLETE',551,675+15*(1-out((local-.35)/.4)),33,GOLD,alpha=out((local-.35)/.4))
  # The actual full-screen world curtain is sampled in the source; extend it to
  # the surrounding composition so the entire shot travels to the new world.
  cover=ease((src-16.7)/.65)*(1-ease((src-18.0)/1.0))
  if cover>0:
   im=Image.blend(im,Image.new('RGB',self.size,'#07101c'),cover*.97)
   if cover>.4:
    self.txt(im,'NEXT STOP',self.w/2,self.h*.43,48 if self.v else 52,CREAM,alpha=cover)
    self.txt(im,'WORLD 2',self.w/2,self.h*.52,96 if self.v else 113,GOLD,alpha=cover)
    d=ImageDraw.Draw(im);left=self.w/2-160;yy=self.h*.63
    d.line((left,yy,left+320,yy),fill='#436354',width=3);progress=ease((src-16.7)/2.3)
    xx=left+320*progress;d.ellipse((xx-7,yy-7,xx+7,yy+7),fill=GOLD)
  return im
 def scene(self,t):
  if t>=43:return self.endcard(t)
  if t>=29:return self.journey(t)
  if t>=28:
   im=self.base.copy();art=self.world(10)
   if self.v:
    self.title(im,'ALL ABOARD',540,294,103,t-28,GOLD)
    self.shot(im,art,(22,550,1036,935),crop=(180,80,1815,1080))
   else:
    self.shot(im,art,(0,0,1920,1080),radius=0)
    self.pill(im,'ALL ABOARD FOR WORLD 2',960,85,32)
   return im
  if t>=24:return self.world_complete(t)
  if t>=22:return self.victory(t)
  if t>=13:return self.gameplay(t)
  if t>=11:
   u=t-11;im=self.base.copy();self.atmosphere(im,t);art=self.world(1+u)
   if self.v:
    self.title(im,'WORLD 1',540,271,116,u,GOLD);self.title(im,'ONE LAST LEVEL',540,412,73,u-.15,CREAM)
    self.shot(im,art,(22,590,1036,903),crop=(180,70,1815,1080))
   else:
    self.title(im,'WORLD 1 · ONE LAST LEVEL',960,132,74,u,GOLD)
    self.shot(im,art,(40,240,1840,805))
   return im
  if t<3.1:
   im=self.base.copy();self.atmosphere(im,t)
   if self.v:self.shot(im,self.platforms[0],(22,230,1036,1450),crop=(580,0,1360,1063),radius=28)
   else:self.platform(im,0)
   if t<.25:im=Image.blend(self.base,im,ease(t/.25))
   return im
  return super().scene(t)
 def frame(self,t):
  im=self.scene(t)
  for cut in CUTS:
   length=.3 if cut not in [24,43] else .45
   if cut<=t<cut+length:
    if cut not in self.cut_cache:self.cut_cache[cut]=self.scene(cut-1/FPS)
    im=Image.blend(self.cut_cache[cut],im,ease((t-cut)/length));break
  return im
 def render(self,preview=False):
  name='vertical' if self.v else 'horizontal'
  if preview:
   for t in [2,4.8,10.3,12.5,14,17.7,20.5,21.8,22.8,24.9,26.4,28.7,30.2,32.5,35.8,38,40.5,42.7,46.5,47.95]:self.frame(t).save(OUT/f'preview-{name}-{t:05.2f}.jpg',quality=94)
   return
  target=OUT/f'BUCA-Luxodd-{name}-v4-silent.mp4'
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
 for name,at in [('question',3.35),('level30',13.15),('next-world',24.1),('welcome',40.15),('turn',43.35)]:
  path=SOURCE/'revision-3/voice-coming-soon.mp3' if name=='turn' else SOURCE/'revision-2/voice-question.mp3' if name=='question' else OUT/f'voice-{name}.mp3'
  a=decode(path);a*=.48/max(.001,float(abs(a).max()));place(voice,a,at)
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
  gain=.16 if at in [3.1,6.6,11,13,HOLD_AT,RELEASE_AT,24,29,43] else .10
  swish(max(0,at-.035),gain,.29,1 if i%2 else -1);tap(at+.18,.105 if gain>.10 else .065,155+(i%4)*23)
 for i,at in enumerate([7.05,8.25,9.3]):swish(at,.17,.38,1);bell(at+.28,784+i*196,.075)
 swish(20.85,.20,1.15);bass(22,.63)
 for at,f in [(22.12,1046.5),(22.20,1318.5),(22.28,1568)]:bell(at,f,.065)
 cue('Assets/Audio/Celebration/HoleInOneTwinFireworks.wav',22,.22)
 cue('Assets/Audio/Celebration/HappyYeah.wav',22.25,.35)
 cue('Assets/Audio/Player/PuckLaunch.ogg',RELEASE_AT,.35);cue('Assets/Audio/Player/HoleSink.ogg',22,.33)
 cue('Assets/Audio/Celebration/World30Victory.wav',24,.18);cue('Assets/Audio/Celebration/HappyYeah.wav',24.45,.24);bass(24,.28)
 # Continuous movement sound follows the real departure and arrival; gates are quiet.
 train=decode(ROOT/'Assets/Audio/Celebration/StationMovingTrain.wav')
 for at,duration in [(29.95,5.25),(39.65,3.30)]:
  count=int(duration*rate);segment=np.tile(train,(math.ceil(count/len(train)),1))[:count].copy()
  segment[:9600]*=np.linspace(0,1,9600)[:,None];segment[-16800:]*=np.linspace(1,0,16800)[:,None]
  place(fx,segment,at,.17)
 cue('Assets/Audio/Celebration/StationWhistle.wav',29.65,.16)
 tap(35.3,.17,95);swish(37.55,.20,1.0);bell(39.85,659.25,.055);bell(40.1,987.77,.055)
 # Warm three-note identity, landing as the final Luxodd title settles and glints.
 bass(43,.16)
 for at,f in [(43.35,523.25),(43.58,659.25),(43.85,783.99),(46.02,1046.5)]:bell(at,f,.09,1.45)
 music[int(22*rate):int(22.12*rate)]*=np.linspace(.60,1,int(.12*rate))[:,None]
 # The narration finishes naturally; only music/effects fade while the final brand stays visible.
 music[-33600:]*=np.linspace(1,0,33600)[:,None];fx[-18000:]*=np.linspace(1,0,18000)[:,None]
 mix=music+voice+fx;mix[:1800]*=np.linspace(0,1,1800)[:,None];peak=float(abs(mix).max());mix*=min(1,.92/max(.001,peak))
 with wave.open(str(OUT/'soundtrack-v4.wav'),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(rate);w.writeframes((mix*32767).astype('<i2').tobytes())
 (OUT/'audio-report.json').write_text(json.dumps({'duration_seconds':48,'impact_seconds':22,'text_cue_seconds':TEXT_CUES,'source_music_timing':'Original supplied music remains at its original time position.','narration':'Synthetic en-US-GuyNeural; new closing line: Coming soon to Luxodd.','text_effects':'Original stereo air swishes, tactile taps and short pitched accents.','train_effect_seconds':[[29.95,35.2],[39.65,42.95]],'removed_voice':'Luxodd Arcade opening line', 'level30_release_seconds':RELEASE_AT,'end_signature_seconds':[43.35,43.58,43.85,46.02],'pre_normalization_peak':peak,'peak':float(abs(mix).max())},indent=2));print('Audio ready',flush=True)

def mux():
 for name in ['horizontal','vertical']:
  target=OUT/f'BUCA-Luxodd-{name}-v4.mp4'
  subprocess.run([FF,'-y','-v','error','-i',str(OUT/f'BUCA-Luxodd-{name}-v4-silent.mp4'),'-i',str(OUT/'soundtrack-v4.wav'),'-map','0:v:0','-map','1:a:0','-c:v','copy','-c:a','aac','-b:a','224k','-t',str(DURATION),'-movflags','+faststart',str(target)],check=True);print(target,flush=True)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('action',choices=['preview','horizontal','vertical','audio','mux']);a=p.parse_args()
 if a.action=='audio':audio()
 elif a.action=='mux':mux()
 elif a.action=='preview':Movie().render(True);Movie(True).render(True)
 else:Movie(a.action=='vertical').render()
