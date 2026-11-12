# PixelInvaders

Status: built, waiting for the play test (stage 4)
Kind: new game

<!-- Written by the game-spec skill. Research and sources: docs/research/pixel-invaders.md -->

## One-page design
- **Pitch:** Defend a dark brushed-metal 3D command trench against waves of upright, shadow-casting pixel-art invaders that march faster with every kill and drop arcade power-ups from passing UFOs.
- **Feeling:** rhythmic, escalating arcade tension. The march starts as a steady pulse and tightens into a frantic sprint for the last alien, punctuated by crisp pixel explosions and brief bursts of overpowered fire.
- **Pillars:**
  1. **The march is the clock.** So we tie formation movement, sprite pose toggling, and the four-note bass heartbeat to one step timer that shrinks as invaders are destroyed. So we won't let enemies drift at arbitrary speeds or desynchronise audio from the formation step.
  2. **Pixel art is the star on a 3D stage.** So we author every character, rank, UFO, bunker block, power-up capsule, explosion and HUD badge as crisp Point-filtered Aseprite sheets standing upright in a lit 3D trench where they cast real shadows and catch laser light. So we won't blur or stretch sprites, and we won't build characters out of 3D meshes.
  3. **Every shot and erode counts.** So we limit the base cannon to one active shot on screen (making misses tense), erode bunkers chunk by chunk where projectiles or low invaders strike, and make UFO power-up drops feel like earned rule-breakers. So we won't allow default button-mashing spam or binary full-health/zero-health bunkers.
  4. **Readable at a glance in the trench.** So we keep the 3D metal deck dark and matte, give each alien rank and power-up capsule both a distinct colour and a distinct silhouette/icon, and reserve bright emission for shots, UFOs and explosions. So we won't clutter the floor or let post-processing smear sprite silhouettes.
- **Core loop:** slide the cannon along the bottom rail, time shots between bunker gaps to thin the marching rows, snipe the top-lane UFO to catch a falling power-up capsule (Rapid Fire, Twin Shot, or Bunker Repair), survive the accelerating final invaders, and clear the wave before they touch the defense line; repeat on the next wave starting one row lower until all 3 lives are lost.
- **Verbs:** move left/right, fire laser, catch a falling power-up capsule, restart after game over.
- **Players, session length, platform, 2D or 3D:** one player, 3 to 8 minutes per run, desktop (keyboard or gamepad via Input System), 2.5D: a perspective 3D command trench built in Blender with upright pixel-art sprites authored in Aseprite.

## Look and feel brief
### References
Details and confidence tags: `docs/research/pixel-invaders.md`. All art, models, textures and sounds are original.
- **Space Invaders (Taito, Tomohiro Nishikado, 1978):** take the 5-row lockstep formation that speeds up as aliens die, the descending four-note heartbeat bassline, the 1-shot base cannon, two-sided bunker erosion, and the periodic mystery UFO across the top lane. Don't copy Taito's trademarked names, exact 1978 sprite bitmaps, cabinet art or sound recordings.
- **Space Invaders Extreme (Taito / Backbone, 2008):** take the perspective-tilted 3D tunnel behind crisp 2D sprites, musical hit chimes, and temporary weapon/repair power-ups with a visible depleting gauge beside the cannon. Don't copy Fever-mode roulette minigames, stage branching, or its UI layout.
- **Octopath Traveler / HD-2D (Square Enix and Acquire, 2018):** take upright, Point-filtered pixel-art billboards standing in a lit 3D diorama where they cast shadows onto a textured floor and receive dynamic light and bloom. Don't copy any character, UI or fantasy aesthetic.

### Chosen direction
You picked **Retro Command Trench**: a recessed, dark brushed-metal 3D command deck with bevelled side rails and warm amber/phosphor lane markers, populated by vibrant multi-colour pixel-art invaders casting shadows onto the deck and lit by punchy laser bloom.

