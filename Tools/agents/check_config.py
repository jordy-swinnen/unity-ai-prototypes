#!/usr/bin/env python3
"""Run the shared dual-agent config check for this repository.

The checker itself lives in the shared APM package, so this repository does not
carry a second, drifting copy. Restore it with:

    apm install
"""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHECK = ROOT / ".claude" / "skills" / "dual-agent-config" / "scripts" / "check_config.py"

if not CHECK.exists():
    print(f"Missing {CHECK.relative_to(ROOT)}. Install the shared skills with:")
    print("  apm install")
    sys.exit(1)

sys.exit(subprocess.call([sys.executable, str(CHECK), "--strict-portable", "--root", str(ROOT)]))
