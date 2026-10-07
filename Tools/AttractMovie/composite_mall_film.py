"""Track the cabinet LCD and insert real BUCA footage into moving film shots.

All outputs live outside Assets. The human shots are locally generated motion;
the inserted game is the recorded, unmodified Unity game. No still-image pans.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "output/buca-mall-film"
FFMPEG = "/private/tmp/buca-local-video/bin/ffmpeg"
FPS = 24
cv2.setNumThreads(2)

# LCD corners in the original 1672 x 941 image references, clockwise from TL.
SCENES = {
    "01-walk": (6, "01-walking", [(1164,267),(1366,249),(1351,405),(1138,402)]),
    "02-approach": (5, "02-approach", [(1080,197),(1420,179),(1389,419),(1041,407)]),
    "03-select": (5, "03-controls", [(838,130),(1451,113),(1407,559),(777,507)]),
    "04-play": (6, "03-controls", [(838,130),(1451,113),(1407,559),(777,507)]),
    "05-focus": (3, "06-focus", None),
    "06-win": (5, "03-controls", [(838,130),(1451,113),(1407,559),(777,507)]),
    "07-celebrate": (6, "04-reaction", [(1005,276),(1437,258),(1413,578),(960,562)]),
    "08-invite": (4, "05-final", [(1005,276),(1437,258),(1413,578),(960,562)]),
}

BRAND_CORNERS = {
    "01-walk": {
        "front": [(1113,474),(1225,477),(1225,518),(1113,514)],
        "stand": [(1066,623),(1253,637),(1253,702),(1066,687)],
        "side": [(1345,471),(1408,463),(1408,507),(1345,515)],
    },
    "02-approach": {
        "front": [(1025,506),(1180,513),(1180,573),(1025,566)],
        "side": [(1381,515),(1479,500),(1479,571),(1381,586)],
    },
    "03-select": {
        "deck": [(881,720),(1068,744),(1068,802),(881,778)],
        "side": [(1404,814),(1524,742),(1524,850),(1404,922)],
    },
    "08-invite": {
        "deck": [(984,687),(1135,705),(1135,746),(984,728)],
        "front": [(933,755),(1147,783),(1147,863),(933,835)],
        "side": [(1401,756),(1509,716),(1509,800),(1401,840)],
    },
}
BRAND_CORNERS["04-play"]=BRAND_CORNERS["03-select"]
BRAND_CORNERS["06-win"]=BRAND_CORNERS["03-select"]
BRAND_CORNERS["07-celebrate"]=BRAND_CORNERS["08-invite"]


def mask_for(quad, shape, border=0):
    mask = np.zeros(shape[:2], np.uint8)
    cv2.fillConvexPoly(mask, np.round(quad).astype(np.int32), 255)
    if border:
        n = abs(border)*2+1
        operation = cv2.dilate if border > 0 else cv2.erode
        mask = operation(mask, np.ones((n,n), np.uint8))
    return mask


def warp_points(points, matrix):
    return cv2.perspectiveTransform(np.asarray(points,np.float32)[None],matrix)[0]


def valid_quad(candidate, previous, maximum_move=15):
    if candidate is None or not np.isfinite(candidate).all():
        return False
    if not cv2.isContourConvex(candidate.astype(np.float32)):
        return False
    area = abs(cv2.contourArea(candidate.astype(np.float32)))
    previous_area = abs(cv2.contourArea(previous.astype(np.float32)))
    return (0.80 < area/max(previous_area,1) < 1.25
            and np.max(np.linalg.norm(candidate-previous,axis=1)) < maximum_move)


def track_screen(name, source=None, calibration=None, label=None):
    duration, anchor, corners = SCENES[name]
    if calibration is not None:
        corners=calibration
    if corners is None:
        return None
    source = source or ART / "motion" / f"{name}.mp4"
    cap = cv2.VideoCapture(str(source))
    ok, first = cap.read()
    if not ok:
        raise RuntimeError(f"Cannot read motion shot {source}")
    h,w = first.shape[:2]
    ref = cv2.imread(str(ART/"anchors"/f"{anchor}.png"))
    rh,rw = ref.shape[:2]
    scale = max(w/rw,h/rh)
    resized = (round(rw*scale),round(rh*scale))
    offset = np.array([(resized[0]-w)//2,(resized[1]-h)//2])
    quad = np.array(corners,np.float32)*scale-offset
    reference = cv2.resize(ref,resized,interpolation=cv2.INTER_AREA)
    reference = reference[offset[1]:offset[1]+h,offset[0]:offset[0]+w]
    refgray = cv2.cvtColor(reference,cv2.COLOR_BGR2GRAY)
    gray = cv2.cvtColor(first,cv2.COLOR_BGR2GRAY)
    sift = cv2.SIFT_create(nfeatures=1300,contrastThreshold=.018)
    # Include the bezel to keep tracking independent of changing game graphics.
    border=8 if label else 20
    initial_mask = mask_for(quad,gray.shape,border=border)
    refkp,refdes = sift.detectAndCompute(refgray,initial_mask)
    matcher = cv2.BFMatcher()

    def reference_lock(current, predicted, maxmove):
        kp,des = sift.detectAndCompute(current,mask_for(predicted,current.shape,border))
        if des is None or refdes is None or len(des)<8:
            return None,0
        pairs = matcher.knnMatch(refdes,des,k=2)
        good = [p[0] for p in pairs if len(p)==2 and p[0].distance < .70*p[1].distance]
        if len(good)<12:
            return None,len(good)
        a=np.float32([refkp[m.queryIdx].pt for m in good])
        b=np.float32([kp[m.trainIdx].pt for m in good])
        matrix,inliers=cv2.findHomography(a,b,cv2.RANSAC,2.4)
        if matrix is None or inliers.sum()<12 or inliers.mean()<.5:
            return None,0
        fixed=warp_points(np.array(corners,np.float32)*scale-offset,matrix)
        return (fixed if valid_quad(fixed,predicted,maxmove) else None),int(inliers.sum())

    locked,count=reference_lock(gray,quad,18)
    if locked is not None:
        quad=locked
    frames=[quad.tolist()]
    diagnostics=[{"frame":0,"reference_inliers":count,"mode":"initial"}]
    previous=gray
    points=cv2.goodFeaturesToTrack(previous,250,.01,5,mask=initial_mask)
    total=int(round(duration*FPS))
    total=min(total,int(cap.get(cv2.CAP_PROP_FRAME_COUNT)))
    for frame_index in range(1,total):
        ok,frame=cap.read()
        if not ok:
            break
        gray=cv2.cvtColor(frame,cv2.COLOR_BGR2GRAY)
        mode="held"
        if points is not None and len(points)>=8:
            nxt,st,err=cv2.calcOpticalFlowPyrLK(previous,gray,points,None,
                winSize=(25,25),maxLevel=3,
                criteria=(cv2.TERM_CRITERIA_EPS|cv2.TERM_CRITERIA_COUNT,30,.01))
            back,bst,berr=cv2.calcOpticalFlowPyrLK(gray,previous,nxt,None,
                winSize=(25,25),maxLevel=3)
            good=(st[:,0]>0)&(bst[:,0]>0)&(np.linalg.norm(back[:,0]-points[:,0],axis=1)<1.4)
            a=points[good,0];b=nxt[good,0]
            if len(a)>=8:
                if label and name!="02-approach":
                    affine,inliers=cv2.estimateAffinePartial2D(a,b,method=cv2.RANSAC,
                        ransacReprojThreshold=1.5)
                    matrix=np.vstack([affine,[0,0,1]]) if affine is not None else None
                else:
                    matrix,inliers=cv2.findHomography(a,b,cv2.RANSAC,1.8)
                if matrix is not None and inliers.sum()>=8:
                    candidate=warp_points(quad,matrix)
                    if valid_quad(candidate,quad,15):
                        quad=candidate
                        mode="flow"
        # Regularly re-lock to the original cabinet to prevent accumulated drift.
        if not label and frame_index%6==0:
            locked,count=reference_lock(gray,quad,9)
            if locked is not None:
                quad=.45*quad+.55*locked
                mode="reference"
        frames.append(quad.tolist())
        diagnostics.append({"frame":frame_index,"mode":mode})
        points=cv2.goodFeaturesToTrack(gray,250,.01,5,
            mask=mask_for(quad,gray.shape,border=border))
        previous=gray
    cap.release()
    result={"source":str(source),"width":w,"height":h,"fps":FPS,"calibration":corners,"version":2,
            "corners":frames,"diagnostics":diagnostics}
    path=ART/"tracking"/f"{name}{'-'+label if label else ''}.json"
    path.parent.mkdir(exist_ok=True)
    path.write_text(json.dumps(result))
    print(f"Tracked {name}: {len(frames)} moving frames",flush=True)
    return result


def clean_wordmark():
    art=Image.new("RGBA",(512,192),(0,0,0,0))
    draw=ImageDraw.Draw(art)
    font=ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf",96)
    sub=ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf",34)
    text="LUXODD"
    draw.text((256,67),text,font=font,fill=(231,233,231,255),anchor="mm")
    left=256-draw.textlength(text,font=font)/2
    cx=left+draw.textlength("LUX",font=font)+draw.textlength("O",font=font)/2
    draw.ellipse((cx-3,67-3,cx+3,67+3),fill=(231,233,231,255))
    draw.text((256,135),"G A M E S",font=sub,fill=(222,226,223,255),anchor="mm")
    return np.array(art)


WORDMARK=clean_wordmark()


def replace_brand(frame,quad):
    # Remove only the generated ink inside a tracked label, preserving the
    # black cabinet's lighting. The replacement lettering is typeset cleanly.
    canonical=np.float32([[0,0],[511,0],[511,191],[0,191]])
    base_to_frame=cv2.getPerspectiveTransform(canonical,quad.astype(np.float32))
    extended=warp_points(np.float32([[-64,-48],[575,-48],[575,239],[-64,239]]),base_to_frame)
    x0,y0,x1,y1=quad_bounds(extended,frame)
    if x1<=x0 or y1<=y0:
        return frame
    rect=np.float32([[0,0],[639,0],[639,287],[0,287]])
    matrix=cv2.getPerspectiveTransform(extended.astype(np.float32),rect)
    patch=cv2.warpPerspective(frame,matrix,(640,288))
    small=cv2.resize(patch,(240,108),interpolation=cv2.INTER_AREA)
    gray=cv2.cvtColor(small,cv2.COLOR_BGR2GRAY)
    local_background=cv2.GaussianBlur(gray.astype(np.float32),(0,0),5)
    ink=((gray.astype(np.float32)-local_background>8)&(gray>32)).astype(np.uint8)*255
    ink=cv2.dilate(ink,np.ones((7,7),np.uint8))
    clean=cv2.inpaint(small,ink,3,cv2.INPAINT_TELEA)
    clean=cv2.resize(clean,(640,288),interpolation=cv2.INTER_LINEAR)
    painted=cv2.resize(ink,(640,288),interpolation=cv2.INTER_LINEAR).astype(np.float32)/255
    alpha=WORDMARK[:,:,3:4].astype(np.float32)/255
    rgb=WORDMARK[:,:,:3][:,:,::-1]
    clean[48:240,64:576]=(clean[48:240,64:576]*(1-alpha)+rgb*alpha).astype(np.uint8)
    painted[48:240,64:576]=np.maximum(painted[48:240,64:576],alpha[:,:,0])
    patch=clean
    local=extended-np.array([x0,y0],np.float32)
    original=frame[y0:y1,x0:x1]
    inverse=cv2.getPerspectiveTransform(rect,local.astype(np.float32))
    output=cv2.warpPerspective(patch,inverse,(x1-x0,y1-y0))
    # Composite only the old/new ink, keeping the cabinet's original texture
    # and reflections everywhere else. Never lay a dark rectangle over it.
    mask=cv2.warpPerspective(painted,inverse,(x1-x0,y1-y0))[:,:,None]
    frame[y0:y1,x0:x1]=(output*mask+original*(1-mask)).astype(np.uint8)
    return frame


def quad_bounds(quad,frame):
    minimum=np.floor(quad.min(axis=0)-4).astype(int)
    maximum=np.ceil(quad.max(axis=0)+5).astype(int)
    return (max(0,minimum[0]),max(0,minimum[1]),
            min(frame.shape[1],maximum[0]),min(frame.shape[0],maximum[1]))


class InsertReader:
    def __init__(self):
        self.selector=cv2.VideoCapture(str(ART/"inserts/three-game-selector.mp4"))
        self.last_selector=-1
        self.cached_selector=None
        self.last_game=-1
        self.cached_game=None

    def at(self,name,time):
        if name in ("01-walk","02-approach","03-select"):
            number=min(149,round(time*30))
            if number!=self.last_selector:
                self.selector.set(cv2.CAP_PROP_POS_FRAMES,number)
                ok,self.cached_selector=self.selector.read()
                if not ok:
                    raise RuntimeError("Selector insert could not be read")
                self.last_selector=number
            return self.cached_selector
        if name=="04-play":
            # The control insert releases at local 4.5 s. Match the actual
            # game's charge/release to that gesture, then show the launch.
            if time<92/FPS:
                seconds=time/(92/FPS)*3
            elif time<4.5:
                seconds=3+(time-92/FPS)/(4.5-92/FPS)*(4.067-3)
            else:
                seconds=4.067+(time-4.5)/1.5*(5.2-4.067)
        elif name=="06-win":
            seconds=5.2+time*1.18
        elif name=="07-celebrate":
            # Keep the real results visible throughout the performance. The
            # recorded game automatically enters Level 2 at source 11.8 s.
            seconds=11.05+time*.055
        else:
            # Continue within the actual victory frames, ending before Next.
            seconds=11.38+time*.055
        number=min(479,round(seconds*30))
        if number!=self.last_game:
            self.cached_game=cv2.imread(str(ART/"gameplay/frames"/f"{number:04d}.jpg"))
            if self.cached_game is None:
                raise RuntimeError(f"Missing real gameplay frame {number}")
            self.last_game=number
        return self.cached_game


def insert_screen(frame,content,quad,preserve_hand=False):
    height,width=frame.shape[:2]
    sh,sw=content.shape[:2]
    src=np.float32([[0,0],[sw-1,0],[sw-1,sh-1],[0,sh-1]])
    # Extend 0.3 px into the LCD edge so the generated image cannot show through.
    center=quad.mean(axis=0)
    dst=quad+(quad-center)*.0015
    x0,y0,x1,y1=quad_bounds(dst,frame)
    if x1<=x0 or y1<=y0:
        return frame
    dst-=np.array([x0,y0],np.float32)
    original=frame[y0:y1,x0:x1]
    transform=cv2.getPerspectiveTransform(src,dst.astype(np.float32))
    image=cv2.warpPerspective(content,transform,(x1-x0,y1-y0),flags=cv2.INTER_LINEAR)
    mask=mask_for(dst,original.shape).astype(np.float32)/255
    mask=cv2.GaussianBlur(mask,(3,3),.5)[:,:,None]
    if preserve_hand:
        hsv=cv2.cvtColor(original,cv2.COLOR_BGR2HSV)
        skin=((hsv[:,:,0]<26)&(hsv[:,:,1]>42)&(hsv[:,:,1]<210)&(hsv[:,:,2]>78)).astype(np.uint8)
        skin=cv2.morphologyEx(skin,cv2.MORPH_CLOSE,np.ones((3,3),np.uint8))
        count,labels,stats,centers=cv2.connectedComponentsWithStats(skin,8)
        inside=mask[:,:,0]>.65
        occlusion=np.zeros(original.shape[:2],np.float32)
        for component in range(1,count):
            if stats[component,cv2.CC_STAT_AREA]<20:
                continue
            pixels=labels==component
            # Only a skin region crossing the LCD bezel can be the boy's hand.
            # Isolated warm areas inside the generated game are replaced.
            left_edge=(dst[0,0]+dst[3,0])*.5
            if (centers[component,0]<left_edge+(dst[:,0].max()-dst[:,0].min())*.32
                    and np.count_nonzero(pixels&inside)>3 and np.count_nonzero(pixels&~inside)>6):
                occlusion[pixels]=1
        occlusion=cv2.GaussianBlur(cv2.dilate(occlusion,np.ones((3,3),np.uint8)),(3,3),.5)
        mask*=1-occlusion[:,:,None]
    # A restrained highlight creates screen glass without hiding real gameplay.
    reflection=(1.016+(.98-1.016)*np.arange(y0,y1,dtype=np.float32)/height)[:,None,None]
    image=np.clip(image.astype(np.float32)*reflection,0,255)
    frame[y0:y1,x0:x1]=(image*mask+original.astype(np.float32)*(1-mask)).astype(np.uint8)
    return frame


def composite(name,width,source=None,track_only=False):
    source=Path(source) if source else ART/"motion"/f"{name}.mp4"
    duration,anchor,corners=SCENES[name]
    height=width*9//16
    folder=ART/f"composited-{height}p"
    target=folder/f"{name}.mp4"
    receipt=ART/"render-receipts"/f"{name}-{height}p.json"
    dependencies=[Path(__file__),source,ART/"inserts/three-game-selector.mp4",
                  ART/"gameplay/frames/0000.jpg",ART/"gameplay/frames/0330.jpg"]
    if name=="04-play":
        dependencies.append(ART/"motion/03-select.mp4")
    fingerprint=hashlib.sha256((str(width)+"|"+"|".join(
        f"{p}:{p.stat().st_size}:{p.stat().st_mtime_ns}" for p in dependencies)).encode()).hexdigest()
    if not track_only and receipt.exists() and target.exists():
        previous=json.loads(receipt.read_text())
        if previous.get("fingerprint")==fingerprint:
            check=cv2.VideoCapture(str(target))
            valid=(int(check.get(cv2.CAP_PROP_FRAME_COUNT))==duration*FPS
                   and int(check.get(cv2.CAP_PROP_FRAME_WIDTH))==width
                   and int(check.get(cv2.CAP_PROP_FRAME_HEIGHT))==height)
            check.release()
            if valid:
                print(f"Reusing verified current composite: {target}",flush=True)
                return
    path=ART/"tracking"/f"{name}.json"
    if corners is not None:
        tracking=json.loads(path.read_text()) if path.exists() else None
        if (tracking is None or Path(tracking["source"])!=source
                or tracking.get("calibration")!=[list(p) for p in corners]):
            tracking=track_screen(name,source)
    else:
        tracking=None
    brands=[]
    for label,calibration in BRAND_CORNERS.get(name,{}).items():
        brand_path=ART/"tracking"/f"{name}-{label}.json"
        brand=json.loads(brand_path.read_text()) if brand_path.exists() else None
        if (brand is None or Path(brand["source"])!=source
                or brand.get("calibration")!=[list(p) for p in calibration]
                or brand.get("version")!=2):
            brand=track_screen(name,source,calibration,label)
        brands.append(brand)
    if track_only:
        return
    folder.mkdir(exist_ok=True)
    cap=cv2.VideoCapture(str(source))
    controls_cap=None
    controls_tracking=None
    controls_brands=[]
    if name=="04-play":
        controls_cap=cv2.VideoCapture(str(ART/"motion/03-select.mp4"))
        controls_tracking=json.loads((ART/"tracking/03-select.json").read_text())
        controls_brands=[json.loads((ART/"tracking"/f"03-select-{label}.json").read_text())
                         for label in BRAND_CORNERS["03-select"]]
    expected=int(round(duration*FPS))
    expected=min(expected,int(cap.get(cv2.CAP_PROP_FRAME_COUNT)))
    args=[FFMPEG,"-y","-hide_banner","-loglevel","warning","-f","rawvideo",
          "-pix_fmt","bgr24","-s",f"{width}x{height}","-r",str(FPS),"-i","-",
          "-an","-c:v","libx264","-preset","fast","-crf","17",
          "-pix_fmt","yuv420p","-threads","2","-movflags","+faststart",str(target)]
    proc=subprocess.Popen(args,stdin=subprocess.PIPE)
    reader=InsertReader()
    qa=[]
    try:
        for i in range(expected):
            source_index=i
            active_tracking=tracking
            active_brands=brands
            active_cap=cap
            if name=="03-select":
                # Readable game-card close-up, then the single genuine black
                # button press/release at the beginning of this motion take.
                lead=expected-29
                source_index=i+29 if i<lead else i-lead
                if i in (0,lead):
                    cap.set(cv2.CAP_PROP_POS_FRAMES,source_index)
            elif name=="04-play" and 92<=i<116:
                # One readable hold/release from the matching control take.
                # Cut away before its later, unrelated taps on other buttons.
                source_index=i-92
                active_tracking=controls_tracking
                active_brands=controls_brands
                active_cap=controls_cap
            elif name=="04-play" and i==116:
                cap.set(cv2.CAP_PROP_POS_FRAMES,source_index)
            ok,frame=active_cap.read()
            if not ok:
                raise RuntimeError(f"Unexpected end of shot {name} at {i}")
            native_h,native_w=frame.shape[:2]
            cx=cy=0
            cw=native_w;ch=native_h
            if name=="02-approach":
                # A cabinet-detail cut follows the opening walk-up. Reframe
                # the actual moving take from the screen down to the controls;
                # this also avoids repeating the boy's approach in the edit.
                u=min(1,i/max(1,expected-1));u=u*u*(3-2*u)
                cx=round((410-110*u)*native_w/1024)
                cw=native_w-cx;ch=round(cw*9/16)
                cy=round(55*native_h/576+(native_h-ch-55*native_h/576)*u)
                cy=max(0,min(native_h-ch,cy))
            elif name in ("03-select","06-win") or (name=="04-play" and i>=92):
                show_lcd=(name=="06-win" or (name=="03-select" and i<expected-29)
                          or (name=="04-play" and i>=116))
                if show_lcd:
                    lcd=np.array(active_tracking["corners"][source_index],np.float32)
                    low=lcd.min(axis=0);high=lcd.max(axis=0)
                    cw=round(max(high[0]-low[0]+40,(high[1]-low[1]+30)*16/9))
                    cw=min(cw,native_w);ch=round(cw*9/16)
                    cx=round((high[0]+low[0]-cw)/2)
                    cx=max(0,min(native_w-cw,cx))
                    cy=max(0,min(native_h-ch,round(low[1]-15)))
                else:
                    cx=round(330*native_w/1024);cy=round(307*native_h/576)
                    cw=round(468*native_w/1024);ch=round(cw*9/16)
            frame=cv2.resize(frame[cy:cy+ch,cx:cx+cw],(width,height),interpolation=cv2.INTER_CUBIC)
            def reframed(quad):
                return (quad-np.array([cx,cy],np.float32))*np.array([width/cw,height/ch],np.float32)
            if active_tracking:
                quad=np.array(active_tracking["corners"][source_index],np.float32)
                quad=reframed(quad)
                frame=insert_screen(frame,reader.at(name,i/FPS),quad,
                    preserve_hand=name=="02-approach" and 1.7<i/FPS<3.4)
            for brand in active_brands:
                quad=np.array(brand["corners"][source_index],np.float32)
                quad=reframed(quad)
                frame=replace_brand(frame,quad)
            proc.stdin.write(frame.tobytes())
            if i%FPS==0 or i==expected-1:
                thumb=cv2.resize(frame,(480,270))
                cv2.putText(thumb,f"{name}  {i/FPS:.2f}s",(8,20),
                    cv2.FONT_HERSHEY_SIMPLEX,.45,(255,255,255),1,cv2.LINE_AA)
                qa.append(thumb)
        proc.stdin.close()
        if proc.wait():
            raise RuntimeError(f"ffmpeg failed for {name}")
    except Exception:
        proc.kill()
        raise
    finally:
        cap.release()
        if controls_cap:
            controls_cap.release()
        reader.selector.release()
    sheet=np.zeros((270*((len(qa)+2)//3),1440,3),np.uint8)
    for index,thumb in enumerate(qa):
        y=index//3*270;x=index%3*480
        sheet[y:y+270,x:x+480]=thumb
    qa_dir=ART/"qa"
    qa_dir.mkdir(exist_ok=True)
    cv2.imwrite(str(qa_dir/f"{name}-{height}p.jpg"),sheet)
    receipt.parent.mkdir(exist_ok=True)
    receipt.write_text(json.dumps({"fingerprint":fingerprint,"frames":expected,
        "width":width,"height":height},indent=2))
    print(f"Composited {target}: {expected/FPS:.3f}s",flush=True)


if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("shot",choices=list(SCENES)+["all"])
    parser.add_argument("--width",type=int,default=1920)
    parser.add_argument("--source")
    parser.add_argument("--track-only",action="store_true")
    args=parser.parse_args()
    names=SCENES if args.shot=="all" else [args.shot]
    for name in names:
        composite(name,args.width,args.source,args.track_only)
