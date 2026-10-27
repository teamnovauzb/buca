"""Bake matching menu type from the project's existing rounded glyph outlines.

The timer uses original seven-segment outlines. Requires fontTools; no external fonts.
"""
import json
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/TwilightMap/ForestHarbor'

def write_font(name, characters):
    builder = FontBuilder(1000, isTTF=True)
    order = ['.notdef', 'space'] + ['uni%04X' % ord(c) for c in characters]
    builder.setupGlyphOrder(order)
    builder.setupCharacterMap({32: 'space', **{ord(c): 'uni%04X' % ord(c) for c in characters}})
    glyphs = {'.notdef': TTGlyphPen(None).glyph(), 'space': TTGlyphPen(None).glyph()}
    metrics = {'.notdef': (500, 0), 'space': (340, 0)}
    for character, (width, contours) in characters.items():
        pen = TTGlyphPen(None)
        for points in contours:
            pen.moveTo(points[0])
            for point in points[1:]:
                pen.lineTo(point)
            pen.closePath()
        key = 'uni%04X' % ord(character)
        glyphs[key] = pen.glyph()
        metrics[key] = (round(width), min(round(x) for contour in contours for x, y in contour))
    builder.setupGlyf(glyphs)
    builder.setupHorizontalMetrics(metrics)
    builder.setupHorizontalHeader(ascent=1050, descent=-160)
    builder.setupNameTable({'familyName': name, 'styleName': 'Regular', 'uniqueFontIdentifier': name+'-1.0', 'fullName': name, 'psName': name})
    builder.setupOS2(sTypoAscender=1050, sTypoDescender=-160, usWinAscent=1100, usWinDescent=180)
    builder.setupPost()
    builder.setupMaxp()
    builder.save(OUT / (name + '.ttf'))

rounded = {}
for glyph in json.loads((ROOT / 'Assets/Editor/ToyBox/RoundedGlyphContours.json').read_text())['glyphs']:
    rounded[glyph['character']] = (glyph['width']*1000+50, [[(round(p['x']*1000), round(p['y']*1000)) for p in contour['points']] for contour in glyph['contours']])
rounded['/'] = (500, [[(50,-40),(140,-40),(450,1000),(360,1000)]])
write_font('BucaRoundedMenu', rounded)

def horizontal(y):
    return [(95,y),(135,y+35),(405,y+35),(445,y),(405,y-35),(135,y-35)]
def vertical(x, y):
    return [(x,y+155),(x+35,y+115),(x+35,y-115),(x,y-155),(x-35,y-115),(x-35,y+115)]
segments = {'a':horizontal(790),'g':horizontal(405),'d':horizontal(20),
            'f':vertical(75,600),'b':vertical(465,600),'e':vertical(75,210),'c':vertical(465,210)}
patterns = ['abcdef','bc','abged','abgcd','fgbc','afgcd','afgecd','abc','abcdefg','abfgcd']
digital = {str(i):(585,[segments[s] for s in pattern]) for i,pattern in enumerate(patterns)}
write_font('BucaDigitalTimer',digital)
print('Saved matching rounded-menu and seven-segment timer fonts.')
