# Agent rules: unity-ai-prototypes

## Pinned versions
- Unity Editor: 6000.6.8f1 (chosen by Jordy; LTS status not verified; from `unity editors -i`)
- Blender: 5.2.3 LTS (from `blender --version`)
- Blender binary on macOS: `/Applications/Blender.app/Contents/MacOS/Blender` (`blender` is not on PATH)

## Layout
- `Assets/`: everything Unity compiles and imports. Tests live in `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`. Unity only compiles test assemblies inside Assets or a package.
- `Assets/Art/Exports/`: `.glb` files written by Blender scripts. Never edit by hand.
- `Assets/Editor/`: editor menu commands, including scene building.
- `Art/source/`: `.blend` source files (Git LFS). Unity doesn't import these.
- `Tools/blender/`: Python scripts for Blender, run headless with `blender -b -P <script>`.
- `docs/prototypes/`: one spec per mechanic. Read the spec before writing code.
- `docs/research/`: background only.

## Rules
- Logic lives in C#. Tuning values live in ScriptableObjects.
- Never hand-edit `.unity` or `.prefab` files. Build scenes in code through a menu command under `Assets/Editor/`.
- Never hand-edit files under `Assets/Art/Exports/`. Change the Blender script and re-run it.
- After every C# change, run the compile check and fix errors before continuing.
- Every mechanic gets an EditMode test for its logic.
- For multi-file changes, plan first (files and tests), then wait for approval.
- Ask before deleting files, changing `ProjectSettings/`, or adding packages.
- Commit only when the compile check and tests pass. Use conventional commit subjects.

## Agent harness config (Claude Code + OpenCode)
Follow the `dual-agent-config` skill whenever you touch agent config.
- `AGENTS.md` is the only source of rules. `CLAUDE.md` stays one line: `@AGENTS.md`. Never put rules in `CLAUDE.md`.
- Skills live in `.claude/skills/<name>/`. Both tools read that folder, so never copy skills into `.opencode/`. Keep skill frontmatter to the portable fields.
- Path-scoped rules (`.claude/rules/*.md` with `paths:`) work in Claude Code only. OpenCode has no equivalent, so don't rely on them for anything both tools must follow.
- Permissions, hooks and model choices differ between the tools. Write each in its own file (`.claude/settings.json`, `opencode.json`) and don't try to translate one into the other.
- MCP servers need an entry in both `.mcp.json` and `opencode.json`. Translate them with the skill's `sync_mcp.py`. The `unity-cli` skill replaces a Unity MCP server, so none is configured.
- After any change, check that both tools load `AGENTS.md`.

## Checks
- Compile check: `unity recompile --project-path .` (needs the Editor open on this project; confirmed from `--help`, not yet run)
- Tests: `unity test . --mode EditMode --output Logs/test-results.xml` (use `--mode PlayMode` for PlayMode; confirmed from `--help`, not yet run)
- The `unity-cli` skill is installed in `.claude/skills/unity-cli/` and is read by both Claude Code and OpenCode.
