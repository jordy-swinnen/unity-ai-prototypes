"""PixelInvaders background music: "Trench Pulse".

A 24-bar, 48-second seamless synthwave command-trench loop at 120 BPM in D minor (matching the D-minor
4-note step heartbeat so SFX and music stay in key). Rendered by:

    Tools/audio/render_music.sh PixelInvaders trench_pulse
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from midi import Song  # noqa: E402

BPM = 120
BEATS_PER_BAR = 4
BARS = 24
LOOP_SECONDS = BARS * BEATS_PER_BAR * 60.0 / BPM

SYNTH_BASS = 38    # Synth Bass 1
EPIANO = 5         # Electric Piano 2
WARM_PAD = 89      # Pad 2 (warm)

P = {
    "G1": 31, "A1": 33, "Bb1": 34, "C2": 36, "D2": 38, "F2": 41,
    "G2": 43, "A2": 45, "Bb2": 46, "C3": 48, "D3": 50, "F3": 53,
    "A3": 57, "Bb3": 58, "C4": 60, "D4": 62, "E4": 64, "F4": 65, "G4": 67, "A4": 69,
    "C5": 72, "D5": 74, "E5": 76, "F5": 77, "G5": 79, "A5": 81,
}

# 8-bar D-minor command-trench progression repeated 3 times (24 bars = 48 s at 120 BPM)
PROGRESSION = [
    ("D2", "D3", ["D4", "F4", "A4", "D5"]),   # Dm
    ("D2", "D3", ["D4", "F4", "A4", "C5"]),   # Dm7
    ("Bb1", "Bb2", ["D4", "F4", "Bb3", "D5"]),# Bbmaj7
    ("C2", "C3", ["C4", "E4", "G4", "C5"]),   # C
    ("G1", "G2", ["Bb3", "D4", "G4", "D5"]),  # Gm7
    ("Bb1", "Bb2", ["D4", "F4", "A4", "D5"]), # Bbmaj7
    ("C2", "C3", ["C4", "E4", "G4", "C5"]),   # C
    ("A1", "A2", ["A3", "E4", "G4", "C5"]),   # Am7 -> resolves to Dm
] * 3

ARP = [0, 1, 2, 3, 2, 1, 2, 3]


def main():
    midi_path = sys.argv[1] if len(sys.argv) > 1 else "trench_pulse.mid"
    song = Song(bpm=BPM, beats_per_bar=BEATS_PER_BAR, name="PixelInvaders Trench Pulse")

    song.program(0, EPIANO)
    song.program(1, SYNTH_BASS)
    song.program(2, WARM_PAD)

    for bar, (low, high, tones) in enumerate(PROGRESSION):
        bar_start = bar * BEATS_PER_BAR

        # Steady eighth-note octave synth bass
        for step in range(8):
            pitch = P[low] if step % 2 == 0 else P[high]
            vel = 58 if step % 2 == 0 else 48
            song.note(1, pitch, bar_start + step * 0.5, 0.40, vel)

        # Crisp tactical arpeggio
        lift = 12 if (bar % 8) >= 4 and (bar % 2 == 1) else 0
        for step, idx in enumerate(ARP):
            vel = 56 if step % 2 == 0 else 48
            song.note(0, min(90, P[tones[idx]] + lift), bar_start + step * 0.5, 0.40, vel)

        # Warm command-trench pad chord
        for tone in tones[:3]:
            song.note(2, P[tone], bar_start, 3.85, 40)

    song.write(midi_path)
    print(f"wrote {midi_path}")
    print(f"loop_seconds={LOOP_SECONDS:.6f}")


if __name__ == "__main__":
    main()
