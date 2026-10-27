"""BUCA horizontal attractor v2 — choreographed 2.5D animation + actual game footage.
All scene and character art comes from the existing game. Non-destructive video composition.
"""
from pathlib import Path
from functools import lru_cache
import math,subprocess,json,sys
import numpy as np,cv2
from PIL import Image,ImageDraw,ImageFont,ImageFilter,ImageEnhance
import imageio_ffmpeg
R=Path(__file__).resolve().parents[2];BASE=R/'output/buca-attractor-2026';OUT=BASE/'cinematic-horizontal';OUT.mkdir(exist_ok=True)
W,H=1920,1080;SIZE=(W,H);FPS=30;DUR=36;FF=imageio_ffmpeg.get_ffmpeg_exe();cv2.setNumThreads(2)
C=['#36b6ff','#ff695d','#ffca49','#61d59f','#b798ff'];CREAM='#fff8df';NAVY='#073a53'
def clamp(x):return min(1,max(0,x))
def smooth(x):x=clamp(x);return x*x*(3-2*x)
def out(x):return 1-(1-clamp(x))**3
def spring(x):return 1-math.exp(-7*max(0,x))*math.cos(10*max(0,x)) if x>0 else 0
@lru_cache(None)
def font(s,body=False):return ImageFont.truetype('/System/Library/Fonts/Supplemental/'+('Arial Bold.ttf' if body else 'Arial Rounded Bold.ttf'),max(1,int(s)))
@lru_cache(12)
def raw(kind,i):
 p=R/'output/buca-luxodd-attract/revision-4/level30/frames' if kind=='game' else R/'output/buca-luxodd-attract/gameplay/world2/frames' if kind=='night' else BASE/'capture'/kind
 return Image.open(p/f'{i:04d}.jpg').convert('RGB')
def src(kind,t):return raw(kind,min({'game':359,'night':179,'party':359,'whale':215,'reveal':659,'journey':239}[kind],max(0,int(t*30))))
def shot(kind,t,cx=960,cy=540,width=1920):
 a=src(kind,t);height=width*H/W;cx=max(width/2,min(1920-width/2,cx));cy=max(height/2,min(1080-height/2,cy))
 return a.transform(SIZE,Image.Transform.EXTENT,(cx-width/2,cy-height/2,cx+width/2,cy+height/2),Image.Resampling.BICUBIC)
def zoom(a,z):return a.transform(SIZE,Image.Transform.EXTENT,(W*(1-1/z)/2,H*(1-1/z)/2,W*(1+1/z)/2,H*(1+1/z)/2),Image.Resampling.BICUBIC)
def scrim(a,amount=.3):return Image.blend(a,Image.new('RGB',SIZE,NAVY),amount)
def place(a,layer,x,y,scale=1,angle=0):
 if scale<=.01:return
 layer=layer.resize((max(1,int(layer.width*scale)),max(1,int(layer.height*scale))),Image.Resampling.LANCZOS)
 if angle:layer=layer.rotate(angle,Image.Resampling.BICUBIC,expand=True)
 a.paste(layer,(round(x-layer.width/2),round(y-layer.height/2)),layer)
@lru_cache(128)
def word(s,size,color=CREAM,depth=9,body=False):
 f=font(size,body);b=f.getbbox(s);ww=math.ceil(f.getlength(s))+80;hh=b[3]-b[1]+80+depth;im=Image.new('RGBA',(ww,hh));d=ImageDraw.Draw(im);pos=(40-b[0],34-b[1])
 for k in range(depth,0,-1):d.text((pos[0]+k*.5,pos[1]+k),s,font=f,fill='#164156',stroke_width=4,stroke_fill='#164156')
 d.text(pos,s,font=f,fill=color,stroke_width=2,stroke_fill='#ffedb1' if color=='#ffca49' else '#063e59')
 return im
def txt(a,s,x,y,size=100,color=CREAM,u=1,angle=0,body=False):
 p=spring(u/.55);place(a,word(s,size,color,9 if size>65 else 3,body),x,y+70*(1-out(u/.4)),min(1.12,p),angle)
def brand(a,y,t,scale=1):
 for i,ch in enumerate('BUCA'):
  p=spring((t-i*.09)/.55);place(a,word(ch,250,C[i],17),960+(i-1.5)*211*scale,y-10*math.sin(t*5+i),max(.01,p)*scale,-3*math.sin(t*4+i))
