from pathlib import Path
import subprocess, sys
root=Path(__file__).resolve().parents[2]
folder=root/'output/attract-v2'
assert all((folder/'frames'/f'{i:04d}.jpg').exists() for i in range(360)), 'Capture is not finished'
font='/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf'
filters=["scale=in_range=pc:out_range=tv:out_color_matrix=bt709", "drawbox=x=0:y=0:w=iw:h=ih:color=0xF5EDD9:t=fill:enable='gte(t,9)'" ]
for char,x,color in [('B',687,'0x14395B'),('U',824,'0xD9623B'),('C',961,'0xD6A12C'),('A',1098,'0x23786F')]:
    filters.append(f"drawtext=fontfile='{font}':text='{char}':fontsize=170:fontcolor={color}:x={x}:y=350:enable='gte(t,9)'")
filters.append(f"drawtext=fontfile='{font}':text='YOUR TURN':fontsize=40:fontcolor=0x14395B:x=(w-tw)/2:y=590:enable='gte(t,9)'")
filters.extend(['fade=t=in:st=0:d=0.12','fade=t=out:st=11.6:d=0.4','format=yuv420p'])
subprocess.run([sys.argv[1],'-hide_banner','-loglevel','warning','-y','-framerate','30','-i',str(folder/'frames/%04d.jpg'),'-i',str(folder/'soundtrack.wav'),'-vf',','.join(filters),'-c:v','libx264','-preset','medium','-crf','18','-color_range','tv','-colorspace','bt709','-color_primaries','bt709','-color_trc','bt709','-c:a','aac','-b:a','192k','-t','12','-movflags','+faststart',str(folder/'BUCA-One-Shot-Wonder.mp4')],check=True)
