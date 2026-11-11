# DioramaTurn: handoff to OpenCode

Written by Claude Code at the point where the user moved the work to OpenCode. Read this after `AGENTS.md`, then the spec and the plan. Delete this file when the work is done (it is a handoff, not documentation).

## What this branch is
Branch `claude/aseprite-pixel-art-tooling-e7cb07`. It will become **one pull request with two parts**:
1. **Part 1, done and committed:** Aseprite pixel-art tooling (`Tools/aseprite/run.sh`, the `aseprite-art` skill, `Assets/Shared/Editor/PixelArtTextureImporter.cs` and its tests, `.gitattributes`, `AGENTS.md` section "Pixel art (Aseprite)"). Verified: `Tools/unity/check.sh all` passed (739 EditMode, 69 PlayMode), a real import of two Aseprite outputs gave Sprite, Point, Uncompressed, no mipmaps, mode Single, and a sheet switched to Multiple stayed Multiple on reimport.
2. **Part 2, in progress:** a third example prototype, **DioramaTurn** (`Assets/Prototypes/DioramaTurn/`), a 2.5D puzzle that combines Aseprite pixel sprites with Blender models.

Nothing is pushed to `main`. The branch is pushed to `origin` only so OpenCode can continue. **Ask the user before opening the PR.**