### Visual pillars
- **Shape language:** each rank has an unmistakable pixel silhouette and frame width: Top rank (**Scout**, 30 pts) is narrow and diamond-domed with twin antennae; Middle rank (**Guard**, 20 pts) is angular and winged with pincer claws; Bottom rank (**Heavy**, 10 pts) is wide and tentacled; the **UFO** is a wide saucer with rotating dome lights; the **Cannon** is a low, wide turret with a central barrel; **Power-up capsules** are rounded pill canisters with a 1-letter/glyph emblem inside. (pillars 2, 4)
- **Colour philosophy:** dark slate-steel base (`#0B0F17` to `#1B2432`) with dim amber tactical lane lines (`#8C5A2B`), contrasted against a 16-colour shared Aseprite palette where each gameplay role has a fixed high-contrast hue and a shape backup. (pillars 2, 4)
- **Lighting:** one cool-white overhead directional key light (`#D8E6FF`) pitched to cast crisp alien and bunker shadows down-trench onto the metal floor, plus small dynamic point lights on player/UFO projectiles and explosions that briefly glint off the brushed metal deck and nearby sprites. (pillars 2, 4)
- **Materials:** dark brushed metal with subtle normal-mapped ribs on the 3D trench, matte dark steel on the outer hull, alpha-clipped Point-filtered URP Lit quads for all pixel sprites (receiving light and casting shadows, zero smoothness), and bright emissive sprite/particle layers for lasers, UFO beacons and explosion cores. The brightest things on screen are active projectiles, explosions, and falling power-up capsules. (pillars 2, 4)
- **Rendering and post-processing:** URP Bloom (tuned so lasers, UFO lights and explosions glow without bleeding across pixel-art alien silhouettes), subtle Vignette (`0.25`) to frame the trench, Neutral tonemapping so the 16-colour pixel palette stays true to its hex values. No depth-of-field blur over the playfield so every alien rank stays razor-sharp. (pillars 2, 4)
- **Camera:** perspective camera pitched down at 55 degrees from behind the player's defense line, narrow field of view (34 degrees), framing the entire trench from the cannon rail at the bottom to the UFO lane at the top. Camera holds steady except for brief directional recoil/shake impulses on hits, bunker erosion, and cannon loss. (pillars 1, 4)
- **UI and text:** clean arcade HUD along the top bevel of the trench showing `SCORE`, `HIGH SCORE`, `WAVE`, and remaining lives as Aseprite mini-cannon icons; when a timed power-up is active, a pixel-art timer bar and badge appear beside the player cannon. (pillars 2, 4)

### Palette
All Aseprite sprites share one 16-colour palette (plus transparent `#00000000`), and the 3D trench materials draw from the dark metal and amber roles so the 2D and 3D assets belong to one world.

| Index | Role | Colour (hex) | Used for |
|---|---|---|---|
| 0 | Transparent | `#00000000` | sprite background |
| 1 | Void black | `#090C12` | sprite outlines, deep trench recesses, camera background |
| 2 | Trench steel | `#161E2B` | 3D trench deck base, dark sprite shading |
| 3 | Hull slate | `#2C394B` | 3D side rails, cannon chassis shadow, capsule rim |
| 4 | Highlight steel | `#63768D` | metal highlights, cannon barrel trim |
| 5 | Phosphor white | `#F0F6FF` | hit-flash frames, projectile cores, HUD text, eyes |
| 6 | Amber grid | `#D9822B` | trench defense line, lane ticks, Rapid Fire (`R`) capsule |
| 7 | Solar gold | `#FFD147` | explosion cores, high-score crown, Rapid Fire highlight |
| 8 | Cyan turret | `#2CE8F5` | player cannon, player laser, Twin Shot (`T`) capsule |
| 9 | Deep cobalt | `#1B76B5` | player cannon mid-tone, Twin Shot shadow |
| 10 | Bunker emerald | `#38D973` | defense bunkers, Shield Repair (`S`) capsule, bottom Heavy rank |
| 11 | Bunker moss | `#1E824C` | bunker shaded/damaged edge, Heavy rank shadow |
| 12 | Guard violet | `#B85CFF` | middle Guard rank (20 pts), alien zig-zag shot |
| 13 | Guard plum | `#6B2FA3` | middle Guard rank shadow |
| 14 | Scout coral | `#FF4769` | top Scout rank (30 pts), UFO saucer body, alien heavy shot |
| 15 | Crimson ember | `#A81D39` | Scout/UFO shadow, outer explosion embers, danger pulse |

Non-colour backup: the three alien ranks differ in silhouette and row position (Scout top/narrow, Guard middle/winged, Heavy bottom/wide); player vs. alien shots travel in opposite directions and have different sprite shapes (straight bolt vs. animated 2-frame zig-zag/cross); the three power-up capsules carry distinct pixel glyphs (`R` / bolt for Rapid Fire, `T` / twin arrows for Twin Shot, `S` / wrench-shield for Shield Repair).

### Surfaces
Decided by the agent from the look brief, following `.claude/skills/game-polish/references/surfaces.md`. Strengths live on `PixelInvadersSettings` (`look.surface`). Materials are named `PixelInvaders<Thing>`.

