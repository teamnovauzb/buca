"""BUCA / ONE MORE SHOT — portrait advertisement, real captured gameplay.
Music: Funkorama, Kevin MacLeod, CC BY 4.0; credit is burned into the closing shot.
"""
from pathlib import Path
from functools import lru_cache
import math,json,subprocess,sys,wave
import numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageFilter,ImageEnhance
import imageio_ffmpeg
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'output/buca-reel';SRC=ROOT/'output/buca-luxodd-attract'
W,H=1080,1920;FPS=30;N=570;FF=imageio_ffmpeg.get_ffmpeg_exe();B=60/101
CREAM='#fff8e8';YELLOW='#f8dc49';DARK='#102c2c';TEAL='#91edd3'
def ease(x):x=max(0,min(1,x));return 1-(1-x)**3
def clamp(x):return max(0,min(1,x))
@lru_cache(None)
def font(s,body=False):return ImageFont.truetype('/System/Library/Fonts/Supplemental/'+('Arial Bold.ttf' if body else 'DIN Condensed Bold.ttf'),s)
@lru_cache(96)
def source(kind,idx):
 p=SRC/('revision-4/level30/frames' if kind=='g' else 'worlds/frames')/f'{idx:04d}.jpg'
 return Image.open(p).convert('RGB')
def shot(kind,sec,cx=960,cy=540,width=607.5):
 im=source(kind,min(359 if kind=='g' else 869,max(0,round(sec*30))))
 hh=width*H/W
 rect=(cx-width/2,cy-hh/2,cx+width/2,cy+hh/2)
 return im.transform((W,H),Image.Transform.EXTENT,rect,Image.Resampling.BICUBIC)
def scrim(im,top=True,amount=180):
 ar=np.zeros((H,W,4),np.uint8);ar[:,:,:3]=[7,19,19]
 yy=np.arange(H)/H;v=np.clip(1-yy/.55,0,1) if top else np.clip((yy-.48)/.52,0,1)
 ar[:,:,3]=(v[:,None]*amount).astype('uint8');return Image.alpha_composite(im.convert('RGBA'),Image.fromarray(ar)).convert('RGB')
def text(im,s,y,size=160,color=CREAM,progress=1,x=510,body=False,align='center',tilt=0):
 if progress<=0:return
 ft=font(size,body);bbox=ft.getbbox(s);ww=math.ceil(ft.getlength(s))+44;hh=bbox[3]-bbox[1]+40
 layer=Image.new('RGBA',(ww,hh));d=ImageDraw.Draw(layer)
 d.text((22-bbox[0],20-bbox[1]+4),s,font=ft,fill=(0,0,0,60));d.text((22-bbox[0],20-bbox[1]),s,font=ft,fill=color)
 p=ease(progress);scale=.87+.13*p
 layer=layer.resize((round(ww*scale),round(hh*scale)),Image.Resampling.LANCZOS)
 if tilt:layer=layer.rotate(tilt,resample=Image.Resampling.BICUBIC,expand=True)
 layer.putalpha(layer.getchannel('A').point(lambda a:round(a*clamp(progress*3))))
 xx=round(x-layer.width/2) if align=='center' else x
 im.paste(layer,(xx,round(y-layer.height/2+85*(1-p))),layer)
def tag(im,label,y=225):
 d=ImageDraw.Draw(im);d.rounded_rectangle((76,y-27,310,y+27),radius=27,fill=YELLOW)
 text(im,label,y,24,DARK,x=193,body=True)
def lines(im,rows,y,size,u,gap=None,colors=None):
 for i,s in enumerate(rows):text(im,s,y+i*(gap or size*.85),size,(colors or [CREAM]*len(rows))[i],(u-i*.10)/.4)
def brand(im):text(im,'BUCA',170,50,x=900)

