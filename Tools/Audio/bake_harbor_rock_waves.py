"""Original stereo shoreline wash: slow incoming surges, rock splash, foamy retreat."""
import math
import random
import struct
import wave
from pathlib import Path

rate, seconds, overlap = 24000, 34, 2
channels = []
for channel in range(2):
    rng = random.Random(20261020 + channel)
    wash = [0.] * (rate * seconds)
    foam = [0.] * len(wash)
    # Uneven spacing and strength make each arriving wave a little different.
    for start, strength in [(-2, .75), (2.1, 1), (6.9, .78), (11.1, .94),
                            (15.8, .7), (20.0, 1), (24.9, .82), (29.2, .95), (33.8, .76)]:
        start += channel * .12
        for i in range(max(0, int(start*rate)), min(len(wash), int((start+6)*rate))):
            age = i/rate-start
            wash[i] += strength * (1-math.exp(-age*3)) * math.exp(-age*.62)
            retreat = max(0, age-.6)
            foam[i] += strength * (1-math.exp(-retreat*4)) * math.exp(-retreat*.9)
    low = body = soft = 0.
    samples = []
    for i in range(len(wash)):
        noise = rng.uniform(-1, 1)
        low += .018*(noise-low)
        body += .16*(noise-body)
        soft += .48*(noise-soft)
        t = i/rate
        texture = .78+.22*math.sin(t*13.7+channel)*math.sin(t*7.1)
        samples.append(low*(.24+.75*wash[i]) + (body-low)*(.035+.26*wash[i])
                       + (soft-body)*.075*foam[i]*texture)
    count = rate*overlap
    for i in range(count):
        w = .5-.5*math.cos(math.pi*i/(count-1))
        samples[-count+i] = samples[-count+i]*(1-w)+samples[i]*w
    channels.append(samples[count:])
gain = .70/max(abs(x) for ch in channels for x in ch)
data = bytearray()
for left, right in zip(*channels):
    data.extend(struct.pack('<hh', round(left*gain*32767), round(right*gain*32767)))
path = Path('Assets/Audio/Ambience/HarborRockWaves.wav')
with wave.open(str(path), 'wb') as out:
    out.setparams((2, 2, rate, 0, 'NONE', 'not compressed'))
    out.writeframes(data)
rms = math.sqrt(sum((x*gain)**2 for ch in channels for x in ch)/(2*len(channels[0])))
report = f'32-second stereo shoreline wave wash, {rate} Hz, 16-bit PCM. Peak -3.10 dBFS; RMS {20*math.log10(rms):.2f} dBFS. Two-second loop crossfade. Original synthesis, no third-party recordings.\n'
path.with_suffix('.source.txt').write_text(report+'Generator: Tools/Audio/bake_harbor_rock_waves.py\n')
print(report)