| Material or object | Surface | Why (pillar) |
|---|---|---|
| Trench deck and side rails (`trench_deck.glb`, `trench_rail.glb`) | procedural brushed-metal and panelled steel texture (512 px albedo + normal map, world-space triplanar via `PixelInvadersLit.shader`) plus baked vertex-colour ambient occlusion (`AO`) in the `.glb` | metal is a real material and the trench ribs need crevice shadow so the 3D stage feels physical without competing with the sprites (pillars 2, 4) |
| Defense line and lane markers (`trench_markers.glb`) | flat warm amber colour + mild emission, no texture, by design | clean tactical lines must read sharply across the deck without texture noise (pillar 4) |
| Upright pixel sprites (Invaders, UFO, Cannon, Bunkers, Capsules) | Point-filtered alpha-clipped sprite texture on upright quads, lit by URP key/point lights and casting shadows, smoothness 0, no normal map | crisp pixels are the hero; receiving scene light and casting shadows grounds them in the 3D trench (pillar 2) |
| Projectiles, explosion sprites, UFO beacon pulse | Point-filtered unlit/emissive sprite quads + Bloom, no surface texture | they are pure light and energy and must be the brightest objects on screen (pillars 3, 4) |
| Spawn and removal | wave spawn drops ranks in row-by-row with a quick vertical scale snap; alien/UFO death swaps to a 1-frame white flash then a 3-frame Aseprite pixel explosion sheet + pixel-chunk burst particles; bunker cells vanish block-by-block with a small emerald spark (no smooth 3D noise dissolve shader on sprites) | pixel-art enemies should shatter into crisp pixel explosions and chunks rather than smooth shader gradients (pillars 2, 3) |

### Audio direction
Punchy retro-arcade synth paired with an energetic synthwave command-trench groove:
- **The march heartbeat:** four descending bass synth notes (`D2, C2, B1, A1`) played one note per formation step; as the step interval shrinks from `0.65 s` down to `0.07 s`, the bassline accelerates into a racing pulse.
- **Sound effects (`Tools/audio/PixelInvaders/make_sounds.py`):** 12 generated `.wav` clips — 4 march step bass notes, player laser pew, twin-shot pew, alien shot buzz, alien pop (pitched slightly higher for 20-pt and 30-pt ranks), bunker crumble tick, UFO looping warble, UFO explosion + bonus chime, power-up pickup arpeggio, cannon death blast, wave clear fanfare, game over descending synth.
- **Music (`Tools/audio/PixelInvaders/music/trench_pulse.py`):** one looping 32-to-48 s minor-key synth/electro background track rendered with FluidSynth (`Tools/audio/render_music.sh PixelInvaders trench_pulse`) played at low volume (`0.28`) so the 4-note step heartbeat and hit chimes stay front and centre; ducks under wave clear and cannon loss.

### Feel table
| Event or action | Visual | Audio | Camera | Time (hit-stop, slow motion) | Pillar |
|---|---|---|---|---|---|
| Cannon moves left / right | cannon sprite tilts 4 degrees into movement, subtle exhaust pixel flicker | none | none | none | 2, 4 |
| Cannon fires (base or powered) | muzzle flash sprite frame (1 frame), cannon recoils down `0.08 m` and eases back, laser bolt lights the deck beneath it | crisp rising square-wave pew (layered double-pew on Twin Shot) | tiny vertical kick (`0.02 m`) | none | 2, 3 |
| Shot misses (hits top rail) | small 2-frame spark on the top trench rail | short dull high tick | none | none | 3 |
| Formation steps sideways | all living aliens toggle between march frame A and B in lockstep and shift one step | next note of the 4-note bass heartbeat | none | none | 1, 2 |
| Formation hits wall & drops a row | aliens step down toward the defense line, amber side rail flashes briefly | heavier bass thud + low warning harmonic when within 2 rows of bunkers | subtle vertical nudge (`0.03 m`) | none | 1, 4 |
| Shot hits an alien | alien swaps to white hit-flash frame during hit-stop, then plays 3-frame pixel explosion + 6 pixel-chunk particles; floating score popup (`10`/`20`/`30`) | punchy retro pop + pitched chime by rank (higher for Scout) | light directional shake (`0.05 m`) | hit-stop `35 ms` | 1, 2, 3 |
| Shot or alien erodes a bunker cell | impacted bunker cells clear immediately, neighbouring edge cells shift to damaged moss tint, emerald pixel sparks fly | gritty short crunch | tiny shake (`0.03 m`) | none | 2, 3 |
| UFO enters top lane | saucer glides across top trench rail with 2-frame rotating dome lights and a warm coral point light | classic oscillating UFO warble (looped while on screen) | none | none | 2, 4 |
| Shot destroys UFO | 1-frame white flash, big 4-frame saucer explosion, point value pops (`100`–`300`), and a colour-coded **Power-up Capsule** spawns and drifts down | deep boom + bright bonus arpeggio | medium shake (`0.10 m`) | hit-stop `60 ms` | 2, 3 |
| Catch a Power-up Capsule | capsule bursts into a ring of pixels; cannon flashes in the capsule's colour; for `Rapid Fire` / `Twin Shot` the HUD gauge beside the cannon fills; for `Shield Repair` damaged bunker cells restore in a bottom-up wave | rising 4-note power-up fanfare | small zoom pulse | none | 2, 3 |
| Power-up timer expires | gauge blinks for the last `1.5 s`, then shrinks away with a soft flash | short two-note power-down blip | none | none | 3, 4 |
| Cannon hit by alien shot (lose a life) | cannon plays 4-frame pixel shatter + ember burst, trench lights flash crimson, active projectiles clear, 1 s pause before respawn | heavy explosion + descending tone, music ducks | strong shake (`0.18 m`, `0.25 s`) | hit-stop `90 ms` | 2, 3 |
| Wave cleared (last alien killed) | last alien explodes with extra sparks, trench lane lines sweep bright amber from bottom to top, next wave drops in one row lower | triumphant 5-note arcade fanfare, music ducks briefly | subtle push-in and settle | brief slow motion `0.5x` for `0.35 s` on final kill | 1, 3 |
| Game over (0 lives or invasion line crossed) | remaining aliens play victory pose toggle, crimson wash on trench rails, Game Over card with score, high score and "Press Space / South to Restart" | low descending alarm and final thud | slow pull-back | hit-stop `100 ms` | 1, 4 |

