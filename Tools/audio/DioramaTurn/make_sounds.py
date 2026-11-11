"""DioramaTurn sounds, written from nothing: no samples, no recordings.

Run:  python3 -I Tools/audio/DioramaTurn/make_sounds.py

Writes 16-bit mono 44.1 kHz .wav files into Assets/Prototypes/DioramaTurn/Audio/. The mood is warm,
wooden and light: marimba (a sine with a fast decay and a few harmonics), pluck, soft woodblock and
music-box tones, all from one C major pentatonic scale so nothing clashes. Every file is under 2
seconds, fades to zero at the end (no click) and peaks at 0.8 (no clipping). The script checks its own
output; whether the sounds feel like toys is a person's call.
"""
import math
import random
import struct
import sys
import wave
from pathlib import Path

RATE = 44100
PEAK = 0.8
FADE_OUT = 0.02
MAX_SECONDS = 2.0
OUT_DIR = Path(__file__).resolve().parents[3] / "Assets" / "Prototypes" / "DioramaTurn" / "Audio"
TAU = 2.0 * math.pi

# C major pentatonic, from C4 up.
PENTA = [261.63, 293.66, 329.63, 392.00, 440.00, 523.25, 587.33, 659.25, 783.99, 880.00]


def noise(seconds, seed):
    rng = random.Random(seed)
    return [rng.uniform(-1.0, 1.0) for _ in range(int(seconds * RATE))]


def lowpass(x, cutoff):
    out, y = [], 0.0
    for i, v in enumerate(x):
        c = cutoff(i / RATE) if callable(cutoff) else cutoff
        a = 1.0 - math.exp(-TAU * c / RATE)
        y += a * (v - y)
        out.append(y)
    return out


def highpass(x, cutoff):
    low = lowpass(x, cutoff)
    return [v - l for v, l in zip(x, low)]


def bandpass(x, low_hz, high_hz):
    return highpass(lowpass(x, high_hz), low_hz)


def envelope(n, attack, tau, hold=0.0):
    out = []
    for i in range(n):
        t = i / RATE
        a = min(1.0, t / attack) if attack > 0 else 1.0
        d = math.exp(-max(0.0, t - hold) / tau)
        out.append(a * d)
    return out


def shaped(x, attack, tau, hold=0.0, amp=1.0):
    env = envelope(len(x), attack, tau, hold)
    return [v * e * amp for v, e in zip(x, env)]


def sine(freq, seconds, glide_to=None, vibrato=0.0, vib_hz=5.0):
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = freq if glide_to is None else freq + (glide_to - freq) * (t / seconds)
        f *= 1.0 + vibrato * math.sin(TAU * vib_hz * t)
        phase += f / RATE
        out.append(math.sin(TAU * phase))
    return out


def marimba(freq, seconds, amp=1.0):
    """A wooden mallet tone: the fundamental and a quiet fourth partial, both decaying fast."""
    body = shaped(sine(freq, seconds), 0.003, seconds * 0.32)
    overtone = shaped(sine(freq * 4.0, seconds), 0.002, seconds * 0.12, amp=0.22)
    return [a * amp + b * amp for a, b in zip(body, overtone)]


def music_box(freq, seconds, amp=1.0):
    """A small metal tine: a clean sine with a long, quiet second partial."""
    body = shaped(sine(freq, seconds), 0.002, seconds * 0.5)
    overtone = shaped(sine(freq * 2.0, seconds), 0.001, seconds * 0.25, amp=0.18)
    return [(a + b) * amp for a, b in zip(body, overtone)]


def pluck(freq, seconds=0.22, amp=1.0):
    return shaped(sine(freq, seconds), 0.002, seconds * 0.28, amp=amp)


def partials(freqs, seconds, taus, amps=None):
    n = int(seconds * RATE)
    amps = amps or [1.0] * len(freqs)
    out = [0.0] * n
    for f, tau, a in zip(freqs, taus, amps):
        for i in range(n):
            t = i / RATE
            out[i] += math.sin(TAU * f * t) * math.exp(-t / tau) * a
    return out


