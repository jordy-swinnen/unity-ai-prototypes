#!/usr/bin/env bash
# Run an Aseprite Lua script headless and check that it really wrote its files.
#
#   Tools/aseprite/run.sh <Prototype> <script.lua>
#
# Runs Tools/aseprite/<Prototype>/<script.lua> with `aseprite --batch`. The script gets two parameters,
# read in Lua as app.params["out_dir"] and app.params["src_dir"]:
#   out_dir  Assets/Prototypes/<Prototype>/Art/Sprites   PNGs and sprite sheets (Unity imports these)
#   src_dir  Art/source/<Prototype>                      .aseprite source files (Git LFS, Unity ignores them)
# Both folders are created if they are missing.
#
# Which Aseprite: the ASEPRITE_PATH environment variable (the aseprite executable itself, for example
#   "$HOME/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite"
# or /home/<you>/aseprite/build/bin/aseprite). If it is unset, `aseprite` on PATH is used.
# This script never installs anything. If no working Aseprite is found it stops with exit code 2.
# The trial version cannot save or export, so it fails the file check below.
#
# The script must print one line per file it writes:  print("wrote " .. path)
# Aseprite exits 255 on a Lua error, but exits 0 when a script does nothing, silently ignores a nil colour
# and does not notice a failed save. So this wrapper also fails unless at least one `wrote` line was printed
# and every file named in one exists, is not empty, is new, and sits inside out_dir or src_dir.
#
# Exit codes: 0 all good, 1 Aseprite or the script failed or a file is missing, 2 bad usage or no usable Aseprite.

set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT" || exit 2

usage() {
  sed -n '2,6p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//' >&2
  exit 2
}

prototype="${1:-}"
script_name="${2:-}"
[ -n "$prototype" ] && [ -n "$script_name" ] && [ "$#" -eq 2 ] || usage
[[ "$prototype" =~ ^[A-Z][A-Za-z0-9]*$ ]] || { echo "Prototype must be PascalCase (for example MedievalChess), got: $prototype" >&2; exit 2; }
[[ "$script_name" =~ ^[A-Za-z0-9_-]+\.lua$ ]] || { echo "Script must be a .lua file name without folders (for example make_icons.lua), got: $script_name" >&2; exit 2; }
script="Tools/aseprite/$prototype/$script_name"
[ -f "$script" ] || { echo "No such script: $script" >&2; exit 2; }

aseprite="${ASEPRITE_PATH:-aseprite}"
if ! command -v "$aseprite" >/dev/null 2>&1 || ! "$aseprite" --version >/dev/null 2>&1; then
  echo "No working Aseprite found (tried: $aseprite)." >&2
  echo "Set ASEPRITE_PATH to the aseprite executable, or put aseprite on PATH. Nothing is installed for you." >&2
  echo "Agents: stop here and ask the user for the path." >&2
  exit 2
fi

out_dir="Assets/Prototypes/$prototype/Art/Sprites"
src_dir="Art/source/$prototype"
mkdir -p "$out_dir" "$src_dir"

log="$(mktemp)"
marker="$(mktemp)"
trap 'rm -f "$log" "$marker"' EXIT

"$aseprite" --batch \
  --script-param out_dir="$out_dir" \
  --script-param src_dir="$src_dir" \
  --script "$script" 2>&1 | tee "$log"
rc="${PIPESTATUS[0]}"

if [ "$rc" -ne 0 ]; then
  echo "run.sh: FAILED, Aseprite exited with $rc (a Lua error prints above)." >&2
  exit 1
fi

wrote=0
bad=0
while IFS= read -r path; do
  wrote=$((wrote + 1))
  case "$path" in
    "$out_dir"/*|"$src_dir"/*) ;;
    *) echo "run.sh: $path is outside $out_dir and $src_dir." >&2; bad=1; continue ;;
  esac
  if [ ! -s "$path" ]; then
    echo "run.sh: $path is missing or empty." >&2; bad=1
  elif [ "$marker" -nt "$path" ]; then
    echo "run.sh: $path was not written by this run (it is older than the start of the run)." >&2; bad=1
  fi
done < <(sed -n 's/^wrote //p' "$log")

if [ "$wrote" -eq 0 ]; then
  echo "run.sh: FAILED, the script printed no 'wrote <path>' line, so there is nothing to check." >&2
  exit 1
fi
[ "$bad" -eq 0 ] || { echo "run.sh: FAILED, see the files above." >&2; exit 1; }
echo "run.sh: ok, $wrote file(s) written for $prototype."
