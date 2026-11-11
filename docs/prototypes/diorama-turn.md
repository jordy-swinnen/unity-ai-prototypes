# DioramaTurn

Status: approved
Kind: new game

<!-- Written by the game-spec skill. Research and sources: docs/research/diorama-turn.md -->

## One-page design
- **Pitch:** A tiny toy-box room on a turntable: turn it a quarter at a time until the flat pieces and blocks line up into a path, then walk a small pixel-art character to the star.
- **Feeling:** playful and clever. Every turn is a small "ohh" when two pieces that were apart suddenly touch, in a bright room that bounces and ticks like a toy.
- **Pillars:**
  1. **Turning is the puzzle.** So we make the quarter turn the only way to change what is reachable, and every room needs at least one turn to finish. So we won't add timers, lives, enemies, fail states or a second puzzle verb.
  2. **A toy you want to touch.** So we ease and bounce every motion, keep the colours bright and warm, and give every tap and turn a soft sound. So we won't make anything grim, heavy, dark or sharp-edged.
  3. **Crisp meets soft.** So we keep the pixel-art sprites flat, point-sharp and at a whole-number scale, and let the 3D blocks be lit, shadowed and a little blurred behind. So we won't smooth, rotate or stretch a sprite, and we won't let the post effects smear the sprite pixels.
  4. **The whole puzzle fits one screen, and the answer is always findable.** So every room shows all its pieces in every view, a solution exists in some view, and a joined link shows a dotted ribbon, not only a colour. So we won't hide required pieces behind others, and no puzzle may need two pieces to overlap on screen.
- **Core loop:** look at the room, turn it a quarter at a time to find a view where the pieces join, tap a reachable spot (or move a cursor and confirm), watch Pip walk and hop across the joins, reach the star, go to the next room.
- **Verbs:** turn left, turn right, walk to a spot, restart the room.
- **Players, session length, platform, 2D or 3D:** one player, 10 to 15 minutes for all three rooms, desktop (mouse, keyboard or gamepad; touch is not tuned), 2.5D: a 3D room seen through an orthographic camera with pixel-art sprites that always face the camera.

## Look and feel brief
### References
Research and confidence tags: `docs/research/diorama-turn.md`. All art, models and sounds are original.
- **Fez (Polytron):** take the quarter turn as the puzzle verb and the rule that depth is ignored when the view lines pieces up. Don't copy Gomez, the fez, the trixel look or the name.
- **Octopath Traveler, HD-2D (Square Enix, Acquire):** take crisp pixel sprites inside lit, softly blurred 3D with a miniature feel. Don't copy any character, sprite, UI or the JRPG tone.
- **Monument Valley (ustwo):** take one-screen puzzles, a tap-to-walk character and the joins made by the view, and an "experience, not a test" difficulty. Don't copy Ida, the totem, the Escher style or any level.

### Chosen direction
You picked **Toy-box diorama**: saturated candy colours, chunky rounded painted-plastic blocks on a wooden plinth, a soft warm key light, a light tilt-shift blur, bouncy motion and marimba-and-pluck sounds. Paper-cut and Pastel-glow were the other options.

