"""Writes the Medieval Chess surface textures: tileable 512 px albedo and normal maps.

Run it headless in Blender (Blender ships numpy, so nothing needs installing):

    /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/textures/MedievalChess/make_textures.py

It writes <name>_albedo.png and <name>_normal.png into Assets/Prototypes/MedievalChess/Art/Textures/.
Never edit those by hand. Change this script and run it again. Everything is original and made from
seeded noise, so the same script always gives the same files.

The albedo maps are "detail" maps: near-neutral colours whose average is about 0.8. The shader multiplies
the material's base colour by them, so the palette in the settings still decides the colour.
Every map tiles: the noise is made in the frequency domain (so it wraps) and the patterns use whole repeats.

  slate      dark squares: strata, fine grain, a few hairline cracks
  limestone  light squares: pores and faint veins
  wood       the frame: planks with grain, ring warp and gaps
  flagstone  the floor: irregular stones with dark mortar
  grain      the fine grain laid over the pieces (bone, cloth, iron and gold all use it lightly)
"""
import struct
import sys
import zlib
from pathlib import Path

import numpy as np

N = 512
OUT = Path(__file__).resolve().parents[3] / "Assets" / "Prototypes" / "MedievalChess" / "Art" / "Textures"
Y, X = np.mgrid[0:N, 0:N] / N  # rows (down the image) and columns, both 0..1


# --- noise that tiles ---

def fractal(slope, seed, stretch=(1.0, 1.0)):
    """Zero-mean, unit-deviation noise with amplitude 1/f^slope. A bigger stretch[1] elongates along x."""
    rng = np.random.default_rng(seed)
    spectrum = np.fft.fft2(rng.standard_normal((N, N)))
    fy = np.fft.fftfreq(N)[:, None] * stretch[0]
    fx = np.fft.fftfreq(N)[None, :] * stretch[1]
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    out = np.real(np.fft.ifft2(spectrum / f ** slope))
    out -= out.mean()
    return out / out.std()


def voronoi(cells, seed, jitter=None):
    """Nearest and second-nearest distance and the nearest cell's index, wrapping at the edges.
    With jitter (0..0.5) the points sit on a grid of cells x cells, nudged, which makes even stones with few thin wedges."""
    rng = np.random.default_rng(seed)
    if jitter is None:
        pts = rng.random((cells, 2))
    else:
        gy, gx = np.mgrid[0:cells, 0:cells]
        pts = np.stack([(gy.ravel() + 0.5 + rng.uniform(-jitter, jitter, cells * cells)) / cells,
                        (gx.ravel() + 0.5 + rng.uniform(-jitter, jitter, cells * cells)) / cells], -1) % 1.0
    dy = np.abs(Y[:, :, None] - pts[None, None, :, 0])
    dx = np.abs(X[:, :, None] - pts[None, None, :, 1])
    dy = np.minimum(dy, 1 - dy)
    dx = np.minimum(dx, 1 - dx)
    d = np.sqrt(dx * dx + dy * dy)
    order = np.argsort(d, axis=2)
    d1 = np.take_along_axis(d, order[:, :, :1], 2)[:, :, 0]
    d2 = np.take_along_axis(d, order[:, :, 1:2], 2)[:, :, 0]
    return d1, d2, order[:, :, 0]


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


# --- the surfaces. Each returns (luminance 0..1, colour tint, height) ---

def slate():
    broad = fractal(1.2, 11)
    strata = fractal(1.6, 12, stretch=(3.0, 0.5))
    fine = fractal(0.2, 13)
    d1, d2, _ = voronoi(9, 14)
    crack = np.exp(-((d2 - d1) / 0.004)) * smoothstep(0.4, 1.2, fractal(1.5, 15))
    lum = 0.80 + 0.07 * broad + 0.05 * strata + 0.03 * fine - 0.30 * crack
    tint = np.stack([0.99 + 0.008 * broad, 1.0 + 0 * broad, 1.01 - 0.006 * broad], -1)
    height = 0.5 * broad + 0.7 * strata + 0.35 * fine - 3.0 * crack
    return lum, tint, height


