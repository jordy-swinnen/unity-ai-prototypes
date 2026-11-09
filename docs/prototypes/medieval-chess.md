# Medieval Chess

Status: built, waiting for the play test (stage 7)
Kind: new game

## One-page design
- **Pitch:** Chess against the computer, played with chunky medieval warriors who fight to the death whenever one takes another.
- **Feeling:** weighty and ceremonial. Heavy figures in a torch-lit stone hall, slow deliberate moves, and a capture that lands like a real blow.
- **Pillars:**
  1. **Every capture is a fight.** So we give every capture a short, animated fight scene where the attacker kills the victim (hit-stop, shake, close camera, a collapse), and checkmate its own finale. So we won't remove a captured piece silently, and we won't let a fight change the game: the rules decide, the animation only shows it.
  2. **The board is always readable.** So we keep the 64 squares, the two sides, the legal moves, the last move and a check clear at all times, with the camera and effects never hiding them. So we won't let a fight, a glow or a camera move leave the player unsure of the position, and fights can be sped up or skipped.
  3. **Weight and ceremony.** So we make pieces heavy: they ease in and out, they thud, clank and raise dust, and the hall is lit by torches. So we won't make them bouncy, cartoonish or fast, and we won't fill the screen with effects.
  4. **A fair opponent you choose.** So the player picks one of three clear difficulties before the game starts, and each plays by the real rules with no hidden advantage. So we won't cheat, and we won't make an easy level that plays random nonsense or a hard level that makes the player wait a long time.
- **Core loop:** pick a difficulty and a side, then take turns with the computer. Choose a piece, choose a legal square, watch the move or the fight, and play on until checkmate or a draw, then play again.
- **Verbs:** pick a piece, move it (or capture with it), promote a pawn, resign, start a new game.
- **Players, session length, platform, 2D or 3D:** one person against the computer, 10 to 30 minutes a game, desktop with mouse, a 3D board seen from the player's side at an angle.

## Look and feel brief
### References
Details and sources: `docs/research/medieval-chess.md`. All art, models and sounds are original.
- **The Lewis chessmen (12th century, real objects):** take the chunky, heavy, carved shapes, strong silhouettes (throne, crozier, shield, mounted figure), grave faces, and red and white as the two sides. Don't copy any actual carving: our models are our own simplified designs.
- **Battle Chess (1988):** take a fight on every capture, one distinct fight per attacker type, and the checkmate as a fight. Don't copy its characters, its animations, its jokes or its sounds, and don't force long fights on the player: we add a speed setting.
- **Heraldry:** take the rule that a figure must stand out from its field (light on dark, dark on light), and gold for highlights. Don't copy any real coat of arms: banners and crests are invented.

### Chosen direction (a mix of two)
You picked "Living pieces (Battle Chess style)". It is combined with the recommended Lewis look: the figures are chunky Lewis-style warriors, and they fight. This is bigger than a static set, so the first version has **six attack routines (one per attacker type) and one collapse for every victim**, not 35 pairings. See Out of scope.