### Visual pillars
- **Shape language:** blocks are rounded cubes with big bevels, the plinth is a thick rounded tray, sprites are round and short (Pip is a big-headed round creature, the star has soft points). No sharp corners, no spikes. (pillar 2)
- **Colour philosophy:** bright and warm. Block colours are decoration only and carry no puzzle meaning, so colour-blind players lose nothing. What carries meaning is shape and effect: the goal is the only star, a join shows a dotted ribbon and a chime, the character is the only moving sprite. (pillars 2, 4)
- **Lighting:** one soft warm key light from upper left and front, soft shadows, a cool sky-coloured fill, and violet-tinted shadows. The light turns with the camera, so the lighting on screen is the same in every view. (pillars 2, 4)
- **Materials:** glossy-matte painted plastic on blocks, warm wood on the plinth, flat unlit-looking pixel art on sprites (lit only by the same key light and fill). The brightest things on screen are the goal star and a freshly joined ribbon. (pillars 3, 4)
- **Rendering and post-processing:** light Bloom (only the star and ribbons), light Vignette, a tilt-shift blur (a full-screen pass that blurs the top and bottom bands of the screen and keeps the middle sharp, so the room stays crisp), warm colour grading with slightly raised saturation, Neutral tonemapping, SMAA on the camera for the 3D edges. Sprites are Point-filtered. (pillar 3)
- **Camera:** orthographic, true isometric (pitch about 35.26 degrees), four views at 45, 135, 225 and 315 degrees, an eased quarter turn with a small overshoot. The room is always fully visible. Orthographic and isometric are chosen because pieces at different depths then line up exactly on a whole-number lattice. (pillars 1, 3, 4)
- **UI and text:** a thin top bar with the room name and a turn counter, two turn arrow buttons, a restart button, a complete card with 1 to 3 stars (turns against par). Rounded font, dark plum text on cream cards. Navigation hints show the keyboard or gamepad keys that are currently in use. (pillars 2, 4)

### Palette
Block, decor and plinth colours repeat in the sprite palette (16 colours at most for all sprites, so the art reads as one set).

| Role | Colour (hex) | Used for |
|---|---|---|
| Backdrop | `#9ADBE8` | camera background, soft sky |
| Plinth wood | `#C98B5A` | the room's base tray |
| Plinth rim | `#8F5A38` | rim and shadow side of the plinth |
| Block tomato | `#FF5A4F` | decorative block colour |
| Block sun | `#FFC83D` | decorative block colour |
| Block mint | `#4CCB9B` | decorative block colour |
| Block sky | `#4C9BFF` | decorative block colour |
| Pip cream | `#FFF1D6` | Pip's body |
| Pip scarf | `#FF7A59` | Pip's scarf, to find Pip at a glance |
| Plank wood | `#E8B67A` | bridge pieces (sprites) |
| Goal gold | `#FFD93B` | the star |
| Goal pink | `#FF9AC8` | the star's base |
| Ink | `#3B2A4D` | sprite outlines, UI text |
| Shadow violet | `#5B4B8A` | tint of shadows and ambient |
| Link glow | `#FFF6B0` | dotted ribbon and join flash |
| Card cream | `#FFF6E4` | UI cards |

Backup for colour: only the goal star, the ribbon and Pip carry meaning, and each also differs by shape, motion or sound.

### Surfaces
Decided from the look brief (rules in `.claude/skills/game-polish/references/surfaces.md`). Strengths are on the settings asset (`look.surface`).

| Material or object | Surface | Why (pillar) |
|---|---|---|
| Blocks and pillars (Blender models) | flat painted colour, slight gloss (smoothness about 0.45), baked ambient occlusion in the vertex colour, no texture | smooth toy plastic has no grain, and a texture would fight the crisp sprites; occlusion gives the solid, handled look (pillars 2, 3) |
| Plinth (Blender model) | procedural wood texture (albedo and normal), world-space | wood is a real material, so flat colour would look like plastic; it is the one "real" surface that makes the toy feel touchable (pillar 2) |
| Sprites (Pip, bridge, star, decor) | flat pixel art on an alpha-clipped URP Lit quad, Point filter, no texture beyond the sheet, no normal map; they receive the key light and cast shadows | sharp flat pixels are the style, and lighting them ties them into the 3D room (pillar 3) |
| Goal star glow | flat + emission, with Bloom | the star must be the brightest thing (pillar 4) |
| Link ribbon | flat + emission dotted line (a LineRenderer with a scrolled dot texture), no custom shader | a join must read without colour: a moving dotted line is a shape and a motion (pillar 4) |
| Backdrop | flat colour, plus Vignette and the tilt-shift blur | a flat sky keeps the eye on the room (pillar 3) |
| Spawn and removal | pieces pop in with a staggered scale overshoot and a small dust puff, and drop and shrink away when the room ends; no dissolve shader | toys pop, they do not burn or fade (pillar 2) |