### Six-area feel review
- **Input:** Left/Right (A/D, Arrow keys, D-pad or Left Stick) moves the cannon smoothly with immediate response; Space / South button / Up / W fires. Main risk: sticky or sluggish horizontal stops when dodging between bunker gaps. Cannon movement is direct velocity with a very tight stop (`0.04 s` settle).
- **Response:** shots spawn on the exact frame of input, and alien hit-flash + hit-stop trigger on the exact contact step. Main risk: the 1-shot base limit feeling unresponsive if the player presses fire just before their old shot hits the top rail. Add a `0.10 s` input buffer so a press right before impact fires the next bolt immediately on clear.
- **Context:** the 3D metal trench rails bound the horizontal march, the amber defense line marks instant invasion defeat, and the four emerald bunkers create narrow firing lanes and cover. Main risk: camera pitch hiding the gap between bunkers and the bottom alien row; 55-degree pitch is verified on a capture.
- **Polish:** 2-frame lockstep march across 55 sprites, pixel explosions, chunk particles, dynamic projectile point lights on the metal floor, and the accelerating 4-note bass heartbeat. Main risk: point lights or bloom washing out pixel art; bloom threshold is kept above sprite albedo luminance (`1.0`).
- **Metaphor:** crisp arcade pixel invaders marching down a physical metallic command trench. Main risk: flat sprites looking pasted onto 3D geometry; dynamic shadows cast by every alien onto the brushed-metal deck tie them together.
- **Rules:** thinning the formation makes the survivors march faster, turning the last 3 aliens into a high-speed duel, while UFO power-ups reward risky shots past the formation. Main risk: power-ups trivialising the wave if they last too long; timed duration (`6.0 s`) and UFO spawn cadence keep them special.

## Mechanics
All simulation rules live in plain deterministic C# (`Assets/Prototypes/PixelInvaders/Scripts/`) with no Unity physics (`Rigidbody`/`Collider`), stepped at a fixed delta time so EditMode and PlayMode tests can drive any scenario deterministically.

- **Playfield coordinates:** a 2D logical plane `(x, z)` inside the 3D trench: horizontal `x` from `-7.0` to `+7.0` (`width = 14.0`), depth `z` from `-5.0` (player defense rail at `z = -4.2`) to `+5.5` (UFO lane at `z = +4.8`). Sprites stand upright on this plane tilted to face the camera cleanly while casting shadows onto the `y = 0` deck.
- **Alien formation:** 5 rows of 11 columns (`55` invaders per wave):
  - Row 4 (top): **Scout** (`30` pts)
  - Rows 3 & 2 (middle): **Guard** (`20` pts)
  - Rows 1 & 0 (bottom): **Heavy** (`10` pts)
  - Horizontal column spacing `1.0 m`, vertical row spacing `0.75 m`. Wave 1 top row starts at `z = +3.6`; each subsequent wave starts `0.30 m` lower (capped at `4` steps lower so the bottom row never spawns on top of the bunkers).
