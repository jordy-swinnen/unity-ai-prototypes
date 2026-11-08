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

## Checks
- Compile check: not yet verified
- Tests: not yet verified
