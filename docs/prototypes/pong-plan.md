# Pong: plan and tasks

Plan for `docs/prototypes/pong.md`. Status: draft, waiting for approval before any code is written.

## Decisions needed first
1. **Importing `.glb` files.** Unity 6 has no built-in `.glb` importer. The existing `crate.glb.meta` uses the `DefaultImporter`, so Unity treats the file as an unknown asset. Two options:
   - Add a glTF package (I believe `com.unity.cloud.gltfast`; confirm the name in Package Manager first). Keeps the `.glb` rule in `AGENTS.md`. Adding a package needs approval.
   - Export `.fbx` instead. Unity imports it natively, but the exported materials need more work to look right in URP.
   - Recommendation: add the glTF package.
2. **Camera.** Fixed straight-down view, as in the spec. A slight tilt can be added later by changing one value in the settings asset.
3. **Existing package.** `Packages/manifest.json` contains `com.unity.ai.assistant`, the deprecated in-Editor AI package from the template. It isn't needed for this work. Removing it needs approval, so it stays for now.

## Units and layout
Unity units are metres. The table top is the plane y = 0. Gameplay uses x (length) and z (width).

| Object | Size (x, y, z) | Position |
|---|---|---|
| Table | 16 x 0.4 x 10 | top face at y = 0 |
| Wall (two) | 16 x 0.6 x 0.5 | z = +5.25 and z = -5.25 |
| Paddle (two) | 0.4 x 0.5 x 2 | x = +7.5 and x = -7.5 |
| Ball | sphere, radius 0.25 | starts at the origin |

## Starting values (in `PongSettings`)
Base ball speed 8 per second. Speed step 0.75 per paddle hit. Maximum speed 20. Maximum bounce angle 60 degrees. Paddle speed 10 per second. Win score 5. Serve pause 1 second.

## Code design
Assembly `Pong`, namespace `Prototypes.Pong`.
- `PongSettings` (ScriptableObject): all the values above.
- `PongState`: ball position and velocity, both paddle positions, scores, phase (`Serving`, `Playing`, `GameOver`), and the serve timer.
- `PongInput`: one number per player, -1 to 1.
- `PongSimulation`: plain C# with no Unity physics. `Step(dt, input)` moves the paddles (clamped to the table), moves the ball, and handles wall bounces, paddle bounces, scoring, serve and game over. The ball-to-paddle check uses the ball's movement during the step, so a fast ball can't pass through a paddle. A seeded random source picks the first serve, so tests are repeatable.
- `PongGame` (MonoBehaviour): reads the keyboard through the Input System (W and S, Up and Down), calls `Step` from `FixedUpdate`, and restarts on a key press at game over.
- `PongView` (MonoBehaviour): copies the state onto the paddle, ball and score objects.
- Score text uses TextMeshPro from the `com.unity.ugui` package, which the project already has.

Editor assembly `Pong.Editor`:
- `PongSceneBuilder`: the menu command `Prototypes/Pong/Build Scene`. It creates `PongSettings` if it's missing, builds the scene from the exported models plus a camera, a light and a score canvas, and saves it to `Assets/Prototypes/Pong/Scenes/Pong.unity`. Running it twice gives the same scene.

Test assemblies `Pong.Tests.EditMode` and `Pong.Tests.PlayMode`.

## Blender script
`Tools/blender/Pong/make_pong_assets.py` creates `paddle`, `ball`, `table` and `wall` from the sizes above. Each has a flat-colour material (table dark green, walls grey, paddles white and ball orange), and the script exports one `.glb` each into `Assets/Prototypes/Pong/Art/Exports/`. It also saves `Art/source/Pong/pong.blend`. Blender's Y axis becomes Unity's -Z on export, which is harmless because every shape is symmetric.

## Tasks
Each stage ends with a stop for your review.

Stage 1: assets
- [ ] Create the folders from `AGENTS.md` (the folders get `.meta` files when Unity opens).
- [ ] Add the glTF package (decision 1) and check that Unity imports `crate.glb`.
- [ ] Write `make_pong_assets.py`, run it headless, and check that all four `.glb` files and the `.blend` exist.
- [ ] Commit: `feat(pong): add Blender asset script and exports`.

Stage 2: rules with tests first
- [ ] Add the four assembly definitions (`Pong`, `Pong.Editor`, `Pong.Tests.EditMode`, `Pong.Tests.PlayMode`).
- [ ] Write the EditMode tests listed in the spec, then make `PongSimulation` pass them.
- [ ] Run `unity recompile` and `unity test --mode EditMode`.
- [ ] Commit: `feat(pong): add game rules with EditMode tests`.

Stage 3: scene and play
- [ ] Write `PongSettings`, `PongGame`, `PongView` and `PongSceneBuilder`.
- [ ] Build the scene with the menu command, run it twice, and check that the scene is the same.
- [ ] Write the PlayMode tests and run `unity test --mode PlayMode`.
- [ ] Play the game and check the acceptance criteria in the spec by hand.
- [ ] Commit: `feat(pong): add playable scene and PlayMode tests`.

Stage 4: tidy up
- [ ] Decide what happens to the placeholder `crate.glb` and `make_crate.py`. Removing them needs approval.
- [ ] Tick off the acceptance criteria in the spec and set its status to done.

## Risks
- The PlayMode test needs to load the built scene. The Editor-only builder may not be reachable from the PlayMode assembly. If so, the test assembly is set to include the Editor platform, or the scene is loaded by path.
- `unity recompile` and `unity test` have not been run on this project yet. If either command fails, I'll report the full output and not change the command to hide it.
- The Unity Editor must be open on the project for `unity recompile`.
