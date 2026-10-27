"""Feature-led BUCA film: skins -> levels -> real win -> rail journey -> upgrade.
Existing assets only; new isolated Unity captures are 2560x1440.
"""
from pathlib import Path
from functools import lru_cache
import sys,math,subprocess,json
import numpy as np,cv2
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import imageio_ffmpeg
R=Path(__file__).resolve().parents[2];BASE=R/'output/buca-attractor-2026';OUT=BASE/'feature-film';CAP=OUT/'capture';W,H=1920,1080;SIZE=(W,H);FPS=30;DUR=46;FF=imageio_ffmpeg.get_ffmpeg_exe();cv2.setNumThreads(2)
CREAM='#fff5d7';NAVY='#08384b';GOLD='#ffd257';COLORS=['#258dd5','#50b897','#ffc632','#d33e6a','#23abb3'];NAMES=['CLASSIC TOY BOX','MINT GARDEN','HONEY BEE','BERRY CREAM','OCEAN CLUB'];LEVELS=[1,7,12,18,24,30]
def ease(x):x=np.clip(x,0,1);return x*x*(3-2*x)
def out(x):return 1-(1-np.clip(x,0,1))**3
@lru_cache(None)
def font(size):return ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf',int(size))
@lru_cache(18)
def raw(kind,i):
 if kind in ['skins','travel','upgrade','boards']:p=CAP/kind
 elif kind in ['party','whale']:p=BASE/'capture'/kind
 elif kind=='game':p=R/'output/buca-luxodd-attract/revision-4/level30/frames'
 else:p=R/'output/buca-luxodd-attract/gameplay/world2/frames'
 f=p/f'{i:04d}.jpg'
 if not f.exists() and '--preview' in sys.argv:
  samples=sorted(p.glob('sample-*.jpg'));f=samples[min(4,int(i/max(1,COUNTS[kind])*5))]
 return Image.open(f).convert('RGB')
COUNTS={'skins':240,'travel':420,'upgrade':660,'boards':180,'party':360,'whale':216,'game':360,'night':180}
def src(kind,t):return raw(kind,int(np.clip(t*30,0,COUNTS[kind]-1)))
def framecrop(im,cx,cy,width):
 sw,sh=im.size;hh=width*H/W;cx=np.clip(cx,width/2,sw-width/2);cy=np.clip(cy,hh/2,sh-hh/2)
 return im.transform(SIZE,Image.Transform.EXTENT,(cx-width/2,cy-hh/2,cx+width/2,cy+hh/2),Image.Resampling.BICUBIC)
def shot(kind,t,cx=None,cy=None,width=None):
 a=src(kind,t)
 if kind=='travel':
  a=a.crop((154,86,2406,1354));cx=None if cx is None else cx-154;cy=None if cy is None else cy-86
 return framecrop(a,a.width/2 if cx is None else cx,a.height/2 if cy is None else cy,min(a.width,a.width if width is None else width))
@lru_cache(None)
def tracking(kind):
 p=CAP/f'{kind}.csv';p=p if p.exists() else CAP/f'{kind}-samples.csv';a=np.loadtxt(p,delimiter=',').reshape(-1,3)
 # Discard old appended duplicate indices after preview/restarts, retaining last capture.
 d={int(row[0]):row[1:] for row in a};ix=np.array(sorted(d));xy=np.array([d[i] for i in ix]);sample='samples' in p.name
 if sample:ix=np.array([.05,.25,.5,.73,.95])*COUNTS[kind]
 return ix,xy

def follow(kind,t,width,offset=(0,-35)):
 ix,xy=tracking(kind);i=t*30;x=np.interp(i,ix,xy[:,0]);y=np.interp(i,ix,xy[:,1]);return shot(kind,t,x+offset[0],y+offset[1],width)
