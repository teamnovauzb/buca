"""Render the eight actual-motion shots sequentially, never in parallel on M2."""
import json
from pathlib import Path
import subprocess
import sys
import time

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/"output/buca-mall-film"
LOGS=ART/"logs"
LOGS.mkdir(exist_ok=True)
for file in sorted((ART/"jobs").glob("*.json")):
    job=json.loads(file.read_text())
    target=ART/job["output"]
    if target.exists() and target.stat().st_size>2000:
        print("Already rendered:",job["name"],flush=True)
        continue
    start=time.time()
    status={"shot":job["name"],"state":"rendering","started_at":start}
    (ART/"render-status.json").write_text(json.dumps(status,indent=2))
    print("Rendering",job["name"],"for",job["duration"],"seconds.",flush=True)
    with (LOGS/(job["name"]+".log")).open("w") as log:
        result=subprocess.run([sys.executable,str(ROOT/"Tools/AttractMovie/run_mall_motion_job.py"),str(file)],stdout=log,stderr=subprocess.STDOUT)
    status.update(state="complete" if result.returncode==0 else "failed",elapsed_seconds=round(time.time()-start),exit_code=result.returncode)
    (ART/"render-status.json").write_text(json.dumps(status,indent=2))
    print(json.dumps(status),flush=True)
    if result.returncode:
        sys.exit(result.returncode)
print("ALL EIGHT MOTION SHOTS RENDERED; COMPOSITING AND FINAL QA STILL REQUIRED.",flush=True)
