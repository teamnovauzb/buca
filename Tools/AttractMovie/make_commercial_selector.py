"""Render a five-second three-game selector insert using the supplied artwork.

This is a screen-compositing asset, not the completed commercial.
"""
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "output/buca-mall-film/inserts"
OUT.mkdir(parents=True, exist_ok=True)
FF = "/private/tmp/buca-video-tools/imageio_ffmpeg/binaries/ffmpeg-macos-aarch64-v7.1"
FONT = "/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf"
refs = [
    "/Users/nurxayot/Desktop/Screenshot 2026-09-27 at 16.58.27.png",
    str(ROOT / "output/buca-mall-film/references/buca-icon.png"),
    "/Users/nurxayot/Desktop/Screenshot 2026-09-27 at 16.58.44.png",
]

# The first artwork's lower half contains prices/prize captions; they are not
# part of the card. Text and outlines are authored here so they stay sharp.
graph = [
    "color=c=0x081b25:s=1920x1080:r=30:d=5[base]",
    "[0:v]crop=537:520:25:15,scale=470:455,pad=500:550:15:35:color=0x071725,setsar=1[tower]",
    # The user-supplied BUCA icon ends above its storefront Free label.
    "[1:v]crop=514:700:0:44,scale=404:550,pad=500:550:48:0:color=0x071725,setsar=1[buca]",
    "[2:v]crop=538:740:42:24,scale=400:550,pad=500:550:50:0:color=0x071725,setsar=1[para]",
    "[base][tower]overlay=130:260[a]",
    "[a][buca]overlay=710:260[b]",
    "[b][para]overlay=1290:260[c]",
    "[c]drawbox=x=706:y=256:w=508:h=644:color=0xf4ce76:t=5,"
    "drawbox=x=130:y=810:w=500:h=90:color=0x123340:t=fill,"
    "drawbox=x=710:y=810:w=500:h=90:color=0xf4ce76:t=fill,"
    "drawbox=x=1290:y=810:w=500:h=90:color=0x123340:t=fill,"
    f"drawtext=fontfile='{FONT}':text='LUXODD':fontcolor=white:fontsize=72:x=(w-tw)/2:y=70,"
    f"drawtext=fontfile='{FONT}':text='GAMES':fontcolor=0x8dbbbf:fontsize=25:x=(w-tw)/2:y=158,"
    f"drawtext=fontfile='{FONT}':text='TOWER RUSH':fontcolor=white:fontsize=40:x=380-tw/2:y=834,"
    f"drawtext=fontfile='{FONT}':text='BUCA':fontcolor=0x09242f:fontsize=44:x=960-tw/2:y=832,"
    f"drawtext=fontfile='{FONT}':text='PARABOX':fontcolor=white:fontsize=40:x=1540-tw/2:y=834,"
    f"drawtext=fontfile='{FONT}':text='LET’S PLAY':fontcolor=0xf4ce76:fontsize=26:x=(w-tw)/2:y=955,"
    "format=yuv420p[out]",
]
cmd=[FF,"-y","-loglevel","error"]
for ref in refs:
    cmd += ["-loop","1","-i",ref]
cmd += ["-filter_complex",";".join(graph),"-map","[out]","-t","5","-r","30",
        "-c:v","libx264","-preset","fast","-crf","16","-map_metadata","-1",
        "-movflags","+faststart",str(OUT/"three-game-selector.mp4")]
subprocess.run(cmd,check=True)
print(OUT/"three-game-selector.mp4")
