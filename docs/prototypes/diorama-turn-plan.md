# DioramaTurn: plan and tasks

Plan for `docs/prototypes/diorama-turn.md`. Status: approved (you approved it and decision 1, option 1). Stage 1 is done: built, its EditMode tests pass, the deliberate-bug check failed as it should, and `check.sh all` is green (808 EditMode, 69 PlayMode). Being built by OpenCode, see `docs/prototypes/diorama-turn-handoff.md`.

## Decisions needed first
1. **Register a renderer in the shared render asset (APPROVED: option 1, the recommended one).** The look needs a tilt-shift blur, which is a full-screen pass on a renderer asset. URP's built-in Depth of Field cannot do it: both its shaders read depth with a perspective-only formula `[read]`, and ours is an orthographic camera. A renderer feature lives on a renderer asset, and `Assets/Settings/PC_RPAsset.asset` holds the list of renderers (`m_RendererDataList`, one entry today). Cameras pick one by index.
   - **Recommended:** create `Assets/Prototypes/DioramaTurn/Settings/DioramaTurnRenderer.asset` (a copy of the PC renderer plus a Full Screen Pass feature with our shader) and append it to the PC asset's list, through Unity's API from the scene builder. The other two prototypes keep index 0 and change nothing. It touches one shared file by one list entry.
   - **Alternative A:** no blur at all. The look loses the miniature softness, nothing shared is touched. Cheapest.
   - **Alternative B:** give DioramaTurn its own URP asset and switch to it at runtime with `QualitySettings.renderPipeline`. No shared file changes, but it is a runtime hack that must also work in the Editor for captures.
   - I will not touch `Assets/Settings/` until you answer. Until then stages 1 and 2 do not need it.
2. **Spec changes made while planning (tell me if you object).** I edited the spec's status to `approved` (you ran `/game-plan`) and made four small changes, each from a fact I checked:
   - Depth of Field replaced by the tilt-shift pass (above).
   - 4x MSAA replaced by SMAA on the camera. `PC_RPAsset` has `m_MSAA: 1` (off), and changing it would change the other prototypes' look. SMAA is a per-camera setting.
   - The largest room is 5 x 5 x 5, not 7 x 6 x 7. At 3 screen pixels per sprite pixel and 1080 high, the visible height is 11.25 units, and a 7 x 6 x 7 room plus its plinth is about 12.2 units tall in isometric.
   - The research note now records these.