# Native game character atlas. Alpha is preserved; pixel aspect ratios never change.
atlas=Image.open(R/'Assets/Art/GrandTrainWelcome/FriendsAtlas.png').convert('RGBA');friends=[]
# Use complete native sprite islands: the authored fixed-width grid clips the rabbit's paw.
aa=np.asarray(atlas);_,labels,stats,_=cv2.connectedComponentsWithStats((aa[:,:,3]>100).astype('uint8'))
islands=sorted([i for i in range(1,len(stats)) if stats[i,4]>100000],key=lambda i:stats[i,0])
assert len(islands)==5
for i in islands:
 x,y,w,h,_=stats[i];pad=5;x0=max(0,x-pad);y0=max(0,y-pad);x1=min(atlas.width,x+w+pad);y1=min(atlas.height,y+h+pad)
 arr=aa[y0:y1,x0:x1].copy();mask=cv2.dilate((labels[y0:y1,x0:x1]==i).astype('uint8'),np.ones((9,9),np.uint8));arr[:,:,3]*=mask
 friends.append(Image.fromarray(arr))
@lru_cache(1)
def stage_bg():
 a=shot('journey',4,width=1690,cy=530).filter(ImageFilter.GaussianBlur(9));return ImageEnhance.Color(a).enhance(.88)
def shadow(a,x,y,rx,ry,opacity=90):
 lay=Image.new('RGBA',(int(rx*2+40),int(ry*2+40)));ImageDraw.Draw(lay).ellipse((20,20,20+rx*2,20+ry*2),fill=(5,25,24,opacity));lay=lay.filter(ImageFilter.GaussianBlur(9));a.paste(lay,(int(x-rx-20),int(y-ry-20)),lay)
def dance(a,t,final=False):
 # Staggered landing, full-body rotation around feet, and independent vertical arcs.
 for i in range(5):
  u=t-i*.22;entry=spring(u/.65);x=278+i*341;floor=934;beat=max(0,math.sin(t*math.pi*4-i*.7));hop=40*beat*beat
  scale=(.79 if final else .82)*max(.01,entry);sp=friends[i];height=sp.height*scale
  shadow(a,x,floor,112-20*beat,19-3*beat,100)
  if u>0:place(a,sp,x, floor-height/2-hop+230*(1-out(u/.45)),scale,4*math.sin(t*math.pi*2+i*.7))
  if not final:
   tag=Image.new('RGBA',(160,72));d=ImageDraw.Draw(tag);d.rounded_rectangle((3,4,157,68),32,fill=C[i],outline='#fff2be',width=3);d.text((80,36),f'LEVEL {(i+1)*5}',font=font(24,True),anchor='mm',fill=NAVY);place(a,tag,x,1002,max(.01,min(1,entry)))
# Small tumbling foil pieces with perspective shading. Kept away from faces.
rng=np.random.default_rng(2041);confetti=[(rng.random(),rng.random(),rng.random(),int(rng.integers(0,5))) for _ in range(48)]
def foil(a,t,strength=1):
 d=ImageDraw.Draw(a)
 for i,(rx,ry,rz,c) in enumerate(confetti):
  x=rx*W+50*math.sin(t*2+i);y=((ry*1300+t*(105+rz*80))%1300)-110
  if 320<y<850 and 150<x<1770:continue
  v=math.cos(t*7+i);l=18+rz*24;ww=3+abs(v)*11;ang=t*3+i;co=math.cos(ang);si=math.sin(ang)
  pts=[(x+px*co-py*si,y+px*si+py*co) for px,py in [(-ww,-l/2),(ww,-l/2),(ww,l/2),(-ww,l/2)]]
  color=C[c] if v>0 else '#ffd984';d.polygon(pts,fill=color);d.line(pts[:2],fill='#fff2cd',width=2)
def streaks(a,p):
 lay=Image.new('RGBA',SIZE);d=ImageDraw.Draw(lay)
 for i in range(17):
  y=70+i*61;xx=int((i*183+p*2700)%2500)-300;d.line((xx,y,xx+120+40*(i%3),y),fill=(255,229,153,int(85*math.sin(math.pi*clamp(p)))),width=2+i%3)
 a.paste(lay,(0,0),lay)
@lru_cache(1)
def ticket():
 a=Image.new('RGBA',(780,420));d=ImageDraw.Draw(a);d.rounded_rectangle((16,22,764,408),45,fill='#8c4f17');d.rounded_rectangle((10,8,760,388),40,fill='#ffc94e',outline='#fff3c2',width=8);d.rounded_rectangle((30,28,740,368),24,fill='#fff5ce',outline='#bb7a25',width=4)
 for x in [100,680]:d.line((x,48,x,348),fill='#109e9f',width=14)
 d.text((390,112),'ALL ABOARD!',font=font(44),anchor='mm',fill=NAVY);d.text((390,210),'WORLD 2',font=font(88),anchor='mm',fill=NAVY);d.text((390,307),'YOUR NEXT ADVENTURE',font=font(25,True),anchor='mm',fill=NAVY)
 return a
