# Neon Pong: plan and tasks

Plan for `docs/prototypes/neon-pong.md`. Status: stage 1 done

## Decisions needed first
1. **No new packages.** Everything fits the packages the project has: URP 17.6.0 (Bloom, Vignette, Tonemapping, Volume), Input System 1.20.1, glTFast 6.20.0 (imports the `.glb` files), uGUI 2.6.0 (score text). Camera shake, hit-stop and the trail are small scripts of our own, not Cinemachine (see `game-polish/references/urp-effects.md`). Nothing to approve here.
2. **Score text.** Plain uGUI `Text` with the built-in font, because a fresh project has no TextMeshPro font resources (pitfall 9). The bold neon look comes from colour, size and bloom. If the text looks too plain in stage 3, importing the TMP resources is a separate decision I will ask about then.
3. **Sound files.** The sounds are `.wav` files written by a Python script (standard library `wave` module only, no installs). Unity imports `.wav` natively. Nothing to approve. The first listen is the person's.
4. **The legacy crate placeholder** (`Assets/Art/Exports/crate.glb`, `Tools/blender/make_crate.py`) is still in the repository. `AGENTS.md` says to remove it once a prototype has real assets. Removing files needs approval, so this is a stage 4 question. It stays for now.
5. **Facts checked now, in the package source and project files:**
   - `UniversalAdditionalCameraData.renderPostProcessing` exists (get and set). Checked in `Library/PackageCache`.
   - `Bloom`, `Vignette` and `Tonemapping` live in `UnityEngine.Rendering.Universal`, and `Bloom.intensity` defaults to 0. A scene that does not set it shows no bloom.
   - Both render pipeline assets in `Assets/Settings/` (`PC_RPAsset`, `Mobile_RPAsset`) already have `m_SupportsHDR: 1`. So HDR is on without a change, and I do not need to touch `ProjectSettings/` or the assets. This resolves the spec's open question about HDR as far as the setting goes. Whether bloom looks right is still judged on a render.
6. **Facts still unverified, to be settled on renders:** emission intensities that give a good glow, whether the point light on the ball earns its cost, the camera tilt, and whether the script-made sounds sound good. Each has a task in stage 3 and a "keep or cut" decision.
7. **Reading the Volume Mask:** the camera's Volume Mask must include the layer of the global Volume. The builder keeps both on `Default`, and a builder test checks it.