3. **No new packages.** URP 17.6.0, Input System 1.20.1, glTFast 6.20.0 and uGUI are already in `Packages/manifest.json` `[read]`. No `ProjectSettings/` change.
4. **Aseprite is needed.** Sprite scripts run through `Tools/aseprite/run.sh` with `ASEPRITE_PATH` set. I pass it inline from the path you gave (the Steam install, Aseprite 1.3.18.6) and never persist it. If it is unset or fails I stop and ask. Blender runs with `/Applications/Blender.app/Contents/MacOS/Blender`.
5. **Sprites are quads, not Unity Sprites.** Unity's sprite renderer cannot use URP Lit (the texture property is `_MainTex` against Lit's `_BaseMap`), and the lit shadow-casting look needs Lit. So each sprite is a quad with a URP Lit material (alpha clip, two-sided, point-filtered texture) that always faces the camera. The sheet's JSON is read at scene-build time into frame rectangles, and a frame is shown by setting the material's tiling and offset. The Part 1 import rule still applies (Point, uncompressed, no mipmaps, Single). No Multiple slicing is needed.
6. **Poses and timings are pure functions where possible.** The simulation (plain C#) emits events; the view reacts. Tests check the rules and the events, never frame timing.
7. **Unverified facts, and when I check them:**
   - Whether `ExportSpriteSheet` from Lua writes frame tags into the JSON (`listTags`): stage 2, with the first Pip script. Fallback: frame ranges for each animation live in the settings and a test checks them against the frame count.
   - Whether a `FullScreenPassRendererFeature` can be added to a renderer asset from code (the feature list is serialised with a map): stage 2 spike, after you approve decision 1. Fallback: build the asset through `SerializedObject`.
   - Whether an alpha-clipped, camera-facing Lit quad casts a clean shadow, and whether it clips into a neighbouring block at its feet (a small push toward the camera is the fix): stage 2 spike.
   - Whether SMAA softens sprite pixels: stage 3, judged on a capture at 1080. Fallback: no anti-aliasing.
   - Sprite snapping to screen pixels and whether it jumps at the end of a turn: stage 3, on a capture and the play test.
   - Whether `capture.sh play` shows motion while the Editor is in the background (a known caveat): the turn is captured at fixed times by setting the camera yaw directly.
   - Whether the solver stays fast: the search space is tiny (at most 125 nodes times 4 views), so no concern.

## Units and layout
Unity units are metres, one cell is one unit. x runs right, z runs forward, y is up. A **cell** (x, y, z) has integer indices 0 to 4. The cell's centre is `(x - 2, y, z - 2)` in world space, so the room's floor centre is the origin. A block fills its cell (a 1 x 1 x 1 cube); its top face is at `y + 0.5`, and that is where Pip stands (pivot at the feet). The camera looks at the room centre `(0, 2, 0)`, orthographic, true isometric (pitch 35.264 degrees), yaw 45, 135, 225 or 315 degrees, 30 units back. The room stays still, the camera turns (this is the same picture as turning the room, and keeps the light rig simple).

On screen: one cell step along a horizontal axis is `(0.707, 0.408)` units (right and down, or its mirror), a cell step up is `0.816` units. Depth step `r_k` is the only move that does not change the screen position.

| Object | Size (x, y, z) | Position | Why |
|---|---|---|---|
| Plinth tray | 7 x 0.6 x 7, rim 0.25 high | top face at y = -0.5, centred on the origin | the wooden base of the diorama |
| Block | 1 x 1 x 1, 0.12 bevel | cell centre | a walkable, solid piece (Blender model, instanced) |
| Pillar block | 0.9 x 1 x 0.9 | cell centre | decoration variant of a block, same top |
| Goal pedestal | 1 x 1 x 1 | cell centre | the Goal block, a round pedestal with the star over it |
| Bridge | sprite quad 1 x 0.5 | cell centre, plank top at the cell's top face | a walkable sprite piece |
| Pip | sprite quad 1 x 1 (32 px at 32 px per unit) | feet at the top face of the node | the character |
| Star | sprite quad 1 x 1 | 0.4 above the Goal node's top face | the goal |
| Decor sprites | 0.5 x 0.5 | on a block's top face, off the walking spot | flowers, mushroom, grass |
| Link ribbon | 0.08 wide dotted line | from node top to node top, in the current view | shows a join |
| Key light | directional, warm, soft shadows | rotates with the camera, from upper left and front | same look in every view |
| Fill | hemisphere ambient, `#9ADBE8` sky, `#5B4B8A` ground | scene | cool fill, violet shadows |
| Camera | orthographic, size 5.625 at 1080 high (96 screen px per unit: 32 px per unit, 3x) | as above | whole-number pixel scale |
| UI | uGUI canvas, scaled with the screen | screen space | HUD, turn arrows, restart, cards |

Pixel scale: `size = screenHeight / 2 / (pixelsPerUnit * scale)`, with `scale` the largest whole number such that the whole room still fits (3 at 1080, 2 at 720, 4 at 1440 if the room fits, never below 1). The scale function is tested.

## Tuning values
Fields of `DioramaTurnSettings` (`Assets/Prototypes/DioramaTurn/Settings/DioramaTurnSettings.asset`), grouped, starting values from the spec:
- `turn`: duration 0.45 s, overshoot 4 degrees, queueMax 1.
- `walk`: stepTime 0.22 s, hopTime 0.35 s, hopHeight 0.6, squash 0.18.
- `rooms`: maxSize (5, 5, 5), parTwoStarsExtra 2.
- `camera`: pitch 35.264, baseYaw 45, pixelsPerUnit 32, targetScale 3, snapSpritesToPixels true, distance 30, spritePushToCamera 0.3.
- `light`: keyColour `#FFF0D6`, keyIntensity 1.1, fillColour `#9ADBE8`, shadowTint `#5B4B8A`, shadowStrength 0.8.
- `post`: bloomIntensity 0.35, bloomThreshold 1.0, vignette 0.2, tiltBandCentre 0.5, tiltBandHalfHeight 0.28, tiltBlur low, saturation +12, contrast +8, antialiasing SMAA.
- `look.surface`: blockSmoothness 0.45, occlusionStrength 0.6, woodTexture 1.0, woodNormal 0.5.
- `feel`: effectStrength 1, goalSlowMo 0.4 for 0.5 s, ribbonPulse, dust and confetti counts, the star-card timings.
- `audio`: master 0.8, music 0.35, musicDuckDb -9 for 1.5 s, per-sound volumes.

## Code design
Assembly `DioramaTurn`, namespace `Prototypes.DioramaTurn`. References `Shared` (TimeFeel for the slow motion), `Unity.InputSystem`, `UnityEngine.UI`. Plain C# first, so tests run it without a scene.

**Rules (stage 1, no engine objects)**
- `Cell` (int x, y, z), `PieceKind` (Block, Bridge, Start, Goal), `RoomData` (size, pieces).
- `ViewRule`: `DepthStep(view)` and `Rotate(view, direction)`. `JoinRule.AreJoined(a, b, view)`: the spec's rule, written once.
- `RoomParser`: layered text maps with a legend into `RoomData`; rejects a bad row length, an unknown character, no Start, two Goals.
- `RoomValidator`: size, one Start, one Goal, no two nodes on one screen spot in any view.
- `JoinGraph`: the joins of a room in one view. `RoutePlanner`: breadth-first shortest route, marks hops, `null` when unreachable.
- `Solver`: least turns to reach the Goal over (node, view) states, plus a witness list of moves; used for par and for tests.
- `Rooms`: the three shipped rooms as text maps.
- `CursorNavigator`: the nearest reachable node in a screen direction.
- `Stars`, `PixelScale`, `SheetFrames` (reads the sheet JSON into rectangles and tags).
- `DioramaSimulation`: the state machine (Idle, Walking, Turning, Complete), `PressTurn(dir)`, `PickNode(node)`, `Restart()`, `Tick(dt)`. It emits `GameEvent`s (TurnStarted, TurnSettled, JoinFormed, JoinBroken, Step, Hop, Unreachable, TurnBlocked, GoalReached, RoomComplete). It never touches Unity.
- `DioramaTurnSettings`: the ScriptableObject above, with range clamps.

**Scene (stage 2 on)**
- `DioramaTurnGame` (MonoBehaviour): owns the simulation, the room flow (title, rooms 1 to 3, finish card) and the loop that feeds events to the view.
- `RoomView`: builds a room's pieces and puts them in place, pops them in and out.
- `CameraRig`: eased turn with overshoot, view angle, pixel-scale ortho size, sprite snapping.
- `SpriteQuad` and `FaceCamera`: a frame by tiling and offset; always faces the camera, pushed toward it by `spritePushToCamera`.
- `PipView`, `StarView`: animation tags (idle, walk, hop, cheer, shake), squash, hop arc.
- `LinkRibbons`: a dotted line per join of the current view, grows and fades on turn.
- `FeelDirector` (events to dust, confetti, bloom pulse, slow motion), `DioramaAudio` (events to sounds and music with ducking), `Hud` (uGUI: top bar, turn arrows, restart, cards, key hints), `DioramaInput` (Input System, mouse, keyboard and gamepad to commands, with a simulated-device seam for tests).

**Editor (assembly `DioramaTurn.Editor`)**
- `DioramaTurnSceneBuilder`: menu `Prototypes/DioramaTurn/Build Scene`, builds the scene twice to the same content. `RendererSetup`: the renderer asset and its list entry (after approval). `SurfaceTextureImporter`: wood textures (normal map type, repeat, mipmaps), same idea as Medieval Chess's own. Sprites need no importer of their own (Part 1).

## Art and audio pipeline
Everything is made by scripts, none by hand, all original. Outputs are never hand-edited.
- **Models:** `Tools/blender/DioramaTurn/make_room_models.py`, run `/Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/DioramaTurn/make_room_models.py`. Writes `Art/Exports/{plinth,block,pillar,goal_pedestal}.glb` and `Art/source/DioramaTurn/room_models.blend` (LFS). Bevelled parts, baked ambient occlusion in the vertex colour (the `BVHTree` recipe in `surfaces.md`), scale applied. Blender +x arrives as Unity -x, so a test checks that the models are mirror-symmetric (blocks are) and that nodes have scale 1.
- **Texture:** `Tools/textures/DioramaTurn/make_textures.py`, run headless in Blender. Writes `Art/Textures/wood_albedo.png` and `wood_normal.png`, 512 px, tiling. Looked at after generation.
- **Pixel art:** `Tools/aseprite/DioramaTurn/*.lua`, run `Tools/aseprite/run.sh DioramaTurn <script>.lua` with `ASEPRITE_PATH` inline. One shared 16-colour palette table (copied at the top of each script, checked by a test). Pip is drawn from one base character map and small per-frame patches, so a diff shows the change. Scripts: `pip.lua` (sheet and JSON, 14 frames), `pieces.lua` (bridge, star sheet, three decor sprites), `ui.lua` (arrows, restart, stars, tap ring, cross), `effects.lua` (confetti, dust, ribbon dot). Each exports to `Art/Sprites/` and saves `Art/source/DioramaTurn/*.aseprite`. I always look at the exported PNG scaled up (outside the repository). If this model cannot read the image, I say so and ask you to look.
- **Audio:** `Tools/audio/DioramaTurn/make_sounds.py` writes about 14 sounds and one music loop into `Audio/` (16-bit mono 44.1 kHz, fade to zero at the end, peak 0.8, seeded noise, self-checks). Whether they sound good is yours to judge.
- **Surfaces (from the spec's table):** blocks flat painted plastic with baked occlusion, no texture; plinth procedural wood in world space (albedo and normal); sprites flat on a lit alpha-clip quad; the star and ribbons flat plus emission with Bloom; backdrop flat; pops (no dissolve) for spawn and removal. Shader work: one hand-written fullscreen shader, `Shaders/DioramaTurnTiltShift.shader`, that blurs by screen row with the band from the settings. Tests come first: the shader compiles with no messages, the textures are square, a power of two, not flat, tile and have unit normals, every model has one vertex colour per vertex and the occlusion varies. Strengths are on `look.surface`, and 0 turns each off.
- **Look items checked for feasibility:** all are buildable here. Risks are in the list of unverified facts above.

## Tasks
Each stage ends with a stop for review (inside `game-build` and `game-polish` the stage stops run back to back, with a commit per stage, unless a check needs you).

### Stage 1: greybox (rules and levels, no scene)
- [x] Run `Tools/unity/check.sh all` first, so a regression is visible. (Not run before stage 1; run at the end of stage 1 instead: 808 EditMode, 69 PlayMode, all green.)
- [x] Create `Assets/Prototypes/DioramaTurn/` with `Scripts`, `Editor`, `Settings`, `Tests/EditMode`, `Tests/PlayMode` and the four assembly definitions; PascalCase, no empty folders, commit every `.meta`.
- [x] Write EditMode tests first: the join rule table in all four views, view rotation, the parser (bad row length, unknown character, no Start, two Goals), the validator, the join graph, route planning with hops and unreachable, the solver on small hand-made rooms, the cursor, the stars, the pixel-scale maths, the sheet JSON reader.
- [x] Write the rules until they pass. `Tools/unity/check.sh editmode`.
- [x] Write `DioramaSimulation` tests first (turn refused while walking, one queued turn, Pip keeps its node, events in order, goal ends the room, restart), then the class.
- [x] Design the three rooms as text maps. Tests: each passes the validator, is solvable, is not solvable with zero turns, and par equals the solver's minimum (room 1: par 1, room 2: par 2 or 3, room 3: par 4 to 6, with one dead-end join).
- [x] The settings class with range clamps and tests (`DioramaTurnSettings`). The asset itself is created by the scene builder in stage 2, so `Settings/` does not exist yet (no empty folders).
- [x] Deliberate-bug check: break the join rule (flip one depth step sign), see tests fail, restore.
- [x] Commit: `feat(diorama-turn): add rules, rooms and solver with EditMode tests`. (The WIP commit `b69b985` already carries the code; this stage closed with the check run and this tick.)


Stage 1 notes (what differed or is still open):
- The starting full check was not run before stage 1 (the repository was green at the last run, 739 EditMode and 69 PlayMode, before this work). After stage 1: EditMode 808 of 808 pass, PlayMode 69 of 69 pass (`check.sh all`).
- Deliberate-bug check done by OpenCode on 2026-11-11: view 0's depth step flipped from (1,1,1) to (-1,1,1) in `ViewRule`. 12 tests failed (join rule table, depth steps, validator, cursor, rooms' validity and par, solver witness moves, simulation, routing) and 796 passed. Restored, and the full run is green again.
- The three rooms were found with a throwaway Python search of the join rule and solver (islands of 1 to 3 blocks, validity and par checked), then pasted into `Rooms.cs` and re-checked by the C# solver in tests. Room 1 par 1 (4 pieces), room 2 par 2 (5 pieces, one bridge), room 3 par 5 (8 pieces, two bridges). They are small and sparse on purpose for the greybox; the "dead-end join" for room 3 is NOT proven (the search only checked par, and that the first view already offers joins). A person should solve all three in the play test and say if room 3 needs a decoy.
- `PixelScale.Scale` takes `roomExtentUnits` (10.5) and `maxScale` (4) as tuning, instead of the spec's `targetScale` 3. Same result: 3 at 1080, 2 at 720, 4 at 1440.
- The simulation emits no `RoomComplete` event. `GoalReached` is the last event, and the game flow (stage 2) shows the card.
### Stage 2: playable (plain shapes, one placeholder sprite)
- [x] Scene builder with primitives (cubes for blocks and plinth, a coloured quad for the bridge), the camera rig, light, HUD, input. Build the scene twice and compare content.
- [x] A first Pip sprite script (three frames) through `run.sh`, to prove the sprite path end to end (JSON reader, tiling and offset, lit quad, shadow). Look at the PNG.
- [ ] Spikes, each on a capture: pixel scale (is a sprite pixel exactly 3 screen pixels at 1080?), sprite shadow and foot clipping, `LinkRibbons`, tag export. (Tag export passed; tilt-shift pass renders; the rest is open.)
- [x] If decision 1 is approved: the renderer asset and the list entry, and the tilt-shift shader spike on a capture.
- [ ] PlayMode tests: scene loads; camera orthographic and exactly on the view angle after each turn; Pip walks to a reachable node and stops; a turn during a walk is refused; each room completes from the solver's witness moves; the next room loads; restart restores the first state.
- [ ] Capture each room in all four views (`capture.sh camera`), check the room fits the frame and against the layout table.
- [ ] Run `Tools/unity/check.sh all`.
- [ ] Commit: `feat(diorama-turn): add playable scene`.

Stage 2 notes (what is done at handoff):
- The view code, scene builder, renderer asset and tilt-shift pass are written and the scene is built; view 0 renders correctly (plinth, four blocks, star, dotted join ribbon, Pip, HUD) and the tilt-shift is visible.
- Two capture-found bugs fixed: a `MaterialPropertyBlock` is not saved with a scene, so the Pip sheet texture is now a serialised field rebound on first use; the camera could bake in a transient tiny game-view height during a capture.
- Deliberate-bug check and PlayMode tests for stage 2 are still to do. The capture-size issue (room renders small in `capture.sh camera` because the tool's render height differs from the game view) is open.

### Stage 3: look (game-polish, small steps, each judged on a render, a commit per step)
- [ ] Baseline capture (`screen`) of each room.
- [ ] Surfaces, in the order textures, occlusion, shader: wood texture, Blender models with occlusion, the materials, the tilt-shift pass (if approved).
- [ ] Final sprites: Pip, bridge, star, decor, UI, effects, from the Aseprite scripts, each looked at scaled up and in the scene.
- [ ] Lighting and shadows, the light turning with the camera, the post volume (Bloom, Vignette, grading), SMAA judged on a capture.
- [ ] One task per feel-table row: turn and settle, join and break (ribbons), tap, steps, hop, blocked, goal, card, next room, restart, idle. Each has a before and an after capture and a test for its logic (timers, strength 0).
- [ ] Audio: the sounds, then the music and the ducking.
- [ ] HUD polish and key hints for the input in use.
- [ ] `feel.effectStrength` at 0 gives no overshoot, particles or slow motion (test).
- [ ] End with the play-test list for you. Commit per step: `feat(diorama-turn): <step>`.

### Stage 4: play test and tidy
- [ ] You play and fill in the "needs a person" criteria, with a real gamepad if you have one.
- [ ] Turn findings into tasks or open questions. Update the spec and tick the criteria.
- [ ] Remove leftovers (spike files, temporary captures) with your approval.
- [ ] Update `README.md` and `AGENTS.md` for the third prototype (needs your OK to touch them).
- [ ] Ask before pushing; then push and open one PR covering the Aseprite tooling and DioramaTurn.

## Verification
| Criterion (from the spec) | How it is checked |
|---|---|
| Join rule table in four views | EditMode `JoinRuleTests` |
| Four turns return to view 0; joins change as the table says | EditMode `ViewRuleTests` |
| Shortest route, unreachable, hops | EditMode `RoutePlannerTests` |
| Turn refused while walking, one queued, Pip keeps its node | EditMode `SimulationTests`, PlayMode `GamePlayTests` |
| Every room valid, solvable, par right, not solvable without a turn | EditMode `RoomsTests` |
| Cursor goes to the nearest reachable node | EditMode `CursorNavigatorTests` |
| Whole-number pixel scale at 1080, 720, 1440 | EditMode `PixelScaleTests` |
| PNGs import as Point Sprites; every pixel in the palette | EditMode `SpriteAssetTests` (and Part 1's importer test) |
| Sheet JSON slices into the expected frames | EditMode `SheetFramesTests` |
| `effectStrength` 0 means no overshoot, squash, particles or slow motion | EditMode `FeelTests` |
| Each room in each view fits the frame, no console error | capture of 12 views, plus a PlayMode console check |
| Each room completes from the solver's moves | PlayMode `RoomFlowTests` |
| Turn feel, join legibility, crisp sprites during the turn, mood, sounds, difficulty, keyboard and gamepad feel | you, in the stage 4 play test (I cannot judge feel or test a real gamepad) |

## Risks
- **A room is too easy or too hard.** The solver gives par and checks solvability, but not how a person finds it. Fallback: retune the text maps after the play test; the tests tell me when a change breaks par.
- **Joins at different depths do not read as joined.** Fallback: stronger ribbon and glow, a brief pulse of both pieces on a turn, and at worst an outline on the pieces of a join.
- **A tilted sprite quad clips into a neighbouring block or casts an odd shadow.** Fallback: the push toward the camera, a simple blob shadow under Pip instead of a cast shadow.
- **SMAA or the tilt-shift softens sprites.** Fallback: SMAA off; keep the sharp band wider so the room never sits in the blur.
- **The renderer cannot be registered from code.** Fallback: author the asset through `SerializedObject`, or Alternative A (no blur).
- **Pixel shimmer during the turn.** Fallback: snap during the turn too, or keep sub-pixel motion and accept it, judged in the play test.
- **The sprite and model art does not match in style.** Fallback: lower the model gloss, add an outline pass to the sprites, tuned on captures.
- **This model cannot read the exported PNGs well enough to judge pixel art.** Fallback: I say so and you look at the PNGs.