## The game in one paragraph
A toy-box room on a turntable, seen by an orthographic true-isometric camera with four snapped views (quarter turns). Pieces (3D blocks and flat sprite "bridges") sit on integer cells. In view k, two pieces are **joined** if `B - A = n + t * r_k` (n a horizontal unit step, t any integer, r_k the view's depth step: (1,1,1), (-1,1,1), (-1,1,-1), (1,1,-1)). So turning the room makes pieces at different depths join on screen. The player turns, taps (or moves a cursor to) a reachable node, and Pip walks and hops to the star. Three rooms. No timers, no fail state. Bright and playful look ("Toy-box diorama").

## Read, in this order
1. `AGENTS.md` (all rules; the layout, the checks, the pixel-art section).
2. `docs/prototypes/diorama-turn.md` (the spec, status `approved`).
3. `docs/prototypes/diorama-turn-plan.md` (the plan, approved; Stage 1 ticked with notes).
4. `docs/research/diorama-turn.md` (sources and confidence tags).
5. `.claude/skills/game-build/references/pitfalls.md` and `unity-loop.md` (every pitfall cost time once).

## Decisions the user has made (do not re-ask)
- Game: diorama puzzle, name `DioramaTurn`, namespace `Prototypes.DioramaTurn`.
- Core puzzle: perspective link. Camera: snap to four views. Verbs: rotate plus tap to walk (keyboard, mouse and gamepad). Scope: three small rooms. Mood: bright and playful. Look: Toy-box diorama. Pillars as written in the spec.
- Spec and plan approved. **Plan decision 1 approved (option 1):** create `Assets/Prototypes/DioramaTurn/Settings/DioramaTurnRenderer.asset` (a copy of the PC renderer plus a Full Screen Pass feature with a hand-written tilt-shift shader) and append it to `m_RendererDataList` in `Assets/Settings/PC_RPAsset.asset` through Unity's API from the scene builder. Do not touch `ProjectSettings/`. No new packages are needed (URP 17.6.0, Input System 1.20.1, glTFast 6.20.0 and uGUI are installed).
- Spec changes made while planning and accepted: tilt-shift pass instead of URP Depth of Field (URP 17.6 DoF reads depth with a perspective-only formula), SMAA on the camera instead of 4x MSAA (the PC asset has MSAA off), largest room 5 x 5 x 5.

## What is done (Stage 1, code and tests; not yet reviewed by the user)
Plain C# in `Assets/Prototypes/DioramaTurn/Scripts/` (no scene, no art yet):
- `Rules/`: `Cell`, `ViewRule` (depth steps, `Rotate`, the integer screen lattice `Screen`/`ScreenUnits`), `JoinRule` (the one place the join rule lives), `RoomData`, `RoomParser` (layered text maps: `.` empty, `b` block, `r` bridge, `s` start, `g` goal; layers bottom to top, rows are z, columns are x), `RoomValidator` (size, and no two nodes on one screen spot in any view), `JoinGraph`, `RoutePlanner` (BFS, marks hops), `Solver` (0-1 BFS over (node, view); par = least turns; returns a witness list of moves), `CursorNavigator`, `Stars`, `PixelScale`, `SheetFrames` (reads an Aseprite json-array sheet into frame rects, durations, tags and Unity UV tiling/offset), `GameEvent`, `DioramaSimulation` (the state machine: Idle, Walking, Turning, Complete; emits events; one queued turn; turns refused while walking).
- `Rooms/Rooms.cs`: the three shipped rooms with par 1, 2 and 5.
- `DioramaTurnSettings.cs`: ScriptableObject with groups `turn`, `walk`, `rooms`, `camera` and `Clamp()`. More groups (`light`, `post`, `look.surface`, `feel`, `audio`) are added in the stages that use them.
- Tests in `Tests/EditMode/` (JoinRule, RoomModel, Routing, Rooms incl. "the solver's moves finish each room in the simulation", Simulation, Support, CursorNavigator). **EditMode: 808 of 808 pass** (the whole project).
- Assembly definitions: `DioramaTurn`, `DioramaTurn.Editor`, `DioramaTurn.Tests.EditMode`, `DioramaTurn.Tests.PlayMode` (no PlayMode tests yet).

## What is left for Stage 1 (small)
- The **deliberate-bug check**: flip one depth-step sign in `ViewRule`/`JoinRule`, confirm `JoinRuleTests` and `RoomsTests` fail, restore.
- Run `Tools/unity/check.sh all` (the PlayMode assembly is empty; the whole run should still be green), check `git status` for stray files (pitfalls 18 and 36: `ProjectSettings/URPProjectSettings.asset` and other prototypes' materials can change after a test run; `git checkout` them), then commit as `feat(diorama-turn): add rules, rooms and solver with EditMode tests` (a WIP commit with the code already exists; amend or add on top, it is local to this branch but now pushed, so **add a new commit, do not rewrite pushed history**).

## What is next (see the plan for the full task lists)
- **Stage 2, playable:** scene builder (`Prototypes/DioramaTurn/Build Scene`) with primitives, camera rig (orthographic, pixel-scale size from `PixelScale`, eased quarter turn with overshoot), `DioramaTurnGame`, `RoomView`, input (Input System: mouse click, Q/E, arrows, Enter, gamepad), a minimal HUD, a first Pip sprite through `Tools/aseprite/run.sh`, the settings asset, PlayMode tests (scene loads, camera exact on each view, Pip walks, turn refused during a walk, rooms complete from the solver's moves, restart), captures of every room in all four views, and the plan's spikes (pixel scale, sprite shadow and foot clipping, ribbons, Aseprite tag export, then the renderer asset and tilt-shift pass).
- **Stage 3, look:** run the `game-polish` skill (surfaces, sprites, models with baked occlusion, wood texture, lighting, post, feel rows, audio, HUD), a commit per step.
- **Stage 4, play test and tidy:** the person plays; update the spec, `README.md` and `AGENTS.md` for the third prototype (ask first); then ask before opening the one PR for both parts.

## Environment notes (important)
- Work in the **main checkout** `/Users/jordy/Projects/Unity AI Prototypes`. The Unity Editor (6000.6.8f1) is open on it; `unity status` must show `ready`. The folder `.claude/worktrees/aseprite-pixel-art-tooling-e7cb07` is an abandoned worktree the user will remove: ignore it.
- Aseprite: scripts run with `Tools/aseprite/run.sh DioramaTurn <script>.lua` and need `ASEPRITE_PATH`. On this Mac it is `"/Users/jordy/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite"` (Aseprite 1.3.18.6). Pass it **inline** for each command, never export or persist it. If it is unset or fails, stop and ask the user. Never install Aseprite. The user's own shell is fish on Linux, so any command you give them must not use bash heredocs.
- Blender: `/Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/DioramaTurn/<script>.py`. Use `parents[3]` to find the repository root from a script in `Tools/blender/<Name>/` (pitfall 26 and 38).
- Throwaway Python used to find the rooms lived outside the repository (`/private/tmp/...`) and is not needed: `Rooms.cs` and its tests are the source of truth. To make another room, search the join rule and solver in Python or just write the map and let `RoomsTests` check validity, solvability and par.
- Claude Code's session was pinned to a worktree, and a hook blocked its Edit tool on the main checkout. It wrote files through Bash with the user's OK. This does not apply to OpenCode.
- Never `mv` an asset in the shell (use `AssetDatabase.MoveAsset`). Never hand-edit `.unity`, `.prefab`, `Art/Exports`, `Art/Sprites`. Commit every `.meta` file. No empty folders under `Assets/`.
- OpenCode models (from `AGENTS.md`): `/game-build` and `/game-polish` run on `opencode-go/glm-5.3-flash#high`, which can read images. If a model cannot read images, say so and ask the user to look at renders and exported PNGs.

## What nobody has checked
- Nothing visual exists yet. No capture, no sprite, no model, no sound.
- Feel, turn timing, whether joins read as joined, and a real gamepad can only be judged by the user.
- The plan's unverified facts (Aseprite tag export, renderer asset from code, sprite shadows, SMAA on pixel art, pixel snapping) are still unverified.
- The room 3 "dead-end join" from the spec is not proven (only par was checked).
