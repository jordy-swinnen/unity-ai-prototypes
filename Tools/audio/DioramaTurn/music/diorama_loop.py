"""DioramaTurn music: "Toy Box Loop".

An eight-bar, 22.9-second seamless loop for the toy-box diorama: a marimba arpeggio, a music-box
melody and a soft low marimba, all on one C major pentatonic scale. Rendered by
Tools/audio/render_music.sh DioramaTurn diorama_loop.

Run on its own to write just the MIDI:
    python3 -I Tools/audio/DioramaTurn/music/diorama_loop.py out.mid
It prints `loop_seconds=<n>`; the wrapper wraps the reverb tail around that point.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from midi import Song  # noqa: E402

BPM = 84
BEATS_PER_BAR = 4
BARS = 8
LOOP_SECONDS = BARS * BEATS_PER_BAR * 60.0 / BPM

MARIMBA = 12
MUSIC_BOX = 10

# C major pentatonic: C D E G A, from C3 up.
P = {name: pitch for name, pitch in {
    "C3": 48, "A2": 45, "F2": 41, "G2": 43, "B3": 59,
    "C4": 60, "D4": 62, "E4": 64, "G4": 67, "A4": 69,
    "C5": 72, "D5": 74, "E5": 76, "G5": 79, "A5": 81,
    "C6": 84,
}.items()}

# One chord per bar: (bass, arpeggio tones from low to high).
CHORDS = [
    ("C3", ["C4", "E4", "G4", "C5"]),
    ("A2", ["A2", "C4", "E4", "A4"]),
    ("F2", ["F2", "A2", "C4", "F2"]),
    ("G2", ["G2", "B3", "D4", "G4"]),
    ("C3", ["C4", "E4", "G4", "C5"]),
    ("A2", ["A2", "C4", "E4", "A4"]),
    ("F2", ["F2", "A2", "C4", "F2"]),
    ("G2", ["G2", "B3", "D4", "G4"]),
]

# A shape for the arpeggio: indexes into the four chord tones, repeated through the bar.
ARP = [0, 2, 3, 2, 1, 2, 3, 2]

# The music-box melody: (bar, beat_in_bar, note, length_beats, velocity).
MELODY = [
    (0, 0.0, "E5", 1.0, 76), (0, 1.0, "G5", 1.0, 74), (0, 2.0, "A5", 1.0, 78), (0, 3.0, "G5", 1.0, 72),
    (1, 0.0, "E5", 2.0, 76), (1, 2.0, "D5", 1.0, 72), (1, 3.0, "C5", 1.0, 74),
    (2, 0.0, "A4", 1.0, 70), (2, 1.0, "C5", 1.0, 72), (2, 2.0, "D5", 1.0, 74), (2, 3.0, "E5", 1.0, 76),
    (3, 0.0, "D5", 2.0, 74), (3, 2.0, "C5", 1.0, 72), (3, 3.0, "B3", 1.0, 68),
    (4, 0.0, "E5", 1.0, 78), (4, 1.0, "G5", 1.0, 76), (4, 2.0, "A5", 1.0, 80), (4, 3.0, "C6", 1.0, 82),
    (5, 0.0, "A5", 2.0, 78), (5, 2.0, "G5", 1.0, 74), (5, 3.0, "E5", 1.0, 72),
    (6, 0.0, "D5", 1.0, 72), (6, 1.0, "E5", 1.0, 74), (6, 2.0, "G5", 1.0, 76), (6, 3.0, "A5", 1.0, 78),
    (7, 0.0, "G5", 2.0, 78), (7, 2.0, "E5", 1.0, 74), (7, 3.0, "D5", 1.0, 72),
]


def main():
    midi_path = sys.argv[1] if len(sys.argv) > 1 else "diorama_loop.mid"
    song = Song(bpm=BPM, beats_per_bar=BEATS_PER_BAR, name="DioramaTurn Toy Box Loop")

    song.program(0, MARIMBA)   # arpeggio
    song.program(1, MUSIC_BOX) # melody
    song.program(2, MARIMBA)   # bass

    for bar, (bass, tones) in enumerate(CHORDS):
        bar_start = bar * BEATS_PER_BAR

        # Marimba arpeggio: eighth notes.
        for step, tone_index in enumerate(ARP):
            pitch = P[tones[tone_index]] + 12 if step % 4 == 3 else P[tones[tone_index]]
            velocity = 54 if step % 2 == 0 else 48
            song.note(0, min(96, pitch), bar_start + step * 0.5, 0.42, velocity)

        # Soft low marimba: one note per bar.
        song.note(2, P[bass], bar_start, 3.8, 46)

    for bar, beat, name, length, velocity in MELODY:
        song.note(1, P[name], bar * BEATS_PER_BAR + beat, length * 0.92, velocity)

    song.write(midi_path)
    print(f"wrote {midi_path}")
    print(f"loop_seconds={LOOP_SECONDS:.6f}")


if __name__ == "__main__":
    main()
