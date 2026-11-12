"""PixelInvaders synthesized arcade sound effects (zero external samples).

Run:
    python3 -I Tools/audio/PixelInvaders/make_sounds.py

Writes 15 16-bit mono 44.1 kHz .wav files into Assets/Prototypes/PixelInvaders/Audio/:
  - step_0.wav .. step_3.wav (the 4-note descending D-minor bass heartbeat: D2, C2, Bb1, A1)
  - shoot.wav, twin_shoot.wav, alien_shoot.wav
  - alien_pop.wav, bunker_hit.wav
  - ufo_loop.wav, ufo_boom.wav
  - powerup.wav, cannon_death.wav, wave_clear.wav, game_over.wav
"""
import math
import random
import struct
import sys
import wave
from pathlib import Path

RATE = 44100
PEAK = 0.78
FADE_OUT = 0.015
OUT_DIR = Path(__file__).resolve().parents[3] / "Assets" / "Prototypes" / "PixelInvaders" / "Audio"
TAU = 2.0 * math.pi


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


def osc_wave(freq, seconds, glide_to=None, square_mix=0.35, vibrato=0.0, vib_hz=6.0):
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = freq if glide_to is None else freq + (glide_to - freq) * (t / seconds)
        f *= 1.0 + vibrato * math.sin(TAU * vib_hz * t)
        phase += f / RATE
        s = math.sin(TAU * phase)
        sq = 1.0 if s >= 0.0 else -1.0
        out.append((1.0 - square_mix) * s + square_mix * sq)
    return out


def mix(*tracks):
    length = max(len(t) for t in tracks)
    out = [0.0] * length
    for t in tracks:
        for i, v in enumerate(t):
            out[i] += v
    return out


def concat(*segments):
    out = []
    for s in segments:
        out.extend(s)
    return out


def step_bass(freq):
    body = shaped(osc_wave(freq * 1.15, 0.11, glide_to=freq, square_mix=0.45), 0.003, 0.045)
    sub = shaped(osc_wave(freq * 0.5, 0.11, square_mix=0.10), 0.002, 0.050, amp=0.65)
    return mix(body, sub)


def make_shoot():
    sweep = shaped(osc_wave(1320.0, 0.14, glide_to=260.0, square_mix=0.55), 0.002, 0.055)
    spark = shaped(lowpass(noise(0.14, 101), 3200.0), 0.001, 0.025, amp=0.25)
    return mix(sweep, spark)


def make_twin_shoot():
    s1 = shaped(osc_wave(1480.0, 0.15, glide_to=310.0, square_mix=0.50), 0.002, 0.060)
    s2 = shaped(osc_wave(1980.0, 0.15, glide_to=440.0, square_mix=0.45), 0.004, 0.055, amp=0.75)
    return mix(s1, s2)


def make_alien_shoot():
    return shaped(osc_wave(420.0, 0.16, glide_to=140.0, square_mix=0.65, vibrato=0.08, vib_hz=28.0), 0.003, 0.070)


def make_alien_pop():
    pop = shaped(osc_wave(520.0, 0.18, glide_to=110.0, square_mix=0.40), 0.002, 0.055)
    chime = shaped(osc_wave(587.33, 0.18, glide_to=880.0, square_mix=0.25), 0.003, 0.065, amp=0.55)
    crunch = shaped(lowpass(noise(0.18, 202), 2400.0), 0.001, 0.040, amp=0.45)
    return mix(pop, chime, crunch)


def make_bunker_hit():
    n = shaped(lowpass(noise(0.10, 303), 1800.0), 0.001, 0.032)
    thud = shaped(osc_wave(190.0, 0.10, glide_to=75.0, square_mix=0.35), 0.002, 0.035, amp=0.7)
    return mix(n, thud)


