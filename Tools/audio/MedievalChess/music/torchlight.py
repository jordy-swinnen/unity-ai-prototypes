"""Medieval Chess music: "Torchlight Hall".

A 16-bar, 64-second seamless loop in D Dorian (D E F G A B C) at 60 BPM: a lute-like nylon guitar
arpeggio, a sparse harp melody, and a low cello drone. Rendered by:

    Tools/audio/render_music.sh MedievalChess torchlight
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from midi import Song  # noqa: E402

BPM = 60
BEATS_PER_BAR = 4
BARS = 16
LOOP_SECONDS = BARS * BEATS_PER_BAR * 60.0 / BPM

LUTE = 24     # Acoustic Guitar (nylon)
HARP = 46     # Orchestral Harp
CELLO = 42    # Cello

# D Dorian pitches
P = {
    "D2": 38, "F2": 41, "G2": 43, "A2": 45, "C3": 48,
    "D3": 50, "E3": 52, "F3": 53, "G3": 55, "A3": 57, "B3": 59, "C4": 60,
    "D4": 62, "E4": 64, "F4": 65, "G4": 67, "A4": 69, "B4": 71, "C5": 72,
    "D5": 74, "E5": 76, "F5": 77, "G5": 79, "A5": 81,
}

# 8-bar Dorian progression, repeated twice for 16 bars (64 s).
CHORDS = [
    ("D2", ["D3", "A3", "D4", "F4"]),   # Dm
    ("C3", ["C3", "G3", "C4", "E4"]),   # C
    ("G2", ["G2", "D3", "G3", "B3"]),   # G (Dorian IV)
    ("D2", ["D3", "A3", "D4", "F4"]),   # Dm
    ("F2", ["F2", "C3", "A3", "C4"]),   # F
    ("C3", ["C3", "G3", "C4", "E4"]),   # C
    ("A2", ["A2", "E3", "A3", "C4"]),   # Am
    ("D2", ["D3", "A3", "D4", "E4"]),   # Dsus2 -> loops cleanly to Dm
] * 2

ARP = [0, 1, 2, 3, 2, 1, 2, 0]

# Sparse harp motif across 8 bars (repeated with a higher second half).
MELODY_A = [
    (0, 0.0, "A4", 1.5, 68), (0, 2.0, "F4", 1.0, 64), (0, 3.0, "D4", 1.0, 62),
    (1, 0.0, "E4", 2.0, 66), (1, 2.5, "G4", 1.5, 65),
    (2, 0.0, "B4", 1.5, 70), (2, 2.0, "A4", 1.0, 66), (2, 3.0, "G4", 1.0, 64),
    (3, 0.0, "A4", 3.5, 68),
    (4, 0.0, "C5", 1.5, 72), (4, 2.0, "A4", 1.0, 68), (4, 3.0, "F4", 1.0, 64),
    (5, 0.0, "G4", 2.0, 68), (5, 2.5, "E4", 1.5, 64),
    (6, 0.0, "E4", 1.5, 65), (6, 2.0, "F4", 1.0, 66), (6, 3.0, "E4", 1.0, 63),
    (7, 0.0, "D4", 3.5, 68),
]

MELODY_B = [
    (8, 0.0, "D5", 1.5, 72), (8, 2.0, "A4", 1.0, 68), (8, 3.0, "F4", 1.0, 65),
    (9, 0.0, "G4", 2.0, 68), (9, 2.5, "C5", 1.5, 70),
    (10, 0.0, "B4", 1.5, 72), (10, 2.0, "D5", 1.0, 74), (10, 3.0, "B4", 1.0, 68),
    (11, 0.0, "A4", 3.5, 70),
    (12, 0.0, "F5", 1.5, 74), (12, 2.0, "C5", 1.0, 70), (12, 3.0, "A4", 1.0, 66),
    (13, 0.0, "C5", 2.0, 70), (13, 2.5, "G4", 1.5, 66),
    (14, 0.0, "A4", 1.5, 68), (14, 2.0, "G4", 1.0, 65), (14, 3.0, "E4", 1.0, 64),
    (15, 0.0, "D4", 3.5, 70),
]


def main():
    midi_path = sys.argv[1] if len(sys.argv) > 1 else "torchlight.mid"
    song = Song(bpm=BPM, beats_per_bar=BEATS_PER_BAR, name="MedievalChess Torchlight Hall")

    song.program(0, LUTE)
    song.program(1, HARP)
    song.program(2, CELLO)

    for bar, (bass, tones) in enumerate(CHORDS):
        bar_start = bar * BEATS_PER_BAR

        # Lute eighth-note fingerpicking.
        for step, tone_index in enumerate(ARP):
            vel = 58 if step % 2 == 0 else 50
            song.note(0, P[tones[tone_index]], bar_start + step * 0.5, 0.46, vel)

        # Low cello drone on the root and fifth.
        song.note(2, P[bass], bar_start, 3.85, 44)
        song.note(2, P[bass] + 7, bar_start, 3.85, 38)

    for bar, beat, name, length, vel in MELODY_A + MELODY_B:
        song.note(1, P[name], bar * BEATS_PER_BAR + beat, length * 0.92, vel)

    song.write(midi_path)
    print(f"wrote {midi_path}")
    print(f"loop_seconds={LOOP_SECONDS:.6f}")


if __name__ == "__main__":
    main()
