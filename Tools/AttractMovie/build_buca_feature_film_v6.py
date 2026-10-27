"""Clean horizontal edit: native game UI only, without added captions or credits."""
import sys, shutil
from functools import lru_cache
from PIL import Image
import build_buca_feature_film_v5 as film

OUT = film.base.BASE / 'clean-horizontal-v6'
OUT.mkdir(exist_ok=True)
PICKUPS = film.OUT
previous_raw = film.base.raw


@lru_cache(20)
def raw(kind, index):
    if kind == 'whale':
        return Image.open(OUT / 'capture/whale' / f'{index:04d}.jpg').convert('RGB')
    return previous_raw(kind, index)


def clean_journey(t):
    proximity = 0
    for event in film.EVENTS:
        age = t - event['arrive']
        push = film.prior.ease((age + .55) / .65) * (1 - film.prior.ease((age - 1.25) / .65))
        proximity = max(proximity, push)
    return film.base.follow('travel', t, 1290 - 230 * proximity, (0, -45))


def clean_ending(t):
    return film.base.shot('upgrade', 21.95, width=2560 - 60 * film.prior.ease((t - 42) / 4))


def no_caption(*args, **kwargs):
    pass


film.base.raw = raw
film.base.label = no_caption
film.base.corner = no_caption
film.prior.corner = no_caption
film.base.mosaic_end = clean_ending
film.journey = clean_journey
film.OUT = OUT
film.MOVIE = OUT / 'BUCA-Horizontal-Clean-v6.mp4'

if __name__ == '__main__':
    film.preview()
    if '--preview' not in sys.argv:
        shutil.copyfile(PICKUPS / 'Soundtrack.wav', OUT / 'Soundtrack.wav')
        shutil.copyfile(PICKUPS / 'sound-cues.json', OUT / 'sound-cues.json')
        film.render()
