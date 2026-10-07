"""Wait for selected local models, then run a single reproducible video job."""
from pathlib import Path
import json
import os
import shutil
import subprocess
import sys
import time

ROOT=Path(__file__).resolve().parents[2]
LOCAL=Path("/private/tmp/buca-local-video")
ART=ROOT/"output/buca-mall-film"
job=json.loads(Path(sys.argv[1]).read_text())
if job.get("reuse_motion"):
    source=ART/job["reuse_motion"]
    out=ART/job["output"]
    out.parent.mkdir(parents=True,exist_ok=True)
    if not source.exists():
        raise FileNotFoundError(f"Moving cabinet plate is not ready: {source}")
    shutil.copy2(source,out)
    print("Using the existing moving cabinet plate for the real-game winning close-up.",flush=True)
    sys.exit(0)
needed=[LOCAL/"models/ltx"/name for name in [
    "connector.safetensors","transformer-distilled-1.1.safetensors",
    "vae_encoder.safetensors","vae_decoder.safetensors",
    "spatial_upscaler_x2_v1_1.safetensors","split_model.json",
]]+[LOCAL/"models/gemma"/name for name in [
    "model-00001-of-00002.safetensors","model-00002-of-00002.safetensors",
    "model.safetensors.index.json","tokenizer.json","tokenizer_config.json",
]]
while any(not p.exists() for p in needed):
    missing=[p.name for p in needed if not p.exists()]
    print("Waiting for model files: "+", ".join(missing),flush=True)
    time.sleep(30)
out=ART/job["output"]
out.parent.mkdir(parents=True,exist_ok=True)
env=os.environ.copy()
env["PATH"]=str(LOCAL/"bin")+os.pathsep+env.get("PATH","")
env["HF_HUB_OFFLINE"]="1"
env["PYTHONUNBUFFERED"]="1"
args=[str(LOCAL/"venv/bin/python"),str(ROOT/"Tools/AttractMovie/generate_mall_motion.py"),
      "generate","--distilled","--low-ram",
      "--model",str(LOCAL/"models/ltx"),"--gemma",str(LOCAL/"models/gemma"),
      "--prompt",job["prompt"],"--image",str(ART/job["anchor"]),
      "--width",str(job["width"]),"--height",str(job["height"]),
      "--frames",str(job["frames"]),"--frame-rate",str(job["frame_rate"]),
      "--seed",str(job["seed"]),"--output",str(out)]
if not job.get("audio",False):
    args += ["--no-audio"]
if job.get("stage2_steps"):
    args += ["--stage2-steps",str(job["stage2_steps"])]
if job.get("end_anchor"):
    args += ["--image",str(ART/job["end_anchor"]),str(job["frames"]-1),"1.0"]
print("Models ready. Beginning actual video generation.",flush=True)
result=subprocess.run(args,env=env)
print("Video generation exit code:",result.returncode,flush=True)
if result.returncode==0 and job["frame_rate"]!=24:
    raw=ART/"raw-motion"/f"{job['name']}-{job['frame_rate']}fps.mp4"
    raw.parent.mkdir(exist_ok=True)
    out.replace(raw)
    print("Interpolating actual movement to the 24 fps edit rate.",flush=True)
    result=subprocess.run([str(LOCAL/"bin/ffmpeg"),"-y","-hide_banner","-loglevel","warning",
        "-i",str(raw),"-vf",
        "tpad=stop_mode=clone:stop_duration=0.25,minterpolate=fps=24:mi_mode=mci:mc_mode=aobmc:me_mode=bidir:vsbmc=1",
        "-t",str(job["duration"]),"-c:v","libx264","-preset","fast","-crf","16",
        "-pix_fmt","yuv420p","-threads","2","-c:a","aac","-b:a","192k",
        "-movflags","+faststart",str(out)],env=env)
sys.exit(result.returncode)
