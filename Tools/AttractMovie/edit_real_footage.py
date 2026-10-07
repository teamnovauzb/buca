"""Local-only BUCA promo edit using the user's genuine cabinet recordings."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/buca-real-footage'
FF = '/private/tmp/buca-video-tools/imageio_ffmpeg/binaries/ffmpeg-macos-aarch64-v7.1'
FONT = '/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf'
SOURCE = Path('/Users/nurxayot/Downloads')
MUSIC = ROOT / 'output/buca-mall-story/fun-music.mp3'
CHEER = ROOT / 'Assets/Audio/Celebration/HappyYeah.wav'
OUT.mkdir(exist_ok=True)

def run(args):
    subprocess.run([FF, '-hide_banner', '-loglevel', 'error', '-nostdin', '-y', *args], check=True)

# Only genuine camera footage. Omit footage of timeouts, close inspection of
# blurry labels, and unrelated dialogs. The footage predates the latest game UI.
shots = [
    ('PXL_20260923_181302459.mp4', 2.0, 4.5),
    ('PXL_20260923_181336411.mp4', 130.0, 6.0),
    ('PXL_20260923_181336411.mp4', 53.0, 7.0),
    ('PXL_20260923_181336411.mp4', 73.5, 7.5),
    ('PXL_20260923_181336411.mp4', 93.0, 6.0),
    ('PXL_20260923_181336411.mp4', 100.0, 6.0),
    ('PXL_20260923_181302459.mp4', 2.5, 3.0),
]
for i, (name, start, duration) in enumerate(shots):
    dest = OUT / f'cut-{i:02}.mp4'
    if '--replace-second-cut' in sys.argv and i != 1 and dest.exists():
        continue
    run(['-ss', str(start), '-i', str(SOURCE/name), '-t', str(duration),
         '-an', '-map_metadata', '-1', '-vf',
         'fps=30,scale=1080:1920:flags=lanczos,setsar=1,eq=brightness=0.015:saturation=1.045:gamma=1.025,unsharp=5:5:0.22:5:5:0',
         '-c:v', 'libx264', '-preset', 'fast', '-crf', '19', '-pix_fmt', 'yuv420p', str(dest)])
    print(f'Cut {i+1}/{len(shots)} ready', flush=True)

listing = OUT/'cuts.txt'
listing.write_text(''.join(f"file 'cut-{i:02}.mp4'\n" for i in range(len(shots))))
run(['-f', 'concat', '-safe', '0', '-i', str(listing), '-an', '-c', 'copy', '-map_metadata', '-1', str(OUT/'picture.mp4')])

def title(text, size, x, y, color='0xFFF1D5', extra=''):
    return f"drawtext=fontfile='{FONT}':text='{text}':fontsize={size}:fontcolor={color}:x={x}:y={y}{extra}"

# Center the complete portrait recording, preserving the controls and screen.
# Soft moving side fill avoids stretching or cropping away real gameplay.
graph = (
    '[0:v]split=2[bg][fg];'
    '[bg]scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,'
    'scale=480:270,boxblur=20:2,scale=1920:1080,eq=brightness=-0.20:saturation=0.5[soft];'
    '[fg]scale=608:1080:flags=lanczos[portrait];'
    '[soft][portrait]overlay=656:0,'
    'drawbox=x=650:y=0:w=3:h=1080:color=0xDFBC77@0.75:t=fill,'
    'drawbox=x=1267:y=0:w=3:h=1080:color=0xDFBC77@0.75:t=fill,'
    + title('BUCA', 116, '(650-text_w)/2', 340) + ','
    + title('SMALL PUCKS.', 29, '(650-text_w)/2', 500) + ','
    + title('BIG FUN.', 42, '(650-text_w)/2', 550, '0xF4C563') + ','
    + title('LUXODD ARCADE', 27, '(650-text_w)/2', 940) + ','
    + title('AIM.', 80, '1270+(650-text_w)/2', 350, '0x9AD9CA', ":enable='lt(t,10.5)'") + ','
    + title('SHOOT.', 80, '1270+(650-text_w)/2', 350, '0xF4C563', ":enable='between(t,10.5,30.999)'") + ','
    + title('WIN!', 80, '1270+(650-text_w)/2', 350, '0xF4C563', ":enable='between(t,31,36.999)'") + ','
    + title('YOUR', 74, '1270+(650-text_w)/2', 340, '0xFFF1D5', ":enable='gte(t,37)'") + ','
    + title('TURN!', 86, '1270+(650-text_w)/2', 445, '0xF4C563', ":enable='gte(t,37)'") + ','
    'fade=t=in:st=0:d=0.4,fade=t=out:st=39.55:d=0.45[v];'
    '[1:a]atrim=0:40,asetpts=PTS-STARTPTS,volume=0.85,'
    "volume=0.55:enable='between(t,35.9,38.1)',afade=t=in:st=0:d=0.3,afade=t=out:st=38.9:d=1.1[music];"
    '[2:a]volume=0.8,adelay=36000|36000[cheer];'
    '[music][cheer]amix=inputs=2:duration=first:normalize=0,alimiter=limit=0.94[a]'
)
run(['-i', str(OUT/'picture.mp4'), '-i', str(MUSIC), '-i', str(CHEER),
     '-filter_complex', graph, '-map', '[v]', '-map', '[a]', '-t', '40',
     '-map_metadata', '-1', '-c:v', 'libx264', '-preset', 'fast', '-crf', '19',
     '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart',
     str(OUT/'BUCA-Real-Play-Preview-1080p.mp4')])
print('1080p preview ready', flush=True)
run(['-i', str(OUT/'BUCA-Real-Play-Preview-1080p.mp4'), '-vf', 'scale=3840:2160:flags=lanczos',
     '-map_metadata', '-1', '-c:v', 'libx264', '-preset', 'fast', '-crf', '20',
     '-pix_fmt', 'yuv420p', '-c:a', 'copy', '-movflags', '+faststart',
     str(OUT/'BUCA-Real-Play-Preview-4K-Upscaled.mp4')])
print('4K delivery file ready (upscaled from phone footage)', flush=True)