### Visual pillars
- **Shape language:** chunky, rounded-bevelled forms with a big head, a wide body and a short, heavy weapon. Each piece has a different silhouette and a different height: pawn (footman with round shield and short spear, smallest), knight (a stout pony and rider with a lance), bishop (a cleric with a mitre and a heavy mace), rook (a crenellated stone tower warden with fists), queen (a crowned warrior with a long sword), king (tallest, crowned, with a great sword). (pillar 2, 3)
- **Colour philosophy:** two sides, **bone** and **cinnabar red**, after the colour evidence on the Lewis pieces. A non-colour backup: the bone side has rounded caps and crowns, the red side has pointed crests, and the sides start at the bottom and top of the board. Gold is reserved for the player's own highlights (selection and legal moves). (pillar 2)
- **Lighting:** a dark hall lit mostly by four flickering torches around the board, warm and low, so the pieces have weight and form. **As built:** a dim cool fill light casts the soft shadows, because with warm light only the squares and both sides turned the same orange. The torches cast no shadows (a render showed no gain). The extra warm flash on a fight became sparks on the blow. (pillar 3)
- **Materials:** matte stone and carved wood for the board, matte painted bone and waxed red for the pieces, dull iron for weapons. Only the torch flames and the gold highlights are emissive. The brightest things on screen are the selected piece's highlight and the flames. (pillar 2, 3)
- **Rendering and post-processing:** mild Bloom (flames and gold only), a stronger Vignette to pull the eye to the board, warm colour grading, Tonemapping chosen on a render (ACES or Neutral). Shadows on. **As built:** ACES, Bloom threshold 1, Vignette 0.38. (pillar 3)
- **Camera:** perspective, pitched down about 50 degrees behind the player's side, narrow field of view (about 35), the whole board always visible. On a capture the camera pushes in and lowers toward the fight, then returns. The player does not move the camera in the first version. (pillar 1, 2)
- **UI and text:** a start screen with three difficulty banners and three side choices, a thin top bar (whose turn, "Thinking...", the result), trays of captured pieces at the board's sides, a promotion choice of four pieces, and a menu. Text in a neutral parchment colour. (pillar 2, 4)

### Palette
| Role | Colour (hex) | Used for |
|---|---|---|
| Hall shadow | `#0F0C0A` | background, deep shadows |
| Stone floor | `#2B2724` | the hall floor around the board |
| Light squares | `#9A9C94` | the 32 light squares (weathered stone) |
| Dark squares | `#5E6660` | the 32 dark squares (slate) |
| Board frame | `#4A3322` | carved wooden border |
| Bone side | `#E8DCC0` | the player-white pieces |
| Cinnabar side | `#B8301F` | the player-red pieces (the side that moves second) |
| Iron | `#5A5F63` | weapons, the rook's stone parts use the light-square colour |
| Torch flame | `#FF9A3C` | the flames and the warm lights |
| Gold | `#E3B341` | selection, legal-move rings and the last move |
| Check red | `#FF3B30` | the pulse on a king in check |
| Parchment | `#E9DFC5` | UI text |

The sides also differ by shape and by position, so colour is not the only cue. Red on slate is the weakest pairing for contrast and for red-green colour blindness, so it is checked on a render, and the crest shape is the backup.

### Surfaces
Decided by the agent from the look brief (rules in `.claude/skills/game-polish/references/surfaces.md`), added after the first play test said the pieces and board looked flat. Strengths are on the settings asset (`look.surface`).

| Material or object | Surface | Why (pillar) |
|---|---|---|
| Light squares | procedural limestone texture and normal map, world-space | heavy, ceremonial stone is the first thing the eye reads (pillars 1 and 2) |
| Dark squares | procedural slate with strata and hairline cracks, world-space | the same, and the two stones differ in texture as well as colour |
| Frame and trays | procedural planks with grain, world-space, turned a quarter on the east and west sides | the grain runs along the board's edge |
| Floor and torch stone | procedural flagstones with mortar, world-space | the hall reads as stone, and no stone repeats its neighbour |
| Pieces, both sides | fine object-space grain, baked ambient occlusion in vertex colours, three material slots (side, iron, gold) | figures in a dim hall need shadow in their folds and under their crowns (pillar 1) |
| A captured piece | burns away along a noise edge with a glowing rim, plus the embers | "every capture is a fight" ends on a visible, readable death (pillar 1) |
| Torch flames, rings, particles | flat colour with emission, no texture, by design | glow and clarity matter more than detail (pillar 3) |

### Audio direction
Weighty and dry: stone and wood on stone, iron on iron, a low war drum, a horn for check and mate. No music and no voices in this version. All sounds are made by a script (filtered noise bursts, decaying low sines and plucked-string tones). Whether they sound good is the person's call. (pillar 3)

