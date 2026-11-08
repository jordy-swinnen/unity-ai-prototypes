# Agent rules: unity-ai-prototypes

## Project
- Prototypes are mostly 3D, some 2D. The project uses the Universal 3D (URP) template.
- Commit messages use plain conventional commits (no `Refs:` trailer).

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
  - `Scenes/`: scenes written by the build command. Never edit by hand.
  - `Settings/`: ScriptableObjects with tuning values.
- `Assets/Shared/`: code and assets used by two or more prototypes, in its own assembly. Prototypes may reference Shared. Shared never references a prototype, and prototypes never reference each other. Move something to Shared only when a second prototype needs it.
- `Art/source/<Name>/`: `.blend` source files (Git LFS). Unity doesn't import these.
- `Tools/blender/<Name>/`: Python scripts for Blender, run headless with `blender -b -P <script>`. They write into `Assets/Prototypes/<Name>/Art/Exports/`.
- `docs/prototypes/<kebab-name>.md`: one spec per prototype or mechanic, from `TEMPLATE.md`. Read the spec before writing code.
- `docs/research/`: background only.
- Legacy placeholder: `Assets/Art/Exports/crate.glb` and `Tools/blender/make_crate.py`. Remove them once the first prototype has real assets.

## Rules
- Logic lives in C#. Tuning values live in ScriptableObjects.
- Never hand-edit `.unity` or `.prefab` files. Build scenes in code through the prototype's menu command under `Assets/Prototypes/<Name>/Editor/`.
- Never hand-edit files under `Assets/Prototypes/<Name>/Art/Exports/`. Change the Blender script and re-run it.
- Work only inside the folder of the prototype you were asked about. Don't edit another prototype or `Assets/Shared/` unless the task says so.
- New prototype: write or find its spec in `docs/prototypes/`, then create the folders above, then code and tests.
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
- OpenCode models (Go plan): `plan` uses `opencode-go/glm-5.3`, `build` and the default use `opencode-go/deepseek-v4.1-flash`. Change them in `opencode.json` only.
- MCP servers need an entry in both `.mcp.json` and `opencode.json`. Translate them with the skill's `sync_mcp.py`. The `unity-cli` skill replaces a Unity MCP server, so none is configured.
- After any change, check that both tools load `AGENTS.md`.

## Checks
- Compile check: `unity recompile --project-path .` (needs the Editor open on this project; exits 0 with no output when it compiles. It failed once with "Invalid response format from Pipeline server" while the Editor was reloading after a package change. Wait until `unity status` shows `ready`, then run it again.)
- Packages: `com.unity.cloud.gltfast` 6.20.0 imports the `.glb` files. Unity 6 can't import `.glb` without it.
- Tests, Editor open on this project: `unity cmd test run --mode EditMode --result-only` (use `--mode PlayMode` for PlayMode). It prints JSON with a `Summary` of total, passed and failed. Run `unity cmd test list --mode EditMode` to see what exists. Verified: 25 EditMode tests pass.
- Tests, Editor closed: `unity test . --mode EditMode --output Logs/test-results.xml`. With the Editor open it refuses and exits 6. Not run with the Editor closed.
- `unity recompile` can exit 7 right after the Editor reloads. Wait until `unity status` shows `ready` and run it again.
- The `unity-cli` skill is installed in `.claude/skills/unity-cli/` and is read by both Claude Code and OpenCode.