CUTS=[1.5,3.5,4.5,5.5,8.5,10,14.5,18.5,19.5,25.5,29.5,32]
def scene(t):
 if t<1.5:
  u=t;im=shot('party',.8+u*.9,cx=550+410*smooth(u/1.4),cy=650,width=880+1040*out(u/1.4));streaks(im,u/1.5)
  if u>.45:txt(im,'ALL ABOARD!',1200,205,132,'#ffca49',u-.45,-4)
  return im
 if t<3.5:
  u=t-1.5;im=stage_bg().copy();brand(im,260,u+.1,1.1);dance(im,u+.9,True);foil(im,t);return im
 if t<4.5:
  u=t-3.5;im=shot('game',4.6+u*.4,710,790,940-90*smooth(u));txt(im,'AIM...',1370,235,165,'#ffca49',u+.1,-5);return im
 if t<5.5:
  u=t-4.5;im=shot('game',5.65+u*.34,1025,785,980-100*smooth(u));txt(im,'LET IT GO!',1310,215,141,CREAM,u+.1,4);return im
 if t<8.5:
  u=t-5.5;im=shot('game',5.5+u,960,540,1920-115*smooth(u/3))
  if u<1.4:txt(im,'CAN YOU SINK IT?',960,100,68,'#ffca49',u+.2)
  return im
 if t<10:
  u=t-8.5;im=shot('game',8.5+u*.7,960,510,1800-80*smooth(u));txt(im,'NICE SHOT!',960,195,155,'#ffca49',u+.12,-3);foil(im,t);return im
 if t<14.5:
  u=t-10;im=stage_bg().copy();txt(im,'FIVE NEW FRIENDS!',960,155,125,CREAM,u+.1,-2);dance(im,u+.05);foil(im,t);return im
 if t<18.5:
  u=t-14.5;st=.35+u*1.85;im=shot('party',st,960,620,1920-150*math.sin(clamp(u/4)*math.pi));
  if u<1.25:txt(im,'LET’S CELEBRATE!',960,180,111,'#ffca49',u+.1,-2)
  return im
 if t<19.5:
  u=t-18.5;im=shot('party',7.8+u*.6,1050,620,1800);z=.15+4.5*u*u;place(im,ticket(),1060-100*smooth(u),590-50*smooth(u),z,12*(1-smooth(u)));return im
 if t<25.5:
  u=t-19.5;st=6+u*16/6
  # The fog opens in the actual capture while a shallow camera push travels to the harbor.
  q=smooth(u/6);im=shot('reveal',st,960+280*q,520,1920-520*q)
  if u<3.5:txt(im,'HELLO, WORLD 2!',960,160,114,'#ffca49',u+.1,-2)
  return im
 if t<29.5:
  u=t-25.5;im=shot('whale',.3+u*1.45,1495,535,1180-80*math.sin(u/4*math.pi))
  if u<1.6:txt(im,'MAKE A SPLASH!',880,160,108,'#ffca49',u+.1,-4)
  return im
 if t<32:
  u=t-29.5;im=shot('night',.2+u*1.8,960,540,1870-120*smooth(u/2.5));txt(im,'MORE TO EXPLORE',960,115,80,CREAM,u+.1);return im
 u=t-32;im=stage_bg().copy();brand(im,230,u+.1,1.02);dance(im,u+2,True);foil(im,t)
 txt(im,'PRESS PLAY!',960,450,103,'#ffca49',u-.2)
 # Restrained small credit, outside the character silhouettes, travels with the export.
 d=ImageDraw.Draw(im);d.rectangle((0,1020,1920,1080),fill='#123f46');d.text((960,1049),'Music: Funkorama — Kevin MacLeod (incompetech.com) • CC BY 4.0 • creativecommons.org/licenses/by/4.0/',font=font(21,True),anchor='mm',fill=CREAM)
 return im
LUT=[]
for gain,lift in [(1.025,1),(1.015,1),(.985,2)]:LUT.extend([round(max(0,min(255,((i/255-.5)*1.035+.5)*255*gain+lift))) for i in range(256)])
@lru_cache(20)
def boundary(c):return scene(c-1/30)
def frame(t):
 im=scene(t)
 # Directional whip and focus-pull transitions have anticipation, acceleration and settle.
 for k,c in enumerate(CUTS):
  length=.23 if c not in [19.5,32] else .32
  if c<=t<c+length:
   p=smooth((t-c)/length);old=boundary(c)
   if c in [3.5,14.5]:
    canvas=Image.new('RGB',SIZE);offset=int(W*p);canvas.paste(old,(-offset,0));canvas.paste(im,(W-offset,0));im=canvas
   else:im=Image.blend(zoom(old,1+.14*p),zoom(im,1.10-.10*p),p)
   if .12<p<.88:im=im.filter(ImageFilter.GaussianBlur(3.2*math.sin(math.pi*p)))
   break
 return im.point(LUT)