def scene(t):
 if t<4*B:
  u=t;im=shot('g',5.95+u*.07,1010,580,550-32*ease(u/(4*B)))
  im=scrim(im,True,220);tag(im,'ONE MORE?')
  lines(im,['ONE.','MORE.','SHOT.'],465,235,u+.4,gap=210,colors=[CREAM,CREAM,YELLOW])
  text(im,'You know the feeling.',1470,39,CREAM,(u-.45)/.4,body=True)
  return im
 if t<6*B:
  u=t-4*B;im=shot('g',1+u,1530,535,410-25*ease(u/(2*B)))
  im=scrim(im,False,190);lines(im,['LOOKS','CUTE.'],1160,200,u+.14,colors=[CREAM,YELLOW]);brand(im);return im
 if t<8*B:
  u=t-6*B;im=shot('g',4+u,1120,550,440-40*ease(u/(2*B)))
  im=scrim(im,True,190);lines(im,['PLAYS','CLEVER.'],370,196,u+.14,colors=[CREAM,YELLOW]);brand(im);return im
 if t<12*B:
  u=t-8*B;im=shot('g',5.45+u,954,540,607.5)
  im=scrim(im,True,145);text(im,'CAN YOU SINK IT?',230,92,YELLOW,(u+.2)/.3)
  return im
 if t<16*B:
  u=t-12*B;sec=7.826+u*.7
  im=shot('g',sec,960,540,607.5).filter(ImageFilter.GaussianBlur(18))
  im=Image.blend(im,Image.new('RGB',(W,H),DARK),.65)
  art=source('g',min(359,round(sec*30))).crop((360,200,1560,1040)).resize((1080,756),Image.Resampling.LANCZOS)
  im.paste(art,(0,570))
  text(im,'YES.',330,245,YELLOW,(u+.15)/.35)
  text(im,'THAT FEELING.',1470,120,CREAM,(u-.05)/.35);return im
 if t<20*B:
  u=t-16*B;im=shot('w',1+u*.9,820,540,607.5)
  im=scrim(im,True,190);lines(im,['A WHOLE','LITTLE WORLD.'],325,125,u+.1,colors=[CREAM,YELLOW]);return im
 if t<24*B:
  u=t-20*B;im=shot('w',19.2+u,1530,570,530)
  im=scrim(im,True,170);lines(im,['NEXT STOP?','MORE FUN.'],325,136,u+.1,colors=[CREAM,YELLOW]);return im
 # Art-directed brand close over a living gameplay background, not a flat slide.
 u=t-24*B;im=shot('g',.2+u*.4,965,545,607.5)
 im=im.filter(ImageFilter.GaussianBlur(4));im=Image.blend(im,Image.new('RGB',(W,H),DARK),.78)
 text(im,'SMALL SHOT. BIG SATISFACTION.',290,30,TEAL,(u+.1)/.4,body=True)
 text(im,'BUCA',650,360,CREAM,(u+.1)/.55)
 text(im,'JUST ONE MORE.',920,105,YELLOW,(u-.1)/.4)
 text(im,'You’re up.',1130,53,CREAM,(u-.35)/.4,body=True)
 p=ease((u-.5)/.4);d=ImageDraw.Draw(im);cx=510
 if p>0:
  d.rounded_rectangle((cx-290*p,1260,cx+290*p,1380),radius=60,fill=YELLOW)
  text(im,'MAKE YOUR SHOT',1320,35,DARK,(u-.65)/.35,body=True)
 # Visible attribution travels with the exported video.
 for i,s in enumerate(['Music: “Funkorama” — Kevin MacLeod','incompetech.com • CC BY 4.0','creativecommons.org/licenses/by/4.0','Edited excerpt / mixed with game effects']):
  text(im,s,1560+i*31,18,CREAM,(u-.8)/.4,body=True)
 return im
CUTS=[4*B,6*B,8*B,12*B,16*B,20*B,24*B]
def frame(t):
 im=scene(t)
 for cut in CUTS:
  if 0<=t-cut<.16:
   p=(t-cut)/.16
   # Short directional wipe; quick motion without a white strobe.
   prev=scene(cut-1/30);offset=round(W*ease(p));im2=Image.new('RGB',(W,H));im2.paste(prev,(-offset,0));im2.paste(im,(W-offset,0));im=im2
   break
 return im

def preview():
 times=[.2,.85,1.7,2.8,3.95,5.25,6.6,7.8,9.9,12.5,14.9,17.5]
 sheet=Image.new('RGB',(4*270,3*505),DARK);d=ImageDraw.Draw(sheet)
 for i,t in enumerate(times):
  im=frame(t);im.save(OUT/f'preview-{t:.2f}.jpg',quality=94);x=i%4*270;y=i//4*505;sheet.paste(im.resize((270,480)),(x,y));d.text((x+9,y+482),f'{t:.2f}s',font=font(20,True),fill=CREAM)
 sheet.save(OUT/'storyboard.jpg',quality=94)
def audio():
 sr=48000
 # Start eight bars into the track for an immediately established groove.
 r=subprocess.run([FF,'-v','error','-ss',str(32*B),'-i',str(OUT/'assets/Funkorama.mp3'),'-t','19','-f','f32le','-ar',str(sr),'-ac','2','-'],capture_output=True,check=True)
 mix=np.frombuffer(r.stdout,np.float32).reshape(-1,2).copy()*.76
 def fx(path,t,gain):
  r=subprocess.run([FF,'-v','error','-i',str(ROOT/path),'-f','f32le','-ar',str(sr),'-ac','2','-'],capture_output=True,check=True);a=np.frombuffer(r.stdout,np.float32).reshape(-1,2);st=round(t*sr);nn=min(len(a),len(mix)-st);mix[st:st+nn]+=a[:nn]*gain
 for t in CUTS:fx('Assets/Audio/UI/PanelOpen.ogg',t,.08)
 fx('Assets/Audio/Player/PuckLaunch.ogg',8*B+.55,.38)
 fx('Assets/Audio/Player/HoleSink.ogg',8*B+1.95,.45)
 fx('Assets/Audio/Celebration/HappyYeah.wav',8*B+2.05,.17)
 mix[:960]*=np.linspace(0,1,960)[:,None];mix[-24000:]*=np.linspace(1,0,24000)[:,None]
 mix*=.94/max(.94,float(abs(mix).max()))
 raw=OUT/'mix.wav'
 with wave.open(str(raw),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes((mix*32767).astype('<i2').tobytes())
 subprocess.run([FF,'-y','-v','error','-i',str(raw),'-af','loudnorm=I=-14:TP=-1.5:LRA=8','-ar','48000',str(OUT/'ad-audio.wav')],check=True)
if __name__=='__main__':
 preview()
 if '--preview' in sys.argv:sys.exit()
 audio();p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','1080x1920','-r','30','-i','-','-i',str(OUT/'ad-audio.wav'),'-c:v','libx264','-crf','17','-preset','fast','-pix_fmt','yuv420p','-threads','4','-c:a','aac','-b:a','256k','-movflags','+faststart','-shortest',str(OUT/'BUCA-One-More-Shot-Reel.mp4')],stdin=subprocess.PIPE)
 for i in range(N):
  p.stdin.write(frame(i/FPS).tobytes())
  if i%90==0:print(f'{i}/{N}',flush=True)
 p.stdin.close();assert p.wait()==0
 print(OUT/'BUCA-One-More-Shot-Reel.mp4',flush=True)
