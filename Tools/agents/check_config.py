#!/usr/bin/env python3
"""Checks that Claude Code and OpenCode read the same rules and skills from this repository.

    python3 Tools/agents/check_config.py

Exits 1 and says what to fix if something would make one tool run on a different rulebook. It checks files only.
To see that each tool really loads them, start a session in each (see "Agent harness config" in AGENTS.md).
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PORTABLE = {"name", "description", "license", "compatibility", "metadata", "allowed-tools"}
problems = []


def frontmatter_keys(path):
    lines = path.read_text(encoding="utf-8").splitlines()
    if not lines or lines[0].strip() != "---":
        return None
    keys = []
    for line in lines[1:]:
        if line.strip() == "---":
            return keys
        m = re.match(r"^([A-Za-z][\w-]*):", line)
        if m:
            keys.append(m.group(1))
    return None


# 1. one source of rules
claude = ROOT / "CLAUDE.md"
if not (ROOT / "AGENTS.md").exists():
    problems.append("AGENTS.md is missing: Claude Code reads CLAUDE.md only, and OpenCode would have no rules.")
if not claude.exists() or claude.read_text(encoding="utf-8").strip() != "@AGENTS.md":
    problems.append("CLAUDE.md must contain exactly one line, @AGENTS.md. OpenCode ignores CLAUDE.md when AGENTS.md exists, so rules there are lost.")

# 2. skills: shared folder, portable frontmatter, name matches folder, references exist
if (ROOT / ".opencode" / "skills").exists():
    problems.append(".opencode/skills exists: skills live only in .claude/skills (both tools read it).")
for skill in sorted((ROOT / ".claude" / "skills").glob("*/SKILL.md")):
    keys = frontmatter_keys(skill)
    folder = skill.parent.name
    if keys is None:
        problems.append(f"{skill.relative_to(ROOT)}: no frontmatter.")
        continue
    extra = sorted(set(keys) - PORTABLE)
    if extra:
        problems.append(f"{skill.relative_to(ROOT)}: non-portable frontmatter {extra} (OpenCode ignores them).")
    m = re.search(r"^name:\s*(\S+)", skill.read_text(encoding="utf-8"), re.M)
    if not m or m.group(1) != folder:
        problems.append(f"{skill.relative_to(ROOT)}: name must equal the folder name '{folder}'.")
    text = skill.read_text(encoding="utf-8")
    # "references/x.md" is relative to this skill; "<other-skill>/references/x.md" points into another skill.
    for owner, name in set(re.findall(r"(?:([\w-]+)/)?references/([\w.-]+\.md)", text)):
        target = (ROOT / ".claude" / "skills" / owner if owner else skill.parent) / "references" / name
        if not target.exists():
            problems.append(f"{skill.relative_to(ROOT)}: mentions {(owner + '/') if owner else ''}references/{name}, which does not exist.")

# 3. Claude-only scoped rules must use the key Claude Code recognises
for rule in (ROOT / ".claude" / "rules").glob("**/*.md") if (ROOT / ".claude" / "rules").exists() else []:
    keys = frontmatter_keys(rule) or []
    bad = sorted(set(keys) & {"match", "applyTo", "glob", "globs"})
    if bad:
        problems.append(f"{rule.relative_to(ROOT)}: {bad} is not recognised, use paths:. The rule never loads.")

# 4. MCP servers must be in both tools or neither
mcp_claude = set()
if (ROOT / ".mcp.json").exists():
    mcp_claude = set(json.loads((ROOT / ".mcp.json").read_text()).get("mcpServers", {}))
mcp_opencode = set()
if (ROOT / "opencode.json").exists():
    mcp_opencode = set(json.loads((ROOT / "opencode.json").read_text()).get("mcp", {}))
if mcp_claude != mcp_opencode:
    problems.append(f"MCP servers differ: .mcp.json has {sorted(mcp_claude)}, opencode.json has {sorted(mcp_opencode)}. Translate with the dual-agent-config skill's sync_mcp.py.")

if problems:
    print("Agent config problems:")
    for p in problems:
        print(f"  - {p}")
    sys.exit(1)
print("Agent config ok: one rules file, portable skills, MCP servers in sync.")
