#!/usr/bin/env bash
# Render one music track from its MIDI script through FluidSynth, post-process it and check it.
#
#   Tools/audio/render_music.sh <Prototype> <track>
#
# The track script lives at Tools/audio/<Prototype>/music/<track>.py and writes a MIDI file whose path
# it gets as its first argument. It must print one line `loop_seconds=<seconds>` when the track loops.
# The render is written to Assets/Prototypes/<Prototype>/Audio/music.wav (16-bit stereo 44.1 kHz).
#
# Which SoundFont: the SOUNDFONT_PATH environment variable, or Tools/audio/soundfonts/GeneralUser-GS.sf2.
# Which FluidSynth: `fluidsynth` on PATH. Nothing is installed for you.
#
# Exit codes: 0 all good, 1 a script, render or check failed, 2 bad usage or missing tools.
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT" || exit 2

usage() {
  sed -n '2,7p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//' >&2
  exit 2
}

prototype="${1:-}"
track="${2:-}"
[ -n "$prototype" ] && [ -n "$track" ] && [ "$#" -eq 2 ] || usage
[[ "$prototype" =~ ^[A-Z][A-Za-z0-9]*$ ]] || { echo "Prototype must be PascalCase, got: $prototype" >&2; exit 2; }
[[ "$track" =~ ^[A-Za-z0-9_-]+$ ]] || { echo "Track must be a simple name, got: $track" >&2; exit 2; }

script="Tools/audio/$prototype/music/$track.py"
[ -f "$script" ] || { echo "No such track script: $script" >&2; exit 2; }

command -v fluidsynth >/dev/null 2>&1 || {
  echo "No fluidsynth on PATH. Install it yourself (brew install fluidsynth) and try again." >&2
  exit 2
}

soundfont="${SOUNDFONT_PATH:-Tools/audio/soundfonts/GeneralUser-GS.sf2}"
[ -f "$soundfont" ] || {
  echo "No SoundFont found (tried: $soundfont). Set SOUNDFONT_PATH or put GeneralUser-GS.sf2 in Tools/audio/soundfonts/." >&2
  exit 2
}

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

echo "render_music: $prototype / $track"
echo "  script:    $script"
echo "  soundfont: $soundfont"

python3 -I "$script" "$tmp/track.mid" | tee "$tmp/script.log"
rc="${PIPESTATUS[0]}"
if [ "$rc" -ne 0 ]; then
  echo "render_music: the track script failed (exit $rc)." >&2
  exit 1
fi
[ -s "$tmp/track.mid" ] || { echo "render_music: the track script wrote no MIDI." >&2; exit 1; }

loop="$(sed -n 's/^loop_seconds=//p' "$tmp/script.log" | tail -1)"

fluidsynth -ni -r 44100 -g 0.55 -F "$tmp/render.wav" "$soundfont" "$tmp/track.mid" >/dev/null 2>&1
rc=$?
if [ "$rc" -ne 0 ] || [ ! -s "$tmp/render.wav" ]; then
  echo "render_music: fluidsynth failed (exit $rc) or wrote no audio." >&2
  exit 1
fi

out="Assets/Prototypes/$prototype/Audio/music.wav"
mkdir -p "$(dirname "$out")"
if [ -n "$loop" ]; then
  python3 -I Tools/audio/postprocess_music.py "$tmp/render.wav" "$out" --loop "$loop"
else
  python3 -I Tools/audio/postprocess_music.py "$tmp/render.wav" "$out"
fi
rc=$?
if [ "$rc" -ne 0 ]; then
  echo "render_music: the post-process checks failed (exit $rc)." >&2
  exit 1
fi

[ -s "$out" ] || { echo "render_music: no output at $out." >&2; exit 1; }
echo "render_music: ok -> $out"
