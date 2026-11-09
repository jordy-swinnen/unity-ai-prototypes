# Medieval Chess: plan and tasks

Plan for `docs/prototypes/medieval-chess.md`. Status: stage 7 prepared, waiting for your play test

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
Done.
- [x] Tests first: each of the six attacker types has a routine with its own blow time and its own pose at the blow, every victim type falls the same way, and every routine ends in the right state.
- [x] Added the knight (draws back, rears its horse's head, charges with a levelled lance), bishop (mace swing from behind the head, book held up), queen (level sword sweep with the body turning) and king (slow raise, hardest overhead blow).
- [x] Added the non-capture plans: walk (one thud per square), knight leap (arc), rook slide, castling (the king strides and the rook hops over), promotion (the pawn kneels, the new piece rises), check brace, checkmate fall, resign.
- [x] `MoveDirector` tests: a quiet move, a capture (travel up, then fight), a pawn and a knight capturing, en passant (the victim is not on the landing square), castling both ways, promotion to each of four pieces, promotion with a capture, check, checkmate, resign, every step starting where the last ended, finite and repeatable at every moment, the total is the sum of the steps.
- [x] Duel scene: keys 1 to 6 pick the attacker, Q to Y the victim, M plays the next of eleven demo moves, F1 to F3 pick the speed. Every attacker and victim pair plays, and every demo ends in the same place at Full, Quick and Off.
- [x] `check.sh all`: 401 EditMode (from 220) and 34 PlayMode (from 23).
- [x] Deliberate-bug check: removed the en passant landing step, 1 EditMode and 1 PlayMode test failed, restored and everything passes.
- [x] Frames at the blow for knight, bishop, queen and king, and mid-step frames for the leap, castling, promotion and checkmate, read on renders.
- [x] Commit: `feat(medieval-chess): add all six fights and the move animations`.

What differed from the plan, and why:
- **The director takes a `MoveDescription`, not a rules `Move`.** The rules do not exist yet. Stage 5 turns a rules move into a description (world positions, ids, flags).
- **Steps share an interface (`IActionPlan`).** A fight and a move plan both have actors, a duration, an optional blow and footfall times. `FightTimeline` takes any plan, and `SequenceTimeline` plays the steps of a move in order.
- **Pieces turn back after a fight,** so a rook that captured sideways does not stay facing sideways.
- **The knight stands further back and charges further** (`knightExtraStandOff`, `knightLunge`) because its lance is long.
- **A long walk is capped at 1.4 s** (`maxWalkTime`), so a queen crossing the board does not take 2.7 s. Footfalls speed up to match.
- **Speed keys are now F1, F2 and F3.** The letters are used for the victim.
- **`Key` clashed with the Input System,** so the channel's key type is `ChannelKey`.
- **The queen's sweep is hard to read from the side.** The duel camera looks along the fight's side, which hides a horizontal sweep. Judge it from the game camera in stage 5.
- **Footfall times exist but are silent.** The thuds and the dust come in stage 6.

### Stage 3: the chess rules
Done.
- [x] Tests first: perft for the start position (depths 1 to 5), Kiwipete (1 to 3), position 3 (1 to 4), position 4 and its mirror, 5 and 6 (1 to 3), with the numbers from the Chess Programming Wiki. FEN read and write, with bad FENs refused. Castling (each way, through, out of and into check, blocked, rights lost by a king move, a rook move, a rook taken), en passant (one move only, both colours, the pinned case, answering a check), promotion (four pieces, with capture), pins, checks, kings side by side, pawn pushes and captures, move flags. Make and unmake restore the position and the hash over long random games. Mate, stalemate, threefold repetition, the fifty-move rule, insufficient material (ten cases), resignation, illegal moves refused.
- [x] Wrote `Position`, `MoveGenerator`, `Fen`, `Perft`, `ChessGame` and the helpers (`Pieces`, `Squares`, `Move`, tables, hash keys). Every published perft number matched on the first run of the real tests.
- [x] `check.sh editmode`: 509 tests pass. Speed: perft(4) of the start in 72 ms, Kiwipete perft(3) in 33 ms, perft(5) of the start (4,865,609 nodes) in 1.7 s, about 2.8 million moves a second. That is plenty for the search in stage 4.
- [x] Played known games: Fool's mate (black wins), the Scholar's mate (white wins), Sam Loyd's ten-move stalemate, a knight shuffle to threefold repetition, and fifty-move and insufficient-material endings from FENs.
- [x] Deliberate-bug check: made a captured rook keep its castling right, and 1 test failed. The perft numbers did **not** catch it (they do not reach a rook captured on its corner at those depths), which is why the direct castling-rights test exists.
- [x] Commit: `feat(medieval-chess): add the chess rules with perft tests`.

What differed from the plan, and why:
- **Perft does not catch everything.** The deliberate bug passed every perft count. The special-rule tests are not optional.
- **FENs are validated.** A position where the side not to move is in check is refused. Three of my own test positions broke that rule and had to be fixed, so the check works.
- **Perft depth 5 of the start position is included** (1.7 s). The plan listed depths 1 to 4.
- **A finished game has no legal moves** (`LegalMoves` is empty after any ending, including a draw), so the controller cannot offer a move after the end.
- **Repetition keys ignore an en passant square nobody can use,** as the FIDE rule says, so a position is the same only if the same moves exist.
- **Not done here:** turning a rules move into a `MoveDescription` for the director. That is stage 5.

### Stage 4: the computer and its three difficulties
Done.
- [x] Tests first: the evaluator (mirror symmetry, material, knight placement, king safety, endgame king, bishop pair, passed and doubled pawns, dead draws score zero), the search (a mate in one at depths 1 to 4, mate in two, a free piece, a defended pawn, an attacked queen, a trap that a shallow search falls into, the time cap, cancelling, repeatability, slack, mate scores), and the player (the three levels and their numbers, only legal moves through whole games, mate in one at every level, Squire slips and Marshal never does, blunder chance 1 and 0, repeatable with a seed, the async player waits, can be cancelled while searching or waiting).
- [x] Wrote `Evaluator`, `Searcher` (alpha-beta, iterative deepening, transposition table, move ordering with killers and history, a search of captures at the end of each line, check extension, repetition and fifty-move draws, the clock), `ComputerPlayer` (a synchronous `Choose` and an async `ChooseAsync` on a worker thread), `DifficultyProfile` and `EvaluationSettings`. The three profiles and the evaluation numbers are on `MedievalChessSettings`.
- [x] `check.sh all`: 565 EditMode (one slow test is `Explicit` and does not run by default) and 34 PlayMode pass, in about 55 s.
- [x] Deliberate-bug check: flipped the sign of the evaluation, 11 tests failed, restored.
- [x] Measured on this machine (Unity Editor, one thread):

| Position | Squire | Knight | Marshal (limit 12 plies, 3 s cap) |
|---|---|---|---|
| Start | 1 ply, 1 ms | 3 plies, 6 ms | 7 plies in 1.3 s |
| Kiwipete (busy) | 1 ply, 1 ms | 3 plies, 106 ms | 5 plies, stopped by the cap at 3.0 s |
| Italian-style middlegame | 1 ply, 2 ms | 3 plies, 74 ms | 6 plies in 2.8 s |
| Sicilian-style middlegame | not run | not run | 6 plies in 2.0 s |
| Rook endgame | 1 ply, 0 ms | 3 plies, 2 ms | 9 plies in 1.3 s |

  The search runs at about 220,000 to 430,000 positions a second. Marshal's depth varies from 5 to 9 plies, typically 6 or 7.
- [x] Measured strength, over many games (adjudicated by material after 100 plies, 3 pawns or more ahead wins): **Knight (3 plies) beat Squire (1 ply) 16 to 0 in 16 games.** **Marshal (4 plies) beat Knight (3 plies) 7 to 3 in 10 games.** Marshal at its real depth should be stronger still.
- [x] Commit: `feat(medieval-chess): add the computer with three difficulties`.

What differed from the plan, and why:
- **Marshal looks up to 8 plies, not 5.** Five plies finished in 0.02 to 1.2 s, so the 3 s cap was mostly unused. The spec said "five moves ahead", but my numbers count plies (half-moves), so five was only two and a half moves each. Marshal now searches until the 3 s cap, or 8 plies. I changed the spec to match.
- **Depth means plies.** Squire looks 1 ply, Knight 3 plies plus a capture search, Marshal up to 8.
- **Knight is fast.** It uses at most 0.11 s of its 1.5 s cap. If it feels too weak in the play test, give it a fourth ply: it is one number.
- **The strength order is proved by games, but a short run is too noisy.** Two games of Marshal against Knight gave a loss and a draw by material, which says little. A test that counted how often each depth agrees with a deeper search was not steady either (depth 1 agreed 8 times out of 10 and depth 3 only 7). So the default run checks Knight against Squire (four games, deterministic), and the Marshal against Knight check is `Explicit` (ten games, about six minutes) and was run by hand: 7 to 3.
- **A dead draw scores exactly zero** (king against king, one minor piece, same-coloured bishops), so the search does not chase an endgame it cannot win.
- **The position tables are made from the numbers in `EvaluationSettings`** (distance from the middle of the board for knights, bishops, queens and the endgame king, and small tables for pawns, rooks and the king's shelter). Nothing is copied from the Simplified Evaluation Function, which I used only for the piece values and the ideas.
- **Not done here:** the pause for show (`minThinkSeconds`) is built into `ChooseAsync`, but the scene does not use it yet. Stage 5 wires it up.

### Stage 5: playable
- [x] Tests first: `BoardTrackerTests`, `MoveDescriberTests`, `BoardGeometryTests` and `GameSceneBuilderTests` in EditMode, and `GamePlayTests` in PlayMode (menu to player turn to animating to computer to game over, illegal clicks do nothing, promotion needs a choice, the computer replies once, Play Again resets, clicks are ignored while a fight plays).
- [x] Wrote the game layer in `Scripts/Game/`: `BoardGeometry`, `MoveDescriber`, `BoardTracker`, `SequenceStage`, `GameLayoutSettings`, `HighlightView`, `InputController`, `GameCameraRig`, `UiView`, `GameController`. Plain look.
- [x] `Prototypes/MedievalChess/Build Scene` writes `Scenes/MedievalChess.unity` (board, frame, trays, rings, start screen, top bar, promotion and result panels). PlayMode tests cover the menu, a move with the computer's reply, a capture's fight and tray, check, promotion, checkmate, stalemate, resign, and mouse picking for all 64 squares from both sides.
- [x] Whole games, computer against a seeded random-ish player, against all three difficulties and both sides, finish without a crash or a stuck state.
- [x] Captured the start screen, the opening position, a selection with legal moves, a knight fight mid-way, a queen fight, the promotion choice and the game over screen.
- [x] Deliberate-bug check: flipped the rank direction in `BoardGeometry`. Three tests failed (square from a point, the board centre, the committed scene). Restored. Result: EditMode 601/601, PlayMode 54/54.
- [x] Commit: `feat(medieval-chess): add the playable game`.

What differed from the plan, and why:
- **No `BoardView` or `PieceView`.** The scene builder makes the tiles, and the existing `PieceActor` from stage 1 is the piece view. `BoardTracker` keeps a piece for each starting square and checks that what is shown equals the rules' position.
- **The computer replies after a pause for show** (`minThinkSeconds`, now wired up through `ChooseAsync`). The search runs on a worker thread, so the frame rate stays smooth while it thinks.
- **The "human" in the whole-game tests is a depth 2 computer player**, because random moves lost to a quick mate and ended the games too early.
- **Fonts are Unity's built-in font.** The font choice stays for stage 7 (needs a licence record).
- **Not judged here:** how fights feel in motion, the look (still a plain, warm-lit greybox), and sound (none yet). Those are stages 6 and 7.

### Stage 6: look and feel (game-polish)
Each step has its own tests and its own commit. Results below.
- [x] Baseline capture (`b0-opening`): flat, no post-processing, no torches, orange bone pieces. Wrote down what was off against the brief.
- [x] **Hall** (`feat: light the hall with torches and add post-processing`): four flickering torches (a pure function of time, scaled by the effect strength), a cool shadow-casting fill light, fog, a global Volume with Bloom, Vignette, ACES tonemapping and light colour grading. `LookSettings` holds every value. Tests: flicker bounds, zero amplitude steady, volume and torches from the settings.
- [x] **Pieces** (`feat: add bevels, faces and side shapes`): bevelled boxes, stepped bases, a face with a heavy brow on every piece, veil, beard, cape, four-point crown, mitre cross, round pawn shield. The bone side has round pauldrons and the cinnabar side pointed horns (`ShoulderBone` and `ShoulderRed`, one pair shown by `PieceActor.Bind`), so the sides differ by shape too.
- [x] **Sound** (`feat: add sounds, particles and the endings`): `Tools/audio/MedievalChess/make_sounds.py` writes 22 sounds. `SoundCues` turns each step into cues (swing, blow, drum, collapse, footfalls, leap, slide, anvil, horns). `SoundBank` plays them. File checks in EditMode.
- [x] **Effects and endings:** dust, sparks and embers by code (`EffectsView`), a pulsing check ring and a small shake, camera moods (the walk in from the hall, checkmate, resigning, a draw), the checkmate in slow motion with a wash, torches that burn down in a draw.
- [x] **Strength and speed:** every visual effect scales with the effect strength and turns off at 0 (tests in EditMode and PlayMode). Quick keeps the sounds, the dust and the embers but not the camera moves. Off plays one clash or one footfall.
- [x] Final renders: start screen, opening, selection, fight at the lance thrust, collapse with sparks, check, checkmate, draw.
- [x] Deliberate-bug check: moved the impact sound before the blow. Six tests failed. Restored.

What differed from the plan, and why:
- **The fill light is cool, not warm.** With only warm light the squares and both sides turned the same orange, which broke pillar 2. A cool fill (`#D1D9ED`) keeps the stone and slate squares readable, and the four torches stay the only warm light. Tonemapping is ACES; Neutral was not compared on a render.
- **Torch shadows are cut.** The four torch lights cast no shadows. Only the fill light does. A render showed no gain worth the cost.
- **The effect strength has no on-screen control.** It is a value on the settings asset (0 to 1). The spec asked only for the animation speed on the start screen. A control is a stage 7 question.
- **Side shapes are on the shoulders, not the heads.** Crowns and mitres already top the head, and anything added on top would break the height budget.
- **The wash is 3 percent alpha.** The UI blends in linear light, so 30 percent covered the board in yellow.
- **Not built (needs a decision):** the start-screen banner hover, the sway on an illegal click, the lift of a picked piece, the thinking candle, pieces lowering their weapons in a draw, the fade of pieces in a new game, a horn when a game starts is a drum and horn in one file.

### Model pass (after stage 6, before the play test)
Asked for after the first look at the pieces: "a bit more impressive". Rebuilt all six models in `make_pieces.py`.
- [x] Turned (lathe) bodies, domes and towers, tapered limbs for legs, necks and tails, ellipsoid heads with a nose and chin, real shields (round, heater), a proper pony for the knight (barrel body, chest, rump, jointed hind legs, hooves, tail, mane, ears, trimmed saddle cloth), an eight-sided stone rook with a door, arrow slits and crenellations.
- [x] Smooth shading with crisp edges where the angle is sharp, and two-segment bevels.
- [x] A third material, `trim` (gold), on crowns, belts, rims, bosses, sword guards, the stole and the mitre band. `PieceActor.Bind` maps it, and the settings have `trimColor`.
- [x] The triangle budget rose from 2,500 to 6,000 per piece (spec updated). Pieces use 1,744 to 3,946.
- [x] Reviewed on close-up renders of both sides, not only at board distance.
- Not done: baked ambient occlusion, textures and shader changes. These need a decision (see the chat).

### Surface pass (after the model pass, approved in chat: "options 1-3")
Asked for after the first play of the game: the pieces and board are flat colours. Three parts, no packages.
- **Texture images (option 1).** `Tools/textures/MedievalChess/make_textures.py` (run headless in Blender, for its numpy) writes tileable 512 px albedo and normal maps for slate, limestone, wood and flagstone into `Art/Textures/` (PNGs are LFS). A hand-written URP shader `Shaders/MedievalLit.shader` maps them in world space for the board, frame and floor (so no tile repeats its neighbour), and as a fine object-space grain on the pieces (so no UV maps are needed).
- **Baked ambient occlusion (option 2).** `make_pieces.py` bakes occlusion into vertex colours by casting rays against all the parts of a piece (its rest pose), and exports them. The shader multiplies it into the light. Baking into vertices needs no UVs and no change to how materials are replaced by name.
- **Dissolve to embers (option 3).** The victim stops shrinking and instead burns away along a noise edge with a glowing rim, driven by a `Dissolve` channel in the fight plan and a material property block set by `PieceActor`. The embers particles from stage 6 stay.
- Tests first: a dissolve that rises 0 to 1 over the collapse and ends hidden; the property block follows the frame; the shader compiles with no messages; the textures exist, tile and are not flat; every exported mesh carries occlusion that varies; the scene's materials use the shader and the textures.
- Tuning values on the settings asset: `look.surface` (texture strength, grain strength, occlusion strength, normal strength) and `fight.dissolveTime`.
- Not verified by me: whether it looks better in motion at play distance; the dissolve's timing against the sound.

- [x] Tests written first (`SurfaceTests`, a dissolve test in `FightPlanTests`, board-surface and shader-name checks in the builder tests).
- [x] Texture generator and PNGs: `Tools/textures/MedievalChess/make_textures.py`, ten 512 px files in `Art/Textures/`, imported by `SurfaceTextureImporter`.
- [x] Shader `Shaders/MedievalLit.shader`, `DuelSceneBuilder.Surface`, strengths in `look.surface`. The east and west frame and the trays turn the wood a quarter.
- [x] Occlusion bake in `make_pieces.py` (about 40 rays per vertex), exported as vertex colours.
- [x] Dissolve: `Dissolve` channel in `ActorPlan`, `fight.dissolveTime` (0.45 s), set per renderer by `PieceActor`. The victim burns instead of shrinking.
- [x] Deliberate-bug check: the dissolve's end value changed, 24 tests failed, restored.
- [x] `check.sh all`: EditMode 729/729, PlayMode 69/69. Renders read: opening position, close-up of the bone row, four frames of a rook taking a queen.
- Not verified by me: the look with Bloom and Vignette (the Game view does not repaint in the background, so I rendered the main camera), how the burn reads in motion at full speed, and the bake on a swinging arm.
- Also done in this pass: the structure rules (`docs/project-structure.md`, `ProjectStructureTests`), the 20 materials renamed without spaces, and the surface decision written into the four skills.

### Stage 7: play test and tidy
- [ ] **You play** each difficulty and fill in the "needs a person" criteria in the spec. The sheet is `medieval-chess-playtest.md`. This is the one thing left that I cannot do.
- [ ] Turn findings into tasks or open questions. Tune values on the settings asset.
- [ ] Decide the font (needs your approval and a licence record).
- [x] Removed the legacy crate placeholder, with your approval.
- [x] Updated the spec to describe the game as built, and ticked the criteria that tests and captures cover.
- [x] The README and `AGENTS.md` list both games.
- [x] PR #2's title and description now describe the finished game. It stays a draft until the play test is done.
- [ ] After the play test: set this plan to complete. When #1 merges, change PR #2's base to `main`.

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
