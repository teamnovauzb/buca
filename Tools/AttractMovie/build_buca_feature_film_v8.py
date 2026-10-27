"""Clear fifth-level pickups: stop, come from the marked box, board, continue."""
import sys, json
from functools import lru_cache
import numpy as np
from PIL import Image, ImageDraw
import build_buca_feature_film_v7 as previous

film = previous.film
OUT = film.base.BASE / 'milestone-pickups-v8'
CAP = OUT / 'capture'
DATA = json.loads((CAP / 'pickup-events.json').read_text())
EVENTS = DATA['pickups']
DURATION = DATA['duration']
FRAMES = round(DURATION * 30)
SAMPLES = np.array([0, 2.9, 3.2, 4, 5.1, 7.7, 8, 8.8, 9.9,
                    12.5, 12.8, 13.6, 14.7, 17.3, 17.6, 18.4, 19.5,
                    22.1, 22.4, 23.2, 24.3, 27.95])
original_raw, original_tracking = film.base.raw, film.base.tracking


@lru_cache(20)
def raw(kind, index):
    if kind != 'travel':
        return original_raw(kind, index)
    path = CAP / 'travel' / f'{index:04d}.jpg'
    if not path.exists() and '--preview' in sys.argv:
        sample = int(np.argmin(abs(SAMPLES - index / 30)))
        path = CAP / 'travel' / f'sample-{sample:04d}.jpg'
    return Image.open(path).convert('RGB')


@lru_cache(None)
def tracking(kind):
    if kind != 'travel':
        return original_tracking(kind)
    path = CAP / 'travel.csv'
    sampled = not path.exists()
    if sampled:
        path = CAP / 'travel-samples.csv'
    rows = np.loadtxt(path, delimiter=',').reshape(-1, 3)
    unique = {int(row[0]): row[1:] for row in rows}
    keys = np.array(sorted(unique))
    indices = SAMPLES[keys] * 30 if sampled else keys
    return indices, np.array([unique[key] for key in keys])


def journey(t):
    close = 0
    for event in EVENTS:
        age = t - event['arrive']
        close = max(close, film.prior.ease((age + .6) / .8) *
                    (1 - film.prior.ease((age - 1.9) / .7)))
    return film.base.follow('travel', t, 1390 - 190 * close, (0, -20))


def scene(t):
    if t < DURATION:
        return journey(t)
    return previous.scene(t - (DURATION - 17))


film.base.raw = raw
film.base.tracking = tracking
film.base.COUNTS['travel'] = FRAMES
film.scene = scene
film.CUTS = [28, 34, 42.5, 45, 48, 54, 58]
film.OUT = OUT
film.MOVIE = OUT / 'BUCA-Every-Five-Levels-Horizontal.mp4'
film.DUR = previous.DUR = 62
film.FRAMES = 1860
film.boundary.cache_clear()
previous.OUT = OUT
previous.SEGMENTS = [
    (12.5, 18.5, 28), (35.5, 44, 34), (44, 46.5, 42.5),
    (46.5, 49.5, 45), (2.5, 8.5, 48), (8.5, 12.5, 54), (49.5, 53.5, 58)]


def audio():
    cues = []
    depart = .6
    for event in EVENTS:
        cues.append(dict(asset='Celebration/StationMovingTrain.wav', at=depart,
                         gain=.16, duration=event['arrive'] - depart))
        cues.append(dict(asset='Player/PickupCollect.ogg', at=event['land'], gain=.27, duration=.8))
        cues.append(dict(asset='UI/ScoreCountChime.wav', at=event['land'] + .05, gain=.15, duration=.8))
        depart = event['arrive'] + DATA['pause']
    cues.append(dict(asset='Celebration/StationMovingTrain.wav', at=depart,
                     gain=.16, duration=DURATION - .3 - depart))
    previous.audio(cues, [(30, 2, .55), (34, 2.7, .68), (40, 2, .65)])


def preview():
    times = [0, 2.9, 3.2, 4, 5.1, 7.7, 8.8, 9.9, 12.5, 13.6, 14.7, 17.3,
             18.4, 19.5, 22.1, 22.4, 23.2, 24.3, 27.95, 31.4, 36.3, 41.6, 43.3, 60.5]
    sheet = Image.new('RGB', (1920, 6 * 298), film.prior.INK)
    draw = ImageDraw.Draw(sheet)
    for i, t in enumerate(times):
        im = film.frame(t)
        im.save(OUT / f'Preview-{t:05.2f}.jpg', quality=95)
        x, y = i % 4 * 480, i // 4 * 298
        sheet.paste(im.resize((480, 270)), (x, y))
        draw.text((x + 8, y + 275), f'{t:.2f}s', font=film.prior.font(17), fill=film.prior.IVORY)
    sheet.save(OUT / 'Storyboard.jpg', quality=95)


if __name__ == '__main__':
    if '--audio' in sys.argv:
        audio()
    elif '--preview' in sys.argv:
        preview()
    else:
        assert len(list((CAP / 'travel').glob('[0-9][0-9][0-9][0-9].jpg'))) == FRAMES
        preview()
        audio()
        film.render()