### Audio direction
Mood: warm, wooden and light. Marimba, pluck, soft woodblock and music-box. Notes come from one pentatonic scale, so nothing clashes. All sounds are generated by scripts in `Tools/audio/DioramaTurn/` (as in the other prototypes). One slow looping marimba and music-box tune at low volume, which ducks under the goal chime. No voices.

### Feel table
| Event or action | Visual | Audio | Camera | Time | Pillar |
|---|---|---|---|---|---|
| Turn starts | the room turns with an eased curve; sprites turn to face the camera; ribbons fade out | soft wooden whoosh, pitch rises a little | quarter turn about the room's centre | none | 1, 2 |
| Turn settles | a small overshoot and settle (4 degrees), a dust puff on the plinth | a wooden "clunk" | settle with the overshoot | none; the ease is the feel | 2 |
| A join forms | a dotted ribbon grows between the two pieces and pulses once; both pieces glow for a moment | two rising marimba notes | none | none | 1, 4 |
| A join breaks | the ribbon shrinks away | one soft falling note | none | none | 1, 4 |
| Tap or cursor confirm on a reachable spot | a ring ripples on the spot; Pip squashes and turns to face it | pluck, pitch by path length | none | none | 2 |
| Pip steps | squash-and-stretch bounce per step, a tiny dust puff | woodblock tick, alternating two pitches | none | none | 2 |
| Pip hops a join | an arc over 0.6 cells, a squash on landing | a "boing" up and a soft landing tap | none | none | 2 |
| Tap on an unreachable spot | a small cross on the spot; Pip shakes its head | soft dull "bonk" | none | none | 4 |
| Turn blocked (Pip is walking) | the turn arrow wobbles once | none, a tiny tick | none | none | 4 |
| Reach the star | star pops, confetti bursts, Pip cheers, bloom pulses once | rising arpeggio, music ducks | slow push-in, the sharp band of the tilt-shift narrows | 0.4x slow motion for 0.5 s | 2 |
| Room complete card | stars drop in one by one (turns against par) | one chime per star | none | none | 2 |
| Next room starts | old room shrinks and drops, new pieces pop in, one after another | marimba note per piece, rising | none | none | 2 |
| Restart room | pieces pop out and back in, Pip drops in | a quick falling then rising "boing" | none | none | 2 |
| Idle | Pip bobs and blinks, the star glimmers | quiet music loop only | none | none | 2 |

### Six-area feel review
- **Input:** two verbs (turn, go to a spot) on mouse, keyboard or gamepad. Main risk: the cursor on keyboard and gamepad must move to the next reachable spot in a predictable screen direction, or it feels slippery.
- **Response:** a turn must start on the press and settle in about 0.45 s. Main risk: too slow and it nags, too fast and the join is missed.
- **Context:** the room, plinth and the star give each action meaning. Main risk: in isometric, "touching" pieces at different depths must read as joined; the ribbon and the glow carry that.
- **Polish:** squash, ribbons, dust, confetti, sounds and the tilt-shift softness. Main risk: too many effects blur the rule that the turn is the star of the game.
- **Metaphor:** a toy room on a turntable you spin by hand, with pieces that click together. Main risk: sprites on a turning room must not look pasted on; the shared light and shadow fix that.
- **Rules:** perfect information, no fail, par as a gentle goal. Main risk: rooms too easy (the answer shows at once) or too hard (a guess-and-check spin); the solver gives par and the rooms are tuned by play test.

