# DioramaTurn: handoff to the next agent (Stage 2, playable greybox)

Handoff written 2026-11-11 by OpenCode on `opencode-go/deepseek-v4.1-flash`. Read this after `AGENTS.md`, then the spec and plan. Delete it once its contents are folded back into the plan.

## Where the work is
- Repository: `/Users/jordy/Projects/Unity AI Prototypes` (main checkout; Unity Editor 6000.6.8f1 is open and `unity status` shows `ready`).
- Branch: `claude/aseprite-pixel-art-tooling-e7cb07`, HEAD after this commit. The branch is **pushed** (ahead by a few commits) — add commits, never rewrite.
- An abandoned linked worktree sits at `.claude/worktrees/aseprite-pixel-art-tooling-e7cb07` (detached HEAD); ignore it, or remove it with the user's OK. Work in the main checkout.
- This branch becomes **one pull request with two parts**: (1) Aseprite pixel-art tooling (done earlier), (2) the DioramaTurn prototype + the FluidSynth music pipeline docs. **Ask before pushing or opening the PR.**

## What is done
**Stage 1, closed and committed.** Rules, three rooms, solver, cursor, stars, pixel scale, sheet reader, the simulation state machine and their EditMode tests. Deliberate-bug check performed (flipping view 0's depth step failed 12 tests). `check.sh all` was green at 808 EditMode + 69 PlayMode.

**Stage 2, most of it (this commit).**
- Runtime view code in `Assets/Prototypes/DioramaTurn/Scripts/View/`: `TurnEase` (quarter-turn curve with overshoot), `CameraMath`, `CameraRig`, `SpriteQuad` (URP Lit alpha-clipped quad, frame by tiling/offset), `RoomView`, `PipView`, `StarView`, `LinkRibbons` (dotted line via a width curve), `Hud`, `DioramaTurnGame` (flow: Title → Playing → Complete → Finished), `DioramaInput` (mouse, keyboard, gamepad).
- Settings gained a `light` group and `camera.referenceHeight` / `camera.clickRadiusViewport`.
- Editor: `DioramaTurnSceneBuilder` (menu `Prototypes/DioramaTurn/Build Scene`) and `RendererSetup` (creates `Settings/DioramaTurnRenderer.asset` — a copy of the PC renderer plus a `FullScreenPassRendererFeature` using `Shaders/DioramaTurnTiltShift.shader` — and appends it to `Assets/Settings/PC_RPAsset.asset`'s renderer list; approved plan decision 1). The scene `Assets/Prototypes/DioramaTurn/Scenes/DioramaTurn.unity` is built and committed.
- A first Pip sprite: `Tools/aseprite/DioramaTurn/pip.lua` (3 frames: idle + 2 walk), exported to `Art/Sprites/pip_sheet.png` + `.json`, tags `idle` and `walk`. **The tag-export spike passed** (frame tags appear in the JSON).
- Spikes verified: the tilt-shift full-screen pass renders (sharp band, blurred top and bottom), the sprite path end to end (JSON → tiling/offset → lit quad), and the room renders correctly in view 0.
- Two bugs found on captures and fixed: the Pip quad was white because a `MaterialPropertyBlock` is not serialised with a scene (the texture is now a serialised field rebound on first use), and the camera size could bake in a transient tiny game-view height.

**Checks run:** `Tools/unity/check.sh compile` green; the scene builds with no console errors; EditMode/PlayMode not yet re-run with the new tests (see below).

## What is next (in order)
1. **Write the Stage 2 tests** (tests first is the repo rule; the code landed before them — correct this):
   - EditMode `SpriteAssetTests`: `pip_sheet.png` imports as a Point-filtered, uncompressed Sprite (mode Single); `SheetFrames.Parse` gives 3 frames of 32×32, sheet 96×32, tags `idle` and `walk`.
   - EditMode `RendererSetupTests`: the renderer asset holds a `FullScreenPassRendererFeature` with the tilt-shift material; the pipeline list contains it and still has the PC renderer at index 0; `Ensure()` twice does not duplicate.
   - EditMode `DioramaTurnSceneBuilderTests`: build to a temp scene; camera is orthographic at pitch 35.264 and the exact view yaw, tagged MainCamera; the light is a soft-shadow directional; the preview room's pieces sit on their cells; Pip's `SpriteQuad` has the sheet parsed; HUD objects exist; the game is wired to everything; building twice gives the same content; the committed scene matches the builder. Delete the temp scene in teardown (pitfall 13).
   - PlayMode `GamePlayTests`: scene loads; camera ends exactly on each view's angle after a turn; Pip walks to a reachable node and stops; a turn during a walk is refused; **each room completes from the solver's witness moves**; the next room loads; restart restores the first state; one test drives a simulated keyboard device to check the input seam (pitfall 19).
2. **`Tools/unity/check.sh all`** and fix anything red. Then a deliberate-bug check for this stage (for example invert `CameraMath.YawForView` or `CursorNavigator` and see the matching test fail).
3. **Capture each room in each of the four views** (`Tools/unity/capture.sh camera`), look at them, and check the room fits the frame and matches the plan's layout table. Judge the captures yourself if your model reads images; otherwise list the paths for the user.
4. Fix the capture-size issue below, then commit `feat(diorama-turn): add playable scene`.
5. Stage 3 (game-polish), then Stage 4 (the user's play test), as the plan says. The music is deferred to the FluidSynth work's final stage by the user's decision.

## Environment notes
- **Aseprite:** `ASEPRITE_PATH="/Users/jordy/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite" Tools/aseprite/run.sh DioramaTurn pip.lua` — pass it inline, never persist it. Never install Aseprite.
- **Blender:** `/Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/DioramaTurn/<script>.py` (no DioramaTurn scripts yet; models are stage 3).
- **Unity:** `Tools/unity/check.sh all|compile|editmode|playmode`, `Tools/unity/capture.sh camera|screen|play <out.png>`. Build the scene with `unity cmd editor invoke_menu --path "Prototypes/DioramaTurn/Build Scene"`. Captures go outside the repository.
- **FluxouidSynth** (for the later music work): `fluidsynth` 2.6.2 is installed at `/opt/homebrew/bin/fluidsynth`; GeneralUser GS 2.0.3 is the agreed SoundFont. See `docs/prototypes/fluidsynth-music-plan.md`.
- **BenchLM MCP** is configured in the global `~/.config/opencode/opencode.jsonc` (free tier, 1,000 reads/month).

## Decisions already made (do not re-ask)
Game, name, namespace, pillars, Toy-box diorama look, spec and plan approved; plan decision 1 approved (renderer asset with the tilt-shift pass, appended to the shared PC pipeline — the only change outside the prototype folder). No new packages. Do not change `ProjectSettings/`. Music is the FluidSynth work's final stage, pilot track = DioramaTurn's loop.

## Known risks and unverified facts
- **Capture size:** `capture.sh camera` renders at its own resolution while the game view reports a different, sometimes tiny, height, so the rig can fall to pixel scale 1 and the room looks small in captures (the Game view is correct). Fix by sizing from the capture's requested height; check on a capture.
- **Sprite shadow and SMAA on pixel art:** unverified; check on a capture (the plan's spike list).
- **Room 3 "dead-end join":** the spec's decoy is still not proven; only par was checked.
- **`feel.effectStrength`** is not implemented yet (all feel values arrive in stage 3).
- Room 3 is 8 pieces and may need retuning after the play test.

## What nobody has checked
Nothing is judged by eye beyond the one view-0 capture: no view angles 1–3, no turn in motion, no Pip walk or hop, no room 2 or 3, no sound, no gamepad. Feel, difficulty, the join reading as joined, sprite crispness and mood are the user's to judge in the stage 4 play test.

## Checks to report at every stop
What was built; which checks ran and what they showed; which could not run; what the user should look at by hand.