## Units and layout
Unity units are metres. The table top is the plane y = 0. Gameplay uses x (the table's length, player 1 on the left at negative x) and z (the width, positive z is the far side from the camera). Up on screen is y.

| Object | Size (x, y, z) | Position | Why |
|---|---|---|---|
| Table | 17 x 0.41 x 11 (0.4 slab plus 0.01 grid lines) | slab top face at y = 0 | slightly bigger than the play area (16 x 10), so the end lines and rails sit on it |
| Rail (two, top and bottom) | 16.4 x 0.3 x 0.3 | z = +5.15 and z = -5.15, resting on y = 0 | the walls the ball bounces off. The play area half width is 5, so the rail inner face is at z = 5 |
| End line (two) | 0.1 x 0.02 x 10 | x = +8 and x = -8 | where a miss is scored, dim emissive |
| Paddle (two) | 0.4 x 0.3 x 2 | x = +7.5 and x = -7.5, resting on y = 0 | half length 1, half thickness 0.2, as in the spec |
| Ball | sphere, radius 0.25 | starts at the origin, centre at y = 0.25 | |
| Camera | perspective, field of view 35 | above the table, pitched down 62 degrees, looking at the origin, distance chosen so the whole table fits | spec: narrow field of view, pitch from a setting |
| Ball light | point light | follows the ball, just above it | tints the table, may be cut |
| Score text | world-space canvas or screen-space overlay | above the table, one number per side | decided in stage 2, overlay is simpler |

Blender's Y axis becomes Unity's -Z on export. Every model is symmetric, so this is harmless, and the Blender script checks sizes and pivots.

## Tuning values
All are fields of `NeonPongSettings` (`Assets/Prototypes/NeonPong/Settings/`). Starting values are from the spec, and the look and feel ones are tuned in stage 3.

- **Table:** half length 8, half width 5, ball radius 0.25, paddle half length 1, paddle half thickness 0.2, paddle x 7.5.
- **Rules:** base speed 9, speed step 0.8, max speed 22, max bounce angle 60, max serve angle 20, serve delay 1 s, paddle speed 10, paddle ease time (for the start and stop of the paddle), win score 5, fixed time step 1/120 s (keeps a fast ball from passing through a paddle).
- **Look:** colours from the palette, emission intensity for the ball, paddles and rails, bloom threshold, intensity and scatter, vignette intensity, trail length and width, point light intensity and range, camera pitch, field of view and distance.
- **Feel:** shake strength for wall, paddle, score and win, shake duration, hit-stop durations (paddle 0.04 s, score 0.08 s), burst size and count, pitch range, global effect strength (0 to 1).

## Code design
Assembly `NeonPong`, namespace `Prototypes.NeonPong`.

- `NeonPongSettings` (ScriptableObject): all the values above.
- `NeonPongState`: ball position and velocity, paddle positions, scores, phase (`Serving`, `Playing`, `GameOver`), serve timer, current rally speed, last winner, and the last events.
- `NeonPongInput`: one number per player, from -1 to 1.
- `NeonPongSimulation`: plain C#, no engine physics. `Step(dt, input)` moves the paddles (with ease and clamping to the table), moves the ball with a swept check against the paddles, handles wall bounces, paddle bounces, speed steps, scoring, serving and game over. It returns or raises events (`WallHit`, `PaddleHit`, `SpeedStep`, `Point`, `Win`, `Restart`), so the feel layer reacts to events and never reads the rules' internals. A seeded random source picks the first serve, so tests are repeatable.
- `FeelState` and small timer types (plain C#): `Shake` (decaying offset, scaled by strength), `HitStop` (a timer that pauses the simulation only), and `FeedbackRouter` (turns events into shake, hit-stop and effect triggers using the settings and the global strength). These are testable without a scene.
- `NeonPongGame` (MonoBehaviour): reads the keyboard through the Input System (W and S, Up and Down, Space after a win), steps the simulation from `FixedUpdate` unless hit-stop is active, and passes events to the feel layer.
- `NeonPongView` (MonoBehaviour): copies the state onto the paddle, ball and score objects.
- Stage 3 components: `CameraShake`, `BallTrailDriver`, `ImpactBurst`, `PaddleFlash`, `NeonAudio` (plays the clips for events, with a pitch from speed). Each is small and driven by settings and events.

Editor assembly `NeonPong.Editor`:
- `NeonPongSceneBuilder`: the menu command `Prototypes/NeonPong/Build Scene`. It creates the settings asset if it is missing, loads the models after `NewScene` (pitfall 2), builds the scene with camera, lights, Volume and canvas, and saves `Assets/Prototypes/NeonPong/Scenes/NeonPong.unity`. Running it twice gives the same content.
- `Tools/unity/` scripts are used for capture and checks, not new ones.

Test assemblies `NeonPong.Tests.EditMode` and `NeonPong.Tests.PlayMode` (the latter covers all platforms, pitfall 4).

## Art and audio pipeline
- **Models:** `Tools/blender/NeonPong/make_neon_pong_assets.py` creates the table (with raised or inset grid lines), rail, end line, paddle (rounded bar) and ball, each under 1,500 triangles. It uses neutral materials, because colours and emission are set in Unity from the palette, and it exports one `.glb` per model to `Assets/Prototypes/NeonPong/Art/Exports/` and saves `Art/source/NeonPong/neon_pong.blend` (Git LFS). Run: `/Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/NeonPong/make_neon_pong_assets.py`.
- **Sounds:** `Tools/audio/NeonPong/make_sounds.py` writes `wall`, `paddle` (several pitch variants), `serve`, `point`, `win` and `restart` as 16-bit mono `.wav` files into `Assets/Prototypes/NeonPong/Audio/`. Square and triangle waves with a fast attack and short decay, each under 1.5 s. Run: `python3 Tools/audio/NeonPong/make_sounds.py`. A test reads each file and checks it is not silent, does not clip and ends near zero.
- **Effects in Unity:** Trail Renderer and Particle System (built in), one Volume profile built in code with Bloom, Vignette and Tonemapping Neutral.
- **Feasibility of the look items:**
  - Neon glow from emission plus bloom: feasible, tuned on renders.
  - Point light on the ball: feasible, may be cut.
  - Paddle lean or stretch: feasible with a scale on the view, small.
  - Colour wash on a win: feasible with a Volume override (Color Adjustments or a tint), checked on a render.
  - Slow motion on the win: feasible by scaling the simulation step, not `Time.timeScale`, so the UI keeps running.

## Tasks
Each stage ends with a stop for review.

### Stage 1: greybox
- [x] Run `Tools/unity/check.sh all` and note the starting result: compile ok, 0 tests in both modes.
- [x] Create the folders and four assembly definitions from `AGENTS.md`. Only folders that hold files exist now (`Audio`, `Scenes` and `Settings` come in later stages).
- [x] Write `make_neon_pong_assets.py`, run it headless, and check the `.glb` and `.blend` files exist, with the sizes, pivots and triangle counts from the layout table. Checked the bounds and material count again after Unity's import: all sizes match, table has 2 materials, the others 1. The script fails with no export if a size, pivot or triangle count is off (it caught the table height, which is 0.41 with the grid lines).
- [x] Write the EditMode tests for the rules from the spec (wall reflection, paddle bounce at centre and edge, speed step and cap, reset, scoring both sides, serve direction, win, restart, paddle limits, repeatability, fast ball does not tunnel through a paddle), before the code (30 tests; they failed to compile first).
- [x] Write `NeonPongSettings`, `NeonPongState`, `NeonPongInput` and `NeonPongSimulation` to pass them. `Tools/unity/check.sh all`: 30/30 EditMode pass. Two tests were wrong at first (a paddle clamp zeroes its speed, and a long game ends at 5 points); I fixed the tests, not the rules.
- [x] Deliberate-bug check: flipped the sign of the bounce angle, 3 tests failed, restored and 30/30 pass.
- [x] Commit: `feat(neon-pong): add Blender assets and rules with EditMode tests`.

### Stage 2: playable
- [ ] Write the builder tests first (every part present, models where the rules expect them, building twice gives the same content, Volume has Bloom with intensity above 0 and Tonemapping Neutral, camera has post-processing on, Volume Mask includes the Volume layer).
- [ ] `NeonPongGame`, `NeonPongView`, input, score text, `NeonPongSceneBuilder`. Build the scene twice and compare content.
- [ ] PlayMode tests: paddles and ball follow the state, input moves the paddles, a point updates the score on screen, a win shows the winner. Run `Tools/unity/check.sh all`.
- [ ] Deliberate-bug check on the view.
- [ ] Capture the scene (`Tools/unity/capture.sh camera`) and check it against the layout table. Bloom and emission are rough here, and that is fine.
- [ ] Commit: `feat(neon-pong): add playable scene and PlayMode tests`.

### Stage 3: look and feel (game-polish)
Each step has a before and after capture, tests for its logic, a keep or cut decision and its own commit. The order follows `game-polish/references/polish-order.md`.
- [ ] Baseline capture.
- [ ] Materials and emission from the palette (ball brightest, then paddles, then rails, then grid).
- [ ] Volume: Bloom, Vignette, Tonemapping Neutral. Tune intensity and threshold on renders. Settle the HDR question on a render.
- [ ] Camera pitch, field of view and distance, judged on a render for the far side.
- [ ] Ball trail that grows with speed, and the point light (keep or cut).
- [ ] Sound generation script and the tests on the files. Wire `NeonAudio` to events with pitch from speed.
- [ ] Feel table rows one by one: wall hit, paddle hit (flash, burst, scale pop, shake, hit-stop), speed step, paddle lean, point scored, win, restart. Tests: shake decays to zero, shake follows the global strength, hit-stop ends, strength 0 disables feedback, events trigger on the right actions.
- [ ] Global effect strength: capture at 0 and at 1 and check the difference.
- [ ] Final before and after renders and the play-test list for the person.
- [ ] Commit per step: `feat(neon-pong): <effect>`.

### Stage 4: play test and tidy
- [ ] The person plays and fills in the "needs a person" criteria.
- [ ] Findings become tasks or open questions. Tune values on the settings asset.
- [ ] Update the spec, tick the criteria and set the plan status to complete.
- [ ] Ask about removing the legacy crate placeholder (needs approval). Remove leftovers only with approval.
- [ ] Ask about pushing. Say what becomes public first.

## Verification
| Criterion | How it is checked |
|---|---|
| Blender script writes every model with planned sizes and pivots | Script run plus a check on the files and on the Unity import (stage 1) |
| Build Scene builds from nothing, twice gives the same content | EditMode builder test (stage 2) |
| Walls reflect with equal angles; centre hits return straight, edge hits at the maximum angle | EditMode rules tests |
| Speed rises by the step, is capped, and resets after a point | EditMode rules tests |
| Each end line scores for the right player; next serve goes toward the loser | EditMode rules tests |
| Game ends at 5, shows the winner, restarts | EditMode rules test and PlayMode test |
| A paddle never leaves the table | EditMode rules test |
| Render shows glowing paddles, ball and rails with visible bloom, ball brightest | `capture.sh camera` render, read by me, confirmed by the person |
| Strength 0 shows no glow beyond the base look and zero shake | Render at strength 0, plus an EditMode test for the shake offset |
| Shake decays, hit-stop ends, effects trigger on the right events | EditMode feel tests, PlayMode test for a hit |
| Every sound exists, is not silent, does not clip, ends near zero | EditMode audio file test |
| `Tools/unity/check.sh all` passes | Run at the end of every stage |
| Looks like neon arcade, not blocks | The person, from renders and play |
| Paddle hits feel punchy; shake and hit-stop are comfortable | The person plays |
| Tension builds across a rally | The person plays |
| Ball and paddles stay visible at top speed | The person plays |
| Keys work; Space restarts after a win | The person plays (tests cover the input mapping, not the real keys) |
| Sounds fit the mood; volume is balanced | The person listens |

## Risks
- **The Editor in the background may not advance Play Mode time** (pitfall 10): a `play` capture can show the first frame. Fallback: `camera` captures for the look, and PlayMode tests for behaviour. The person judges motion.
- **Bloom may need a different setup than expected** (no glow on the render): check the camera's post-processing flag, the Volume Mask, the Volume profile and the emission values in that order.
- **Too much glow smears the ball at top speed.** Fallback: lower intensity or a higher threshold, then the global strength.
- **Script-made sounds may sound thin.** Fallback: adjust the waveforms and envelopes in the script. Outside audio would need licensing recorded in the spec and approval.
- **Hit-stop can feel like lag if it is too long.** The first values are small (40 ms and 80 ms) and live on the settings asset.
- **A fast ball tunnelling through a paddle.** Mitigation: a fixed small time step and a swept check, with a test.
- **Imported glTF materials may not take emission well.** Fallback: the builder assigns its own URP Lit materials by name and ignores the imported ones.
- **The Unity Editor must be open on the project** for every check. If `unity status` is not ready, I stop and ask.