## Mechanics
- **Room:** a set of pieces on integer cells (x, y, z) inside a box of at most 5 x 5 x 5 (the largest room that fits the screen at 3 screen pixels per sprite pixel at 1080 high). Pieces: **Block** (a Blender model, walkable on top), **Bridge** (a sprite piece, walkable), **Start** (a block where Pip begins), **Goal** (a block with the star on top). A **node** is a walkable piece's cell. Levels are written as layered text maps (one string per row, one character per piece, a legend) so a diff shows what changed.
- **Views:** four views, numbered 0 to 3, a quarter turn apart. In view k, nodes A and B on cells are **joined** if B - A = n + t * r_k, where n is one of (+1,0,0), (-1,0,0), (0,0,+1), (0,0,-1), t is any whole number (t = 0 is a normal neighbour), and r_k is the view's depth step: r_0 = (1,1,1), r_1 = (-1,1,1), r_2 = (-1,1,-1), r_3 = (1,1,-1). In words: two nodes are joined if, on screen, they are exactly one cell step apart, whatever their depth. Joins are two-way. This is exact only because the camera is true isometric.
- **Readability rule (validator):** in a valid room no two nodes differ by t * r_k for any view k and any t other than 0, so two nodes never sit on the same screen spot. Every room fits in the box and has exactly one Start and one Goal.
- **Walking:** the player picks a node. The game finds the shortest route over the joins of the current view (breadth-first search). If there is none, the tap is "unreachable" and nothing moves. Pip walks one node per step (0.22 s). A step across a join (t not 0) is a hop (0.35 s).
- **Turning:** a turn is accepted only when Pip is standing still. If a turn is pressed during a turn, one more is queued, and the rest are ignored. Walking is not allowed during a turn. When the view changes, Pip stays on the node it stands on.
- **Winning:** when Pip reaches the Goal node the room is complete. Par is the least number of turns that solves the room, found by a solver over (node, view) states. Stars: 3 for par or less, 2 for par + 1 or par + 2, else 1. There is no failing.
- **Rooms:** three. Room 1: one join and one turn (teaches the verb). Room 2: two joins, two or three turns, a Bridge piece. Room 3: four to six turns and one dead-end join that looks right in the first view. Every room needs at least one turn to finish.
- **Input map:** mouse: click a node to walk, drag sideways or use the arrow buttons to turn. Keyboard: Q and E (or Left and Right with Shift) turn, arrow keys move a cursor to the nearest reachable node in that screen direction, Enter or Space confirms, R restarts. Gamepad: the shoulder buttons turn, the left stick moves the cursor, South button confirms, North button restarts.
- **Flow:** title card, room 1, room 2, room 3, finish card with Play again. Nothing is saved.

