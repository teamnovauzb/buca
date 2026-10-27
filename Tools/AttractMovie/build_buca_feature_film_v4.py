"""Requested refinement: no skin badge, no animal party, clean locomotive, refined titles."""
from pathlib import Path
from functools import lru_cache
import math,json,sys,subprocess
import cv2,numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import imageio_ffmpeg
import build_buca_feature_film as base
R=base.R;OLD=base.OUT;OUT=base.BASE/'refined-horizontal-v4';OUT.mkdir(exist_ok=True)
W,H=1920,1080;SIZE=(W,H);FPS=30;DUR=43.5;FRAMES=1305;FF=imageio_ffmpeg.get_ffmpeg_exe()
INK='#102e40';IVORY='#fff8e9';GOLD='#ead39a'
original_raw=base.raw;original_tracking=base.tracking
@lru_cache(18)
def raw(kind,index):
 if kind=='party':raise AssertionError('Removed animal celebration must never enter the v4 edit')
 if kind!='upgrade':return original_raw(kind,index)
 path=OUT/'capture/upgrade'/f'{index:04d}.jpg'
 if not path.exists() and '--preview' in sys.argv:return original_raw(kind,index)
 return Image.open(path).convert('RGB')
@lru_cache(None)
def tracking(kind):
 if kind!='upgrade':return original_tracking(kind)
 p=OUT/'capture/upgrade.csv'
 if not p.exists():return original_tracking(kind)
 a=np.loadtxt(p,delimiter=',').reshape(-1,3);d={int(r[0]):r[1:] for r in a};i=np.array(sorted(d));return i,np.array([d[n] for n in i])
@lru_cache(None)
def font(n,weight='demi'):return ImageFont.truetype('/System/Library/Fonts/Avenir Next.ttc',round(n),index={'regular':7,'medium':5,'demi':2,'bold':0}[weight])
def ease(x):x=float(np.clip(x,0,1));return x*x*(3-2*x)
@lru_cache(160)
def lettering(s,size,color,track=0,weight='demi'):
 f=font(size,weight);b=f.getbbox(s);width=math.ceil(sum(f.getlength(c) for c in s)+track*max(0,len(s)-1))+64;height=b[3]-b[1]+64
 face=Image.new('RGBA',(width,height));d=ImageDraw.Draw(face);x=32
 for c in s:d.text((x,32-b[1]),c,font=f,fill=color);x+=f.getlength(c)+track
 shadow=Image.new('RGBA',face.size,(2,22,32,0));shadow.putalpha(face.getchannel('A').point(lambda a:int(a*.66)));shadow=shadow.filter(ImageFilter.GaussianBlur(5))
 result=Image.new('RGBA',face.size);result.alpha_composite(shadow,(0,4));result.alpha_composite(face);return result

def text(a,s,x,y,size,color=IVORY,alpha=1,track=0,weight='demi',scale=1,align='center'):
 if alpha<=0:return
 layer=lettering(s,size,color,track,weight)
 if abs(scale-1)>.001:layer=layer.resize((round(layer.width*scale),round(layer.height*scale)),Image.Resampling.LANCZOS)
 if alpha<1:layer=layer.copy();layer.putalpha(layer.getchannel('A').point(lambda v:int(v*alpha)))
 xx=x-layer.width/2 if align=='center' else x-32
 a.paste(layer,(round(xx),round(y-layer.height/2)),layer)
@lru_cache(24)
def wash(height,strength):
 arr=np.zeros((height,W,4),np.uint8);arr[:,:,:3]=[5,26,38];y=np.arange(height)/height;arr[:,:,3]=(strength*(1-y[:,None])**1.5).astype('uint8');return Image.fromarray(arr)
