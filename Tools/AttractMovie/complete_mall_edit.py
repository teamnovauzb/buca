"""Continue the current render into composited deliverables as each shot finishes.

This is a finite production job, not a scheduled task. The final films still
require visual review before they are presented as approved deliverables.
"""
import json
from pathlib import Path
import subprocess
import sys
import time

import cv2

from composite_mall_film import ART,ROOT,SCENES


def composite(name,width):
    subprocess.run([sys.executable,str(ROOT/"Tools/AttractMovie/composite_mall_film.py"),
        name,"--width",str(width)],check=True)


def finish(height):
    subprocess.run([sys.executable,str(ROOT/"Tools/AttractMovie/finish_mall_film.py"),
        "--height",str(height)],check=True)


def ready(path,duration):
    if not path.exists():
        return False
    cap=cv2.VideoCapture(str(path))
    count=cap.get(cv2.CAP_PROP_FRAME_COUNT)
    fps=cap.get(cv2.CAP_PROP_FPS)
    cap.release()
    return abs(fps-24)<.01 and count>=duration*24


def write_status(state,shot=None):
    (ART/"edit-status.json").write_text(json.dumps({"state":state,"shot":shot,
        "updated_at":time.time(),"visual_review":"pending"},indent=2))


for name,(duration,anchor,corners) in SCENES.items():
    source=ART/"motion"/f"{name}.mp4"
    print(f"Waiting for complete moving shot: {name}",flush=True)
    write_status("waiting_for_motion",name)
    while True:
        state=json.loads((ART/"render-status.json").read_text())
        if state["state"]=="failed":
            write_status("motion_render_failed",state.get("shot"))
            raise RuntimeError(f"Motion renderer failed: {state}")
        writer_finished=(state.get("shot")!=name or state["state"]=="complete")
        if writer_finished and ready(source,duration):
            break
        time.sleep(15)
    # Give the writer its brief final flush; the GPU can continue the next shot.
    time.sleep(2)
    write_status("compositing_1080p",name)
    composite(name,1920)

write_status("assembling_preview")
subprocess.run([sys.executable,str(ROOT/"Tools/AttractMovie/finish_mall_film.py"),
    "--audio-only"],check=True)
finish(1080)
for name in SCENES:
    write_status("compositing_4k",name)
    composite(name,3840)
write_status("assembling_4k")
finish(2160)
write_status("rendered_awaiting_visual_review")
print("Both complete 40-second films rendered. Human visual review remains required.",flush=True)
