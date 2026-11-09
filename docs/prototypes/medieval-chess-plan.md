# Medieval Chess: plan and tasks

Plan for `docs/prototypes/medieval-chess.md`. Status: draft

## Decisions needed first
1. **Share the shake and hit-stop code (needs your approval).** Neon Pong already has `ShakeState` and `TimeFeel`, and Medieval Chess needs the same two things. `AGENTS.md` says to move code to `Assets/Shared/` when a second prototype needs it, and not to edit another prototype unless the task says so.
   - **Recommended:** move those two classes (and their tests) into a new `Assets/Shared/` assembly, and make Neon Pong reference it. This touches Neon Pong (PR #1), but the change is mechanical and its tests stay green.
   - **Alternative:** copy the two small classes into Medieval Chess. Nothing in Neon Pong changes, and the code is duplicated.
   - I will not touch Neon Pong until you answer.
2. **Stage order differs from the skill's default, on purpose.** The skill's default is greybox, playable, look, tidy. The spec says the animated kill is the riskiest part and you asked for it explicitly, so the plan builds it first, on a bare board, with plain shapes (stage 1). The look stage still comes after the game is playable.
3. **Two pull requests in total.** PR #1 (Neon Pong, base `main`) and PR #2 (Medieval Chess, base `feat/neon-pong`). Every stage below is a commit on `feat/chess` in PR #2, and I push at each stage stop so the PR shows progress. When #1 is merged, PR #2's base is changed to `main`.
4. **No new packages.** URP, Input System, glTFast and uGUI are already in the project, and the computer's search uses `System.Threading`. Nothing to approve.
5. **No changes to `ProjectSettings/` or the shared URP assets.** I checked `Assets/Settings/PC_RPAsset.asset`: main light shadows, additional light shadows and soft shadows are already supported. Shadows are then switched on per light in the scene, so no change is needed. (Caveat: I did not check the Mobile asset, which is used by the lowest quality level.)
6. **Font.** The built-in font stays. A medieval font needs a free licence file in the repository and your approval, so it is listed for stage 7, not a blocker.
7. **Facts checked now, with a throwaway model that I deleted:**
   - Blender's glTF export keeps a parent and child hierarchy, and Unity imports it as nested transforms with the names kept. A part whose origin is at a joint (a shoulder) keeps that pivot, so it can be rotated by code. Checked in Unity 6000.6.8f1.
   - An object scale in Blender becomes a node scale in Unity. The script must apply scale to the mesh, so nodes have scale 1.
   - **Blender +x arrives as Unity -x** (the import mirrors x). Neon Pong never noticed because its models are symmetric. The piece script must account for this, and a test must check that a part meant for the right side ends up on the right side.
   - A "root empty" parent was merged into the asset's own root node, so the model's root is the asset name. Code finds parts by name below the root.
8. **Facts still unverified, to settle in the stages that need them:**
   - Whether a Marshal search reaches depth 5 in 3 s on this machine (stage 4 measures it, and the time cap protects the player either way).
   - How long the EditMode perft tests and the strength test take (stage 3 and 4 measure them, and slow ones can be cut down).
   - Whether four torches plus a key light with shadows are worth their cost (stage 6, judged on a render).
   - Red against slate readability, and the fight length. Judged on renders and in play.
9. **Design choice that makes the animation testable: poses are a pure function of time.** A fight is a plan, and `Evaluate(t)` returns every part's pose at time `t`. The scene only applies that pose. So tests can check any moment exactly, and a capture can freeze a fight at an exact time without relying on frame timing (a problem found in Neon Pong).

## Units and layout
Unity units are metres. The board top is the plane y = 0, up is y. Files a to h run along x, ranks 1 to 8 run along z: square (file `f`, rank `r`, both counted from 0) has its centre at `(f - 3.5, 0, r - 3.5)`. The bone side starts on ranks 1 and 2 (near the camera, at negative z) facing +z, and the red side starts on ranks 7 and 8 facing -z. The camera sits at negative z, looking toward +z and down.

| Object | Size (x, y, z) | Position | Why |
|---|---|---|---|
| Board tiles (64) | 1 x 0.12 x 1 each, two materials | top face at y = 0, centres as above | the squares the rules use |
| Board frame | 9.4 x 0.3 x 9.4 outside, 0.7 border | top at y = 0.05 | a carved border, with room for the coordinates |
| Hall floor | 24 x 0.2 x 24 | top at y = -0.3 | dark stone around the board |
| Pawn | 0.55 wide, 0.5 tall | on a square, y = 0 | a footman, the smallest piece |
| Knight | 0.7 long, 0.9 tall | on a square | a pony and rider |
| Bishop | 0.6 wide, 0.8 tall | on a square | a cleric with a mitre |
| Rook | 0.7 wide, 0.75 tall | on a square | a tower warden |
| Queen | 0.65 wide, 1.0 tall | on a square | a crowned warrior |
| King | 0.7 wide, 1.1 tall | on a square | the tallest |
| Captured trays (2) | 1.4 x 0.1 x 8 | x = -6 and x = 6, top at y = 0 | captured pieces stand here as 0.6 scale figures |
| Torches (4) | stand 0.2 wide, 2.0 tall, flame on top | (±5.4, 0, ±5.4) | the main light |
| Banners (2) | 0.8 x 1.6, hung from a post | behind the far side, z = 6.5, x = ±2 | the hall |
| Selection and move rings | 0.9 wide discs, 0.02 tall | on a square, y = 0.01 | selection and legal moves |
| Camera | perspective, field of view 35 | pitched down 50 degrees, about 15 m from the board centre (tuned on a render) | the whole board and both trays in view |
| Key light | directional, warm, low, shadows on | pitched about 40 degrees from behind the camera side | form and weight |
| Torch lights (4) | point, warm, flicker | at each flame | warm pools on the board |

A piece's origin is at the centre of its base on the board top, and it faces local +z. The Duel scene (stage 1) uses the same board plane with two pieces on it.

## Tuning values
All fields of `MedievalChessSettings` (`Assets/Prototypes/MedievalChess/Settings/`). Starting values are the spec's.
- **Board:** square size 1, frame width 0.7, piece heights above.
- **Moves:** step time 0.35 s per square, knight leap 0.55 s, rook slide 0.5 s, lift 0.12 m over 0.12 s.
- **Fight:** approach 0.35 s, per-attacker wind-up and blow times (about 0.6 s total), hit-stop 0.07 s, collapse 0.6 s, camera push 35 percent and lower 25 percent, shake strength 0.12 m, total about 1.8 s at Full, Quick factor 3.
- **Difficulty (a list of three profiles):** name, depth, quiescence on or off, slack in centipawns, blunder chance, minimum think time, time cap, table size. Squire: depth 1, slack 120, blunder 1 in 7, delay 0.8 s. Knight: depth 3, quiescence, slack 10, cap 1.5 s. Marshal: depth up to 5, cap 3 s, table 2^18.
- **Evaluation:** piece values 100, 320, 330, 500, 900, the game-phase rule, mate score 100000, and the position-table scale.
- **Look:** palette hex values, emission of the flames, ring and check pulse, Bloom, Vignette, torch light intensity and range, flicker amplitude and speed, camera pitch, field of view and distance, shadow strength.
- **Feel:** effect strength 0 to 1, shake duration and decay, wash alpha, checkmate slow motion scale and duration, ember and dust counts, animation speed (Full, Quick, Off), master volume.

## Code design
Assembly `MedievalChess`, namespace `Prototypes.MedievalChess`. Plain C# first, so tests run it without a scene.

**Rules (stage 3)**
- `Piece`, `PieceType`, `Side`, `Square` (0 to 63), `Move` (from, to, promotion piece, flags: capture, en passant, castle, double push, promotion).
- `Position`: the board, side to move, castling rights, en passant square, move counters and a hash. `MakeMove` and `UnmakeMove` restore the position exactly.
- `MoveGenerator`: legal moves, with a quicker pseudo-legal pass for the search.
- `Fen`: read and write positions.
- `ChessGame`: the position plus history, the result (checkmate, stalemate, threefold, fifty-move, insufficient material, resignation), and repetition counting.

**Computer (stage 4)**
- `Evaluator`: material plus original piece-square tables, with the game-phase switch for the king.
- `Search`: alpha-beta with iterative deepening, quiescence, move ordering, a transposition table, a time cap and a cancel flag.
- `DifficultyProfile` (data in the settings) and `ComputerPlayer`: picks a move for a profile and a seeded random source. Runs the search on a worker thread, and the game polls it each frame. The worker never touches Unity objects.

**Animation (stages 1 and 2)**
- `PartPose` (position offset, rotation, scale for one part), `Track` (keyframes with easing for one part), `Easing`.
- `FightPlan`: tracks for the attacker, the victim and the camera, a list of marked moments (the blow, the collapse start) and a total length. `Evaluate(t)` is a pure function.
- `FightLibrary`: one attack routine per piece type (pawn, rook in stage 1, the other four in stage 2), the collapse, and the move plans (walk, knight leap, rook slide, castling, promotion, check brace, resign, checkmate finale).
- `MoveDirector`: turns a rules `Move` and the result into a list of plans to play: a quiet move, a capture fight, en passant, castling, promotion, check and checkmate.
- `Timeline`: plays a plan with speed (Full, Quick, Off) and hit-stop, and reports the moments it passes.
- Shared (decision 1): `ShakeState` and `TimeFeel`.

**Scene**
- `MedievalChessSettings` (ScriptableObject).
- `PieceView`: one piece's parts (found by name) and `PartAnimator` applying poses to them.
- `BoardView`: square to world position, and back from a ray.
- `GameController` (MonoBehaviour): the state machine (Menu, PlayerTurn, ComputerThinking, Animating, ChoosingPromotion, GameOver), the game, the computer, and the move queue.
- `InputController`: the mouse through the Input System, a ray against the board plane, and the square it hits.
- `HighlightView`, `CameraRig` (shake, push-in, fight cut-in), `GameAudio`, and `UiView` (start screen, top bar, trays, promotion choice, result). Text uses uGUI `Text` with the built-in font, as in Neon Pong.
- `Editor/MedievalChessSceneBuilder`: two menu commands. `Prototypes/MedievalChess/Build Duel Scene` (stage 1) and `Prototypes/MedievalChess/Build Scene` (stage 5).

Test assemblies `MedievalChess.Tests.EditMode` and `MedievalChess.Tests.PlayMode` (all platforms, as before).

## Art and audio pipeline
- **Pieces:** `Tools/blender/MedievalChess/make_pieces.py` writes six `.glb` files (`pawn`, `knight`, `bishop`, `rook`, `queen`, `king`) into `Assets/Prototypes/MedievalChess/Art/Exports/`, and `Art/source/MedievalChess/pieces.blend`. Each has a node hierarchy with named parts: `Body`, `Head`, `ArmR` (pivot at the shoulder) with `Weapon`, `ArmL` with `Shield`, `Base`. The knight adds `Horse` and `HorseHead`, and the rook has `Tower`, `FistL`, `FistR`. Units are mirrored for the x flip (decision 7), and the script checks sizes, pivots, part names and triangle counts and refuses to export if something is off. Stage 1 makes plain shapes from boxes, cylinders and spheres. Stage 6 adds bevels, the crests and the Lewis-style details, and the part names stay the same, so animations keep working.
- **Board and props:** `make_board.py` writes `board.glb` (tiles in two materials, the frame), `torch.glb` and `banner.glb`. Highlight rings, flames, embers and dust are made in Unity.
- **Sounds:** `Tools/audio/MedievalChess/make_sounds.py` (standard library only) writes about 16 sounds as in Neon Pong, checking its own output (length, silence, clipping, ending near zero).
- **Run:** `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/blender/MedievalChess/<script>`, and `python3 -I Tools/audio/MedievalChess/make_sounds.py`.
- **Feasibility of the look items:**
  - Chunky Lewis-style pieces: buildable from bevelled primitives, with the faces and carving simplified. The knight (pony and rider) is the hardest silhouette.
  - Fights: procedural, by rotating and moving named parts, with no skeleton. Six attack routines is realistic. 35 pairings is not.
  - Embers and dust: Particle System, no package. Flames: particles plus a flickering light.
  - Shadows, Bloom, Vignette: already used in Neon Pong, with the lessons recorded in `urp-effects.md` (emission in linear light, linear-light UI blending).
  - Fights as a camera cut-in: camera push and shake by code, no Cinemachine.

## Tasks
Each stage ends compiled, tested and committed, then stops for your review. All commits go on `feat/chess`, and I push at each stop.

### Stage 1: the duel slice (one piece kills another)
Done. Decision 1 was approved (move to Shared).
- [x] Run `Tools/unity/check.sh all` and note the starting result: compile ok, 76/76 EditMode and 12/12 PlayMode (Neon Pong).
- [x] Move `ShakeState` and `TimeFeel` to `Assets/Shared/Feel/` with their tests (`Shared.Tests.EditMode`), and make Neon Pong reference `Shared`. Neon Pong's counts stayed 76 and 12 in total, and its render of a paddle hit looked the same before and after. Commit `3556714`.
- [x] Create the folders and four assembly definitions.
- [x] Write `make_pieces.py` (plain articulated shapes for all six pieces) and run it. The script refuses to export if a part is missing, a height is off by more than 0.06, a base is wider than 0.9, or a part has a scale that is not 1. It caught three wrong heights on its first run. Checked in Unity by `PieceModelTests`: part names, weapon under the right arm and shield under the left, the right arm on +x, joint pivots, scale 1, heights, triangle budget (180 to 774 per piece), material names `body` and `iron`.
- [x] Write the EditMode tests first (fight plan, timeline, models), then `PartPose`-style channels, `ActorPlan`, `FightPlan`, `FightLibrary` (pawn and rook), `FightTimeline` and `PieceActor`. `check.sh editmode`: 220/220 (144 new).
- [x] `Build Duel Scene` (a strip of board with two pieces' models, camera, key light, label) and `DuelController`. Built twice gives the same content, and a test checks the committed scene matches a fresh build.
- [x] PlayMode tests: the duel plays to the right end state at Full, Quick and Off, arms return to rest, the camera pushes in and settles back exactly, strength 0 never moves the camera, replays end the same. 23/23 including Neon Pong's 12.
- [x] Deliberate-bug check: made the victim start falling before the blow, 8 tests failed, restored and 220/220 pass.
- [x] Frozen frames of both fights (start, approach, wind-up, blow, collapse, end) read on renders. Both read as one piece killing the other.
- [x] Commit: `feat(medieval-chess): add the duel slice with pawn and rook fights`.

What differed from the plan, and why:
- **Rook slam angle:** the first version slammed straight down beside the rook, so the fists never touched the pawn. The fists now come down and forward (-80 degrees), and the stand-off is 0.8 m. Found on the blow frame.
- **Camera push:** moving the camera toward the duel without re-aiming it cut the rook's head off. The camera now keeps looking at the focus point while it pushes and lowers.
- **Hit-stop in Quick:** Quick has no hit-stop, as well as no camera cut and no shake.
- **Step owns the camera:** `DuelController.Step` moves the fight and the camera, so tests and captures drive it by hand with `Step` and `ShowAt`.
- **Speed lives on the controller:** the F, Q and O keys change the controller's `speed`, not the settings asset, so playing never edits an asset.
- **No `.blend` file:** the script is the source, so there is no `Art/source/MedievalChess/` file this time.
- **Colours:** bone looks tan and the red looks bright under the warm key light. The look stage will tune this.

### Stage 2: all six fights and the moves
- [ ] Tests first: each of the six attacker types has a routine, with its own blow time and its own pose at the blow, and every routine ends in the right state.
- [ ] Add the knight (lance charge), bishop (mace swing), queen (sword sweep) and king (overhead blow) routines.
- [ ] Add the non-capture plans: walk, knight leap, rook slide, castling, promotion (kneel, flash, rise), check brace, checkmate finale and resign.
- [ ] `MoveDirector` tests: a quiet move, a capture, en passant (the victim is not on the target square), castling (two pieces) and promotion each give the right plans and end state.
- [ ] Update the Duel scene so every attacker and victim pair can be played, and freeze a frame at the blow for each of the six.
- [ ] Deliberate-bug check, and commit: `feat(medieval-chess): add all six fights and the move animations`. Stop for review.

### Stage 3: the chess rules
- [ ] Tests first: perft for the start position (depths 1 to 4), Kiwipete (1 to 3), positions 3 (1 to 4), 4, 5 and 6 (1 to 3), using the numbers in the spec. FEN read and write. Castling, en passant and promotion special cases. Check, checkmate, stalemate, threefold repetition, fifty-move rule, insufficient material. Make and unmake restore the position.
- [ ] Write `Position`, `MoveGenerator`, `Fen` and `ChessGame`. Run `check.sh editmode` and note how long the tests take.
- [ ] Play a game from a list of known moves to a known checkmate, and one to a known stalemate.
- [ ] Deliberate-bug check (break en passant or castling through check), and commit: `feat(medieval-chess): add the chess rules with perft tests`. Stop for review.

### Stage 4: the computer and its three difficulties
- [ ] Tests first: only legal moves, mate in one found at Knight and Marshal, a free queen is not left hanging at Knight and Marshal, Squire sometimes plays worse than its best, a fixed seed gives the same move, the time cap holds within a margin, a cancelled search stops, and the strength order over seeded games (Squire against Knight, Knight against Marshal, with depth limits so the test stays fast).
- [ ] Write `Evaluator`, `Search`, `DifficultyProfile` data and `ComputerPlayer` with the worker thread.
- [ ] Measure: how deep Marshal gets in 3 s, how long Knight takes, and how long the strength test takes. Adjust the profiles and write the real numbers in the plan and spec.
- [ ] Deliberate-bug check (make the evaluator reward losing material), and commit: `feat(medieval-chess): add the computer with three difficulties`. Stop for review.

### Stage 5: playable
- [ ] Tests first: the controller state machine (menu to player turn to animating to computer to game over), an illegal click does nothing, a promotion needs a choice before the move, the computer replies once, Play Again resets. Builder tests (every part, building twice, committed scene matches).
- [ ] Write `BoardView`, `PieceView`, `InputController`, `HighlightView`, `UiView` (start screen with difficulty and side, top bar, trays, promotion choice, result), `GameController` and the camera rig basics. Plain look for now.
- [ ] `Build Scene`, then PlayMode tests: the start screen starts a game with the chosen side and difficulty, a click and a legal square moves a piece, a capture plays its fight and then the board matches the rules, a game over shows the result.
- [ ] Play a full game against each difficulty in a test run (computer against computer with a fixed seed) to catch a crash or a stuck state.
- [ ] Capture the start screen, the opening position, a selection with legal moves, a fight and the game over screen (`capture.sh play`).
- [ ] Deliberate-bug check, and commit: `feat(medieval-chess): add the playable game`. Stop for review.

### Stage 6: look and feel (game-polish)
Each step has before and after captures, tests for its logic and its own commit, as in Neon Pong.
- [ ] Baseline capture, then the Lewis-style piece details and crests (bevels, faces, side shapes) in `make_pieces.py`. Check the six types and both sides can be told apart.
- [ ] Board, frame, hall floor, torches and banners. Palette, materials and emission in linear light.
- [ ] Lighting (key light with shadows, torch lights with flicker), then Bloom, Vignette and tonemapping judged on renders. Keep or cut the torch shadows.
- [ ] Camera: pitch, distance, the fight cut-in, push-in and slow push for checkmate.
- [ ] Sound script and tests on the files, then wire sounds to every feel-table row.
- [ ] Feel rows one by one: selection and legal move rings, check pulse, ember and dust bursts, hit-stop and shake on the blow, the checkmate wash and slow motion, draw and resign.
- [ ] The animation speed setting (Full, Quick, Off) and the global effect strength. Renders and tests at strength 0.
- [ ] Final renders (start screen, opening, selection, fight at its blow, check, checkmate, draw).
- [ ] Commit per step: `feat(medieval-chess): <effect>`. Stop for the play test.

### Stage 7: play test and tidy
- [ ] You play each difficulty and fill in the "needs a person" criteria in the spec.
- [ ] Turn findings into tasks or open questions. Tune values on the settings asset.
- [ ] Decide the font (needs your approval and a licence record), and the legacy Neon Pong leftovers.
- [ ] Update the spec, tick the criteria and set this plan to complete. Update the README to list both games.
- [ ] Update PR #2's description, and change its base to `main` if #1 has been merged.

## Verification
| Criterion | How it is checked |
|---|---|
| Blender script writes every model with sizes, pivots, part names and triangle counts | The script's own checks, plus a check on the imported parts in Unity (stage 1, stage 6) |
| Move generation matches the perft counts | EditMode perft tests (stage 3) |
| Castling, en passant and promotion special cases | EditMode rules tests (stage 3) |
| Checkmate, stalemate, repetition, fifty-move, insufficient material, no self-check | EditMode rules tests (stage 3) |
| Three difficulties show on the start screen and start the game with the right side to move | PlayMode test (stage 5) |
| The computer plays only legal moves, finds mate in one, does not hang its queen, Squire is beatable | EditMode AI tests (stage 4) |
| Strength order Squire, Knight, Marshal | EditMode seeded strength test (stage 4) |
| The computer respects its time cap and the game stays responsive | EditMode timing test, PlayMode check of frame progress while it thinks (stages 4 and 5) |
| Every capture plays the right fight and leaves the right state, animation never changes game state | EditMode plan tests (stages 1 and 2), PlayMode tests (stages 1 and 5) |
| Quick, Off and strength 0 give the same state, faster or no fight, zero shake | EditMode and PlayMode tests (stages 1 and 6) |
| A render shows the board, pieces, torches and highlights, and the six types and two sides are told apart | `capture.sh play` and frozen-frame renders, read by me, confirmed by you (stages 5 and 6) |
| Every sound exists, is audible, does not clip and ends near zero | The script's checks and an EditMode file test (stage 6) |
| `Tools/unity/check.sh all` passes | Run at the end of every stage |
| Looks medieval and heavy, pieces look like warriors | You, from renders and play (stage 7) |
| All six types and both sides tell apart, including red on dark squares | You (stage 7) |
| A capture feels like a fight with weight, and Quick and Off are fast enough | You (stage 7) |
| The three difficulties feel right and none makes you wait | You (stage 7) |
| Check, checkmate and draw are clear | You (stage 7) |
| Sounds fit and are balanced | You (stage 7) |
| Mouse controls feel precise, promotion is easy | You (stage 7) |

## Risks
- **The fight does not read as a kill** with simple jointed shapes. Mitigation: stage 1 tests this on a bare board before anything else. Fallback: change the routines, add a short impact flash and the collapse's embers earlier, or simplify the pieces. If it still does not read, I stop and tell you before building the rest.
- **Procedural fights look stiff.** Fallback: more keyframes, easing and overlap (the head and weapon trailing the body), and a camera shake and hit-stop to cover rough poses.
- **A perft or strength test is too slow** for the EditMode loop. Fallback: lower the depths, and keep the slow ones out of `check.sh` by marking them, and say so in the plan.
- **Marshal does not reach depth 5 in 3 s.** Fallback: lower its target depth and re-balance Knight, and record the real numbers. The time cap always protects the player.
- **A search on a worker thread leaks or never stops.** Mitigation: a cancel flag checked inside the search, a test that a cancelled search stops, and the thread ends when the game object is destroyed.
- **The node hierarchy or the x mirror behaves differently** for a complicated model than for the probe. Fallback: export each part as its own `.glb` and assemble the piece in code.
- **Red pieces on slate squares are hard to see.** Fallback: lighten the squares or the red, add a thin lit rim, and rely on the crest shape.
- **The knight's silhouette is poor.** Fallback: make the horse head larger and the rider smaller, or use a bold horse head on a base.
- **Shadows and flickering lights cost too much or hide pieces.** Fallback: drop the torch shadows and keep the key light's.
- **Moving the shared code breaks Neon Pong.** Mitigation: its tests and a render comparison are part of the first task of stage 1. Fallback: copy the two classes instead (decision 1, the alternative).
- **The Unity Editor must be open on the project,** and a scene file changed on disk while open can block it with a dialog (this happened once). I will not restore scene files while the scene is open.
