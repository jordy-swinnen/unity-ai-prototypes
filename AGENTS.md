# Agent rules: unity-ai-prototypes

An AI-native starting point for Unity games. Fork it or use it as a template, then edit this file for your project.

## Project
- Defaults (change them in your fork): mostly 3D with some 2D, the Universal 3D (URP) template, plain conventional commits (no `Refs:` trailer).
- The repository holds two example prototypes, Neon Pong and Medieval Chess. Games and features live in their own folders under `Assets/Prototypes/`.

## Workflow: use the skills
Games and features go through four skills in `.claude/skills/`. Each ends with a stop for the user's review, and the next one starts only after approval.
1. `game-spec`: idea or feature to a spec in `docs/prototypes/<kebab-name>.md` (pillars, researched look-and-feel brief, feel table, mechanics, criteria, tests).
2. `game-plan`: approved spec to a staged plan in `docs/prototypes/<kebab-name>-plan.md`.
3. `game-build`: build the plan one stage at a time, tests first, with compile, test and render checks. Stage stops inside `game-build` are not hard stops: run the stages back to back and commit per stage, and stop only for a blocking question (a package, a deletion, an approval the rules require) or when a stage's own check needs a person.
4. `game-polish`: apply the look brief and feel table to a working game, in small steps judged on renders. Same rule: work the steps back to back with a commit per step, and stop when the play-test list is ready, not between steps.
- Don't write code for a game that has no approved spec and plan. Small fixes to existing code don't need them.
- You can check numbers, tests and renders. You can't judge how a game feels. Say what you could not check, and leave the play test to the user.

## Pinned versions
- Unity Editor: 6000.6.8f1 (not an LTS release; the version Unity Hub recommended; from `unity editors -i`)
- Blender: 5.2.3 LTS (from `blender --version`)
- Blender binary on macOS: `/Applications/Blender.app/Contents/MacOS/Blender` (`blender` is not on PATH)
- Aseprite: 1.3.18.6 (from `"$ASEPRITE_PATH" --version`). The person installs it, you never do. It is found through the `ASEPRITE_PATH` environment variable, set to the `aseprite` executable itself (on macOS inside the app: `.../Aseprite.app/Contents/MacOS/aseprite`; on Linux for example `~/aseprite/build/bin/aseprite`). `Tools/aseprite/run.sh` falls back to `aseprite` on PATH.

## Layout
Many people add prototypes here. Each prototype is self-contained, named in PascalCase (`<Name>`) and lives in its own folder.

- `Assets/Prototypes/<Name>/`: one prototype. Unity only compiles test assemblies inside Assets or a package, so tests live here too.
  - `Scripts/`: runtime code, assembly `<Name>`, namespace `Prototypes.<Name>`.
  - `Editor/`: editor code, assembly `<Name>.Editor`. Holds the menu command `Prototypes/<Name>/Build Scene`.
  - `Tests/EditMode/` and `Tests/PlayMode/`: assemblies `<Name>.Tests.EditMode` and `<Name>.Tests.PlayMode`.
  - `Art/Exports/`: `.glb` files written by Blender scripts. Never edit by hand.
  - `Art/Textures/`: `.png` textures written by a script (`<name>_albedo.png`, `<name>_normal.png`). Never edit by hand.
  - `Art/Sprites/`: `.png` pixel art and sprite sheets (with their `.json`) written by an Aseprite script. Never edit by hand. Unity imports everything here as a Point-filtered Sprite.
  - `Audio/`: sounds written by scripts.
  - `Materials/`: materials written by the scene builder. Never edit by hand.
  - `Shaders/`: hand-written URP shaders.
  - `Scenes/`: scenes written by the build command. Never edit by hand.
  - `Settings/`: ScriptableObjects with tuning values.