def aim(a,u):
 alpha=ease(u/.3)*(1-ease((u-1.85)/.35));layer=wash(255,155)
 if alpha<1:layer=layer.copy();layer.putalpha(layer.getchannel('A').point(lambda v:int(v*alpha)))
 a.paste(layer,(0,0),layer)
 fx=Image.new('RGBA',SIZE);d=ImageDraw.Draw(fx)
 for i,word in enumerate(['Aim','Hold','Release']):
  p=ease((u-i*.1)/.42);opacity=alpha*p;x=650+i*310
  text(a,f'0{i+1}',x,55,18,GOLD,opacity,3,'medium')
  text(a,word,x,108+16*(1-p),59,IVORY,opacity,0,'demi')
  if opacity>0:d.line((x-58*p,155,x+58*p,155),fill=(234,211,154,int(190*opacity)),width=2)
 for x in [805,1115]:d.ellipse((x-2,105,x+2,109),fill=(234,211,154,int(180*alpha)))
 a.paste(fx,(0,0),fx)
def welcome(a,u):
 p=ease(u/.7);alpha=ease(u/.38);layer=wash(320,210)
 if alpha<1:layer=layer.copy();layer.putalpha(layer.getchannel('A').point(lambda v:int(v*alpha)))
 a.paste(layer,(0,0),layer)
 text(a,'WELCOME TO',960,56+12*(1-p),26,GOLD,alpha,7,'medium')
 text(a,'World 2',960,140+18*(1-p),111,IVORY,alpha,1,'bold',1.025-.025*p)
 fx=Image.new('RGBA',SIZE);d=ImageDraw.Draw(fx);half=220*p
 d.line((960-half,221,960+half,221),fill=(234,211,154,int(155*alpha)),width=2)
 if p>.2:
  x=960-half+2*half*ease((u-.25)/1.3);d.ellipse((x-4,217,x+4,225),fill=(255,248,222,int(230*alpha)))
 a.paste(fx,(0,0),fx)
def modern_label(a,s,x,y,size=64,color=IVORY,u=1,align='center'):
 if s=='AIM. HOLD. RELEASE.':aim(a,u-.1);return
 if s=='WELCOME TO WORLD 2':welcome(a,u);return
 p=ease(u/.4);text(a,s,x,y+16*(1-p),size,color,p,.5,'demi',align=align)
def corner(a,small,big,u,color=GOLD):
 lay=Image.new('RGBA',(790,210));d=ImageDraw.Draw(lay)
 for x in range(790):d.line((x,0,x,210),fill=(5,26,38,round(150*(1-x/790)**1.6)))
 a.paste(lay,(0,30),lay);p=ease(u/.4)
 text(a,small,48,77,24,IVORY,p,1.8,'medium',align='left');text(a,big,45,142+12*(1-p),54,color,p,0,'demi',align='left')
base.raw=raw;base.tracking=tracking;base.label=modern_label;base.corner=corner;base.color_badge=lambda *args,**kwargs:None
CUTS=[2.5,8.5,12.5,18.5,25.5,34,36.5,39.5]
def scene(t):return base.scene(t if t<25.5 else t+2.5)
@lru_cache(12)
def boundary(t):return scene(t-1/30)
def frame(t):
 im=scene(t)
 for c in CUTS:
  duration=.24 if c not in [25.5,39.5] else .38
  if c<=t<c+duration:
   p=ease((t-c)/duration);old=boundary(c)
   if c in [8.5,18.5]:
    off=round(W*p);canvas=Image.new('RGB',SIZE);canvas.paste(old,(-off,0));canvas.paste(im,(W-off,0));im=canvas.filter(ImageFilter.GaussianBlur(2.3*math.sin(math.pi*p)))
   else:im=Image.blend(base.zoom(old,1+.07*p),base.zoom(im,1.06-.06*p),p)
   break
 return im

def preview():
 ts=[3.3,5.7,8.1,13.1,14,16.7,22.8,25.9,27.6,29.8,31.7,33.5,34.8,38,40.5,42.5];sheet=Image.new('RGB',(1920,4*298),INK);d=ImageDraw.Draw(sheet)
 for i,t in enumerate(ts):
  im=frame(t);im.save(OUT/f'Preview-{t:04.1f}.jpg',quality=95);x=i%4*480;y=i//4*298;sheet.paste(im.resize((480,270)),(x,y));d.text((x+8,y+275),str(t),font=font(17),fill=IVORY)
 sheet.save(OUT/'Storyboard.jpg',quality=95)