- **Step march and acceleration:**
  - All living invaders move together in discrete steps. On each step, every living invader advances horizontally by `stepHorizontal = 0.22 m` in the current direction (`+1` right or `-1` left) and toggles its sprite frame (`0 <-> 1`).
  - If any living invader's next horizontal step would cross `xMin = -6.2` or `xMax = +6.2`, the formation instead steps down by `stepVertical = 0.35 m`, reverses horizontal direction, and toggles its sprite frame.
  - **Step interval formula:** `interval = lerp(minStepInterval, maxStepInterval, pow((aliveCount - 1) / 54.0, speedCurveExponent))`, where `maxStepInterval = 0.65 s` (55 alive), `minStepInterval = 0.065 s` (1 alive), and `speedCurveExponent = 1.35`. Each step advances the 4-note bass cycle (`0 -> 1 -> 2 -> 3 -> 0`).
  - **Invasion defeat:** if any living invader reaches or crosses `defenseLineZ = -3.5`, the game ends immediately in **Game Over** regardless of remaining lives.
- **Player cannon and shooting:**
  - The cannon moves along `z = -4.2` between `x = -6.0` and `x = +6.0` at `cannonSpeed = 7.5 m/s`.
  - **Base weapon:** at most `1` player projectile can exist on screen at a time (`playerShotSpeed = 14.0 m/s` toward `+z`). Pressing fire within `fireInputBuffer = 0.10 s` before the active shot expires queues the next shot.
  - **Alien firing:** only the lowest living invader in each column may fire. An alien shot spawns every `alienFireInterval` (scales from `1.25 s` down to `0.55 s` as the wave progresses or aliens thin), up to `maxAlienShots = 3` simultaneous alien projectiles on screen, travelling toward `-z` at `alienShotSpeed = 6.5 m/s`.
  - **Shot-vs-shot collision:** if a player bolt collides with an alien projectile, both cancel in a small spark burst.
- **Destructible pixel bunkers:**
  - `4` bunkers sit evenly spaced along `z = -2.8` (`x = -4.5, -1.5, +1.5, +4.5`).
  - Each bunker is a `12 x 8` boolean grid of `0.10 m` cells shaped like a classic bevelled arch (top corners bevelled, bottom-centre arch cut out, authored as a character map in code and matching the Aseprite bunker sprite).
  - When a player shot (from below) or alien shot (from above) hits an intact cell, that cell and any intact orthogonal neighbours within `bunkerBlastRadius = 1` cell are destroyed, and the projectile is consumed.
  - When a descending alien overlaps any intact bunker cell, that cell is scraped away immediately without stopping the alien.
- **UFO and dropped power-up capsules:**
  - Every `ufoSpawnInterval = 14.0 s` (provided at least `8` aliens remain in the wave and no UFO is active), a UFO enters at `z = +4.8` from either the left or right edge and crosses at `ufoSpeed = 3.6 m/s`.
  - Destroying the UFO awards `100`, `150`, `200`, or `300` points (cycled deterministically by the player's shot counter, in homage to Nishikado's original rule) and drops one **Power-up Capsule** from the UFO's position that falls toward `-z` at `capsuleFallSpeed = 3.2 m/s`.
  - **Three capsule types** (cycled deterministically or from a seeded sequence so tests can verify each):
    1. **Rapid Fire (`R`, amber):** for `powerupDuration = 6.0 s`, allows up to `3` simultaneous player shots on screen with a minimum fire cooldown of `0.16 s`.
    2. **Twin Shot (`T`, cyan):** for `powerupDuration = 6.0 s`, each fire action launches a pair of parallel bolts offset by `±0.22 m` (up to `2` salvos = `4` bolts on screen).
    3. **Shield Repair (`S`, emerald):** instantaneous effect that restores all `4` bunkers back to their full `12 x 8` arch mask and grants a `50` pt bonus.
  - Picking up a new timed power-up (`Rapid Fire` or `Twin Shot`) replaces the current timed weapon and resets the timer to `6.0 s`.
- **Lives, wave progression, and scoring:**
  - Player starts with `startingLives = 3`. Taking an alien projectile hit decrements lives by `1`, clears active projectiles and falling capsules, pauses for `respawnDelay = 1.0 s`, and respawns the cannon at `x = 0`.
  - Clearing all `55` invaders starts a `waveClearDelay = 1.6 s` celebration, then spawns `wave + 1` one step lower.
  - High score persists in memory across restarts during the session (and initializes to `1000`).

