"""Writes the DioramaTurn wood texture: tileable 512 px albedo and normal maps for the plinth.

Run it headless in Blender (Blender ships numpy, so nothing needs installing):

    /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/textures/DioramaTurn/make_textures.py

Writes wood_albedo.png and wood_normal.png into Assets/Prototypes/DioramaTurn/Art/Textures/.
Never edit those by hand. Everything is original, made from seeded noise, and tiles: the noise is made
in the frequency domain and the plank pattern uses whole repeats. The albedo is a "detail" map whose
average is near 0.9, so the plinth colour from the settings still decides the tone.
"""
import struct
import sys
import zlib
from pathlib import Path

import numpy as np

N = 512
OUT = Path(__file__).resolve().parents[3] / "Assets" / "Prototypes" / "DioramaTurn" / "Art" / "Textures"
Y, X = np.mgrid[0:N, 0:N] / N


def fractal(slope, seed, stretch=(1.0, 1.0)):
    """Zero-mean, unit-deviation noise with amplitude 1/f^slope. The FFT makes it wrap."""
    rng = np.random.default_rng(seed)
    spectrum = np.fft.fft2(rng.standard_normal((N, N)))
    fy = np.fft.fftfreq(N)[:, None] * stretch[0]
    fx = np.fft.fftfreq(N)[None, :] * stretch[1]
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    out = np.real(np.fft.ifft2(spectrum / f ** slope))
    out -= out.mean()
    return out / out.std()


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def wood():
    """Warm toy wood: planks with soft rings, a little grain and thin gaps."""
    planks = 4
    plank_id = np.floor(Y * planks)
    local = Y * planks - plank_id
    rng = np.random.default_rng(71)
    offset = rng.uniform(-0.05, 0.05, planks)[plank_id.astype(int)]
    shift = rng.uniform(0, 1, planks)[plank_id.astype(int)]
    warp = fractal(1.6, 72, stretch=(1.0, 9.0))
    rings = np.sin(2 * np.pi * (5 * local + 0.4 * warp + shift)) * 0.5 + 0.5
    streak = fractal(1.0, 73, stretch=(1.0, 26.0))
    gap = np.exp(-np.minimum(local, 1 - local) / 0.010)
    lum = 0.80 + 0.13 * rings ** 1.4 + 0.03 * streak + offset - 0.30 * gap
    tint = np.stack([1.03 + 0 * lum, 0.98 + 0 * lum, 0.90 + 0 * lum], -1)
    height = 0.55 * rings + 0.2 * streak - 2.4 * gap
    return lum, tint, height


def normal_map(height, target_slope):
    dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * 0.5
    drow = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * 0.5
    scale = target_slope / np.sqrt((dx * dx + drow * drow).mean())
    n = np.stack([-dx * scale, drow * scale, np.ones_like(dx)], -1)
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def write_png(path, rgb01):
    data = (np.clip(rgb01, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)
    raw = b"".join(b"\x00" + data[r].tobytes() for r in range(data.shape[0]))

    def chunk(tag, body):
        return struct.pack(">I", len(body)) + tag + body + struct.pack(">I", zlib.crc32(tag + body) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", data.shape[1], data.shape[0], 8, 2, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    path.write_bytes(png)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    lum, tint, height = wood()
    albedo = lum[:, :, None] * tint
    write_png(OUT / "wood_albedo.png", albedo)
    write_png(OUT / "wood_normal.png", normal_map(height, 0.5))
    print(f"wood: albedo mean {albedo.mean():.3f} sd {lum.std():.3f}")
    print(f"Wrote 2 textures to {OUT}")


main()
sys.exit(0)
