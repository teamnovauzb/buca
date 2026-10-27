"""Prepare motion-edit states from the user's actual Luxodd screenshots.
Only local video assets are composed; no website content is modified.
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'output/buca-luxodd-attract/revision-2';DESK=Path('/Users/nurxayot/Desktop')
def screen(name):
 im=Image.open(DESK/name).convert('RGB');scale=im.width/1952
 return im.crop((0,round(206*scale),im.width,round(1269*scale))).resize((1952,1063),Image.Resampling.LANCZOS)
a=screen('Screenshot 2026-10-11 at 20.09.56.png');b=screen('Screenshot 2026-10-11 at 20.10.02.png')
# Remove only the macOS screenshot-preview thumbnail using the identical background.
b.paste(a.crop((1710,790,1952,980)),(1710,790));c=b.copy()
c.paste(a.crop((440,327,748,799)),(440,327))
raw=Image.open(DESK/'Screenshot 2026-10-11 at 20.16.51.png').convert('RGB');s=raw.width/1072
patch=raw.crop(tuple(round(v*s) for v in (48,124,858,1246))).resize((536,742),Image.Resampling.LANCZOS);c.paste(patch,(697,280))
para=b.crop((791,285,1141,780)).resize((280,422),Image.Resampling.LANCZOS).filter(ImageFilter.GaussianBlur(4));para=Image.blend(para,Image.new('RGB',para.size,'#10251a'),.28)
mask=Image.new('L',para.size);ImageDraw.Draw(mask).rounded_rectangle((0,0,279,421),radius=12,fill=255);mask=mask.filter(ImageFilter.GaussianBlur(2));c.paste(para,(455,337),mask)
# Depth of field keeps the product card prominent and avoids emphasizing stale description copy.
for name,im in [('tower',a),('parabox',b),('buca',c)]:
 region=(713,917,1225,1023);patch=im.crop(region).filter(ImageFilter.GaussianBlur(5));im.paste(patch,region[:2]);im.save(OUT/f'platform-{name}.png')