## Tuning values
Every number sits on `PixelInvadersSettings` (`Assets/Prototypes/PixelInvaders/Settings/PixelInvadersSettings.asset`), grouped by role:
- `arena`: `halfWidth = 7.0`, `cannonLimitX = 6.0`, `alienLimitX = 6.2`, `cannonZ = -4.2`, `defenseLineZ = -3.5`, `bunkerZ = -2.8`, `wave1TopZ = 3.6`, `ufoZ = 4.8`.
- `formation`: `rows = 5`, `columns = 11`, `colSpacing = 1.0`, `rowSpacing = 0.75`, `stepHorizontal = 0.22`, `stepVertical = 0.35`, `maxStepInterval = 0.65`, `minStepInterval = 0.065`, `speedCurveExponent = 1.35`, `waveDropStep = 0.30`, `maxWaveDropSteps = 4`, `waveClearDelay = 1.6`.
- `combat`: `startingLives = 3`, `cannonSpeed = 7.5`, `cannonStopTime = 0.04`, `playerShotSpeed = 14.0`, `fireInputBuffer = 0.10`, `alienShotSpeed = 6.5`, `alienFireIntervalStart = 1.25`, `alienFireIntervalMin = 0.55`, `maxAlienShots = 3`, `respawnDelay = 1.0`, `hitRadius = 0.32`.
- `bunkers`: `count = 4`, `cols = 12`, `rows = 8`, `cellSize = 0.10`, `blastRadiusCells = 1`.
- `ufoAndPowerups`: `ufoSpawnInterval = 14.0`, `minAliensForUfo = 8`, `ufoSpeed = 3.6`, `capsuleFallSpeed = 3.2`, `powerupDuration = 6.0`, `rapidFireMaxShots = 3`, `rapidFireCooldown = 0.16`, `twinShotOffset = 0.22`, `twinShotMaxSalvos = 2`, `shieldRepairBonus = 50`.
- `scores`: `heavyPoints = 10`, `guardPoints = 20`, `scoutPoints = 30`, `ufoPointsTable = [100, 150, 200, 300]`, `defaultHighScore = 1000`.
- `camera`: `pitchDeg = 55.0`, `fovDeg = 34.0`, `distance = 13.5`, `lookAtZ = 0.2`, `pixelsPerUnit = 16`.
- `look`: `bloomIntensity = 0.65`, `bloomThreshold = 1.0`, `vignetteIntensity = 0.25`, `keyLightIntensity = 1.15`, `shotLightIntensity = 1.8`, `shotLightRange = 2.2`, `surface.textureStrength = 0.85`, `surface.normalStrength = 0.55`, `surface.occlusionStrength = 0.65`.
- `feel`: `effectStrength = 1.0` (range `0..1`, scales shake, hit-stop, slow-mo, particles and recoil; `0` disables them), `alienHitStop = 0.035`, `ufoHitStop = 0.060`, `deathHitStop = 0.090`, `waveClearSlowMoScale = 0.5`, `waveClearSlowMoDuration = 0.35`, `shakeFire = 0.02`, `shakeAlienKill = 0.05`, `shakeBunkerHit = 0.03`, `shakeUfoKill = 0.10`, `shakeDeath = 0.18`.
- `audio`: `masterVolume = 0.8`, `musicVolume = 0.28`, `musicDuckDb = -8.0`, `musicDuckDuration = 1.2`.

## Art and audio assets
- **Pixel art (`Tools/aseprite/PixelInvaders/*.lua` via `Tools/aseprite/run.sh`, `.aseprite` sources in `Art/source/PixelInvaders/`, `.png` + `.json` sheets in `Assets/Prototypes/PixelInvaders/Art/Sprites/`, 16-colour shared palette):**
  1. `invaders_sheet` (`16 x 16` per frame):
     - **Scout** (top rank, coral): march A, march B, white hit-flash (3 frames).
     - **Guard** (middle rank, violet): march A, march B, white hit-flash (3 frames).
     - **Heavy** (bottom rank, emerald): march A, march B, white hit-flash (3 frames).
     - **UFO** (`24 x 16` per frame): dome spin A, dome spin B, white hit-flash (3 frames).
  2. `cannon_and_shots_sheet`:
     - **Cannon** (`16 x 16`): idle, tilt-left, tilt-right, fire-recoil, shatter-1, shatter-2, shatter-3, shatter-4 (8 frames).
     - **Projectiles & flashes** (`8 x 8`): player bolt, twin bolt, muzzle flash, alien zig-zag A, alien zig-zag B (5 frames).
  3. `powerups_and_fx_sheet`:
     - **Power-up capsules** (`12 x 12`, 2-frame pulse each): Rapid Fire (`R`), Twin Shot (`T`), Shield Repair (`S`) (6 frames).
     - **Explosions** (`16 x 16`): alien pixel burst (3 frames), UFO big burst (4 frames), rail/bunker spark (2 frames) (9 frames).
  4. `bunker_and_hud_sheet`:
     - **Bunker cell tiles** (`8 x 8`): intact emerald block, damaged moss block, arch corner tiles (4 frames) + full `24 x 16` reference bunker preview.
     - **HUD icons** (`12 x 12`): life mini-cannon, high-score crown, Rapid badge, Twin badge, Shield badge (5 frames).
  - Total sprite budget: under `150 KB` across all PNGs and JSONs.