def mix(voices):
    """voices: (delay_seconds, samples). Sums, fades the end, scales the peak to PEAK."""
    length = max(int(d * RATE) + len(s) for d, s in voices)
    buf = [0.0] * length
    for delay, samples in voices:
        start = int(delay * RATE)
        for i, v in enumerate(samples):
            buf[start + i] += v
    fade = int(FADE_OUT * RATE)
    for i in range(fade):
        buf[length - 1 - i] *= (i / fade) ** 2
    top = max(abs(v) for v in buf)
    return [v / top * PEAK for v in buf]


# ---- the sounds ----

def turn_whoosh():
    whoosh = shaped(bandpass(noise(0.32, 3), 250, 1600), 0.1, 0.16, amp=0.7)
    rise = shaped(sine(180, 0.32, 240), 0.05, 0.18, amp=0.25)
    return mix([(0, whoosh), (0, rise)])


def turn_clunk():
    body = shaped(sine(150, 0.18, 90), 0.001, 0.05)
    wood = shaped(lowpass(noise(0.1, 5), 900), 0.001, 0.02, amp=0.6)
    return mix([(0, body), (0, wood)])


def join_up():
    return mix([(0, marimba(PENTA[2], 0.4)), (0.13, marimba(PENTA[4], 0.5))])


def join_down():
    return mix([(0, marimba(PENTA[4], 0.35)), (0.12, marimba(PENTA[2], 0.45))])


def tap_pluck():
    return mix([(0, pluck(PENTA[5], 0.25))])


def step_a():
    return mix([(0, marimba(PENTA[0] * 2, 0.12, 0.5))])


def step_b():
    return mix([(0, marimba(PENTA[1] * 2, 0.12, 0.5))])


def hop_boing():
    boing = shaped(sine(220, 0.28, 520), 0.004, 0.12)
    tick = shaped(highpass(noise(0.03, 7), 2000), 0.0005, 0.008, amp=0.4)
    return mix([(0, boing), (0, tick)])


def landing():
    return mix([(0, shaped(sine(120, 0.12, 80), 0.001, 0.03)), (0, shaped(lowpass(noise(0.04, 9), 700), 0.001, 0.012, amp=0.5))])


def bonk():
    return mix([(0, shaped(sine(190, 0.16, 130), 0.001, 0.04)),
                (0, shaped(lowpass(noise(0.05, 11), 1000), 0.001, 0.012, amp=0.5))])


def tick():
    return mix([(0, shaped(highpass(lowpass(noise(0.025, 13), 3000), 800), 0.0005, 0.006))])


def star_arpeggio():
    notes = [(0.0, PENTA[5]), (0.1, PENTA[6]), (0.2, PENTA[7]), (0.3, PENTA[8]), (0.42, PENTA[9])]
    return mix([(d, music_box(f, 0.6)) for d, f in notes])


def star_chime():
    return mix([(0, partials([783.99, 1174.66, 1567.98], 1.4, [0.5, 0.3, 0.18], [0.7, 0.4, 0.25]))])


def pop():
    return mix([(0, shaped(sine(300, 0.14, 620), 0.002, 0.05)),
                (0, shaped(highpass(noise(0.02, 17), 2500), 0.0005, 0.005, amp=0.35))])


def restart():
    fall = shaped(sine(520, 0.2, 300), 0.002, 0.08)
    rise = shaped(sine(300, 0.26, 560), 0.004, 0.1)
    return mix([(0, fall), (0.2, rise)])


SOUNDS = {
    "turn_whoosh": turn_whoosh,
    "turn_clunk": turn_clunk,
    "join_up": join_up,
    "join_down": join_down,
    "tap_pluck": tap_pluck,
    "step_a": step_a,
    "step_b": step_b,
    "hop_boing": hop_boing,
    "landing": landing,
    "bonk": bonk,
    "tick": tick,
    "star_arpeggio": star_arpeggio,
    "star_chime": star_chime,
    "pop": pop,
    "restart": restart,
}


def write(path, samples):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(round(v * 32767))) for v in samples))


def check(path):
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
