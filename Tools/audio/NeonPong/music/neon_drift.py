"""Neon Pong music: "Neon Drift".

A 32-bar, 64-second seamless loop at 120 BPM in A minor: a steady synth bass pulse, a bright electric
piano arpeggio and a warm synth pad. Rendered by:

    Tools/audio/render_music.sh NeonPong neon_drift
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from midi import Song  # noqa: E402

BPM = 120
BEATS_PER_BAR = 4
BARS = 32
LOOP_SECONDS = BARS * BEATS_PER_BAR * 60.0 / BPM

SYNTH_BASS = 38    # Synth Bass 1
EPIANO = 5         # Electric Piano 2
WARM_PAD = 89      # Pad 2 (warm)

P = {
    "F1": 29, "G1": 31, "A1": 33, "C2": 36, "D2": 38, "E2": 40,
    "F2": 41, "G2": 43, "A2": 45, "C3": 48, "D3": 50, "E3": 52,
    "A3": 57, "B3": 59, "C4": 60, "D4": 62, "E4": 64, "G4": 67,
    "A4": 69, "B4": 71, "C5": 72, "D5": 74, "E5": 76, "G5": 79, "A5": 81,
}

# 8-bar progression repeated 4 times (32 bars = 64 s at 120 BPM).
PROGRESSION = [
    ("A1", "A2", ["A3", "C4", "E4", "A4"]),  # Am
    ("A1", "A2", ["A3", "C4", "E4", "G4"]),  # Am7
    ("F1", "F2", ["A3", "C4", "E4", "A4"]),  # Fmaj7
    ("G1", "G2", ["B3", "D4", "G4", "B4"]),  # G
    ("D2", "D3", ["A3", "D4", "E4", "A4"]),  # Dsus2
    ("F1", "F2", ["A3", "C4", "E4", "G4"]),  # Fmaj7
    ("C2", "C3", ["G4", "C4", "E4", "G4"]),  # C
    ("E2", "E3", ["B3", "D4", "E4", "G4"]),  # Em7 -> back to Am
] * 4

ARP = [0, 1, 2, 3, 2, 1, 2, 3]


def main():
    midi_path = sys.argv[1] if len(sys.argv) > 1 else "neon_drift.mid"
    song = Song(bpm=BPM, beats_per_bar=BEATS_PER_BAR, name="NeonPong Neon Drift")

    song.program(0, EPIANO)
    song.program(1, SYNTH_BASS)
    song.program(2, WARM_PAD)

    for bar, (low, high, tones) in enumerate(PROGRESSION):
        bar_start = bar * BEATS_PER_BAR

        # Eighth-note octave synth bass pulse.
        for step in range(8):
            pitch = P[low] if step % 2 == 0 else P[high]
            vel = 62 if step % 2 == 0 else 52
            song.note(1, pitch, bar_start + step * 0.5, 0.4, vel)

        # Bright arpeggio, lifting an octave in the second half of each 16-bar phrase.
        lift = 12 if (bar % 16) >= 8 and (bar % 2 == 1) else 0
        for step, idx in enumerate(ARP):
            vel = 60 if step % 2 == 0 else 52
            song.note(0, min(92, P[tones[idx]] + lift), bar_start + step * 0.5, 0.42, vel)

        # Warm pad chord across the bar.
        for tone in tones[:3]:
            song.note(2, P[tone], bar_start, 3.85, 42)

    song.write(midi_path)
    print(f"wrote {midi_path}")
    print(f"loop_seconds={LOOP_SECONDS:.6f}")


if __name__ == "__main__":
    main()
