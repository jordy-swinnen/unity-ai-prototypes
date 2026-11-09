"""Neon Pong sounds, written from nothing: no samples, no recordings.

Run:  python3 -I Tools/audio/NeonPong/make_sounds.py

Writes 16-bit mono 44.1 kHz .wav files into Assets/Prototypes/NeonPong/Audio/. Square and
triangle tones with a fast attack, an exponential decay and a short fade to zero at the end
(so there is no click). Each file is under 1.5 seconds and peaks at 0.8 (no clipping).
The script checks its own output. Whether the sounds sound good is a person's call.
"""
import math
import struct
import sys
import wave
from pathlib import Path

RATE = 44100
PEAK = 0.8
ATTACK = 0.002
FADE_OUT = 0.012
MAX_SECONDS = 1.5
OUT_DIR = Path(__file__).resolve().parents[3] / "Assets" / "Prototypes" / "NeonPong" / "Audio"


def square(phase):
    return 1.0 if (phase % 1.0) < 0.5 else -1.0


def triangle(phase):
    p = phase % 1.0
    return 4.0 * p - 1.0 if p < 0.5 else 3.0 - 4.0 * p


def sine(phase):
    return math.sin(2.0 * math.pi * phase)


def voice(shape, f0, f1, seconds, tau, amp, delay=0.0):
    """One tone that slides from f0 to f1 Hz. Returns (start_sample, samples)."""
    n = int(seconds * RATE)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / RATE
        f = f0 + (f1 - f0) * (t / seconds)
        phase += f / RATE
        env = min(1.0, t / ATTACK) * math.exp(-t / tau)
        out.append(shape(phase) * env * amp)
    return int(delay * RATE), out


def mix(voices):
    length = max(start + len(s) for start, s in voices)
    buf = [0.0] * length
    for start, samples in voices:
        for i, v in enumerate(samples):
            buf[start + i] += v
    # Fade the end to zero, then scale the loudest sample to PEAK.
    fade = int(FADE_OUT * RATE)
    for i in range(fade):
        buf[length - 1 - i] *= (i / fade) ** 2
    top = max(abs(v) for v in buf)
    return [v / top * PEAK for v in buf]


SOUNDS = {
    # A short low blip.
    "wall": lambda: mix([voice(square, 196, 196, 0.09, 0.03, 0.6)]),
    # Hard contact: a bright square blip over a low thump. The game raises its pitch with speed.
    "paddle": lambda: mix([
        voice(square, 392, 392, 0.14, 0.045, 0.35),
        voice(sine, 110, 70, 0.14, 0.05, 0.6),
    ]),
    # Rising blip.
    "serve": lambda: mix([voice(triangle, 330, 494, 0.14, 0.12, 0.6)]),
    # Falling tone with a low thump.
    "point": lambda: mix([
        voice(square, 440, 110, 0.4, 0.2, 0.35),
        voice(sine, 90, 50, 0.25, 0.08, 0.7),
    ]),
    # A three-note rising phrase.
    "win": lambda: mix([
        voice(triangle, 330, 330, 0.2, 0.15, 0.6, 0.0),
        voice(triangle, 415, 415, 0.2, 0.15, 0.6, 0.2),
        voice(triangle, 554, 554, 0.5, 0.25, 0.6, 0.4),
        voice(square, 554, 554, 0.5, 0.2, 0.15, 0.4),
    ]),
    # Rising blip, a little longer than the serve.
    "restart": lambda: mix([voice(triangle, 262, 523, 0.16, 0.12, 0.6)]),
}


def write(path, samples):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(round(v * 32767))) for v in samples))


def check(path):
    """Read the file back and check length, silence, clipping and the last samples."""
    with wave.open(str(path), "rb") as w:
        assert w.getnchannels() == 1 and w.getsampwidth() == 2 and w.getframerate() == RATE
        raw = w.readframes(w.getnframes())
    data = struct.unpack("<%dh" % (len(raw) // 2), raw)
    seconds = len(data) / RATE
    peak = max(abs(v) for v in data) / 32768.0
    tail = max(abs(v) for v in data[-20:]) / 32768.0
    problems = []
    if seconds >= MAX_SECONDS:
        problems.append(f"{seconds:.2f} s is too long")
    if peak < 0.1:
        problems.append("silent")
    if peak > 0.95:
        problems.append("clips")
    if tail > 0.01:
        problems.append(f"ends at {tail:.3f}, not near zero")
    return seconds, peak, tail, problems


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    failed = False
    for name, make in SOUNDS.items():
        path = OUT_DIR / f"{name}.wav"
        write(path, make())
        seconds, peak, tail, problems = check(path)
        print(f"{path.name}: {seconds:.2f} s, peak {peak:.2f}, ends at {tail:.4f}")
        for p in problems:
            print(f"  PROBLEM: {p}")
            failed = True
    sys.exit(1 if failed else 0)


main()