def previews():
 ts=[.5,2.8,4,5,6.8,8.9,11.5,13.2,16,19.1,21.5,25,26.6,28.5,30.8,34.3];sheet=Image.new('RGB',(1920,4*294),'#052c3c');d=ImageDraw.Draw(sheet)
 for i,t in enumerate(ts):
  a=frame(t);a.save(OUT/f'frame-{t:04.1f}.jpg',quality=94);x=i%4*480;y=i//4*294;sheet.paste(a.resize((480,270)),(x,y));d.text((x+8,y+273),str(t),fill='white')
 sheet.save(OUT/'Storyboard.jpg',quality=95)
def soundtrack():
 sr=48000;n=sr*DUR
 b=subprocess.check_output([FF,'-v','error','-ss',str(32*60/101),'-i',str(R/'output/buca-reel/assets/Funkorama.mp3'),'-af','atempo=1.188118812','-t',str(DUR),'-f','f32le','-ar',str(sr),'-ac','2','-']);mix=np.frombuffer(b,np.float32).reshape(-1,2).copy()[:n]*.62
 def cue(path,at,gain):
  b=subprocess.check_output([FF,'-v','error','-i',str(R/'Assets/Audio'/path),'-f','f32le','-ar',str(sr),'-ac','2','-']);a=np.frombuffer(b,np.float32).reshape(-1,2);st=round(at*sr);nn=min(len(a),n-st);mix[st:st+nn]+=a[:nn]*gain/max(.1,float(abs(a).max()))
 cue('Celebration/StationWhistle.wav',0,.4);cue('Celebration/StationMovingTrain.wav',0,.22);cue('Player/PuckLaunch.ogg',6,.6);cue('Player/HoleSink.ogg',7.4,.55);cue('Celebration/HappyYeah.wav',7.6,.38);cue('Celebration/HappyYeah.wav',14.8,.32);cue('Celebration/World2Welcome.wav',23,.22);cue('HarborWhale/Splash.wav',25.6,.4);cue('HarborWhale/Splash.wav',27.88,.5)
 for at in CUTS:cue('UI/PanelOpen.ogg',at,.15)
 # Crisp kick, handclap and shaker support the dance choreography without new dialogue.
 rng=np.random.default_rng(71)
 for beat in range(72):
  st=int(beat*.5*sr);tt=np.arange(int(.18*sr))/sr;kick=np.sin(2*np.pi*(47*tt+2.2*(1-np.exp(-tt*32))))*np.exp(-tt*25);nn=min(len(kick),n-st);mix[st:st+nn]+=kick[:nn,None]*.095
  if beat%2:
   tt=np.arange(int(.12*sr))/sr;noise=rng.normal(size=len(tt));noise=np.convolve(noise,[1,-.8],mode='same');clap=noise*np.exp(-tt*35)*.024;mix[st:st+len(clap)]+=clap[:,None]
  st+=int(.25*sr);tt=np.arange(int(.055*sr))/sr;hat=rng.normal(size=len(tt))*np.exp(-tt*85)*.012;nn=min(len(hat),n-st)
  if nn>0:mix[st:st+nn]+=hat[:nn,None]
 mix[:960]*=np.linspace(0,1,960)[:,None];mix[-12000:]*=np.linspace(1,0,12000)[:,None]
 subprocess.run([FF,'-y','-v','error','-f','f32le','-ar','48000','-ac','2','-i','-','-af','loudnorm=I=-14:TP=-2:LRA=8','-ar','48000',str(OUT/'Soundtrack.wav')],input=mix.astype(np.float32).tobytes(),check=True)
def render():
 p=subprocess.Popen([FF,'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-s','1920x1080','-r','30','-i','-','-i',str(OUT/'Soundtrack.wav'),'-c:v','libx264','-preset','fast','-crf','17','-pix_fmt','yuv420p','-threads','5','-c:a','aac','-b:a','256k','-t','36','-movflags','+faststart',str(OUT/'BUCA-Cinematic-Horizontal-v2.mp4')],stdin=subprocess.PIPE)
 for i in range(DUR*FPS):
  p.stdin.write(frame(i/FPS).tobytes())
  if i%90==0:print(f'{i}/{DUR*FPS}',flush=True)
 p.stdin.close();assert p.wait()==0
if __name__=='__main__':
 previews()
 if '--preview' not in sys.argv:soundtrack();render()