## Tuning values
Every value is a field on the settings asset `DioramaTurnSettings`, in groups.
- `turn.duration` 0.45 s. `turn.overshootDeg` 4. `turn.queueMax` 1.
- `walk.stepTime` 0.22 s. `walk.hopTime` 0.35 s. `walk.hopHeight` 0.6 cells. `walk.squash` 0.18.
- `rooms.maxSize` (5, 5, 5). `rooms.parTwoStarsExtra` 2.
- `camera.pitchDeg` 35.264. `camera.baseYawDeg` 45. `camera.pixelsPerUnit` 32. `camera.targetScreenPixelsPerSpritePixel` 3 at 1080 high (the largest whole number that fits, never below 1). `camera.snapSpritesToPixels` true (at rest).
- `light.keyColour` `#FFF0D6`, `light.keyIntensity` 1.1, `light.fillColour` `#9ADBE8`, `light.shadowTint` `#5B4B8A`.
- `post.bloomIntensity` 0.35, `post.bloomThreshold` 1.0, `post.vignette` 0.2, `post.tiltBandCentre` 0.5 and `post.tiltBandHalfHeight` 0.28 (the screen fractions kept sharp), `post.tiltBlur` low, `post.saturation` +12, `post.contrast` +8.
- `look.surface`: block smoothness 0.45, occlusion strength 0.6, wood texture strength 1.0, wood normal strength 0.5.
- `feel.effectStrength` 1 (range 0 to 1, scales overshoot, squash, particles, ribbons' pulse, bloom pulse and the tilt-shift blur; 0 means no overshoot, no particles and no slow motion), `feel.goalSlowMo` 0.4 for 0.5 s.
- `audio.master` 0.8, `audio.music` 0.35, `audio.musicDuckDb` -9 for 1.5 s.

## Art and audio assets
- **Blender models** (`Tools/blender/DioramaTurn/*.py` to `Art/Exports/*.glb`): plinth tray, rounded block, pillar block, goal pedestal. Four models, each under 5,000 triangles, ambient occlusion in the vertex colour. Source `.blend` files in `Art/source/DioramaTurn/`.
- **Texture** (`Tools/textures/DioramaTurn/`, run in Blender): one wood albedo and normal, 512 px, tiles.
- **Pixel art** (`Tools/aseprite/DioramaTurn/*.lua`, the `aseprite-art` skill, `.aseprite` sources in `Art/source/DioramaTurn/`, exports in `Art/Sprites/`, 16-colour shared palette):
  - Pip, 32 x 32, one sheet: idle 2 frames, walk 4, hop 3, cheer 3, shake-head 2 (14 frames).
  - Bridge piece, 32 x 32, one image (plank). Goal star, 32 x 32, 4 frames.
  - Decor on blocks, 16 x 16: flower, mushroom, grass tuft.
  - UI, 16 x 16: turn arrow left and right, restart, star full and empty, tap ring (3 frames), cross.
  - Effects, small: confetti (3 shapes), dust puff (3 frames), ribbon dot (4 x 4).
  - Budget: under 200 KB of PNG in all.
- **Audio** (`Tools/audio/DioramaTurn/`, generated by script): about 14 short sounds (turn whoosh, clunk, join up, join down, tap pluck, step tick, hop boing, landing, bonk, tick, star arpeggio, star chime, pop, restart) and one 16 to 24 s music loop. Budget: under 3 MB.
- **Total new binary files:** under 5 MB.

## Acceptance criteria
### Checked by tests or captures
- [ ] The join rule gives the expected result for a table of example node pairs in all four views, including t = 0, t = +1, t = -1 and pairs that are not joined.
- [ ] Turning left or right four times returns to view 0; a turn changes the set of joins as the table says.
- [ ] Walking finds the shortest route, reports "unreachable" when none exists, and marks steps across a join as hops.
- [ ] A turn is refused while Pip walks, one extra turn is queued during a turn, and no turn changes Pip's node.
- [ ] Every shipped room passes the validator (size, one Start, one Goal, no two nodes on one screen spot in any view).
- [ ] Every shipped room is solvable, its par equals the solver's minimum turns, and it is not solvable with zero turns.
- [ ] The cursor moves to the nearest reachable node in the pressed screen direction and stays put when there is none.
- [ ] The scale maths gives a whole number of screen pixels per sprite pixel at 1080, 720 and 1440 high, never below 1.
- [ ] Every PNG under `Art/Sprites/` imports as a Point-filtered Sprite (from Part 1) and every sprite pixel is in the shared palette.
- [ ] The sheet's JSON slices into the expected number of frames for Pip, the star and the effects.
- [ ] With `feel.effectStrength` at 0 the overshoot, the squash, the particles and the slow motion are all 0.
- [ ] A capture of each room in each of the four views shows the whole room inside the frame, with no error in the console.
- [ ] A PlayMode run reaches the Goal in each room using only the input that the solver gives (turns and node picks) and ends in "room complete".
### Needs a person
- [ ] Turn a room slowly and fast: the quarter turn feels good (0.45 s is a starting guess), starts at once and settles with a small bounce, and the user never loses track of which piece is which.
- [ ] A join reads as a join: when two pieces line up, the player sees it without being told (ribbon, glow, chime) and without confusion about pieces at different depths.
- [ ] The sprites stay crisp and do not shimmer during the turn, and the 3D blur around them looks like a toy diorama, not a smear. The scale looks right at 1080 and at 720.
- [ ] The mood reads bright and playful, not busy. Colours are not harsh next to each other.
- [ ] The sounds feel like toys: the chimes are pleasant when repeated, the music is not tiring in 10 minutes, and the ducking under the star is clean.
- [ ] Room difficulty: room 1 takes under a minute, room 2 a few minutes, room 3 five to ten, without a stall.
- [ ] Keyboard and gamepad feel good: the cursor goes where the player expects. (I cannot test a real gamepad here; the tests drive the input with simulated devices.)

## Tests
- EditMode: join rule (table of pairs per view); view rotation and the turn queue; node graph for a view; breadth-first route with hops; solver (minimum turns and moves) on small hand-made rooms and on the three shipped rooms; level text parser (bad row length, unknown character, no Start, two Goals all rejected); validator; cursor navigation; pixel-scale maths; sprite snapping function; sheet JSON slicer; settings ranges and the `effectStrength` zero case; the palette checker; a star calculation (par and par + extras).
- PlayMode: the scene builds and loads; the camera is orthographic and ends each turn at the exact view angle; Pip walks to a reachable node and stops; a turn pressed during a walk is refused; a room is completed from the solver's moves; the next room loads; restart restores the first state; effects are off at strength 0.

## Out of scope
- More than three rooms, a level editor, level select, saving or settings screens (only the effect-strength setting).
- Free camera, other axes of rotation, a tilt, zoom, or non-quarter turns.
- Stairs, ladders, moving pieces, switches, keys, enemies, collectibles, timers, or a hint system.
- A touch-first interface (touch works like a mouse but is not tuned).
- Story, dialogue, localisation, voices.
- Anything from Fez, Octopath Traveler or Monument Valley beyond the ideas named above.

## Open questions
- **Isometric camera (assumed, please confirm):** the join rule needs a true isometric orthographic camera, so that pieces line up on a whole-number lattice. A flat side-on Fez-style view would be simpler to read but shows less of the 3D blocks and the lighting. I assumed isometric.
- **Sprites face the camera (assumed):** sprites always face the camera, so a sprite piece looks the same in all four views; only its position changes. That means no sprite has a "front" and no piece needs four drawings.
- **Pixel snapping (to test):** snapping sprites to screen pixels at rest may show a small jump at the end of a turn; whether to snap during a turn is decided on a capture and a play test.
- **Blur without Depth of Field (changed while planning):** the installed URP's Depth of Field shaders read depth with a perspective-only formula `[read]` (`GaussianDepthOfField.shader` and `BokehDepthOfField.shader` in URP 17.6), so they cannot be trusted with an orthographic camera. The blur is a screen-space tilt-shift pass instead (needs no depth). It needs a renderer feature on a renderer asset, see the plan's decisions.
- **Sprite shadows (to spike):** whether alpha-clipped sprites cast clean shadows, and whether SMAA softens sprite pixels (the shared render asset has MSAA off, so SMAA is on the camera instead), is to be tested in the plan's first stage.
- **Name:** `DioramaTurn` as chosen.

## Sources
Details and confidence tags in `docs/research/diorama-turn.md`.
- https://veeenu.github.io/blog/fez-world `[read]` (one developer's analysis of the Fez effect; not Polytron's own account)
- https://en.wikipedia.org/wiki/Fez_(video_game), https://en.wikipedia.org/wiki/HD-2D, https://en.wikipedia.org/wiki/Monument_Valley_(video_game) `[secondary]`
- Siliconera and Nintendo Life HD-2D interviews, GDC Vault listing for Monument Valley, a Japanese write-up of the GDC 2018 talk `[secondary]`
- The Unreal Engine Octopath Traveler spotlight returned an error and was not read `[unverified]`
- The isometric lattice rule (depth step (1,1,1) per view) is my own reasoning from the projection; it is checked by tests, not by a source `[unverified]`