def audio():
 sr=48000;n=round(DUR*sr)
 b=subprocess.check_output([FF,'-v','error','-ss',str(48*60/101),'-i',str(R/'output/buca-reel/assets/Funkorama.mp3'),'-af','atempo=1.188118812','-t',str(DUR),'-f','f32le','-ar',str(sr),'-ac','2','-']);music=np.frombuffer(b,np.float32).reshape(-1,2).copy()[:n]*.58;fx=np.zeros((n,2),np.float32)
 cues=[]
 for cue in json.loads((OLD/'sound-cues.json').read_text()):
  at=cue['at'];path=cue['asset']
  if 'HappyYeah' in path or 25.5<=at<28:continue
  at=at if at<25.5 else at-2.5
  b=subprocess.check_output([FF,'-v','error','-i',str(R/'Assets/Audio'/path),'-f','f32le','-ar',str(sr),'-ac','2','-']);a=np.frombuffer(b,np.float32).reshape(-1,2).copy()
  limit=7 if path.endswith('StationMovingTrain.wav') and at>5 else 2.5 if path.endswith('StationMovingTrain.wav') else 3.3 if path.endswith('WoodenTrainChukLoop.wav') else 2.4 if path.endswith('World2Welcome.wav') else 100
  a=a[:int(limit*sr)]
  if len(a)>2400:a[-2400:]*=np.linspace(1,0,2400)[:,None]
  a*=cue['gain']/max(.1,float(abs(a).max()));st=round(at*sr);nn=min(len(a),n-st);fx[st:st+nn]+=a[:nn];cues.append(dict(cue,at=at))
 env=np.ones(n,np.float32)
 for at,duration,gain in [(14.5,2,.55),(25.5,2.7,.68),(31.5,2,.65)]:env[round(at*sr):round((at+duration)*sr)]=gain
 env=cv2.GaussianBlur(env[::240].reshape(1,-1),(0,0),sigmaX=14).ravel();env=np.interp(np.arange(n),np.arange(len(env))*240,env);mix=music*env[:,None]+fx;rng=np.random.default_rng(2026)
 for beat in range(math.ceil(DUR*2)):
  st=round(beat*.5*sr);tt=np.arange(round(.18*sr))/sr;k=np.sin(2*np.pi*(49*tt+2.1*(1-np.exp(-tt*35))))*np.exp(-tt*24);nn=min(len(k),n-st);mix[st:st+nn]+=k[:nn,None]*.07
  if beat%2:
   tt=np.arange(round(.1*sr))/sr;k=rng.normal(size=len(tt))*np.exp(-tt*40)*.02;nn=min(len(k),n-st);mix[st:st+nn]+=k[:nn,None]
 mix[:960]*=np.linspace(0,1,960)[:,None];mix[-18000:]*=np.linspace(1,0,18000)[:,None]
 subprocess.run([FF,'-y','-v','error','-f','f32le','-ar','48000','-ac','2','-i','-','-af','loudnorm=I=-14:TP=-2:LRA=8','-ar','48000',str(OUT/'Soundtrack.wav')],input=mix.astype('float32').tobytes(),check=True)
 (OUT/'sound-cues.json').write_text(json.dumps(cues,indent=2))
def render():
 p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','1920x1080','-r','30','-i','-','-i',str(OUT/'Soundtrack.wav'),'-c:v','libx264','-preset','fast','-crf','17','-pix_fmt','yuv420p','-threads','5','-c:a','aac','-b:a','256k','-t',str(DUR),'-movflags','+faststart',str(OUT/'BUCA-Horizontal-Refined-v4.mp4')],stdin=subprocess.PIPE)
 for i in range(FRAMES):
  p.stdin.write(frame(i/FPS).tobytes())
  if i%120==0:print(f'{i}/{FRAMES}',flush=True)
 p.stdin.close();assert p.wait()==0
if __name__=='__main__':
 if '--preview' in sys.argv:preview()
 elif '--audio' in sys.argv:audio()
 else:preview();audio();render()
