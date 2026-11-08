#!/usr/bin/env bash
# Compile and test a Unity project that is open in the Editor, through the Unity CLI.
#
#   Tools/unity/check.sh compile     refresh assets, wait for the Editor, recompile scripts
#   Tools/unity/check.sh editmode    compile, then run the EditMode tests
#   Tools/unity/check.sh playmode    compile, then run the PlayMode tests
#   Tools/unity/check.sh all         compile, then both test modes
#
# Needs: the `unity` CLI on PATH, python3, and the Unity Editor open on this project.
# Exit codes: 0 all good, 1 a compile error or a failing test, 2 the Editor or CLI is not usable.
# The same steps are described in AGENTS.md. This script just saves typing them.

set -u
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT" || exit 2

for tool in unity python3; do
  command -v "$tool" >/dev/null 2>&1 || { echo "Missing tool: $tool" >&2; exit 2; }
done

editor_ready() {
  unity status --no-banner 2>&1 | tail -1 | grep -q "ready"
}

wait_for_editor() {
  for _ in $(seq 1 30); do
    editor_ready && return 0
    sleep 4
  done
  echo "The Unity Editor is not ready. Open this project in the Editor first." >&2
  return 1
}

refresh_assets() {
  unity cmd script eval \
    --code 'UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport); return "ok";' \
    --timeout 90 --result-only --no-banner >/dev/null 2>&1
}

compile() {
  wait_for_editor || return 2
  refresh_assets
  local rc=1
  # Right after a reload the Editor can answer "ready" before it accepts commands (exit 7). Retry.
  for _ in 1 2 3; do
    sleep 3
    editor_ready || continue
    unity recompile --project-path "$ROOT" --timeout 180 --no-banner >/tmp/unity-recompile.$$ 2>&1
    rc=$?
    [ "$rc" -eq 0 ] && break
  done
  if [ "$rc" -eq 0 ]; then
    echo "compile: ok"
    rm -f /tmp/unity-recompile.$$
    return 0
  fi
  echo "compile: FAILED (exit $rc)"
  cut -c1-300 /tmp/unity-recompile.$$ | head -20
  rm -f /tmp/unity-recompile.$$
  return 1
}

# Reads the JSON from `test run` or `test status` on stdin, prints a summary, exits 1 on failures.
summarise() {
  python3 -I -c '
import json, sys
raw = sys.stdin.read()
try:
    data = json.loads(raw)
except Exception:
    print("tests: could not read the result:", raw[:300])
    sys.exit(2)
summary = data.get("Summary") or data.get("summary") or {}
results = data.get("Results") or data.get("results") or []
total = summary.get("Total", summary.get("total", 0))
passed = summary.get("Passed", summary.get("passed", 0))
failed = summary.get("Failed", summary.get("failed", 0))
print(f"tests: {passed}/{total} passed, {failed} failed")
if total == 0:
    print("  (no tests found in this mode)")
for r in results:
    if r.get("Status") != "Passed":
        message = (r.get("Message") or "").replace("\n", " ")[:300]
        print("  ", r.get("Status"), r.get("FullName", "").split(".")[-1], "::", message)
sys.exit(1 if failed else 0)
'
}

# How many tests Unity can see in a mode (EditMode or PlayMode). Prints -1 if it can't tell.
count_tests() {
  unity cmd test list --mode "$1" --timeout 90 --result-only --no-banner 2>/dev/null | python3 -I -c '
import json, sys
try:
    print(json.load(sys.stdin).get("Count", -1))
except Exception:
    print(-1)
'
}

run_editmode() {
  echo "--- EditMode"
  local count
  count="$(count_tests EditMode)"
  if [ "$count" = "0" ]; then
    echo "tests: 0/0 (no EditMode tests found)"
    return 0
  fi
  unity cmd test run --mode EditMode --timeout 300 --result-only --no-banner 2>/dev/null | summarise
}

run_playmode() {
  echo "--- PlayMode"
  # With no PlayMode tests no run starts, and `test status` would still show the last old result.
  local count
  count="$(count_tests PlayMode)"
  if [ "$count" = "0" ]; then
    echo "tests: 0/0 (no PlayMode tests found)"
    return 0
  fi
  # PlayMode runs in the background: start it, then poll until the status says completed.
  unity cmd test run --mode PlayMode --async-tests --timeout 60 --result-only --no-banner >/dev/null 2>&1
  local status=""
  for _ in $(seq 1 90); do
    sleep 4
    status="$(unity cmd test status --result-only --no-banner 2>/dev/null | python3 -I -c 'import sys, json
try:
    print(json.load(sys.stdin).get("status", ""))
except Exception:
    print("")')"
    [ "$status" = "completed" ] && break
  done
  if [ "$status" != "completed" ]; then
    echo "tests: PlayMode run did not finish (last status: ${status:-none})"
    return 1
  fi
  unity cmd test status --result-only --no-banner 2>/dev/null | summarise
}

mode="${1:-}"
case "$mode" in
  compile)
    compile; exit $?
    ;;
  editmode)
    compile || exit $?
    run_editmode; exit $?
    ;;
  playmode)
    compile || exit $?
    run_playmode; exit $?
    ;;
  all)
    compile || exit $?
    worst=0
    run_editmode || worst=1
    run_playmode || worst=1
    exit $worst
    ;;
  *)
    sed -n '2,12p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    exit 2
    ;;
esac
