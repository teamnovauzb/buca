"""Horizontal film revision: five visible World 1 pickups on the native map.

Retains v4's clean skin scenes, ticket-free upgrade, and refined typography.
The editor capture is isolated: no game progress or animal saves are changed.
"""
from pathlib import Path
from functools import lru_cache
import math, json, sys, subprocess
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import build_buca_feature_film_v4 as prior

base = prior.base
R = base.R
OUT = base.BASE / 'animal-pickups-v5'
CAP = OUT / 'capture'
W, H, FPS = 1920, 1080, 30
SIZE = W, H
DUR = 53.5
FRAMES = round(DUR * FPS)
FF = prior.FF
MOVIE = OUT / 'BUCA-Horizontal-Animal-Pickups-v5.mp4'
EVENTS = json.loads((CAP / 'pickup-events.json').read_text())['pickups']
prior_raw, prior_tracking, prior_shot = base.raw, base.tracking, base.shot


@lru_cache(20)
def raw(kind, index):
    if kind != 'travel':
        return prior_raw(kind, index)
    return Image.open(CAP / 'travel' / f'{index:04d}.jpg').convert('RGB')


@lru_cache(None)
def tracking(kind):
    if kind != 'travel':
        return prior_tracking(kind)
    rows = np.loadtxt(CAP / 'travel.csv', delimiter=',').reshape(-1, 3)
    unique = {int(row[0]): row[1:] for row in rows}
    indices = np.array(sorted(unique))
    return indices, np.array([unique[i] for i in indices])


def shot(kind, t, cx=None, cy=None, width=None):
    if kind != 'travel':
        return prior_shot(kind, t, cx, cy, width)
    # Crop the map itself, keeping the native controls outside the cinematic view.
    im = base.src(kind, t).crop((154, 86, 2406, 1110))
    cx = im.width / 2 if cx is None else cx - 154
    cy = im.height / 2 if cy is None else cy - 86
    width = min(im.height * W / H, im.width if width is None else width)
    return base.framecrop(im, cx, cy, width)


base.raw = raw
base.tracking = tracking
base.shot = shot
base.COUNTS['travel'] = 510


def pickup_title(im, event, age):
    alpha = prior.ease((age + .15) / .28) * (1 - prior.ease((age - 1.5) / .35))
    if alpha <= 0:
        return
    wash = prior.wash(230, 180).copy()
    wash.putalpha(wash.getchannel('A').point(lambda a: int(a * alpha)))
    im.paste(wash, (0, 0), wash)
    prior.text(im, f"LEVEL {event['level']:02}", 960, 52, 24,
               prior.GOLD, alpha, 5, 'medium')
    prior.text(im, f"{event['animal'].title()} joins the ride", 960,
               111 + 12 * (1 - prior.ease(age / .35)), 55,
               prior.IVORY, alpha)
    fx = Image.new('RGBA', SIZE)
    d = ImageDraw.Draw(fx)
    for i in range(5):
        x = 924 + i * 18
        color = (234, 211, 154, round(255 * alpha)) if i < event['level'] // 5 else (255, 248, 233, round(70 * alpha))
        d.ellipse((x - 3, 164, x + 3, 170), fill=color)
    im.paste(fx, (0, 0), fx)


def journey(t):
    proximity = 0
    current = None
    for event in EVENTS:
        age = t - event['arrive']
        push = prior.ease((age + .55) / .65) * (1 - prior.ease((age - 1.25) / .65))
        proximity = max(proximity, push)
        if -.15 <= age <= 1.85:
            current = event, age
    width = 1290 - 230 * proximity
    im = base.follow('travel', t, width, (0, -45))
    if current:
        pickup_title(im, *current)
    elif t < 1.5:
        a = prior.ease(t / .25) * (1 - prior.ease((t - 1.1) / .3))
        layer = prior.wash(200, 165).copy()
        layer.putalpha(layer.getchannel('A').point(lambda v: int(v * a)))
        im.paste(layer, (0, 0), layer)
        prior.text(im, 'A friend every 5 levels', 960, 86, 54,
                   prior.IVORY, a)
    elif t > 15.7:
        prior.text(im, 'All aboard!', 960, 92, 61, prior.IVORY,
                   prior.ease((t - 15.7) / .3))
    return im


def scene(t):
    if t < 2.5:
        im = base.follow('travel', .4 + t * .5, 1150 + 450 * prior.ease(t / 2.5), (0, -45))
        prior.corner(im, 'BUCA', 'LET’S GO!', t + .15)
        return im
    if t < 18.5:
        return prior.scene(t)
    if t < 35.5:
        return journey(t - 18.5)
    return prior.scene(t - 10)


CUTS = [2.5, 8.5, 12.5, 18.5, 35.5, 44, 46.5, 49.5]


@lru_cache(12)
def boundary(t):
    return scene(t - 1 / FPS)


def frame(t):
    im = scene(t)
    for cut in CUTS:
        duration = .38 if cut in [35.5, 49.5] else .24
        if cut <= t < cut + duration:
            p = prior.ease((t - cut) / duration)
            old = boundary(cut)
            if cut in [8.5, 18.5]:
                offset = round(W * p)
                canvas = Image.new('RGB', SIZE)
                canvas.paste(old, (-offset, 0))
                canvas.paste(im, (W - offset, 0))
                im = canvas.filter(ImageFilter.GaussianBlur(2.3 * math.sin(math.pi * p)))
            else:
                im = Image.blend(base.zoom(old, 1 + .07 * p), base.zoom(im, 1.06 - .06 * p), p)
            break
    return im