def limestone():
    broad = fractal(1.1, 21)
    fine = fractal(0.1, 22)
    pores = smoothstep(2.0, 3.0, fine)
    vein = np.exp(-(np.abs(fractal(1.7, 23)) / 0.07) ** 2) * 0.5
    lum = 0.84 + 0.05 * broad + 0.025 * fine - 0.20 * pores - 0.10 * vein
    tint = np.stack([1.0 + 0.006 * broad, 0.995 + 0 * broad, 0.97 - 0.008 * broad], -1)
    height = 0.4 * broad + 0.3 * fine - 2.0 * pores - 0.5 * vein
    return lum, tint, height


def wood():
    planks = 4
    plank_id = np.floor(Y * planks)
    local = Y * planks - plank_id
    rng = np.random.default_rng(31)
    offset = rng.uniform(-0.07, 0.07, planks)[plank_id.astype(int)]
    shift = rng.uniform(0, 1, planks)[plank_id.astype(int)]
    warp = fractal(1.5, 32, stretch=(1.0, 10.0))
    rings = np.sin(2 * np.pi * (6 * local + 0.45 * warp + shift)) * 0.5 + 0.5
    streak = fractal(0.9, 33, stretch=(1.0, 30.0))
    gap = np.exp(-np.minimum(local, 1 - local) / 0.012)
    lum = 0.60 + 0.26 * rings ** 1.3 + 0.04 * streak + offset - 0.42 * gap
    tint = np.stack([1.04 + 0 * lum, 0.97 + 0 * lum, 0.86 + 0 * lum], -1)
    height = 0.6 * rings + 0.25 * streak - 3.0 * gap
    return lum, tint, height


def flagstone():
    d1, d2, cell = voronoi(4, 41, jitter=0.32)
    rng = np.random.default_rng(42)
    tone = rng.uniform(-0.09, 0.09, 16)[cell]
    lift = rng.uniform(-0.2, 0.2, 16)[cell]
    edge = d2 - d1
    mortar = 1 - smoothstep(0.006, 0.02, edge)
    rough = fractal(0.9, 43)
    fine = fractal(0.2, 44)
    lum = 0.78 + tone + 0.05 * rough + 0.03 * fine - 0.38 * mortar
    tint = np.stack([1.0 + 0 * lum, 0.99 + 0 * lum, 0.97 + 0 * lum], -1)
    height = lift + 0.4 * rough + 0.2 * fine - 2.2 * mortar + 1.0 * smoothstep(0.0, 0.05, edge)
    return lum, tint, height


def grain():
    broad = fractal(0.9, 51)
    fine = fractal(0.15, 52)
    pits = smoothstep(2.2, 3.2, fractal(0.05, 53))
    lum = 0.82 + 0.07 * broad + 0.05 * fine - 0.18 * pits
    tint = np.stack([1.0 + 0 * lum, 1.0 + 0 * lum, 1.0 + 0 * lum], -1)
    height = 0.4 * broad + 0.5 * fine - 1.5 * pits
    return lum, tint, height


SURFACES = {"slate": slate, "limestone": limestone, "wood": wood, "flagstone": flagstone, "grain": grain}
SLOPE = {"slate": 0.45, "limestone": 0.35, "wood": 0.5, "flagstone": 0.6, "grain": 0.3}


# --- output ---

def normal_map(height, target_slope):
    """Tangent-space normal (Unity's green-up) from a height field, scaled so the typical slope is target_slope."""
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
    for name, make in SURFACES.items():
        lum, tint, height = make()
        albedo = lum[:, :, None] * tint
        write_png(OUT / f"{name}_albedo.png", albedo)
        write_png(OUT / f"{name}_normal.png", normal_map(height, SLOPE[name]))
        print(f"{name}: albedo mean {albedo.mean():.3f} sd {lum.std():.3f}")
    print(f"Wrote {len(SURFACES) * 2} textures to {OUT}")


main()
sys.exit(0)