- **Blender 3D models (`Tools/blender/PixelInvaders/make_trench.py` -> `Assets/Prototypes/PixelInvaders/Art/Exports/*.glb`, `.blend` in `Art/source/PixelInvaders/`):**
  - `trench_deck.glb`: recessed command floor with bevelled cross-ribs and vertex-colour ambient occlusion (`AO`).
  - `trench_rail.glb`: angled left/right containment walls with bolt/panel detail and vertex-colour `AO`.
  - `trench_markers.glb`: thin raised defense-line bar (`z = -3.5`) and side lane tick markers.
  - Budget: under `4,000` triangles total across all 3 models.
- **Procedural textures (`Tools/textures/PixelInvaders/make_textures.py`, run headless in Blender):**
  - `metal_deck_albedo.png` and `metal_deck_normal.png` (`512 x 512`, tileable brushed panelled steel).
- **Shader (`Assets/Prototypes/PixelInvaders/Shaders/PixelInvadersLit.shader`):**
  - World-space triplanar brushed-metal texture + vertex-colour `AO` for the 3D trench models.
- **Audio (`Tools/audio/PixelInvaders/make_sounds.py` and `Tools/audio/PixelInvaders/music/trench_pulse.py`):**
  - 12 short `.wav` SFX clips + 1 looping `music.wav` track rendered with FluidSynth (`GeneralUser-GS.sf2`). Total audio budget: under `4 MB`.

## Acceptance criteria
### Checked by tests or captures
- [x] Headless Aseprite script(s) run via `Tools/aseprite/run.sh PixelInvaders ...`, write `.aseprite` files to `Art/source/PixelInvaders/` and `.png` + `.json` files to `Assets/Prototypes/PixelInvaders/Art/Sprites/`, every PNG imports as a Point-filtered Sprite with no compression, every opaque pixel belongs to the 16-colour palette, and the JSON slices into the exact expected frame counts.
- [x] Headless Blender script writes `trench_deck.glb`, `trench_rail.glb` and `trench_markers.glb` within the triangle budget and with populated vertex-colour `AO`; headless texture script writes valid `512 x 512` tileable albedo and normal PNGs; `PixelInvadersLit.shader` compiles with zero errors.
- [x] A fresh wave spawns `55` invaders in `5` rows of `11` (`11` Scouts, `22` Guards, `22` Heavies) and `4` intact bunkers with identical arch masks.
- [x] Stepping the simulation advances all living invaders horizontally in lockstep, toggles their pose (`0 <-> 1`), reverses and drops the formation by `stepVertical` at the horizontal boundary, and shrinks the step interval monotonically as `aliveCount` drops from `55` to `1`.
- [x] Base weapon allows only `1` active player shot at a time, honours the `0.10 s` fire buffer, destroys hit invaders, awards `10`/`20`/`30` points by rank, and increments high score when score exceeds it.
- [x] Only the bottom-most living invader in each column can fire; alien shots and player shots erode bunker cells from above and below, and low-marching invaders scrape away overlapping bunker cells.
- [x] Destroying a UFO awards points from the deterministic shot-count table (`100`, `150`, `200`, `300`) and spawns a falling power-up capsule (`Rapid Fire`, `Twin Shot`, or `Shield Repair`).
- [x] Catching `Rapid Fire` allows up to `3` active shots for `6.0 s`; catching `Twin Shot` fires paired bolts for `6.0 s`; catching `Shield Repair` restores eroded bunker cells immediately.
- [x] Clearing all `55` invaders advances to `wave + 1` with its top row starting one step lower (up to `4` steps).
- [x] Losing all `3` lives or allowing any living invader to reach `defenseLineZ` triggers `Game Over`, and pressing restart resets to Wave 1 with 3 lives while preserving `highScore`.
- [x] With `feel.effectStrength = 0`, camera shake, hit-stop, slow-motion and recoil offsets are all `0`.
- [x] `Tools/unity/check.sh all` passes with zero errors, and a `Tools/unity/capture.sh play` render shows the 3D trench, all 3 alien ranks, the 4 bunkers, the cannon and the HUD inside the camera frame.