### Feel table
| Event or action | Visual | Audio | Camera | Time | Pillar |
|---|---|---|---|---|---|
| Start screen: hover a banner | the banner lifts and its edge glows | soft wooden creak | none | none | 4 |
| Pick difficulty and side, start | the pieces fade in, torches light up | a drum beat and a horn | slow push from the hall to the board | none | 3, 4 |
| Hover a square | a faint gold ring on the square | quiet stone tick | none | none | 2 |
| Pick a piece | the piece straightens and lifts its weapon, a gold ring and gold rings on every legal square, capture squares ringed in red | low iron clink | none | none | 2, 3 |
| Illegal click or pick the wrong colour | the piece sways side to side | dull wood knock | none | none | 2 |
| Move (no capture) | the piece walks, rides or stomps along its path (knight leaps, rook slides), dust at each footfall | a stone thud per step | camera holds | none | 3 |
| **Capture** | the attacker steps up to the victim, plays its attack routine and kills it, the victim collapses into embers and dust, then the attacker steps onto the square | a swing, an impact, a collapse and a drum hit | pushes in and lowers toward the fight, shakes on the blow, then returns | hit-stop about 70 ms on the blow | 1 |
| Capture, speed setting | "Quick" plays the fight at triple speed with no camera cut, "Off" shows the move only | same sounds, shorter | none | none | 2 |
| En passant | as a capture, with the attacker turning to the side pawn | as a capture | as a capture | as a capture | 1 |
| Castling | the king strides and the rook follows with a hop | two thuds | holds | none | 3 |
| Promotion | the pawn kneels, a warm forge flash, it rises as the chosen piece | an anvil ring | small push in | none | 1, 3 |
| Check | the king's square pulses red and the king braces, the threatening piece is outlined | a low horn | small shake | none | 2 |
| Checkmate | the final capture or move plays, then the king falls, banners of the winner raise, a warm wash | a drum roll and a fanfare | slow push toward the king | slow motion about 30% for about 1.2 s | 1, 3 |
| Stalemate or draw | the pieces lower their weapons, the torches dim a little | a single low bell | slow pull back | none | 3 |
| Computer is thinking | a small candle or hourglass beside the opponent's side, the cursor is locked | a quiet wick crackle | none | none | 4 |
| Computer moves | the same move or fight as the player's | same | same | same | 1, 4 |
| Resign | the player's king kneels and drops its sword | a low horn | slow push in | none | 3 |
| New game | the pieces fade out and in, the board resets | a drum beat | pull back to the start view | none | 3 |

**Built, and not built.** Built: every row with a sound, a camera move or a particle effect except those below. Not built (stage 7 decides which are worth it): the start-screen banner hover, the sway on an illegal click (a knock plays), the lift and raised weapon of a picked piece (the rings show it), the thinking candle (the top bar says "thinking"), pieces lowering their weapons in a draw (the torches dim and a bell tolls), and the fade of pieces in a new game (the camera walks in from the hall). A promotion's forge flash is a burst of sparks.

### Six-area feel review
- **Input:** one mouse click picks a piece, a second click picks a square, a click on an empty square or the same piece cancels. Risk: a click on a square that is hidden behind a tall piece. The click uses the board plane, not the models.
- **Response:** the highlights and the piece's lift start on the same frame as the click, and the computer's move starts within about a second. Risk: a long fight makes the game feel slow. The speed setting is the answer.
- **Context:** the hall, the torches and the trays make the board a place, but they must not pull attention. Risk: torch light and shadows hiding pieces or squares on the far side.
- **Polish:** the feel table above. Risk: too many effects at once on a capture, which hides what happened.
- **Metaphor:** heavy figures must look and sound heavy: slow starts, thuds, clanks. Risk: a quick bouncy animation that feels like a toy.
- **Rules:** the standard rules, no surprises. Risk: a bug in an unusual rule (en passant, castling through check, promotion), which is why the rules are tested against known perft counts.

