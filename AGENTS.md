# Agent rules: unity-ai-prototypes

An AI-native starting point for Unity games. Fork it or use it as a template, then edit this file for your project.

## Project
- Defaults (change them in your fork): mostly 3D with some 2D, the Universal 3D (URP) template, plain conventional commits (no `Refs:` trailer).
- The repository holds two example prototypes, Neon Pong and Medieval Chess. Games and features live in their own folders under `Assets/Prototypes/`.

## Workflow: use the skills
Games and features go through four skills in `.claude/skills/`. Each ends with a stop for the user's review, and the next one starts only after approval.
1. `game-spec`: idea or feature to a spec in `docs/prototypes/<kebab-name>.md` (pillars, researched look-and-feel brief, feel table, mechanics, criteria, tests).
2. `game-plan`: approved spec to a staged plan in `docs/prototypes/<kebab-name>-plan.md`.
3. `game-build`: build the plan one stage at a time, tests first, with compile, test and render checks.
4. `game-polish`: apply the look brief and feel table to a working game, in small steps judged on renders.
- Don't write code for a game that has no approved spec and plan. Small fixes to existing code don't need them.
- You can check numbers, tests and renders. You can't judge how a game feels. Say what you could not check, and leave the play test to the user.

## Pinned versions
- Unity Editor: 6000.6.8f1 (not an LTS release; the version Unity Hub recommended; from `unity editors -i`)
- Blender: 5.2.3 LTS (from `blender --version`)
- Blender binary on macOS: `/Applications/Blender.app/Contents/MacOS/Blender` (`blender` is not on PATH)

## Layout
Many people add prototypes here. Each prototype is self-contained, named in PascalCase (`<Name>`) and lives in its own folder.

- `Assets/Prototypes/<Name>/`: one prototype. Unity only compiles test assemblies inside Assets or a package, so tests live here too.
  - `Scripts/`: runtime code, assembly `<Name>`, namespace `Prototypes.<Name>`.
  - `Editor/`: editor code, assembly `<Name>.Editor`. Holds the menu command `Prototypes/<Name>/Build Scene`.
  - `Tests/EditMode/` and `Tests/PlayMode/`: assemblies `<Name>.Tests.EditMode` and `<Name>.Tests.PlayMode`.
  - `Art/Exports/`: `.glb` files written by Blender scripts. Never edit by hand.
  - `Audio/`: sounds written by scripts.
  - `Scenes/`: scenes written by the build command. Never edit by hand.
  - `Settings/`: ScriptableObjects with tuning values.
- `Assets/Shared/`: code and assets used by two or more prototypes, in its own assembly. Prototypes may reference Shared. Today it holds `Feel/ShakeState` and `Feel/TimeFeel` (camera shake and hit-stop), used by Neon Pong and Medieval Chess. Shared never references a prototype, and prototypes never reference each other. Move something to Shared only when a second prototype needs it.
- `Art/source/<Name>/`: `.blend` source files (Git LFS). Unity doesn't import these.
- `Tools/blender/<Name>/`: Python scripts for Blender, run headless with `blender -b -P <script>`. They write into `Assets/Prototypes/<Name>/Art/Exports/`.
- `Tools/audio/<Name>/`: Python scripts that write sounds.
- `Tools/unity/`: `check.sh` (compile and tests) and `capture.sh` (renders).
- `docs/prototypes/<kebab-name>.md`: one spec per game or feature, from `TEMPLATE.md`. Read the spec before writing code.
- `docs/research/`: background and sourced research notes.

## Rules
- Logic lives in C#. Tuning values live in ScriptableObjects.
- Never hand-edit `.unity` or `.prefab` files. Build scenes in code through the prototype's menu command under `Assets/Prototypes/<Name>/Editor/`.
- Never hand-edit files under `Assets/Prototypes/<Name>/Art/Exports/`. Change the Blender script and re-run it.
- Work only inside the folder of the prototype you were asked about. Don't edit another prototype or `Assets/Shared/` unless the task says so.
- All art, models and sounds are original or clearly licensed, with the source recorded in the spec. Never copy another game's assets.
- After every C# change, run the compile check and fix errors before continuing.
- Every mechanic gets an EditMode test for its logic. Write the test first.
- For multi-file changes, plan first (files and tests), then wait for approval.
- Ask before deleting files, changing `ProjectSettings/`, adding packages, pushing, adding or changing a license, or changing GitHub settings. Say what becomes public before you ask.
- Never change global git config, and never rewrite history that is already pushed. Set `user.name` and `user.email` in this repository's local config only.
- Commit only when the compile check and tests pass. Use conventional commit subjects.

## Agent harness config (Claude Code + OpenCode)
Follow the `dual-agent-config` skill whenever you touch agent config.
- `AGENTS.md` is the only source of rules. `CLAUDE.md` stays one line: `@AGENTS.md`. Never put rules in `CLAUDE.md`.
- Skills live in `.claude/skills/<name>/`. Both tools read that folder, so never copy skills into `.opencode/`. Keep skill frontmatter to the portable fields.
- Path-scoped rules (`.claude/rules/*.md` with `paths:`) work in Claude Code only. OpenCode has no equivalent, so don't rely on them for anything both tools must follow.
- Permissions, hooks and model choices differ between the tools. Write each in its own file (`.claude/settings.json`, `opencode.json`) and don't try to translate one into the other.
- OpenCode models (Go plan): `plan` uses `opencode-go/glm-5.3`, `build` and the default use `opencode-go/deepseek-v4.1-flash`. Change them in `opencode.json` only.
- MCP servers need an entry in both `.mcp.json` and `opencode.json`. Translate them with the skill's `sync_mcp.py`. The `unity-cli` skill replaces a Unity MCP server, so none is configured.
- After any change, check that both tools load `AGENTS.md`.

## Checks
The Unity Editor must be open on this project. `unity status` should show `ready`.
- Everything: `Tools/unity/check.sh all` (compile, then EditMode and PlayMode tests). Also `compile`, `editmode` and `playmode`. It exits non-zero on a compile error or a failing test.
- Render: `Tools/unity/capture.sh camera <out.png>`, `screen` or `play`. Write captures outside the repository. Only `screen` and `play` include post-processing (Bloom, Vignette), so judge a look on those.
- The raw `unity` commands behind these, and the traps found while building the first prototype, are in `.claude/skills/game-build/references/unity-loop.md` and `pitfalls.md`.
- Packages: `com.unity.cloud.gltfast` 6.20.0 imports the `.glb` files. Unity 6 can't import `.glb` without it.
- Tests with the Editor closed: `unity test . --mode EditMode --output Logs/test-results.xml`. With the Editor open it refuses and exits 6. Not run in this repository.
- Pull requests: `.github/workflows/tests.yml` runs both test modes in CI. It needs Actions turned on and the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` secrets (see the README). Don't turn Actions on or add secrets without asking.
- The `unity-cli` skill is installed in `.claude/skills/unity-cli/` and is read by both Claude Code and OpenCode.