- `Assets/Shared/`: code and assets used by two or more prototypes, in its own assembly. Prototypes may reference Shared. Today it holds `Feel/ShakeState` and `Feel/TimeFeel` (camera shake and hit-stop), used by Neon Pong and Medieval Chess, and `Editor/PixelArtTextureImporter` (the import rule for `Art/Sprites/`, in the editor-only assembly `Shared.Editor`). Shared never references a prototype, and prototypes never reference each other. Move something to Shared only when a second prototype needs it.
- `Art/source/<Name>/`: `.blend` and `.aseprite` source files (Git LFS). Unity doesn't import these.
- `Tools/blender/<Name>/`: Python scripts for Blender, run headless with `blender -b -P <script>`. They write into `Assets/Prototypes/<Name>/Art/Exports/`.
- `Tools/aseprite/<Name>/`: Lua scripts for Aseprite, run headless with `Tools/aseprite/run.sh <Name> <script.lua>`. They write into `Assets/Prototypes/<Name>/Art/Sprites/` and `Art/source/<Name>/`.
- `Tools/audio/<Name>/`: Python scripts that write sounds.
- `Tools/textures/<Name>/`: Python scripts that write textures, run headless in Blender (it ships numpy).
- `Tools/unity/`: `check.sh` (compile and tests) and `capture.sh` (renders).
- `docs/prototypes/<kebab-name>.md`: one spec per game or feature, from `TEMPLATE.md`. Read the spec before writing code.
- `docs/research/`: background and sourced research notes.

## Project structure
From Unity's guide ([organizing your project](https://unity.com/how-to/organizing-your-project)) and this repository's needs. The details, and what we did not adopt, are in `docs/project-structure.md`. `Assets/Shared/Tests/EditMode/ProjectStructureTests.cs` checks most of these, so `check.sh` fails when one is broken.
- Names are PascalCase with no spaces, for every file and folder under `Assets/`. Materials are `<Name><Thing>` (`MedievalChessTileLight`).
- Only `Prototypes/`, `Shared/`, `ThirdParty/`, `Sandbox/` and `Settings/` (the render pipeline) sit at the top of `Assets/`. Anything we did not make (Asset Store, downloads) goes in `Assets/ThirdParty/<Vendor>/`, never mixed into our own folders. Personal experiments go in `Assets/Sandbox/<user>/`, which git ignores.
- Folders are split by asset type inside each prototype (the list above). No empty folders. Commit every `.meta` file.
- Move or rename an asset through Unity (`AssetDatabase.MoveAsset` or `RenameAsset`, run with `unity cmd script eval`), so its `.meta` and GUID move with it. Never `mv` an asset in the shell.
- The namespace matches the folder: `Prototypes.<Name>` (and `Prototypes.<Name>.Tests`), `Shared`. One assembly per folder part, named like the folder. Prototypes never reference each other, and Shared never references a prototype.
- A scene holds one game or one level. If a game needs several levels, build one scene each and load them additively. Reusable objects become prefabs (nested where it helps), built by code like scenes.
- Document a new structure decision in `docs/project-structure.md` and keep this list short.

## Rules
- Logic lives in C#. Tuning values live in ScriptableObjects.
- Never hand-edit `.unity` or `.prefab` files. Build scenes in code through the prototype's menu command under `Assets/Prototypes/<Name>/Editor/`.
- Never hand-edit files under `Assets/Prototypes/<Name>/Art/Exports/`. Change the Blender script and re-run it. The same goes for `Art/Sprites/` and its Aseprite scripts.
- Work only inside the folder of the prototype you were asked about. Don't edit another prototype or `Assets/Shared/` unless the task says so.
- All art, models, textures and sounds are original or clearly licensed, with the source recorded in the spec. Never copy another game's assets.
- Surfaces are your decision, not a question for the person: for every material in the look brief, decide flat colour, texture, baked occlusion or a shader effect, write why in the spec's Surfaces table, and build it. Follow `.claude/skills/game-polish/references/surfaces.md`, and ask only for the cases under "When to ask" there (a package, a download, a large result, a real style choice, a real performance cost).
- After every C# change, run the compile check and fix errors before continuing.
- Every mechanic gets an EditMode test for its logic. Write the test first.
- For multi-file changes, plan first (files and tests), then wait for approval.
- Ask before deleting files, changing `ProjectSettings/`, adding packages, pushing, adding or changing a license, or changing GitHub settings. Say what becomes public before you ask.
- Never change global git config, and never rewrite history that is already pushed. Set `user.name` and `user.email` in this repository's local config only.
- Commit only when the compile check and tests pass. Use conventional commit subjects.