@lru_cache(128)
def label_art(s,size,color):
 f=font(size);box=f.getbbox(s);layer=Image.new('RGBA',(int(f.getlength(s))+40,box[3]-box[1]+45));d=ImageDraw.Draw(layer)
 d.text((20-box[0]+2,20-box[1]+4),s,font=f,fill='#09394b',stroke_width=4,stroke_fill='#09394b');d.text((20-box[0],20-box[1]),s,font=f,fill=color,stroke_width=1,stroke_fill='#09394b');return layer

def label(a,s,x,y,size=64,color=CREAM,u=1,align='center'):
 p=out(u/.35);layer=label_art(s,size,color);x=x-layer.width/2 if align=='center' else x;y+=30*(1-p)
 if p<1:layer=layer.copy();layer.putalpha(layer.getchannel('A').point(lambda v:int(v*p)))
 a.paste(layer,(round(x),round(y-layer.height/2)),layer)
def corner(a,small,big,u,color=GOLD):
 layer=Image.new('RGBA',(720,205));d=ImageDraw.Draw(layer)
 for x in range(720):d.line((x,0,x,205),fill=(4,30,43,round(160*(1-x/720)**1.3)))
 a.paste(layer,(0,36),layer);label(a,small,45,80,27,CREAM,u,'left');label(a,big,40,153,60,color,u-.06,'left')
def color_badge(a,index,u):
 lay=Image.new('RGBA',(570,86));d=ImageDraw.Draw(lay);d.rounded_rectangle((3,3,567,83),40,fill='#fff4d8',outline=COLORS[index],width=5);d.ellipse((20,19,68,67),fill=COLORS[index]);d.text((94,44),NAMES[index],font=font(29),anchor='lm',fill=NAVY)
 a.paste(lay,(W-615,42+int(28*(1-out(u/.25)))),lay)
def mosaic_end(t):
 u=t-42;im=shot('upgrade',21.95,width=2560)
 # The final full-map view still shows both worlds and the destination train.
 shade=Image.new('RGBA',SIZE);d=ImageDraw.Draw(shade);d.rounded_rectangle((475,340,1445,720),44,fill=(6,38,51,220));im.paste(shade,(0,0),shade)
 label(im,'YOUR JOURNEY STARTS HERE',960,430,52,GOLD,u+.1);label(im,'PRESS PLAY',960,570,122,CREAM,u-.05)
 d=ImageDraw.Draw(im);d.rectangle((0,1023,W,H),fill='#0b3547');d.text((960,1051),'Music: Funkorama — Kevin MacLeod (incompetech.com) • CC BY 4.0 • creativecommons.org/licenses/by/4.0/',font=font(20),anchor='mm',fill=CREAM)
 return im

def scene(t):
 if t<2.5:
  u=t;im=follow('travel',.05+u*.95,1250+900*ease(u/2.5),(-120,-65));corner(im,'BUCA','LET’S GO!',u+.15);return im
 if t<8.5:
  u=t-2.5;st=u*8/6;index=min(4,int(st/1.6));q=math.sin(math.pi*u/6)**2
  im=shot('skins',st,1280+110*q,700-50*q,2560-380*q);color_badge(im,index,u%1.2)
  # Short spotlight pulses land on the actual selected button, never recolor game assets.
  phase=u%1.2
  if .06<phase<.32:
   p=(phase-.06)/.26;layer=Image.new('RGBA',SIZE);d=ImageDraw.Draw(layer);x=335+index*305;rad=40+35*p;d.ellipse((x-rad,775-rad,x+rad,775+rad),outline=(255,248,215,int(170*(1-p))),width=3);im.paste(layer,(0,0),layer)
  return im
 if t<12.5:
  u=t-8.5;st=u*1.5;idx=min(5,int(st));im=shot('boards',st,width=2560);corner(im,'FIND YOUR NEXT CHALLENGE',f'LEVEL {LEVELS[idx]:02}',u%.6667+.15,COLORS[idx%5]);return im
 if t<18.5:
  u=t-12.5;st=3.8+u;im=shot('game',st,960,540,1920-90*ease(u/6))
  if u<2.2:label(im,'AIM. HOLD. RELEASE.',960,103,55,GOLD,u+.1)
  return im
 if t<25.5:
  u=t-18.5;st=.4+u*1.91;width=1450+250*math.sin(u/7*math.pi)**2;im=follow('travel',st,width,(-100,-50));corner(im,'EVERY WIN TAKES YOU FURTHER','FOLLOW YOUR TRAIN',u+.1)
  return im
 if t<28:
  u=t-25.5;im=shot('party',3.7+u*1.1,960,590,1920);return im
 if t<36.5:
  u=t-28;st=float(np.interp(u,[0,2.7,6,8.5],[0,7,16,22]))
  # Hero shot of the real model changing, then pull back for the real 30-to-31 route.
  if u<2.7:im=follow('upgrade',st,1150+200*ease(u/2.7),(0,-35))
  elif u<6:im=follow('upgrade',st,1350+1210*ease((u-2.7)/3.3),(-90,85))
  else:im=shot('upgrade',st)
  if u<2.6:corner(im,'LEVEL 30 COMPLETE','A NEW TRAIN',u+.1)
  elif u>6:label(im,'WELCOME TO WORLD 2',960,112,65,GOLD,u-6)
  return im
 if t<39:
  u=t-36.5;im=shot('whale',.45+u*1.8,1470,540,1250);return im
 if t<42:
  u=t-39;im=shot('night',.25+u*1.75,960,540,1920-180*ease(u/3));label(im,'A WHOLE NEW WORLD TO PLAY',960,105,58,CREAM,u+.1);return im
 return mosaic_end(t)
