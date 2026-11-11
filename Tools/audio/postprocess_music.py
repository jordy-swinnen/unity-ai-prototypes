"""Post-process a FluidSynth render into a game music clip, and check it.

    python3 -I Tools/audio/postprocess_music.py render.wav out.wav [--loop SECONDS]

What it does:
  - trims leading silence;
  - for a looping track, wraps the reverb tail (everything after the musical loop end) around to the
    start, so the seam has no click and nothing is cut;
  - normalizes the peak to 0.8 and reports the RMS loudness;
  - writes 16-bit stereo 44.1 kHz WAV and prints the checks.

Exit code 1 when the clip is empty, too quiet, clips, or the loop seam clicks.
"""
import argparse
import math
import struct
import sys
import wave

PEAK = 0.8
MIN_SECONDS = 8.0
MAX_SECONDS = 130.0
SILENCE = 0.002       # amplitude below which a sample counts as silence
SEAM_LIMIT = 0.02     # first vs last sample jump that counts as a click
RMS_TARGET = -18.0    # dBFS, music under the effects
RMS_TOLERANCE = 4.0


def read(path):
    with wave.open(path, "rb") as w:
        channels, width, rate, frames = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        raw = w.readframes(frames)
    assert width == 2, "expected 16-bit audio"
    data = struct.unpack("<%dh" % (len(raw) // 2), raw)
    frames_list = [data[i:i + channels] for i in range(0, len(data), channels)]
    return frames_list, channels, rate


def write(path, frames, channels, rate):
    flat = [v for frame in frames for v in frame]
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(b"".join(struct.pack("<h", max(-32768, min(32767, int(round(v))))) for v in flat))


def first_loud(frames):
    for i, frame in enumerate(frames):
        if max(abs(v) for v in frame) / 32768.0 > SILENCE:
            return i
    return 0


def rms_db(frames):
    if not frames:
        return -120.0
    total = sum(v * v for frame in frames for v in frame) / (len(frames) * len(frames[0]))
    return 10.0 * math.log10(max(total / (32768.0 * 32768.0), 1e-12))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source")
    parser.add_argument("target")
    parser.add_argument("--loop", type=float, default=None, help="musical loop length in seconds")
    args = parser.parse_args()

    frames, channels, rate = read(args.source)
    start = first_loud(frames)
    frames = frames[start:]
    if not frames:
        print("postprocess: the render is silent", file=sys.stderr)
        return 1

    loop_frames = int(round(args.loop * rate)) if args.loop else None
    if loop_frames:
        if loop_frames >= len(frames):
            print(f"postprocess: loop end {args.loop:.2f}s is not before the render end {len(frames) / rate:.2f}s", file=sys.stderr)
            return 1
        tail = frames[loop_frames:]
        body = frames[:loop_frames]
        wrapped = []
        for i in range(len(body)):
            v = list(body[i])
            if i < len(tail):
                v = [a + b for a, b in zip(v, tail[i])]
            wrapped.append(v)
        frames = wrapped

    top = max(max(abs(v) for v in frame) for frame in frames) / 32768.0
    if top <= 0:
        print("postprocess: the render is silent", file=sys.stderr)
        return 1
    gain = PEAK / top
    frames = [[v * gain for v in frame] for frame in frames]

    seconds = len(frames) / rate
    level = rms_db(frames)
    first = max(abs(v) for v in frames[0]) / 32768.0
    last = max(abs(v) for v in frames[-1]) / 32768.0
    seam = abs(first - last)

    write(args.target, frames, channels, rate)

    problems = []
    if seconds < MIN_SECONDS:
        problems.append(f"{seconds:.2f}s is shorter than {MIN_SECONDS:.0f}s")
    if seconds > MAX_SECONDS:
        problems.append(f"{seconds:.2f}s is longer than {MAX_SECONDS:.0f}s")
    if abs(level - RMS_TARGET) > RMS_TOLERANCE:
        problems.append(f"RMS {level:.1f} dBFS is outside {RMS_TARGET:.0f} +- {RMS_TOLERANCE:.0f} dB")
    if args.loop and seam > SEAM_LIMIT:
        problems.append(f"loop seam jump {seam:.3f} is above {SEAM_LIMIT}")
    if top * gain > 1.0:
        problems.append("clips")

    print(f"postprocess: {args.target}")
    print(f"  duration {seconds:.2f} s, {channels} ch, {rate} Hz")
    print(f"  peak {PEAK:.2f}, RMS {level:.1f} dBFS")
    print(f"  loop {'%.2f s (tail wrapped)' % args.loop if args.loop else 'no'}, seam jump {seam:.3f}")
    for p in problems:
        print(f"  PROBLEM: {p}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
