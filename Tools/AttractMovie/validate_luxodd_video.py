"""Decode both exports and make contact sheets from the delivered MP4 files."""
from pathlib import Path
import argparse,json,subprocess
import cv2,numpy as np
from PIL import Image,ImageDraw
import imageio_ffmpeg
p=argparse.ArgumentParser();p.add_argument('--revision',type=int,default=2);args=p.parse_args();version=f'v{args.revision}'
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/f'output/buca-luxodd-attract/revision-{args.revision}';FF=imageio_ffmpeg.get_ffmpeg_exe()
report={}
for name in ['horizontal','vertical']:
 path=OUT/f'BUCA-Luxodd-{name}-{version}.mp4';cap=cv2.VideoCapture(str(path))
 record={'width':int(cap.get(3)),'height':int(cap.get(4)),'fps':cap.get(5),'frames':int(cap.get(7)),'bytes':path.stat().st_size}
 assert (record['width'],record['height'])==((1920,1080) if name=='horizontal' else (1080,1920))
 assert record['frames']==1440 and record['fps']==30
 result=subprocess.run([FF,'-v','error','-i',str(path),'-f','null','-'],capture_output=True,text=True)
 assert result.returncode==0 and not result.stderr,result.stderr
 record['full_decode']='passed';record['duration_seconds']=48;record['impact_frame']=660
 audio=subprocess.run([FF,'-v','error','-i',str(path),'-map','0:a:0','-f','f32le','-ac','2','-ar','48000','-'],capture_output=True,check=True).stdout
 pcm=np.frombuffer(audio,np.float32);record['decoded_audio_peak']=float(abs(pcm).max());assert len(pcm)>47*48000*2
 assert record['decoded_audio_peak']<1,'Audio clipping detected'
 if args.revision==5:
  times=[.7,2.6,6.6,8.4,10.8,12.2,14.4,18,20.6,21.8,22.8,25.3,27,28.6,30.8,34,35.8,36.5,38.2,42.6,45.2,47.95]
 elif args.revision==4:
  times=[2,4.8,10.3,12.5,14,17.7,20.5,21.8,22.8,24.9,26.4,28.7,30.2,32.5,35.8,38,40.5,42.7,46.5,47.95]
 else:
  times=[2,4.8,8.3,10.3,13,18.5,22.0,22.6,27,31.9,32.9,34.3,37,41.5,46.5,47.95] if args.revision==3 else [2,4.8,8.3,10.3,13,18.5,22.0,22.6,27,32,37,45]
 w,h=(480,270) if name=='horizontal' else (270,480)
 sheet=Image.new('RGB',(w*4,(h+28)*((len(times)+3)//4)),(7,20,20));d=ImageDraw.Draw(sheet)
 for i,t in enumerate(times):
  cap.set(cv2.CAP_PROP_POS_FRAMES,round(t*30));ok,frame=cap.read();assert ok
  im=Image.fromarray(cv2.cvtColor(frame,cv2.COLOR_BGR2RGB)).resize((w,h),Image.Resampling.LANCZOS);x=i%4*w;y=i//4*(h+28);sheet.paste(im,(x,y));d.text((x+8,y+h+8),f'{t:.1f}s',fill='white')
 sheet.save(OUT/f'final-{version}-{name}-contact.jpg',quality=94)
 full_frames=[6.6,8.4,10.8,12.2,18,22.8,25.3,30.8,35.8,38.2,42.6,47.95] if args.revision==5 else [2,10.3,14,22.8,25.8,32.5,35.8,40.5,42.7,47.95] if args.revision==4 else [10.3,32.9,34.3,41.5,47.95] if args.revision==3 else []
 for t in full_frames:
  cap.set(cv2.CAP_PROP_POS_FRAMES,round(t*30));ok,frame=cap.read();assert ok
  Image.fromarray(cv2.cvtColor(frame,cv2.COLOR_BGR2RGB)).save(OUT/f'final-{name}-{t:.2f}.jpg',quality=95)
 cap.release();report[name]=record
(OUT/'final-validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