## Mechanics
- **Board and sides:** standard 8 by 8 board with the standard starting position. The player chooses the bone side (moves first), the cinnabar side, or random. The board is shown from the player's side.
- **Moves and rules:** all standard chess rules: pawn moves (single, double, capture, en passant, promotion), knights, bishops, rooks, queens, king, castling (not out of, through or into check, and not if the king or rook has moved), check and checkmate, stalemate, threefold repetition, the fifty-move rule and a draw when neither side can mate (king against king, king and a single minor piece against king). The rules are plain C# with no engine physics. The scene only shows the state.
- **Moving a piece:** click one of your pieces: it is selected and the legal squares are marked. Click a marked square to move. Click another own piece to change the selection, or anywhere else to cancel. A pawn that reaches the last rank opens a choice of queen, rook, bishop or knight before the move is made.
- **The fight:** a capture is decided by the rules first, and the animation then shows it. The attacker walks to the square next to the victim, plays its routine (pawn: spear thrust, knight: lance charge, bishop: mace swing, rook: a two-fisted slam, queen: a sword sweep, king: a heavy overhead blow), the victim collapses into embers, and the attacker takes the square. The fight is one routine per attacker type and one collapse for all victims. The game state is already updated, and input stays locked until the animation ends (or is skipped).
- **Animation speed:** a setting with Full, Quick (three times faster, no camera cut) and Off (pieces slide, no fight). The choice is on the start screen and the menu. The global effect strength (0 to 1) scales shake, flashes, the camera push and hit-stop.
- **The three difficulties** (the computer plays by the same rules, sees only the position, and has no hidden advantage):
  - **Squire (easy):** looks one move ahead. It picks at random among moves that are within 120 centipawns of its best, and one move in seven it plays any legal move. It takes a free piece but often misses a two-move trap. It "thinks" for about 0.8 s so its move is easy to follow.
  - **Knight (medium):** a three-move search with a capture search at the end of it, and picks the best move, choosing randomly only between moves that are within 10 centipawns. It sees a piece hanging, forks and simple mates in two. Time cap 1.5 s.
  - **Marshal (hard):** iterative deepening up to 8 plies (half-moves) inside a 3 s cap, with a transposition table, move ordering and a capture search. Measured on a development machine it reaches 5 to 9 plies, typically 6 or 7. It plays the strongest move it finds. A person who plays chess regularly should find it hard.
- **How the computer evaluates:** material (pawn 100, knight 320, bishop 330, rook 500, queen 900 centipawns), plus its own position tables that reward centre control, developed pieces, a castled king in the middlegame and an active king in the endgame, and a mate score that prefers the quicker mate. The search runs on a background thread and can be cancelled, so the game never freezes.
- **Game end:** the game ends on checkmate, a draw, or a resignation. The result shows with a Play Again button and a Change Difficulty button.
- **First slice (the riskiest part):** because the fight is the point of this game and the hardest thing to get right, the plan builds one piece killing another, animated, on a bare board, before the chess rules exist.

## Tuning values
All on one settings asset. Feel and look values are starting points, tuned on renders and in play.
- **Board:** square size 1 m, frame width 0.6 m, piece heights from 0.5 (pawn) to 1.1 (king).
- **Moves:** step time per square 0.35 s (stone thud each step), knight leap 0.55 s, rook slide 0.5 s, selection lift 0.12 m over 0.12 s.
- **Fight:** approach 0.35 s, wind-up and blow per attacker (about 0.6 s total), the blow's hit-stop 0.07 s, collapse 0.6 s, camera push 35 percent closer, lowered 25 percent, shake strength 0.12 m, fight total about 1.8 s at Full. Quick speed factor 3.
- **Difficulty:** Squire depth 1, slack 120 centipawns, blunder chance 1 in 7, delay 0.8 s. Knight depth 3, quiescence on, slack 10, time cap 1.5 s. Marshal depth up to 8, time cap 3 s, table size 2^18 entries.
- **Evaluation:** piece values above, game-phase threshold (both queens off, or a queen with at most one minor piece per side), mate score 100000.
- **Look:** emission for the torch flames, gold ring and check pulse, Bloom intensity and threshold, Vignette intensity, torch light intensity and range, flicker amplitude and speed, camera pitch, field of view and distance.
- **Feel:** global effect strength 0 to 1, shake duration and decay, wash alpha, slow-motion scale and duration for checkmate, dust and ember counts, master volume.

