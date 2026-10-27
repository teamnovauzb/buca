"""Open with an uninterrupted level 1 -> 30 journey and five ordered pickups."""
import sys, math, json, subprocess
import numpy as np
import cv2
from PIL import Image, ImageDraw
import build_buca_feature_film_v6 as clean

film = clean.film
OUT = film.base.BASE / 'final-horizontal-v7'
OUT.mkdir(exist_ok=True)
FPS, DUR = 30, 51
SEGMENTS = [
    # Previous clean edit start/end -> new start. Used for matching native effects.
    (18.5, 35.5, 0),
    (12.5, 18.5, 17),
    (35.5, 44, 23),
    (44, 46.5, 31.5),
    (46.5, 49.5, 34),
    (2.5, 8.5, 37),
    (8.5, 12.5, 43),
    (49.5, 53.5, 47),
]


def scene(t):
    if t < 17:
        return clean.clean_journey(t)
    if t < 23:
        return film.prior.scene(12.5 + t - 17)
    if t < 31.5:
        return film.prior.scene(25.5 + t - 23)
    if t < 34:
        return film.prior.scene(34 + t - 31.5)
    if t < 37:
        return film.prior.scene(36.5 + t - 34)
    if t < 43:
        return film.prior.scene(2.5 + t - 37)
    if t < 47:
        return film.prior.scene(8.5 + t - 43)
    return film.prior.scene(39.5 + t - 47)


film.scene = scene
film.CUTS = [17, 23, 31.5, 34, 37, 43, 47]
film.OUT = OUT
film.MOVIE = OUT / 'BUCA-World1-to-World2-Horizontal.mp4'
film.DUR = DUR
film.FRAMES = DUR * FPS
film.boundary.cache_clear()


def preview():
    times = [0, 1.7, 2.55, 3.2, 5.55, 6.2, 8.55, 9.2,
             11.55, 12.2, 14.55, 15.2, 16.85, 19.3, 21.2, 25.3,
             29.4, 30.6, 32.3, 35.4, 39.4, 42.4, 45.5, 49.5]
    sheet = Image.new('RGB', (1920, 6 * 298), film.prior.INK)
    draw = ImageDraw.Draw(sheet)
    for i, t in enumerate(times):
        im = film.frame(t)
        im.save(OUT / f'Preview-{t:05.2f}.jpg', quality=95)
        x, y = i % 4 * 480, i // 4 * 298
        sheet.paste(im.resize((480, 270)), (x, y))
        draw.text((x + 8, y + 275), f'{t:.2f}s', font=film.prior.font(17), fill=film.prior.IVORY)
    sheet.save(OUT / 'Storyboard.jpg', quality=95)


def audio(extra_cues=None, music_ducks=None):
    sr, n = 48000, 48000 * DUR
    ff = film.FF
    data = subprocess.check_output([ff, '-v', 'error', '-ss', str(48 * 60 / 101),
        '-i', str(film.R / 'output/buca-reel/assets/Funkorama.mp3'), '-af', 'atempo=1.188118812',
        '-t', str(DUR), '-f', 'f32le', '-ar', str(sr), '-ac', '2', '-'])
    music = np.frombuffer(data, np.float32).reshape(-1, 2).copy()[:n] * .58
    assert len(music) == n
    fx = np.zeros((n, 2), np.float32)
    cues = []
    for item in json.loads((clean.OUT / 'sound-cues.json').read_text()):
        for start, end, dest in SEGMENTS:
            if start <= item['at'] < end:
                mapped = dict(item, at=item['at'] - start + dest)
                if mapped['at'] != 0:
                    cues.append(mapped)
                break
    cues.extend(extra_cues or [])
    cues.append(dict(asset='Celebration/StationWhistle.wav', at=.1, gain=.27, duration=1.8))
    for item in cues:
        data = subprocess.check_output([ff, '-v', 'error', '-i', str(film.R / 'Assets/Audio' / item['asset']),
            '-f', 'f32le', '-ar', str(sr), '-ac', '2', '-'])
        a = np.frombuffer(data, np.float32).reshape(-1, 2).copy()[:round(item['duration'] * sr)]
        fade = min(2400, len(a) // 4)
        a[:fade] *= np.linspace(0, 1, fade)[:, None]
        a[-fade:] *= np.linspace(1, 0, fade)[:, None]
        a *= item['gain'] / max(.1, float(abs(a).max()))
        st = round(item['at'] * sr)
        nn = min(len(a), n - st)
        fx[st:st + nn] += a[:nn]
    env = np.ones(n, np.float32)
    for at, duration, gain in (music_ducks or [(19, 2, .55), (23, 2.7, .68), (29, 2, .65)]):
        env[round(at * sr):round((at + duration) * sr)] = gain
    env = cv2.GaussianBlur(env[::240].reshape(1, -1), (0, 0), sigmaX=14).ravel()
    env = np.interp(np.arange(n), np.arange(len(env)) * 240, env)
    mix = music * env[:, None] + fx
    rng = np.random.default_rng(2026)
    for beat in range(DUR * 2):
        st = round(beat * .5 * sr)
        tt = np.arange(round(.18 * sr)) / sr
        k = np.sin(2 * np.pi * (49 * tt + 2.1 * (1 - np.exp(-tt * 35)))) * np.exp(-tt * 24)
        nn = min(len(k), n - st)
        mix[st:st + nn] += k[:nn, None] * .07
        if beat % 2:
            tt = np.arange(round(.1 * sr)) / sr
            k = rng.normal(size=len(tt)) * np.exp(-tt * 40) * .02
            nn = min(len(k), n - st)
            mix[st:st + nn] += k[:nn, None]
    mix[:960] *= np.linspace(0, 1, 960)[:, None]
    mix[-18000:] *= np.linspace(1, 0, 18000)[:, None]
    subprocess.run([ff, '-y', '-v', 'error', '-f', 'f32le', '-ar', str(sr), '-ac', '2', '-i', '-',
        '-af', 'loudnorm=I=-14:TP=-2:LRA=8', '-ar', str(sr), str(OUT / 'Soundtrack.wav')],
        input=mix.astype('float32').tobytes(), check=True)
    (OUT / 'sound-cues.json').write_text(json.dumps(cues, indent=2))


if __name__ == '__main__':
    preview()
    if '--preview' not in sys.argv:
        audio()
        film.render()