CUTS=[2.5,8.5,12.5,18.5,25.5,28,36.5,39,42]
@lru_cache(15)
def boundary(t):return scene(t-1/30)
def zoom(a,z):return framecrop(a,960,540,W/z)
def frame(t):
 im=scene(t)
 for cut in CUTS:
  dur=.22 if cut not in [28,42] else .38
  if cut<=t<cut+dur:
   p=ease((t-cut)/dur);before=boundary(cut)
   if cut in [8.5,18.5]:
    off=int(W*p);b=Image.new('RGB',SIZE);b.paste(before,(-off,0));b.paste(im,(W-off,0));im=b.filter(ImageFilter.GaussianBlur(2.3*math.sin(math.pi*p)))
   else:im=Image.blend(zoom(before,1+.07*p),zoom(im,1.06-.06*p),p)
   break
 return im

def preview():
 ts=[.6,2.2,3.3,4.5,5.7,6.9,8.1,9,10.3,11.7,14,16.7,19.5,21.8,24.4,26.7,28.6,30.2,32.5,35.8,37.3,40.1,43,45];tw,th=480,270;sheet=Image.new('RGB',(1920,6*298),NAVY);d=ImageDraw.Draw(sheet)
 for i,t in enumerate(ts):
  a=frame(t);a.save(OUT/f'Preview-{t:04.1f}.jpg',quality=95);x=i%4*tw;y=i//4*298;sheet.paste(a.resize((tw,th)),(x,y));d.text((x+10,y+275),f'{t:.1f}s',font=font(17),fill=CREAM)
 sheet.save(OUT/'Storyboard.jpg',quality=95)