## Art and audio assets
- **Models (Blender scripts, `.glb`):** six articulated pieces (pawn, knight, bishop, rook, queen, king), each built from separate parts in one node hierarchy (body, head, weapon arm, shield arm, base, and a horse for the knight), at most 6,000 triangles per piece (raised from 2,500 for the model pass; the pieces use 1,700 to 3,900), the same mesh for both sides with three materials (the side colour, iron and gold trim). The board with its frame and 64 squares (at most 2,000 triangles), a torch with a bracket, a banner, and a ring marker. No rig or skeleton: Unity animates the parts by code.
- **Textures (Blender-run Python script, `.png`):** `Tools/textures/MedievalChess/make_textures.py` writes tileable 512 px albedo and normal maps for slate, limestone, wood, flagstone and a fine grain from seeded noise (original, no source to license).
- **Shader (hand-written URP, `Shaders/MedievalLit.shader`):** world-space triplanar textures for the set, object-space grain and baked occlusion for the pieces, and a dissolve with a glowing rim.
- **Effects (Unity):** torch flames, embers, dust, sparks on weapon hits, glow rings for selection and legal moves, the check pulse.
- **Sounds (Python script, `.wav`):** about 16 sounds, each under 2 s: footstep thud, knight leap, rook slide, six attack swings, an impact, a collapse, selection clink, illegal knock, check horn, checkmate fanfare, draw bell, promotion anvil, drum hit, UI creak.
- **Fonts:** the built-in font for now. See Open questions.

