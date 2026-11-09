#!/usr/bin/env bash
# Save a picture of what the game looks like, so a change can be judged on a render.
#
#   Tools/unity/capture.sh camera <out.png> [width height]
#       Render the scene's main camera. Works in Edit Mode. It does NOT apply post-processing
#       (no Bloom, no Vignette), so it is not the way to judge a look.
#   Tools/unity/capture.sh screen <out.png>
#       Capture the Game window, including UI and post-processing. Open the scene you want
#       first. Use this one to judge the look.
#   Tools/unity/capture.sh play <out.png> [seconds]
#       Enter Play Mode, wait, capture the Game window, leave Play Mode (default 5 seconds).
#
# Needs the `unity` CLI on PATH and the Unity Editor open on this project.
# Caveat: while the Editor is in the background, Play Mode may not advance time, so a `play`
# capture can show the first frame. Bring the Editor to the front if you need motion.
# Put output files outside the repository (for example in a temp folder), not in Assets/.

set -u
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT" || exit 2
command -v unity >/dev/null 2>&1 || { echo "Missing tool: unity" >&2; exit 2; }

mode="${1:-}"
out="${2:-}"
if [ -z "$mode" ] || [ -z "$out" ]; then
  sed -n '2,16p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
  exit 2
fi
mkdir -p "$(dirname "$out")"
# An old file at the same path would make a failed capture look like a success.
rm -f "$out"

grab() {
  unity cmd capture game_view "$@" --output-path "$out" --timeout 90 --result-only --no-banner 2>&1 \
    | grep -E 'savedPath|"error"|message' | head -5
  [ -s "$out" ]
}

case "$mode" in
  camera)
    width="${3:-1280}"
    height="${4:-720}"
    grab --source camera --width "$width" --height "$height"
    ;;
  screen)
    grab --source screen
    ;;
  play)
    seconds="${3:-5}"
    unity cmd editor.playmode start --timeout 60 --result-only --no-banner >/dev/null 2>&1
    sleep "$seconds"
    grab --source screen
    rc=$?
    unity cmd editor.playmode stop --timeout 60 --result-only --no-banner >/dev/null 2>&1
    exit $rc
    ;;
  *)
    echo "Unknown mode: $mode" >&2
    exit 2
    ;;
esac
