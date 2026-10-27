"""Original short card-turn Foley: release, paper flutter, padded landing and brass latch."""
from pathlib import Path
import math
import random
import struct
import wave

RATE = 48000
SECONDS = .52
LAND = .42 * .88
rng = random.Random(311024)
count = round(RATE * SECONDS)
noise = [rng.uniform(-1, 1) for _ in range(count)]
excitation = [0.0] * count
for at, volume, decay in [(0.008, .34, .002), (.176, .10, .0025),
                          (LAND, 1, .0035), (LAND + .019, .19, .002)]:
    begin = round(at * RATE)
    for i in range(begin, min(count, begin + round(.055 * RATE))):
        excitation[i] += volume * noise[i] * math.exp(-(i - begin) / RATE / decay)

body = [0.0] * count
for frequency, decay, volume in [(172, .018, .30), (389, .012, .20),
                                  (873, .009, .11), (2240, .005, .035)]:
    radius = math.exp(-1 / (RATE * decay))
    a = 2 * radius * math.cos(2 * math.pi * frequency / RATE)
    b = radius * radius
    previous = older = 0.0
    for i in range(count):
        sample = excitation[i] + a * previous - b * older
        older, previous = previous, sample
        body[i] += sample * math.sqrt(1 - radius) * volume

raw = []
low = mid = 0.0
for i in range(count):
    t = i / RATE
    low += .028 * (noise[i] - low)
    mid += .21 * (noise[i] - mid)
    u = min(1, max(0, (t - .025) / (LAND - .025)))
    sweep = math.sin(math.pi * u) ** 1.5
    flutter = .72 + .20 * math.sin(t * 99) + .08 * math.sin(t * 217)
    raw.append(body[i] + (mid - low) * sweep * flutter * .42 + excitation[i] * .12)

gain = .68 / max(map(abs, raw))
channels = []
for delay_ms in (9, 12):
    delay = round(RATE * delay_ms / 1000)
    samples = []
    for i, value in enumerate(raw):
        room = raw[i - delay] * .055 if i >= delay else 0
        fade = max(0, min(1, i / (RATE * .001), (count - 1 - i) / (RATE * .025)))
        samples.append((value + room) * gain * fade)
    channels.append(samples)

path = Path('Assets/Audio/UI/WorkshopCardFlip.wav')
with wave.open(str(path), 'wb') as audio:
    audio.setparams((2, 2, RATE, 0, 'NONE', 'not compressed'))
    audio.writeframes(b''.join(struct.pack('<hh', round(a * 32767), round(b * 32767))
                               for a, b in zip(*channels)))
path.with_suffix('.source.txt').write_text(
    'Original procedural card-turn Foley: release click, soft paper flutter, padded card landing '
    'and damped brass latch at 0.3696 seconds. No external recordings or samples. '
    'Stereo 48 kHz PCM, 0.52 seconds. Generator: Tools/Audio/bake_workshop_flip.py.\n')
print(path)
print('Peak dBFS:', round(20 * math.log10(max(abs(v) for c in channels for v in c)), 2))