## Pixel art (Aseprite)
Pixel art is made by scripts, like models (Blender) and textures. Follow the `aseprite-art` skill (`.claude/skills/aseprite-art/`), which has the process and two template scripts.
- **Scripts only.** Never draw pixel art by hand or in the Aseprite GUI, and never edit a file in `Art/Sprites/`. Change the Lua script and run it again.
- **Run:** `Tools/aseprite/run.sh <Prototype> <script.lua>` runs `Tools/aseprite/<Prototype>/<script.lua>` with `aseprite --batch`. Exit 0 is good, 1 is a failed script or a missing file, 2 is bad usage or no usable Aseprite.
- **`ASEPRITE_PATH` is required** (see Pinned versions). If it is unset or Aseprite does not run, stop and ask the person. Never install Aseprite, and never pick a binary yourself.
- **Params:** the wrapper passes `out_dir` (`Assets/Prototypes/<Prototype>/Art/Sprites`) and `src_dir` (`Art/source/<Prototype>`). The script reads them with `app.params["out_dir"]` and `app.params["src_dir"]`, saves the `.aseprite` source to `src_dir`, and exports PNGs and sheets to `out_dir`. Every script prints `wrote <path>` for each file; `run.sh` fails if one is missing, empty, stale or outside those two folders.
- **Drawings are character maps plus a named palette table,** one string per row and a legend from character to colour name, so a diff shows what changed. Assert on row length and unknown characters: Aseprite silently ignores a bad one.
- **Sprite sheets** are exported with the `--sheet <png> --data <json> --format json-array` settings. From a script that is `app.command.ExportSpriteSheet{..., dataFormat = SpriteSheetDataFormat.JSON_ARRAY}`. The same from a saved file on the command line: `"$ASEPRITE_PATH" --batch <src>.aseprite --sheet <png> --data <json> --format json-array`.
- **Always look at the exported PNG** (scale it up first, outside the repository). Say if your model cannot read images.
- **Unity import:** every texture under a `/Art/Sprites/` path is imported as a Sprite with Point filter, no compression and no mipmaps (`Assets/Shared/Editor/PixelArtTextureImporter.cs`, tested). A sprite sheet comes in as one Single sprite, so slicing it from the JSON is the prototype's own editor code.

## Music (FluidSynth)
Music is notes in code, rendered by instruments, like models and sprites. Follow the `music-composition` skill (`.claude/skills/music-composition/`).
- **Scripts only.** Never hand-edit a rendered `.wav`. Change the track script and render it again.
- **Track:** `Tools/audio/<Name>/music/<track>.py` composes with `Tools/audio/midi.py`, writes a MIDI file and prints `loop_seconds=<n>` for a looping track.
- **Render:** `Tools/audio/render_music.sh <Prototype> <track>`. It needs `fluidsynth` on PATH and a SoundFont (`SOUNDFONT_PATH`, or the committed `Tools/audio/soundfonts/GeneralUser-GS.sf2`). It fails loudly on a broken script, a missing MIDI or an empty render, and checks duration, peak, RMS and the loop seam.
- **SoundFont:** a build-time input only, committed under Git LFS. Licence, author and source are in `docs/audio-credits.md`. Never ship it as a runtime asset.
- **One track at a time, and the person listens.** An agent can check length, silence, clipping, loudness and the seam; it cannot judge whether the music is good, and must never claim it is.
- **Unity import:** `Assets/Shared/Editor/MusicClipImporter.cs` streams `<Prototype>/Audio/music.wav`; looping and ducking are on the AudioSource.
- Sound effects stay in each prototype's `make_sounds.py`; this section is music only.

