# Unity AI Prototypes

A shared repository for small Unity game prototypes, mostly 3D with some 2D. Models are made with Blender scripts, and coding agents (Claude Code and OpenCode) build much of the code from written specs. Anyone can add a prototype.

## What you need
- Unity **6000.6.8f1** (installed through Unity Hub). This is not an LTS release. It's the version Unity Hub recommended when the project was created. Use the Universal 3D (URP) project as it is.
- Blender **5.2.3 LTS**, used headless (without the window) to export models.
- Git and [Git LFS](https://git-lfs.com). Run `git lfs install` once on your machine.
- Optional: the Unity CLI (`unity`), Claude Code and OpenCode.

## Get started
```bash
git clone https://github.com/jordy-swinnen/unity-ai-prototypes.git
cd unity-ai-prototypes
git lfs install
git lfs pull
```
Then add the folder in Unity Hub and open it with 6000.6.8f1. The first open takes a while because Unity builds its `Library/` folder. That folder is not committed.

## Repository layout
```
Assets/Prototypes/<Name>/     one folder per prototype (PascalCase)
  Scripts/                    runtime code, assembly <Name>
  Editor/                     editor code and the "Build Scene" menu command
  Tests/EditMode/             logic tests
  Tests/PlayMode/             scene and behaviour tests
  Art/Exports/                .glb files written by Blender scripts
  Scenes/                     scenes written by the build command
  Settings/                   ScriptableObjects with tuning values
Assets/Shared/                code and assets used by two or more prototypes
Art/source/<Name>/            .blend source files (Git LFS)
Tools/blender/<Name>/         Blender Python scripts
docs/prototypes/<name>.md     one spec per prototype
docs/research/                background notes
AGENTS.md                     rules for coding agents (source of truth)
```
Unity only compiles test assemblies inside `Assets/`, and only imports files from `Assets/`. That's why tests and exported models live there.

## Add a prototype
1. **Write the spec.** Copy `docs/prototypes/TEMPLATE.md` to `docs/prototypes/<kebab-name>.md` and fill it in. Say what the player does, how you can tell it works, and which tests prove it. `docs/prototypes/pong.md` is the first example.
2. **Create the folder** `Assets/Prototypes/<Name>/` with the sub-folders above. Give each code folder its own assembly definition, named as shown in `AGENTS.md`. Prototypes never reference each other. Shared code goes in `Assets/Shared/`, and only when a second prototype needs it.
3. **Make the art.** Write a Python script in `Tools/blender/<Name>/` that builds each model and exports a `.glb` into `Assets/Prototypes/<Name>/Art/Exports/`. Run it like this on macOS:
   ```bash
   /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/<Name>/<script>.py
   ```
   On other systems, use the path to your Blender program. Don't edit the exported `.glb` files by hand. Change the script and run it again.
4. **Write the code and the tests.** Game rules live in plain C# so EditMode tests can run them without the Unity physics engine. Tuning values live in ScriptableObjects.
5. **Build the scene in code.** A menu command `Prototypes/<Name>/Build Scene` creates the scene from nothing. Don't edit `.unity` or `.prefab` files by hand, because an agent can't reproduce hand placement.
6. **Check it.** Compile, then run the tests (see below).
7. **Commit** with a conventional commit message, for example `feat(pong): add ball bounce rules`. Open a pull request if you don't have write access.

## Checks
With the Unity Editor open on the project (the usual case when working with an agent):
```bash
unity recompile --project-path .
unity cmd test run --mode EditMode --result-only
unity cmd test run --mode PlayMode --async-tests --result-only
unity cmd test status --result-only
```
The EditMode command prints JSON. Look at `Summary` for the totals. PlayMode tests start in the background: run `unity cmd test status --result-only` again until `status` is `completed`.

With the Editor closed (for example on a build machine), `unity test` starts its own Editor in batch mode. It refuses to run while the project is open elsewhere.
```bash
unity test . --mode EditMode --output Logs/test-results.xml
```
The batch-mode form hasn't been run on this project yet.

## Working with coding agents
- `AGENTS.md` holds all the rules. Claude Code reads it through `CLAUDE.md`, which contains only `@AGENTS.md`. OpenCode reads `AGENTS.md` directly. Don't put rules in `CLAUDE.md`.
- Both tools read skills from `.claude/skills/`. The `unity-cli` skill there is by Unity and is not covered by this repository's license.
- OpenCode models are set in `opencode.json`. Planning uses `opencode-go/glm-5.3`, and building uses `opencode-go/deepseek-v4.1-flash`. Claude Code models are set in Claude Code, not in this repo.
- Agents plan multi-file changes first and wait for approval before they write code.

## License
MIT. See `LICENSE`. The files under `.claude/skills/unity-cli/` belong to Unity and are not under this license.
