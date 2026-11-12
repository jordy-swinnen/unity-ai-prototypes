"""Writes the PixelInvaders brushed-metal deck textures: tileable 512 px albedo and normal maps.

Run headless in Blender (which ships numpy):
    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/textures/PixelInvaders/make_textures.py

Writes metal_deck_albedo.png and metal_deck_normal.png into Assets/Prototypes/PixelInvaders/Art/Textures/.
Never edit those by hand. Made from seeded frequency-domain noise and whole-number panel repeats so the
textures wrap seamlessly across all edges.
"""
import struct
import sys
import zlib
from pathlib import Path

import numpy as np

N = 512
OUT = Path(__file__).resolve().parents[3] / "Assets" / "Prototypes" / "PixelInvaders" / "Art" / "Textures"
Y, X = np.mgrid[0:N, 0:N] / N


def fractal(slope, seed, stretch=(1.0, 1.0)):
    """Zero-mean, unit-deviation periodic noise with amplitude 1/f^slope."""
    rng = np.random.default_rng(seed)
    spectrum = np.fft.fft2(rng.standard_normal((N, N)))
    fy = np.fft.fftfreq(N)[:, None] * stretch[0]
    fx = np.fft.fftfreq(N)[None, :] * stretch[1]
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    out = np.real(np.fft.ifft2(spectrum / (f ** slope)))
    out -= out.mean()
    return out / out.std()


def brushed_metal():
    """Anisotropic brushed steel with subtle bevelled hull plate seams."""
    # Fine horizontal brushed grain + subtle cross micro-scratches
    brush_x = fractal(1.15, 201, stretch=(1.0, 18.0))
    brush_y = fractal(1.35, 202, stretch=(10.0, 1.0))
    mottle = fractal(1.75, 203, stretch=(1.0, 1.0))

    # Periodic hull plates (4 x 4 whole-number repeats, offset from wrap edge so border is interior)
    u = (X * 4.0 + 0.5) % 1.0
    v = (Y * 4.0 + 0.5) % 1.0
    edge_dist = np.minimum(np.minimum(u, 1.0 - u), np.minimum(v, 1.0 - v))
    seam = np.exp(-edge_dist / 0.018)

    lum = 0.86 + 0.045 * brush_x + 0.020 * brush_y + 0.030 * mottle - 0.14 * seam
    lum = np.clip(lum, 0.55, 1.0)
    tint = np.stack([0.96 * np.ones_like(lum), 0.99 * np.ones_like(lum), 1.04 * np.ones_like(lum)], -1)
    height = 0.35 * brush_x + 0.15 * brush_y + 0.25 * mottle - 1.15 * seam
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
    lum, tint, height = brushed_metal()
    albedo = np.clip(lum[:, :, None] * tint, 0.0, 1.0)
    write_png(OUT / "metal_deck_albedo.png", albedo)
    write_png(OUT / "metal_deck_normal.png", normal_map(height, 0.35))
    print(f"metal_deck: albedo mean {albedo.mean():.3f} sd {lum.std():.3f}")
    print(f"Wrote 2 textures to {OUT}")


main()
sys.exit(0)