## Acceptance criteria
### Checked by tests or captures
- [x] The Blender script runs headless and writes every model with the planned sizes, pivots and part names, within the triangle budget.
- [x] Move generation matches the known perft counts for the start position (depths 1 to 4) and for the test positions listed under Tests.
- [x] Castling, en passant and promotion work and have their special cases covered (through check, the rook or king moved, the pawn's one-move window, promoting to each piece).
- [x] Checkmate, stalemate, threefold repetition, the fifty-move rule and insufficient material are all detected, and a move that leaves your own king in check is never legal.
- [x] The three difficulties exist and show on the start screen. Choosing one and a side starts a game with the right side to move.
- [x] The computer only ever plays legal moves, finds a mate in one at Knight and Marshal, and does not leave its queen to be taken for nothing at Knight and Marshal. Squire is beatable: over a fixed set of seeded positions it sometimes plays worse than the best move.
- [x] In a fixed seeded set of games, Knight scores at least as much as Squire and Marshal at least as much as Knight. (The default test plays Knight against Squire. Marshal against Knight is an `[Explicit]` test of about six minutes, which I ran by hand: Marshal at 4 plies won 7 to 3 in 10 games.)
- [x] The computer never goes over its time cap by more than a small margin, and the game stays responsive while it thinks.
- [x] Every capture plays one fight: the right routine for the attacker, the victim collapses, the final positions match the rules' state, and the animation never changes the game state. En passant, castling and promotion also end in the right state.
- [x] With the animation setting on Quick or Off, or the effect strength at 0, the game state is the same and the fight is faster or absent, with the shake offset at zero.
- [x] A render shows the board, all pieces, the torches and the highlights, with the two sides and all six piece types told apart, and the legal squares readable.
- [x] Every sound exists, is not silent, does not clip and ends near zero.
- [x] `Tools/unity/check.sh all` passes.
Checked on 9 November 2026 with `Tools/unity/check.sh all`: EditMode 667/667 and PlayMode 69/69, and renders of every screen.

### Needs a person
How to check each one is in `docs/prototypes/medieval-chess-playtest.md`.
- [ ] The look matches the brief: it reads as medieval and heavy, not as plain blocks, and the pieces look like warriors.
- [ ] All six piece types and both sides can be told apart at a glance, including red on the dark squares.
- [ ] A capture feels like a fight with weight: the blow lands, the victim falls convincingly, and it is not too long or too silly. Quick and Off are fast enough for a long game.
- [ ] The three difficulties feel right: Squire is beatable, Knight is a real game, Marshal is hard, and none of them makes the player wait too long.
- [ ] Checks and checkmate are clear and dramatic, and the draw is clear.
- [ ] The sounds fit the mood and the volume is balanced.
- [ ] The mouse controls feel precise: no mis-clicks, the highlights are clear, and the promotion choice is easy.

## Tests
- **EditMode (rules):** perft for the start position depths 1 to 4 (20, 400, 8,902, 197,281), Kiwipete depths 1 to 3 (48, 2,039, 97,862), position 3 depths 1 to 4 (14, 191, 2,812, 43,238), position 4 depths 1 to 3 (6, 264, 9,467), position 5 depths 1 to 3 (44, 1,486, 62,379) and position 6 depths 1 to 3 (46, 2,079, 89,890), with the FENs in the research notes. FEN reading and writing. Special rules (each case above). Draw detection. A game played from a list of moves reaches the expected result. Make and unmake restore the position exactly.
- **EditMode (AI):** only legal moves, mate in one, no free queen, each difficulty's settings, the time cap, the seeded strength ordering, repeatability with a fixed seed, and that a cancelled search stops.
- **EditMode (animation logic):** the fight planner picks the routine for each attacker type and a collapse for the victim, a plan for each special capture ends in the right state, the speed settings scale the times, the effect strength at 0 removes the shake and hit-stop, shake decays to zero, hit-stop ends.
- **EditMode (builder and audio):** the scene has every part, building twice gives the same content, the committed scene matches the builder, the Volume has Bloom and a Vignette, every sound file passes the checks.
- **PlayMode:** the start screen starts a game with the chosen difficulty and side, a click on a piece and a legal square moves it, an illegal click does nothing, a capture plays the fight and then the board is right, promotion opens its choice, the computer replies after the player, a game over shows the result and Play Again works.

## Out of scope
- All 35 attacker-and-victim fight pairings of Battle Chess. The first version has six attacker routines and one collapse. Special pairings (for example knight against knight) can come later as a feature spec.
- Characters with a skeleton and hand-made animation, voices, music, and gore (victims dissolve into embers).
- An orbiting or zooming camera, an undo button, hints, a move clock, saving, online play, two players on one computer, other board sizes and other variants.
- An opening book, an endgame table and a rating system.
- A settings screen beyond the animation speed and the effect strength.

## Open questions
- **Font:** the built-in font is plain. A medieval font needs a free licence (for example an Open Font Licence file) recorded in the repository, and adding a font file needs your approval.
- **Fight length:** about 1.8 s at Full is a guess. Judge it in play, and cut it if it wears out.
- **Readability:** red pieces on the dark slate squares, the knight's silhouette (pony and rider) and the pawn against the rook. Judged on renders.
- **Search time:** measured in stage 4. Marshal reaches 5 to 9 plies in 3 s on a development machine. A slower machine reaches less, and the time cap protects the player either way.
- **Shadows and torches:** decided in stage 6. The torches cast no shadows, and the cool fill light does.
- **How good simple generated sounds are:** the person decides after listening.
- **Tonemapping:** ACES is used. Neutral was not compared on a render, so judge the colours in play.

## Sources
See `docs/research/medieval-chess.md`. Lewis chessmen: Wikipedia [read]. Battle Chess: Wikipedia [read]. Heraldic tinctures: Wikipedia [read], no colour values given. Perft counts: Chess Programming Wiki [read]. Evaluation ideas and piece values: Chess Programming Wiki, Simplified Evaluation Function [read]. Search techniques: [secondary]. Search strength and timing: [unverified].
