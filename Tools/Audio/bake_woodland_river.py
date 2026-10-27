"""Prepare a CC0 field recording as BUCA's seamless woodland river loop.

Run with Python 3 on macOS where afconvert supplies the MP3 decoder.
The source is retained under Tools so rebuilding does not require a download.
"""
import array
import hashlib
import math
from pathlib import Path
import subprocess
import sys
import wave

RATE = 32000
SOURCE = Path('Tools/Audio/Sources/ForestStreamStoneBridge_gerainsan_CC0.mp3')
SOURCE_HASH = 'e36c90e626c374690c10f8bbeffd6ece800275ab3232a83ba60c0e8bd4b22f67'
SOURCE_PAGE = 'https://freesound.org/people/gerainsan/sounds/733774/'
SOURCE_MEDIA = 'https://cdn.freesound.org/previews/733/733774_3577171-hq.mp3'
OUTPUT = Path('output/woodland-river-natural')
OUTPUT.mkdir(parents=True, exist_ok=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest() == SOURCE_HASH, 'Unexpected source recording'
decoded = OUTPUT / 'source-decoded.wav'
subprocess.run(['/usr/bin/afconvert', '-f', 'WAVE', '-d', f'LEI16@{RATE}',
                '-c', '2', str(SOURCE), str(decoded)], check=True)
with wave.open(str(decoded), 'rb') as stream:
    assert stream.getnchannels() == 2 and stream.getsampwidth() == 2
    assert stream.getframerate() == RATE
    stream.setpos(12 * RATE)
    raw = array.array('h', stream.readframes(26 * RATE))
if sys.byteorder != 'little':
    raw.byteswap()
assert len(raw) == 26 * RATE * 2

channels = []
coefficient = math.exp(-2 * math.pi * 65 / RATE)
overlap = 2 * RATE
for channel in range(2):
    previous_in = previous_out = 0.0
    values = []
    # Remove microphone rumble while retaining the real splashes and babble
    for sample in raw[channel::2]:
        value = sample / 32768
        filtered = coefficient * (previous_out + value - previous_in)
        previous_in, previous_out = value, filtered
        values.append(filtered)
    for i in range(overlap):
        phase = i / (overlap - 1) * math.pi / 2
        values[-overlap+i] = values[-overlap+i] * math.cos(phase) + values[i] * math.sin(phase)
    channels.append(values[overlap:])

rms = math.sqrt(sum(v*v for channel in channels for v in channel) / (2 * len(channels[0])))
gain = .12 / rms
# Gently contain occasional close splashes without hiding the quieter current.
channels = [[.72 * math.tanh(v * gain / .72) for v in channel] for channel in channels]
peak = max(abs(v) for channel in channels for v in channel)
rms = math.sqrt(sum(v*v for channel in channels for v in channel) / (2 * len(channels[0])))
frames = array.array('h')
for left, right in zip(*channels):
    frames.extend((round(left * 32767), round(right * 32767)))
if sys.byteorder != 'little':
    frames.byteswap()
folder = Path('Assets/Audio/Ambience')
with wave.open(str(folder / 'WoodlandRiverWater.wav'), 'wb') as stream:
    stream.setparams((2, 2, RATE, 0, 'NONE', 'not compressed'))
    stream.writeframes(frames.tobytes())
seam = max(abs(channel[-1] - channel[0]) for channel in channels)
normal_step = max(abs(channel[i] - channel[i-1]) for channel in channels for i in range(1,len(channel)))
assert seam <= normal_step, 'Loop boundary has an abnormal transient'
assert peak <= .721, 'Audio is clipping'
report = (f'Natural stream field recording by gerainsan under CC0\n'
          f'Stereo 24 second loop at {RATE} Hz\n'
          f'Peak {20*math.log10(peak):.1f} dBFS and RMS {20*math.log10(rms):.1f} dBFS\n'
          f'Loop boundary step {seam:.5f} below largest normal step {normal_step:.5f}\n'
          '65 Hz rumble filter with gentle peak limiting and two second equal-power loop crossfade\n')
(OUTPUT / 'audio-validation.txt').write_text(report)
credit = (f'WoodlandRiverWater.wav\n'
          'Source: WATRFlow_Forest Stream Under A Small Stone Bridge_OlGer_(gs)\n'
          f'Creator: gerainsan\nSource page: {SOURCE_PAGE}\n'
          f'Public HQ player recording: {SOURCE_MEDIA}\n'
          'License: CC0 1.0 Universal\nhttps://creativecommons.org/publicdomain/zero/1.0/\n'
          'Retrieved 2026-10-20\n'
          'Edited: excerpt, 65 Hz high-pass, 32000 Hz stereo, normalized with gentle peak limiting, 2 second crossfade into a 24 second loop\n'
          f'Source SHA256: {SOURCE_HASH}\n'
          'Prepared with Tools/Audio/bake_woodland_river.py\n')
(folder / 'WoodlandRiverWater.SOURCE.txt').write_text(credit)
(SOURCE.parent / 'ForestStreamStoneBridge.LICENSE.txt').write_text(credit)
print(report)