def make_ufo_loop():
    # Seamless 0.50s warble (3 complete cycles of 6 Hz LFO, carrier 440 Hz = 220 integer cycles)
    seconds = 0.50
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = 440.0 + 110.0 * math.sin(TAU * 6.0 * t)
        phase += f / RATE
        out.append(0.75 * math.sin(TAU * phase) + 0.25 * math.sin(TAU * 2.0 * phase))
    return out


def make_ufo_boom():
    boom = shaped(lowpass(noise(0.45, 404), 950.0), 0.003, 0.14)
    drop = shaped(osc_wave(660.0, 0.45, glide_to=85.0, square_mix=0.50), 0.002, 0.16, amp=0.8)
    arp = concat(
        shaped(osc_wave(587.33, 0.08, square_mix=0.3), 0.002, 0.05, amp=0.5),
        shaped(osc_wave(783.99, 0.08, square_mix=0.3), 0.002, 0.05, amp=0.5),
        shaped(osc_wave(1174.66, 0.22, square_mix=0.3), 0.002, 0.10, amp=0.6),
    )
    return mix(boom, drop, arp)


def make_powerup():
    notes = [293.66, 349.23, 440.00, 587.33]  # D4, F4, A4, D5
    parts = [shaped(osc_wave(f, 0.09, square_mix=0.35), 0.002, 0.065) for f in notes]
    return concat(*parts)


def make_cannon_death():
    blast = shaped(lowpass(noise(0.55, 505), 1100.0), 0.002, 0.18)
    sub = shaped(osc_wave(220.0, 0.55, glide_to=45.0, square_mix=0.55), 0.003, 0.20, amp=0.85)
    return mix(blast, sub)


def make_wave_clear():
    notes = [(293.66, 0.10), (349.23, 0.10), (440.00, 0.10), (587.33, 0.12), (880.00, 0.28)]
    return concat(*[shaped(osc_wave(f, d, square_mix=0.35), 0.003, d * 0.75) for f, d in notes])


def make_game_over():
    notes = [(293.66, 0.18), (261.63, 0.18), (233.08, 0.18), (146.83, 0.42)]
    return concat(*[shaped(osc_wave(f, d, square_mix=0.45), 0.004, d * 0.70) for f, d in notes])


def save_wav(name, samples, loop_seamless=False):
    assert len(samples) > 0, f"{name}: empty sample list"
    peak = max(abs(x) for x in samples)
    assert peak > 1e-4, f"{name}: silent audio"
    scale = PEAK / peak
    out = [x * scale for x in samples]

    if not loop_seamless:
        fade_n = min(len(out) // 4, int(FADE_OUT * RATE))
        for i in range(fade_n):
            out[len(out) - 1 - i] *= i / float(fade_n)
        out[-1] = 0.0

    path = OUT_DIR / name
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(RATE)
        pcm = [int(max(-1.0, min(1.0, v)) * 32767.0) for v in out]
        wf.writeframes(struct.pack(f"<{len(pcm)}h", *pcm))
    print(f"wrote {path} ({len(out) / RATE:.2f}s, peak={max(abs(x) for x in out):.2f})")


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    # 4-note descending D-minor bass heartbeat (D2, C2, Bb1, A1)
    step_freqs = [73.42, 65.41, 58.27, 55.00]
    for idx, f in enumerate(step_freqs):
        save_wav(f"step_{idx}.wav", step_bass(f))

    save_wav("shoot.wav", make_shoot())
    save_wav("twin_shoot.wav", make_twin_shoot())
    save_wav("alien_shoot.wav", make_alien_shoot())
    save_wav("alien_pop.wav", make_alien_pop())
    save_wav("bunker_hit.wav", make_bunker_hit())
    save_wav("ufo_loop.wav", make_ufo_loop(), loop_seamless=True)
    save_wav("ufo_boom.wav", make_ufo_boom())
    save_wav("powerup.wav", make_powerup())
    save_wav("cannon_death.wav", make_cannon_death())
    save_wav("wave_clear.wav", make_wave_clear())
    save_wav("game_over.wav", make_game_over())


if __name__ == "__main__":
    main()