def preview():
    times = [.7, 3.3, 9.5, 13.1, 19.1, 20.55, 21.05, 21.7,
             24.05, 24.7, 27.05, 27.7, 30.05, 30.7, 33.05, 33.7,
             35.1, 37.6, 41.7, 43.5, 44.8, 48, 50.5, 52.5]
    sheet = Image.new('RGB', (1920, 6 * 298), prior.INK)
    d = ImageDraw.Draw(sheet)
    for i, t in enumerate(times):
        im = frame(t)
        im.save(OUT / f'Preview-{t:05.2f}.jpg', quality=95)
        x, y = i % 4 * 480, i // 4 * 298
        sheet.paste(im.resize((480, 270)), (x, y))
        d.text((x + 8, y + 275), f'{t:.2f}s', font=prior.font(17), fill=prior.IVORY)
    sheet.save(OUT / 'Storyboard.jpg', quality=95)


def audio():
    sr = 48000
    n = round(DUR * sr)
    data = subprocess.check_output([FF, '-v', 'error', '-ss', str(48 * 60 / 101),
        '-i', str(R / 'output/buca-reel/assets/Funkorama.mp3'), '-af', 'atempo=1.188118812',
        '-t', str(DUR), '-f', 'f32le', '-ar', str(sr), '-ac', '2', '-'])
    music = np.frombuffer(data, np.float32).reshape(-1, 2).copy()[:n] * .58
    assert len(music) == n
    fx = np.zeros((n, 2), np.float32)
    cues = []

    def cue(path, at, gain, limit=100):
        data = subprocess.check_output([FF, '-v', 'error', '-i', str(R / 'Assets/Audio' / path),
            '-f', 'f32le', '-ar', str(sr), '-ac', '2', '-'])
        a = np.frombuffer(data, np.float32).reshape(-1, 2).copy()[:round(limit * sr)]
        fade = min(2400, len(a) // 4)
        a[:fade] *= np.linspace(0, 1, fade)[:, None]
        a[-fade:] *= np.linspace(1, 0, fade)[:, None]
        a *= gain / max(.1, float(abs(a).max()))
        st = round(at * sr)
        nn = min(len(a), n - st)
        fx[st:st + nn] += a[:nn]
        cues.append(dict(asset=path, at=at, gain=gain, duration=nn / sr))

    for item in json.loads((prior.OUT / 'sound-cues.json').read_text()):
        at, path = item['at'], item['asset']
        if 18.5 <= at < 25.5 and path.endswith('StationMovingTrain.wav'):
            continue
        at = at if at < 25.5 else at + 10
        limit = 2.5 if path.endswith('StationMovingTrain.wav') else 3.3 if path.endswith('WoodenTrainChukLoop.wav') else 2.4 if path.endswith('World2Welcome.wav') else 100
        cue(path, at, item['gain'], limit)

    for start, end in [(.4, 1.8), (3.4, 4.8), (6.4, 7.8), (9.4, 10.8), (12.4, 13.8), (15.4, 16.7)]:
        cue('Celebration/StationMovingTrain.wav', 18.5 + start, .16, end - start)
    for event in EVENTS:
        cue('Player/PickupCollect.ogg', 18.5 + event['land'], .27, .8)
        cue('UI/ScoreCountChime.wav', 18.5 + event['land'] + .05, .15, .8)

    env = np.ones(n, np.float32)
    for at, duration, gain in [(14.5, 2, .55), (35.5, 2.7, .68), (41.5, 2, .65)]:
        env[round(at * sr):round((at + duration) * sr)] = gain
    env = cv2.GaussianBlur(env[::240].reshape(1, -1), (0, 0), sigmaX=14).ravel()
    env = np.interp(np.arange(n), np.arange(len(env)) * 240, env)
    mix = music * env[:, None] + fx
    rng = np.random.default_rng(2026)
    for beat in range(math.ceil(DUR * 2)):
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
    subprocess.run([FF, '-y', '-v', 'error', '-f', 'f32le', '-ar', str(sr), '-ac', '2', '-i', '-',
        '-af', 'loudnorm=I=-14:TP=-2:LRA=8', '-ar', str(sr), str(OUT / 'Soundtrack.wav')],
        input=mix.astype('float32').tobytes(), check=True)
    (OUT / 'sound-cues.json').write_text(json.dumps(cues, indent=2))


def render():
    process = subprocess.Popen([FF, '-y', '-v', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24',
        '-s', '1920x1080', '-r', str(FPS), '-i', '-', '-i', str(OUT / 'Soundtrack.wav'),
        '-c:v', 'libx264', '-preset', 'fast', '-crf', '17', '-pix_fmt', 'yuv420p', '-threads', '5',
        '-c:a', 'aac', '-b:a', '256k', '-t', str(DUR), '-movflags', '+faststart', str(MOVIE)],
        stdin=subprocess.PIPE)
    for i in range(FRAMES):
        process.stdin.write(frame(i / FPS).tobytes())
        if i % 120 == 0:
            print(f'{i}/{FRAMES}', flush=True)
    process.stdin.close()
    assert process.wait() == 0


if __name__ == '__main__':
    if '--preview' in sys.argv:
        preview()
    elif '--audio' in sys.argv:
        audio()
    else:
        preview()
        audio()
        render()