### Needs a person
- [ ] Play a full wave from 55 aliens down to the last 1: the accelerating 4-note heartbeat and step speed feel tense and fair, and the final alien is exciting rather than impossible to hit.
- [ ] Judge the 2.5D look in motion: the pixel-art invaders, bunkers and capsules stay crisp against the brushed-metal 3D trench, their shadows ground them on the deck, and laser bloom feels punchy without washing out sprite details.
- [ ] Shoot through bunker gaps and catch a UFO power-up: bunker erosion reads clearly chunk-by-chunk, and `Rapid Fire` / `Twin Shot` / `Shield Repair` feel rewarding for their `6 s` window.
- [ ] Listen to the step bassline, hit chimes, UFO warble and background synth track together: nothing clips or clashes, and the music sits comfortably underneath the step heartbeat.

## Tests
- **EditMode (`PixelInvaders.Tests.EditMode`):**
  - Formation initialization (5x11 ranks, positions, scores), horizontal lockstep movement, boundary drop-and-reverse, and step-interval acceleration curve (`55` vs `25` vs `1` alive).
  - Base 1-shot rule, `0.10 s` fire input buffer, shot-vs-invader collision, shot-vs-shot cancellation, and lowest-per-column alien shooter selection.
  - Bunker `12 x 8` arch grid initialization, top-down alien shot erosion, bottom-up player shot erosion, and low-invader overlap scraping.
  - UFO spawn timing, deterministic shot-count score table, capsule drop, and all three power-up effects (`Rapid Fire` shot cap, `Twin Shot` dual bolts + expiry after `6.0 s`, `Shield Repair` bunker restoration).
  - Wave clear transition (lower start position per wave, clamped at 4 steps), cannon life loss, invasion line immediate Game Over, session high-score persistence across restart, and `effectStrength = 0` disabling shake/hit-stop/slow-mo.
  - Asset and structure checks: Aseprite sprites exist, use Point filter, match the 16-colour palette, and JSON frame counts match; Blender `.glb` meshes have vertex `AO`; procedural textures are `512 x 512` power-of-two and tileable; shader has no compile errors; audio clips are non-silent and unclipped.
- **PlayMode (`PixelInvaders.Tests.PlayMode`):**
  - Built scene loads with camera, lights, post-processing volume, trench models, 55 invader views, 4 bunker views, cannon view and HUD.
  - Simulated horizontal input moves the cannon within bounds; fire input spawns a bolt that destroys an invader and updates the HUD score; catching a spawned capsule updates the weapon state; restart after Game Over resets the wave.

## Out of scope
- Two-player alternating or co-op modes.
- Galaga-style curved bezier dive-bombing paths or boss fights (kept pure to Classic Invaders + UFO power-ups).
- Persistent disk/cloud leaderboards or settings menus.
- Custom fonts or localisation.

## Open questions
- **Prototype folder name (`PixelInvaders`, assumed):** named `PixelInvaders` (`Assets/Prototypes/PixelInvaders/`, `docs/prototypes/pixel-invaders.md`) to sit alongside `NeonPong` and `MedievalChess`.
- **Bunker rendering representation (decided for plan):** each bunker's `12 x 8` cells will be rendered as crisp Point-filtered pixel sprites driven directly by the simulation's cell state (intact vs. damaged neighbour vs. empty), so erosion is 100% deterministic and casts real 3D shadows matching the rest of the sprite cast.
- **`DioramaTurn` fate:** per your instruction, `DioramaTurn` stays untouched while we build and play-test `PixelInvaders`, and we will decide whether to remove `DioramaTurn` after finishing the game.

## Sources
Details and full notes in `docs/research/pixel-invaders.md`.
- https://shmuplations.com/nishikado `[read]` (Tomohiro Nishikado 2000 Developer Interview on *Breakout* inspiration, alien sprites, microprocessor speed-up, and shot-counted UFOs)
- https://www.taito.co.jp/en/steam/sie/help `[read]` (Official Taito *Space Invaders Extreme* manual on timed power-up items, weapon types, and chain/rate HUD gauges)
- https://en.wikipedia.org/wiki/Space_Invaders, https://en.wikipedia.org/wiki/Space_Invaders_Extreme, https://en.wikipedia.org/wiki/HD-2D `[secondary]`
- https://retrocean.com/nes/space-invaders, https://www.ign.com/articles/2008/06/13/space-invaders-extreme-review-3, https://www.eurogamer.net/space-invaders-extreme-review `[secondary]`
- https://docs.unity.com/en-us/engine/6000.3/manual/render-pipelines/universal-render-pipeline/urp-reference/universalrp-asset `[read]`
