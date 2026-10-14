#!/usr/bin/env python3
"""Reproduce original game sounds; no external recordings or dependencies."""
import math
import random
import struct
import wave
from pathlib import Path

RATE = 48000
TAU = math.tau
DEST = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio'
DEST.mkdir(parents=True, exist_ok=True)


def write(name, data, looping=False):
    # Blend the seam into the beginning of looped noise to avoid boundary clicks.
    if looping:
        fade = 1200
        for i in range(fade):
            amount = i / fade
            data[-fade + i] = data[-fade + i] * (1 - amount) + data[i] * amount
        # The tail now leads into sample fade; discard the overlapping head.
        data = data[fade:]
    peak = max(abs(v) for v in data) or 1
    scale = .82 / max(1, peak)
    with wave.open(str(DEST / (name + '.wav')), 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, v * scale)) * 32767)) for v in data))


def engine(name, firing):
    rng = random.Random(71 + firing)
    data, low = [], 0
    for i in range(RATE * 2):
        t = i / RATE
        phase = TAU * firing * t + .025 * math.sin(TAU * 7 * t)
        # Uneven firing accents and an exhaust resonator give the pulse a rasp.
        pulse = sum(math.sin(phase * h + h * .18) * math.exp(-h * .19) / math.sqrt(h) for h in range(1, 15))
        low += .09 * (rng.uniform(-1, 1) - low)
        value = pulse * (.32 + .045 * math.sin(TAU * firing / 2 * t))
        value += .075 * math.sin(phase / 2) + low * .14
        data.append(math.tanh(value * 1.5) * .65)
    write(name, data, True)


def noise(name, lowpass, amplitude, tonal=0):
    rng = random.Random(91)
    data, low, slow = [], 0, 0
    for i in range(RATE * 2):
        t = i / RATE
        n = rng.uniform(-1, 1)
        low += lowpass * (n - low)
        slow += .015 * (n - slow)
        wobble = 1 + .18 * math.sin(TAU * 11 * t)
        data.append((low - slow * .45) * amplitude * wobble + tonal * math.sin(TAU * 810 * t + 8 * math.sin(TAU * 3 * t)))
    write(name, data, True)


engine('engine-idle', 50)
engine('engine-load', 100)
noise('tyre-scrub', .72, .7, .06)
noise('road-wind', .12, .6)
noise('nitro-air', .38, .55, .015)
rng = random.Random(113)
impact = []
low = 0
for i in range(int(RATE * .45)):
    t = i / RATE
    low += .25 * (rng.uniform(-1, 1) - low)
    envelope = min(1, t * 1000) * math.exp(-t * 13)
    impact.append(envelope * (.6 * low + .5 * math.sin(TAU * (92 * t - 55 * t * t))))
write('impact', impact)
for name, freqs, duration in [('countdown', [660], .13), ('go', [880, 1320], .32), ('lap', [660, 880], .3), ('finish', [523.25, 659.25, 783.99], .65), ('click', [1200], .025)]:
    data = []
    for i in range(int(RATE * duration)):
        t = i / RATE
        env = min(1, t * 250) * min(1, (duration - t) * 60) * math.exp(-t * 3)
        data.append(sum(math.sin(TAU * f * t) for f in freqs) / len(freqs) * env * .4)
    write(name, data)
print('Generated 11 original mono 48 kHz audio assets.')
