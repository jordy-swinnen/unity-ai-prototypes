"""Medieval Chess sounds, written from nothing: no samples, no recordings.

Run:  python3 -I Tools/audio/MedievalChess/make_sounds.py

Writes 16-bit mono 44.1 kHz .wav files into Assets/Prototypes/MedievalChess/Audio/. The sounds are
low sines (thuds, drums, horns, bells), filtered noise (swings, slides, rumbles) and a few
inharmonic partials (iron, anvil). Every file fades to zero at the end (no click), peaks at 0.8
(no clipping) and is under 2 seconds. The noise is seeded, so a rerun writes the same files.
The script checks its own output. Whether the sounds sound good, and how loud each one sits
against the others, is a person's call: the volumes are set in the settings asset.
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
OUT_DIR = Path(__file__).resolve().parents[3] / "Assets" / "Prototypes" / "MedievalChess" / "Audio"
TAU = 2.0 * math.pi


def noise(seconds, seed):
    rng = random.Random(seed)
    return [rng.uniform(-1.0, 1.0) for _ in range(int(seconds * RATE))]


def lowpass(x, cutoff):
    """One-pole low pass. cutoff may be a number or a function of time in seconds."""
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
    """Fast attack, optional hold, exponential decay. Returns n values."""
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


def sine_wave(freq, seconds, glide_to=None, vibrato=0.0, vib_hz=5.0):
    """A sine that can glide from freq to glide_to and wobble a little (a horn or a creak)."""
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = freq if glide_to is None else freq + (glide_to - freq) * (t / seconds)
        f *= 1.0 + vibrato * math.sin(TAU * vib_hz * t)
        phase += f / RATE
        out.append(math.sin(TAU * phase))
    return out


def horn_wave(freq, seconds, glide_to=None):
    """A brassy tone: a few harmonics with the upper ones fading, a slight wobble."""
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = freq if glide_to is None else freq + (glide_to - freq) * (t / seconds)
        f *= 1.0 + 0.006 * math.sin(TAU * 5.2 * t)
        phase += f / RATE
        out.append(sum(math.sin(TAU * phase * h) / h for h in (1, 2, 3, 4, 5)) / 2.2)
    return out


def partials(freqs, seconds, taus, amps=None):
    """Inharmonic partials, each with its own decay: iron, an anvil, a bell."""
    n = int(seconds * RATE)
    amps = amps or [1.0] * len(freqs)
    out = [0.0] * n
    for f, tau, a in zip(freqs, taus, amps):
        for i in range(n):
            t = i / RATE
            out[i] += math.sin(TAU * f * t) * math.exp(-t / tau) * a
    return out


def mix(voices):
    """voices: (delay_seconds, samples). Sums them, fades the end, scales the peak to PEAK."""
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

def footstep():
    # a boot on stone: a low thud and a short dull scrape
    body = shaped(sine_wave(95, 0.22, 50), 0.002, 0.06)
    scuff = shaped(lowpass(noise(0.12, 1), 900), 0.001, 0.025, amp=0.5)
    return mix([(0, body), (0, scuff)])


def knight_leap():
    # hooves: two quick knocks and a whoosh, ending in a heavy landing
    knock = lambda seed: shaped(lowpass(noise(0.08, seed), 1400), 0.001, 0.02)
    whoosh = shaped(bandpass(noise(0.4, 3), 300, 1800), 0.12, 0.2, amp=0.5)
    land = shaped(sine_wave(80, 0.25, 45), 0.002, 0.07)
    return mix([(0, knock(2)), (0.07, knock(5)), (0.1, whoosh), (0.42, land)])


def rook_slide():
    # stone dragged over stone: a rough low grind that swells and stops
    grind = lowpass(noise(0.6, 4), lambda t: 500 + 600 * math.sin(t * 6))
    swell = [v * math.sin(math.pi * min(1.0, i / (0.6 * RATE))) ** 0.7 for i, v in enumerate(grind)]
    thud = shaped(sine_wave(70, 0.2, 45), 0.002, 0.06)
    return mix([(0, swell), (0.52, [v * 1.2 for v in thud])])


def swing(low, high, seconds, whoosh_tau):
    return lambda: mix([(0, shaped(bandpass(noise(seconds, 7), low, high), seconds * 0.35, whoosh_tau))])


def impact():
    # iron on iron: a bright crack, a ringing body and a low blow
    crack = shaped(highpass(noise(0.06, 9), 2500), 0.0005, 0.012)
    ring = partials([520, 1180, 1750, 2410], 0.5, [0.18, 0.12, 0.09, 0.06], [0.6, 0.4, 0.3, 0.2])
    blow = shaped(sine_wave(120, 0.3, 55), 0.002, 0.09, amp=1.2)
    return mix([(0, crack), (0, ring), (0, blow)])


def collapse():
    # a figure falling apart: a long low rumble with falling crackle
    rumble = shaped(lowpass(noise(0.9, 11), lambda t: 700 - 450 * t), 0.02, 0.3)
    rng = random.Random(12)
    crackle = [0.0] * int(0.9 * RATE)
    for _ in range(26):
        at = int(rng.random() ** 1.6 * 0.8 * RATE)
        for j in range(int(0.004 * RATE)):
            if at + j < len(crackle):
                crackle[at + j] += rng.uniform(-1, 1) * (1 - j / (0.004 * RATE)) * 0.6
    return mix([(0, rumble), (0, lowpass(crackle, 3000))])


def drum():
    # a low war drum: a deep skin tone with a short beater thump
    skin = shaped(sine_wave(68, 0.9, 42), 0.002, 0.24)
    beater = shaped(lowpass(noise(0.05, 13), 400), 0.001, 0.012, amp=0.7)
    return mix([(0, skin), (0, beater)])


def select_clink():
    return mix([(0, partials([880, 1320, 2200], 0.25, [0.08, 0.05, 0.03], [0.6, 0.4, 0.3])), (0, shaped(sine_wave(160, 0.1, 110), 0.001, 0.03, amp=0.4))])


def illegal_knock():
    # a dull knock on wood
    return mix([(0, shaped(sine_wave(190, 0.14, 150), 0.001, 0.035)), (0, shaped(lowpass(noise(0.05, 15), 1200), 0.001, 0.01, amp=0.5)), (0.11, shaped(sine_wave(170, 0.14, 140), 0.001, 0.03, amp=0.7))])


def hover_tick():
    return mix([(0, shaped(highpass(lowpass(noise(0.03, 16), 3500), 900), 0.0005, 0.006))])


def check_horn():
    # a low warning horn: two blasts
    a = shaped(horn_wave(116, 0.5), 0.05, 0.4)
    b = shaped(horn_wave(110, 0.7), 0.05, 0.5)
    return mix([(0, a), (0.55, b)])


def mate_fanfare():
    # a drum roll under a rising three-note horn call
    roll = []
    for k in range(9):
        roll.append((k * 0.1, shaped(sine_wave(70, 0.3, 50), 0.002, 0.07, amp=0.5 + 0.06 * k)))
    notes = [(0.0, 130.8, 0.45), (0.5, 164.8, 0.45), (1.0, 196.0, 0.9)]
    horns = [(d, shaped(horn_wave(f, s), 0.04, s * 0.7)) for d, f, s in notes]
    return mix(roll + horns)


def draw_bell():
    return mix([(0, partials([196, 392, 588, 880, 1180], 1.9, [0.9, 0.7, 0.5, 0.35, 0.2], [0.8, 0.6, 0.45, 0.3, 0.15]))])


def promotion_anvil():
    return mix([(0, partials([410, 880, 1370, 2050, 2790], 1.2, [0.35, 0.25, 0.18, 0.12, 0.08], [0.6, 0.5, 0.4, 0.3, 0.2])),
                (0, shaped(highpass(noise(0.05, 17), 2000), 0.0005, 0.01)), (0, shaped(sine_wave(110, 0.2, 70), 0.002, 0.06))])


def ui_creak():
    # a wooden hinge: a slow, uneven pitch wobble
    creak = sine_wave(240, 0.4, 330, vibrato=0.08, vib_hz=23)
    grit = lowpass(noise(0.4, 19), 1500)
    voice = [c * 0.5 + g * 0.5 for c, g in zip(creak, grit)]
    return mix([(0, shaped(voice, 0.06, 0.2))])


def resign_horn():
    return mix([(0, shaped(horn_wave(98, 1.4, 82), 0.12, 0.7))])


def game_start():
    return mix([(0, shaped(sine_wave(68, 0.9, 42), 0.002, 0.24)), (0.35, shaped(horn_wave(131, 1.0), 0.08, 0.55))])


SOUNDS = {
    "footstep": footstep,
    "knight_leap": knight_leap,
    "rook_slide": rook_slide,
    "swing_pawn": swing(500, 2500, 0.25, 0.07),
    "swing_knight": swing(250, 1800, 0.4, 0.13),
    "swing_bishop": swing(150, 1200, 0.4, 0.12),
    "swing_rook": swing(80, 700, 0.45, 0.14),
    "swing_queen": swing(900, 4500, 0.3, 0.07),
    "swing_king": swing(100, 900, 0.55, 0.18),
    "impact": impact,
    "collapse": collapse,
    "drum": drum,
    "select_clink": select_clink,
    "illegal_knock": illegal_knock,
    "hover_tick": hover_tick,
    "check_horn": check_horn,
    "mate_fanfare": mate_fanfare,
    "draw_bell": draw_bell,
    "promotion_anvil": promotion_anvil,
    "ui_creak": ui_creak,
    "resign_horn": resign_horn,
    "game_start": game_start,
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