def audio():
 sr=48000;n=sr*DUR
 b=subprocess.check_output([FF,'-v','error','-ss',str(48*60/101),'-i',str(R/'output/buca-reel/assets/Funkorama.mp3'),'-af','atempo=1.188118812','-t',str(DUR),'-f','f32le','-ar',str(sr),'-ac','2','-']);music=np.frombuffer(b,np.float32).reshape(-1,2).copy()[:n]*.58;fx=np.zeros((n,2),np.float32)
 cues=[]
 def cue(path,t,gain,limit=None):
  b=subprocess.check_output([FF,'-v','error','-i',str(R/'Assets/Audio'/path),'-f','f32le','-ar',str(sr),'-ac','2','-']);a=np.frombuffer(b,np.float32).reshape(-1,2).copy()
  if limit is not None:a=a[:int(limit*sr)]
  if len(a)>2400:a[-2400:]*=np.linspace(1,0,2400)[:,None]
  a*=gain/max(.1,float(abs(a).max()));st=int(t*sr);nn=min(len(a),n-st);fx[st:st+nn]+=a[:nn];cues.append({'asset':path,'at':t,'gain':gain})
 cue('Celebration/StationWhistle.wav',0,.3,2);cue('Celebration/StationMovingTrain.wav',0,.22,2.5)
 for t in [2.5,3.7,4.9,6.1,7.3]:cue('UI/NavTick.ogg',t,.3)
 for t in [8.5,9.167,9.833,10.5,11.167,11.833]:cue('UI/PanelOpen.ogg',t,.15)
 cue('Player/PuckLaunch.ogg',14.7,.65);cue('Player/HoleSink.ogg',16.1,.65);cue('Celebration/HappyYeah.wav',16.3,.36,2)
 cue('Celebration/StationMovingTrain.wav',18.5,.2,7);cue('Celebration/HappyYeah.wav',25.7,.28,1.8)
 for t in [28.75,29,29.3,29.7,30.1]:cue('Celebration/ToyPartClick.wav',t,.28)
 cue('Celebration/MoonPuffReady.wav',30,.32);cue('Celebration/WoodenTrainChukLoop.wav',30.7,.18,3.3);cue('Celebration/World2Welcome.wav',34,.25,2.4);cue('HarborWhale/Splash.wav',36.6,.35);cue('HarborWhale/Splash.wav',38.3,.4)
 for t in CUTS:cue('UI/PanelOpen.ogg',t,.15)
 # Music briefly dips for the shot and train upgrade so the game's sound carries the story.
 env=np.ones(n,np.float32)
 for at,duration,gain in [(14.5,2,.55),(28,2.7,.68),(34,2,.65)]:env[int(at*sr):int((at+duration)*sr)]=gain
 env=cv2.GaussianBlur(env[::240].reshape(1,-1),(0,0),sigmaX=14).ravel();env=np.interp(np.arange(n),np.arange(len(env))*240,env);mix=music*env[:,None]+fx
 rng=np.random.default_rng(2026)
 for beat in range(DUR*2):
  st=int(beat*.5*sr);tt=np.arange(int(.18*sr))/sr;k=np.sin(2*np.pi*(49*tt+2.1*(1-np.exp(-tt*35))))*np.exp(-tt*24);nn=min(len(k),n-st);mix[st:st+nn]+=k[:nn,None]*.07
  if beat%2:
   tt=np.arange(int(.1*sr))/sr;k=rng.normal(size=len(tt))*np.exp(-tt*40)*.02;mix[st:st+len(k)]+=k[:,None]
 mix[:960]*=np.linspace(0,1,960)[:,None];mix[-18000:]*=np.linspace(1,0,18000)[:,None]
 subprocess.run([FF,'-y','-v','error','-f','f32le','-ar','48000','-ac','2','-i','-','-af','loudnorm=I=-14:TP=-2:LRA=8','-ar','48000',str(OUT/'Soundtrack.wav')],input=mix.astype('float32').tobytes(),check=True)
 (OUT/'sound-cues.json').write_text(json.dumps(cues,indent=2))
def render():
 p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','1920x1080','-r','30','-i','-','-i',str(OUT/'Soundtrack.wav'),'-c:v','libx264','-preset','fast','-crf','17','-pix_fmt','yuv420p','-threads','5','-c:a','aac','-b:a','256k','-t',str(DUR),'-movflags','+faststart',str(OUT/'BUCA-Full-Journey-Horizontal-v3.mp4')],stdin=subprocess.PIPE)
 for i in range(DUR*FPS):
  p.stdin.write(frame(i/FPS).tobytes())
  if i%120==0:print(f'{i}/{DUR*FPS}',flush=True)
 p.stdin.close();assert p.wait()==0
if __name__=='__main__':
 if '--audio' in sys.argv:audio()
 elif '--preview' in sys.argv:preview()
 else:preview();audio();render()