## Agent harness config (Claude Code + OpenCode)
Follow the `dual-agent-config` skill whenever you touch agent config.
- `AGENTS.md` is the only source of rules. `CLAUDE.md` stays one line: `@AGENTS.md`. Never put rules in `CLAUDE.md`.
- Skills live in `.claude/skills/<name>/`. Both tools read that folder, so never copy skills into `.opencode/`. Keep skill frontmatter to the portable fields.
- Path-scoped rules (`.claude/rules/*.md` with `paths:`) work in Claude Code only. OpenCode has no equivalent, so don't rely on them for anything both tools must follow.
- Permissions, hooks and model choices differ between the tools. Write each in its own file (`.claude/settings.json`, `opencode.json`) and don't try to translate one into the other.
- Shared skills come from `gh@jordy-swinnen/agent-skills`. Install or update them with `opkg install gh@jordy-swinnen/agent-skills/skills/dual-agent-config --platforms claude`, then the same command for `agent-handoff` and `opencode-go-plus-build-plan-recommender`; add `--force` when updating. Use the skill subpath so the package's own `AGENTS.md` is never merged here. Keep `--platforms claude` so they land only in `.claude/skills/`, which both tools read; never create `.opencode/skills/`, and never run bare `opkg install` here. Do not edit the installed copies here: change the shared repo and reinstall.
- OpenCode models (Go plan): the phase commands in `.opencode/commands/` route `/game-spec` and `/game-plan` to `opencode-go/mimo-v2.6-pro`, and `/game-build` and `/game-polish` to `opencode-go/deepseek-v4.1-flash`; both accept images, so render judgment stays possible. Defaults stay in `opencode.json` (`deepseek-v4.1-flash` for build and the default, `mimo-v2.6-pro` for plan).
- When a session is stuck, switch models with `/models` instead of editing config: flash `#max` for a hard debug, `glm-5.2` when the GLM-5.3 220-request/5-hour wall bites or flash thrashes in compile-fix loops, `gemini-4-argon` only for polish render judgment (image input, small allowance). Judge the first polish stage's keep-or-cut calls honestly before switching.
- MCP servers need an entry in both `.mcp.json` and `opencode.json`. Translate them with the skill's `sync_mcp.py`. The `unity-cli` skill replaces a Unity MCP server, so none is configured.
- After any change, run `python3 Tools/agents/check_config.py` (one rules file, portable skill frontmatter, MCP parity). It checks files only. To see that each tool loads the rules, start a session in each and ask for the first heading of the rules and the skill names. Last verified with OpenCode: it loads `AGENTS.md` (including `Project structure`) and the four game skills.

## Checks
The Unity Editor must be open on this project. `unity status` should show `ready`.
- Everything: `Tools/unity/check.sh all` (compile, then EditMode and PlayMode tests, including the project structure tests). Also `compile`, `editmode` and `playmode`. It exits non-zero on a compile error or a failing test.
- Render: `Tools/unity/capture.sh camera <out.png>`, `screen` or `play`. Write captures outside the repository. Only `screen` and `play` include post-processing (Bloom, Vignette), so judge a look on those.
- The raw `unity` commands behind these, and the traps found while building the first prototype, are in `.claude/skills/game-build/references/unity-loop.md` and `pitfalls.md`.
- Packages: `com.unity.cloud.gltfast` 6.20.0 imports the `.glb` files. Unity 6 can't import `.glb` without it.
- Tests with the Editor closed: `unity test . --mode EditMode --output Logs/test-results.xml`. With the Editor open it refuses and exits 6. Not run in this repository.
- Pull requests: `.github/workflows/tests.yml` runs both test modes in CI. It needs Actions turned on and the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` secrets (see the README). Don't turn Actions on or add secrets without asking.
- The `unity-cli` skill is installed in `.claude/skills/unity-cli/` and is read by both Claude Code and OpenCode.
