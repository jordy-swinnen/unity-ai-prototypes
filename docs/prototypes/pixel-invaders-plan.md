# PixelInvaders: plan and tasks

Plan for `docs/prototypes/pixel-invaders.md`. Status: stages 1 to 3 done, waiting for play test (stage 4)

## Decisions needed first
1. **Which Unity Editor checkout to target during `game-build`.**
   - This session is running in the git worktree `/Users/jordy/.local/share/opencode/worktree/875cd7/curious-rocket`, while Unity 6000.6.8f1 is currently open on `/Users/jordy/Projects/Unity AI Prototypes` (port 7800; a second Editor is open on another repo on port 7801).
   - **Option A (Recommended):** Open a Unity Editor instance on this worktree (`unity open /Users/jordy/.local/share/opencode/worktree/875cd7/curious-rocket` with `UNITY_PROJECT_PATH` set to this worktree) so all work stays isolated on this worktree branch.
   - **Option B:** Move this session to `/Users/jordy/Projects/Unity AI Prototypes` and reuse the already-open Editor on port 7800.
2. **No new packages and no changes to `ProjectSettings/` or `Assets/Settings/`.**
   - `com.unity.render-pipelines.universal` (17.6.0), `com.unity.inputsystem` (1.20.1), `com.unity.cloud.gltfast` (6.20.0), and `com.unity.ugui` (2.0.0) are already installed.
   - `Assets/Shared/` already provides `ShakeState`, `TimeFeel`, `PixelArtTextureImporter`, and `MusicClipImporter`. `PixelInvaders` references `Shared` without modifying it.
3. **Verified local toolchains (no installs needed):**
   - **Aseprite:** `1.3.18.6-arm64` verified at `$HOME/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite` (passed via `ASEPRITE_PATH` to `Tools/aseprite/run.sh`).
   - **Blender:** `5.2.3 LTS` verified at `/Applications/Blender.app/Contents/MacOS/Blender`.
   - **FluidSynth:** verified at `/opt/homebrew/bin/fluidsynth` with `Tools/audio/soundfonts/GeneralUser-GS.sf2`.
4. **Upright pixel sprites on alpha-clipped URP Lit quads, and 1-quad dynamic texture per bunker.**
   - As proven in `DioramaTurn`, Unity's `SpriteRenderer` does not use URP's `Universal Render Pipeline/Lit` shader (`_BaseMap` vs `_MainTex`). To cast crisp 3D shadows onto the brushed-metal trench deck and receive dynamic scene light, each upright sprite (Invaders, UFO, Cannon, Bunkers, Capsules) is a quad tilted `35°` back toward the `55°` camera (so it faces the camera dead-on) using an alpha-clipped URP Lit material (`Smoothness = 0`, Point-filtered sprite sheet texture, UV rect set from the Aseprite JSON).
   - Each of the 4 bunkers is a single `1.2 m x 0.8 m` alpha-clipped Lit quad whose `24 x 16` Point-filtered `Texture2D` is initialized from the Aseprite bunker sprite and updated whenever cells erode or repair. This keeps the scene at 4 bunker quads total while casting exact eroded shadows onto the metal deck as holes are blasted through a bunker.
5. **Unverified facts and when I check them:**
   - Whether upright alpha-clipped quads cast clean shadows onto the trench floor without shadow acne at their base: checked on the Stage 2/3 captures (mitigated by lifting sprite quad bases `0.02 m` above `y = 0`).
   - Whether `Tools/unity/check.sh` needs `--project-path` when two Unity Editors are open on the machine (`7800` and `7801`): checked at the start of Stage 1; if needed, `UNITY_PROJECT_PATH` is exported in the shell invocation without editing shared scripts unless requested.

---

## Units and layout
Unity units are metres. The trench floor is the plane `y = 0`, `x` runs left (`-`) to right (`+`), `z` runs up-screen (`-z` near the player cannon at the bottom of the frame, `+z` toward the UFO lane at the top of the frame), and `y` is height above the deck. All sprite quads are pitched so their local normal faces the `55°` camera while their bottom edge rests at `y = 0.02`.

| Object | Size (x, y, z) | Position | Why |
|---|---|---|---|
| Trench deck (`trench_deck.glb`) | `15.0 x 0.4 x 13.0` | top surface at `y = 0`, centred at `(0, -0.2, 0.2)` | recessed brushed-metal floor that catches alien and bunker shadows |
| Trench side rails (`trench_rail.glb`, 2) | `0.8 x 0.75 x 13.0` | `x = -7.1` and `x = +7.1`, `y = 0`, `z = 0.2` | physical containment walls at the horizontal march bounds |
| Defense line & lane markers (`trench_markers.glb`) | `13.4 x 0.03 x 0.08` | `z = -3.5` (defense bar) + side tick marks | marks the invasion defeat line in warm amber |
| Player cannon (sprite quad) | `0.95 x 0.95` (`16 x 16` px) | `x` in `[-6.0, +6.0]`, `z = -4.2` | bottom defense rail |
| Bunkers (4 sprite quads) | `1.20 x 0.80` (`12 x 8` logical cells, `24 x 16` px) | `x = -4.5, -1.5, +1.5, +4.5`, `z = -2.8` | evenly spaced cover between cannon and formation |
| Invader formation (`5 x 11` sprite quads) | `0.80 x 0.80` (`16 x 16` px each) | cols spaced `1.0 m`, rows spaced `0.75 m`; Wave 1 top row at `z = +3.6` (`z = +0.6` to `+3.6`) | fits 11 columns (`10.0 m` wide) with room to march between `x = ±6.2` |
| UFO saucer (sprite quad) | `1.20 x 0.80` (`24 x 16` px) | `z = +4.8`, crosses between `x = -6.8` and `+6.8` | top bonus lane above the Scout row |
| Power-up capsule (sprite quad) | `0.65 x 0.65` (`12 x 12` px) | spawns at UFO kill `(x, 4.8)`, falls to `z = -4.2` | readable falling pill with `R`/`T`/`S` glyph |
| Projectiles (player & alien quads) | `0.35 x 0.55` (`8 x 8` px) | active shot coordinates `(x, z)` | high-contrast emissive bolts |
| Camera | Perspective, `FOV = 34°`, `pitch = 55°` | `(0, 11.2, -7.6)` looking at `(0, 0, 0.2)` (`distance ≈ 13.5 m`) | frames `x` in `[-7.5, +7.5]` and `z` in `[-5.0, +5.5]` with 3D trench depth |
| Key light | Directional, `#D8E6FF`, intensity `1.15`, soft shadows | rotation `(62°, -18°, 0°)` | casts crisp alien and bunker shadows down-trench |
| HUD Canvas | uGUI Screen Space Overlay | top bezel bar + power-up gauge beside cannon | `SCORE`, `HIGH SCORE`, `WAVE`, lives icons, power-up timer |

---

## Tuning values
All fields live on `PixelInvadersSettings` (`Assets/Prototypes/PixelInvaders/Settings/PixelInvadersSettings.asset`), grouped into serialised structs:
- **`ArenaSettings arena`:** `halfWidth = 7.0f`, `cannonLimitX = 6.0f`, `alienLimitX = 6.2f`, `cannonZ = -4.2f`, `defenseLineZ = -3.5f`, `bunkerZ = -2.8f`, `wave1TopZ = 3.6f`, `ufoZ = 4.8f`, `topRailZ = 5.2f`, `bottomRailZ = -4.8f`.
- **`FormationSettings formation`:** `rows = 5`, `columns = 11`, `colSpacing = 1.0f`, `rowSpacing = 0.75f`, `stepHorizontal = 0.22f`, `stepVertical = 0.35f`, `maxStepInterval = 0.65f`, `minStepInterval = 0.065f`, `speedCurveExponent = 1.35f`, `waveDropStep = 0.30f`, `maxWaveDropSteps = 4`, `waveClearDelay = 1.6f`.
- **`CombatSettings combat`:** `startingLives = 3`, `cannonSpeed = 7.5f`, `cannonStopTime = 0.04f`, `playerShotSpeed = 14.0f`, `fireInputBuffer = 0.10f`, `alienShotSpeed = 6.5f`, `alienFireIntervalStart = 1.25f`, `alienFireIntervalMin = 0.55f`, `maxAlienShots = 3`, `respawnDelay = 1.0f`, `alienHitRadius = 0.34f`, `cannonHitRadius = 0.32f`, `shotCancelRadius = 0.22f`.
- **`BunkerSettings bunkers`:** `count = 4`, `cols = 12`, `rows = 8`, `cellSize = 0.10f`, `blastRadiusCells = 1`.
- **`UfoPowerupSettings ufo`:** `ufoSpawnInterval = 14.0f`, `minAliensForUfo = 8`, `ufoSpeed = 3.6f`, `ufoHitRadius = 0.48f`, `capsuleFallSpeed = 3.2f`, `capsulePickupRadius = 0.45f`, `powerupDuration = 6.0f`, `rapidFireMaxShots = 3`, `rapidFireCooldown = 0.16f`, `twinShotOffset = 0.22f`, `twinShotMaxSalvos = 2`, `shieldRepairBonus = 50`.
- **`ScoreSettings scores`:** `heavyPoints = 10`, `guardPoints = 20`, `scoutPoints = 30`, `ufoPointsTable = { 100, 150, 200, 300 }`, `defaultHighScore = 1000`.
- **`CameraSettings camera`:** `pitchDeg = 55.0f`, `fovDeg = 34.0f`, `distance = 13.5f`, `lookAtZ = 0.2f`, `pixelsPerUnit = 16`.
- **`LookSettings look`:** `bloomIntensity = 0.65f`, `bloomThreshold = 1.0f`, `vignetteIntensity = 0.25f`, `keyLightIntensity = 1.15f`, `shotLightIntensity = 1.8f`, `shotLightRange = 2.2f`, `textureStrength = 0.85f`, `normalStrength = 0.55f`, `occlusionStrength = 0.65f`.
- **`FeelSettings feel`:** `effectStrength = 1.0f` (`0..1`), `alienHitStop = 0.035f`, `ufoHitStop = 0.060f`, `deathHitStop = 0.090f`, `waveClearSlowMoScale = 0.5f`, `waveClearSlowMoDuration = 0.35f`, `shakeFire = 0.02f`, `shakeAlienKill = 0.05f`, `shakeBunkerHit = 0.03f`, `shakeUfoKill = 0.10f`, `shakeDeath = 0.18f`.
- **`AudioSettings audio`:** `masterVolume = 0.8f`, `musicVolume = 0.28f`, `musicDuckDb = -8.0f`, `musicDuckDuration = 1.2f`.

---

## Code design
Four assemblies under `Assets/Prototypes/PixelInvaders/`:
- `PixelInvaders` (`Assets/Prototypes/PixelInvaders/Scripts/PixelInvaders.asmdef`, namespace `Prototypes.PixelInvaders`, references `Shared`, `Unity.InputSystem`, `UnityEngine.UI`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`).
- `PixelInvaders.Editor` (`Assets/Prototypes/PixelInvaders/Editor/PixelInvaders.Editor.asmdef`, namespace `Prototypes.PixelInvaders.Editor`, Editor-only).
- `PixelInvaders.Tests.EditMode` (`Assets/Prototypes/PixelInvaders/Tests/EditMode/PixelInvaders.Tests.EditMode.asmdef`, namespace `Prototypes.PixelInvaders.Tests.EditMode`, Editor-only).
- `PixelInvaders.Tests.PlayMode` (`Assets/Prototypes/PixelInvaders/Tests/PlayMode/PixelInvaders.Tests.PlayMode.asmdef`, namespace `Prototypes.PixelInvaders.Tests.PlayMode`, all platforms).

### Pure C# simulation (Stage 1, zero MonoBehaviour/scene dependencies)
- **`PixelInvadersSettings` (ScriptableObject):** holds all tuning structs above with `Validate()` range clamping.
- **`InvaderRank` (`Heavy = 0`, `Guard = 1`, `Scout = 2`), `PowerupKind` (`None`, `RapidFire`, `TwinShot`, `ShieldRepair`), `GamePhase` (`Playing`, `Respawning`, `WaveClear`, `GameOver`).**
- **`BunkerGrid`:** `12 x 8` cell mask per bunker initialized from a 12-char-by-8-row arch pattern; `HitAtWorld(x, z, radiusCells)` erodes impacted cells, `ScrapeBox(minX, maxX, minZ, maxZ)` clears cells overlapped by low aliens, `RepairAll()` restores the full arch, and `GetCellVisualState(col, row)` returns `Empty`, `Intact`, or `DamagedEdge` (an intact cell adjacent to a newly eroded hole).
- **`SheetFrames`:** parses Aseprite JSON-array files into UV rectangles (`Rect`) and tag frame ranges (`from`, `to`).
- **`PixelInvadersInput`:** `float moveAxis` (`-1..+1`), `bool firePressed`, `bool restartPressed`.
- **`GameEvent` & `GameEventKind`:** value-type events emitted during `Step(dt, input)`: `ShotFired`, `ShotMissed`, `FormationStepped(noteIndex, droppedRow)`, `InvaderKilled(rank, x, z, points)`, `BunkerEroded(bunkerIndex, x, z)`, `UfoSpawned`, `UfoDestroyed(x, z, points, droppedPowerup)`, `PowerupCollected(kind, x, z)`, `PowerupExpired(kind)`, `CannonDestroyed(x, z, livesLeft)`, `WaveCleared(wave)`, `WaveStarted(wave)`, `GameOver(invasion)`.
- **`PixelInvadersSimulation`:** owns deterministic state (formation array of 55 `InvaderState`, step timer, step direction, 4-note bass index, cannon `x` and velocity, active player shots, active alien shots, 4 `BunkerGrid`s, UFO state, falling capsule state, active timed power-up and timer, score, highScore, wave, lives, shot counter, seeded PRNG for alien column fire). `Step(float dt, PixelInvadersInput input)` advances one fixed tick and populates `Events`.

### Runtime view & presentation (Stages 2 & 3)
- **`SpriteSheetAtlas`:** holds material/texture references and parsed `SheetFrames` for the four Aseprite sheets, applying UV scale/offset via `MaterialPropertyBlock`.
- **`BunkerTextureView`:** maintains the `24 x 16` Point-filtered `Texture2D` for each bunker quad from `BunkerGrid.GetCellVisualState`.
- **`PixelInvadersView` (MonoBehaviour):** syncs the 55 invader quads, 4 bunker quads, cannon quad, UFO quad, capsule quad, projectile quads + point lights, transient explosion/spark sprite pools, score popups, camera shake (`ShakeState`), hit-stop/slow-mo (`TimeFeel`), and uGUI HUD (`SCORE`, `HIGH SCORE`, `WAVE`, life icons, active power-up bar).
- **`PixelInvadersAudio` (MonoBehaviour):** plays the 4-note bass step heartbeat, laser pews, rank-pitched alien pops, UFO warble loop, power-up fanfare, and ducks the looping `music.wav` track.
- **`PixelInvadersGame` (MonoBehaviour):** reads keyboard/gamepad via `UnityEngine.InputSystem`, steps `PixelInvadersSimulation` at a fixed step (paused during `TimeFeel` hit-stop and scaled during slow-mo), and forwards `Events` to `PixelInvadersView` and `PixelInvadersAudio`.

### Editor (`PixelInvaders.Editor`)
- **`PixelInvadersSceneBuilder`:** menu command `Prototypes/PixelInvaders/Build Scene` creating `Assets/Prototypes/PixelInvaders/Settings/PixelInvadersSettings.asset`, materials in `Materials/`, and deterministic scene `Assets/Prototypes/PixelInvaders/Scenes/PixelInvaders.unity`.
- **`SurfaceTextureImporter`:** `AssetPostprocessor` setting `Art/Textures/*_normal.png` to `NormalMap` and `*_albedo.png` to `Default` sRGB Repeat with mipmaps.

---

## Art and audio pipeline
All assets are generated by scripts and never edited by hand:
1. **Aseprite pixel art (`Tools/aseprite/PixelInvaders/*.lua`):**
   - Run with `ASEPRITE_PATH="$HOME/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite" Tools/aseprite/run.sh PixelInvaders <script>.lua`.
   - Scripts:
     - `invaders.lua` -> `Art/Sprites/invaders_sheet.{png,json}` and `Art/source/PixelInvaders/invaders.aseprite` (Scout 3 frames, Guard 3 frames, Heavy 3 frames, UFO 3 frames).
     - `cannon_and_shots.lua` -> `Art/Sprites/cannon_and_shots_sheet.{png,json}` and `Art/source/PixelInvaders/cannon_and_shots.aseprite` (cannon 8 frames, projectiles & muzzle flash 5 frames).
     - `powerups_and_fx.lua` -> `Art/Sprites/powerups_and_fx_sheet.{png,json}` and `Art/source/PixelInvaders/powerups_and_fx.aseprite` (3 capsules × 2 frames, alien burst 3 frames, UFO burst 4 frames, spark 2 frames).
     - `bunker_and_hud.lua` -> `Art/Sprites/bunker_and_hud_sheet.{png,json}` and `Art/source/PixelInvaders/bunker_and_hud.aseprite` (bunker preview & tiles, life icon, crown icon, R/T/S badges).
   - Every exported PNG is scaled up to a temporary file outside the repo and inspected visually with `read`.
2. **Blender 3D trench (`Tools/blender/PixelInvaders/make_trench.py`):**
   - Run with `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/blender/PixelInvaders/make_trench.py`.
   - Writes `Art/Exports/{trench_deck,trench_rail,trench_markers}.glb` (with `BVHTree` baked vertex-colour `AO` attribute and applied scale) and `Art/source/PixelInvaders/trench.blend`.
3. **Procedural brushed-metal textures (`Tools/textures/PixelInvaders/make_textures.py`):**
   - Run with `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/textures/PixelInvaders/make_textures.py`.
   - Writes `Art/Textures/metal_deck_albedo.png` and `Art/Textures/metal_deck_normal.png` (`512 x 512`, tileable frequency-domain brushed steel + panel seams). Inspected with `read`.
4. **URP Shader (`Assets/Prototypes/PixelInvaders/Shaders/PixelInvadersLit.shader`):**
   - Hand-written URP Lit shader with world-space triplanar mapping for the brushed-metal deck/rails and vertex-colour `AO` multiplication.
5. **Audio & Music (`Tools/audio/PixelInvaders/make_sounds.py` and `Tools/audio/PixelInvaders/music/trench_pulse.py`):**
   - `python3 -I Tools/audio/PixelInvaders/make_sounds.py` writes 12 `.wav` SFX clips to `Assets/Prototypes/PixelInvaders/Audio/`.
   - `Tools/audio/render_music.sh PixelInvaders trench_pulse` renders `Assets/Prototypes/PixelInvaders/Audio/music.wav` via FluidSynth and checks duration, peak, RMS and loop seam.

---

## Tasks
Inside `game-build` and `game-polish`, stages run back to back with a commit per stage, stopping only for a blocking question or when the Stage 4 play-test list is ready.

### Stage 1: greybox (folders, assemblies, EditMode tests first, pure C# simulation)
- [x] Verify the Unity Editor connection for this checkout and run `Tools/unity/check.sh all` to record the baseline test counts.
- [x] Create `Assets/Prototypes/PixelInvaders/` with `Scripts/`, `Editor/`, `Tests/EditMode/`, `Tests/PlayMode/` and the four `.asmdef` files (`PixelInvaders`, `PixelInvaders.Editor`, `PixelInvaders.Tests.EditMode`, `PixelInvaders.Tests.PlayMode`).
- [x] Write the EditMode test suite first (`SimulationTests.cs`, `BunkerGridTests.cs`, `SheetFramesTests.cs`):
  - `5 x 11` formation spawn, rank assignment (`10`/`20`/`30` pts), horizontal lockstep step, pose toggle (`0 <-> 1`), boundary drop-and-reverse, and monotonic step-interval acceleration (`55` vs `25` vs `1` alive).
  - Base 1-shot cap, `0.10 s` fire input buffer, shot-vs-invader kill + score + high-score update, shot-vs-shot cancellation, and lowest-per-column alien shooter rule.
  - `BunkerGrid` `12 x 8` arch initialization, top-down alien shot erosion, bottom-up player shot erosion, damaged-edge neighbour state, and low-invader scraping.
  - UFO spawn cadence, deterministic shot-count points (`100`, `150`, `200`, `300`), capsule drop, and all 3 power-ups (`Rapid Fire` 3-shot cap, `Twin Shot` dual bolts + `6.0 s` expiry, `Shield Repair` full bunker restore + `50` pts).
  - Wave clear transition (next wave starts one step lower, clamped at `4`), life loss + respawn, invasion line immediate Game Over, restart preserving `highScore`, and `effectStrength = 0` clamping feel values to zero.
- [x] Implement `PixelInvadersSettings`, `BunkerGrid`, `SheetFrames`, `GameEvent`, and `PixelInvadersSimulation` until all EditMode tests pass (`Tools/unity/check.sh editmode`).
- [x] Deliberate-bug check: invert the step-interval acceleration curve (or break the 1-shot limit), verify the corresponding EditMode tests fail, and restore.
- [x] Commit: `feat(pixel-invaders): add simulation rules and EditMode tests`.

### Stage 2: playable (first Aseprite sheet, scene builder, input, view, PlayMode tests)
- [x] Generate the initial Aseprite sheets (`invaders.lua`, `cannon_and_shots.lua`, `powerups_and_fx.lua`, `bunker_and_hud.lua`) via `Tools/aseprite/run.sh`, inspect the scaled-up PNGs, and add `SpriteAssetTests` (Point filter, uncompressed, 16-colour palette compliance, JSON frame counts).
- [x] Implement `SpriteSheetAtlas`, `BunkerTextureView`, `PixelInvadersView`, `PixelInvadersGame`, and `PixelInvadersSceneBuilder` (`Prototypes/PixelInvaders/Build Scene`).
- [x] Build `Assets/Prototypes/PixelInvaders/Scenes/PixelInvaders.unity` twice via `Prototypes/PixelInvaders/Build Scene` and verify deterministic scene content in an EditMode scene-builder test.
- [x] Write and run PlayMode tests (`PixelInvadersPlayModeTests.cs`): scene loads with all 55 invaders, 4 bunkers, cannon and HUD; horizontal input moves cannon within bounds; firing destroys an invader and updates HUD score; catching a capsule activates the power-up; Game Over and restart reset the wave.
- [x] Capture the scene (`Tools/unity/capture.sh camera` and `screen`) to verify layout and framing against the layout table.
- [x] Run `Tools/unity/check.sh all` and perform a deliberate-bug check in `PixelInvadersView`.
- [x] Commit: `feat(pixel-invaders): add Aseprite sheets, playable scene and PlayMode tests`.

### Stage 3: look (`game-polish` — surfaces, 3D trench, lighting, VFX, audio, music)
- [x] Take a baseline `screen` capture of the Stage 2 scene.
- [x] **Surfaces & 3D Trench:** write and run `Tools/textures/PixelInvaders/make_textures.py` (`metal_deck_albedo.png`, `metal_deck_normal.png`), `Tools/blender/PixelInvaders/make_trench.py` (`trench_deck.glb`, `trench_rail.glb`, `trench_markers.glb` with vertex `AO`), `SurfaceTextureImporter`, and `Shaders/PixelInvadersLit.shader`. Add `SurfaceAndModelTests` (shader compiles, textures tile/power-of-two/unit normals, `.glb` triangle count and vertex `AO` variation). Rebuild scene and inspect capture. Commit: `feat(pixel-invaders): add 3D command trench models, metal textures and lit shader`.
- [x] **Lighting, post-processing & feel juice:** add URP Volume (Bloom, Vignette, Neutral tonemapping), projectile dynamic point lights, hit-flash frames, pixel explosion and spark pools, floating score popups, cannon tilt/recoil, camera shake (`ShakeState`), and hit-stop/slow-mo (`TimeFeel`). Verify on `screen`/`play` captures and test `effectStrength = 0`. Commit: `feat(pixel-invaders): add lighting, bloom, explosions and camera/time feel`.
- [x] **Audio & FluidSynth music:** run `Tools/audio/PixelInvaders/make_sounds.py` (12 SFX clips) and `Tools/audio/render_music.sh PixelInvaders trench_pulse` (`Audio/music.wav`), wire `PixelInvadersAudio` with the accelerating 4-note heartbeat and music ducking, and add `AudioAssetTests` (non-silent, unclipped, loop seam). Commit: `feat(pixel-invaders): add arcade synth SFX and FluidSynth music loop`.

### Stage 4: play test and tidy
- [x] Run `Tools/unity/check.sh all` and `python3 Tools/agents/check_config.py`.
- [x] Present the final captures and the "Needs a person" play-test checklist for you to play in the Editor.
- [x] Fold in any play-test tuning feedback (added `AudioListener` to `Main Camera` and enabled `playOnAwake` on `MusicSource`) and remove `DioramaTurn`.

---

## Verification
| Criterion | How it is checked |
|---|---|
| Aseprite scripts write `.aseprite`, `.png` and `.json`; Point filter; 16-colour palette; frame counts | `SpriteAssetTests` (EditMode) + `Tools/aseprite/run.sh` exit 0 + visual inspection of scaled PNGs |
| Blender `.glb` trench models (triangle budget, vertex `AO`), `512 x 512` metal textures, `PixelInvadersLit.shader` | `SurfaceAndModelTests` (EditMode) |
| `5 x 11` wave spawn, lockstep horizontal march, boundary drop, monotonic step acceleration | `SimulationTests` (EditMode) |
| Base 1-shot rule, `0.10 s` input buffer, rank scoring (`10`/`20`/`30`), shot-vs-shot cancel, lowest-in-column alien fire | `SimulationTests` (EditMode) |
| Bunker `12 x 8` arch erosion from above, below, and low alien contact | `BunkerGridTests` (EditMode) + `PixelInvadersPlayModeTests` (PlayMode) |
| UFO shot-table scoring (`100`–`300`) and dropped `Rapid Fire`, `Twin Shot`, `Shield Repair` capsules | `SimulationTests` (EditMode) + `PixelInvadersPlayModeTests` (PlayMode) |
| Wave clear lower start position (up to 4 steps), 3 lives, invasion line Game Over, restart keeping `highScore` | `SimulationTests` (EditMode) + `PixelInvadersPlayModeTests` (PlayMode) |
| `feel.effectStrength = 0` disables shake, hit-stop, slow-mo and recoil | `SimulationTests` & `PixelInvadersPlayModeTests` |
| Scene builds deterministically and renders trench, 3 ranks, 4 bunkers, cannon and HUD in frame | `SceneBuilderTests` (EditMode) + `Tools/unity/capture.sh screen` / `play` |
| Accelerating heartbeat feel, 2.5D visual cohesion, power-up pacing, and audio balance | **Person** (Stage 4 play-test checklist) |

---

## Risks
- **Two Unity Editors open on the machine (`7800` on main checkout, `7801` on another repo):** `unity cmd` without a matching project path will fail with `AMBIGUOUS_EDITOR` or hit the wrong checkout. Mitigation: resolve Decision 1 before Stage 1 and ensure the active checkout has its own connected Editor.
- **Shadow acne at the bottom of upright sprite quads resting on `y = 0`:** alpha-clipped quads intersecting the deck plane can self-shadow their bottom pixel row. Mitigation: place sprite quad bottoms at `y = 0.02 m` above the deck surface.
- **Bloom washing out bright pixel sprites (`#F0F6FF` phosphor white):** if sprite albedo triggers Bloom, alien eyes and HUD badges will blur. Mitigation: keep URP Lit sprite albedo at `<= 0.95` linear and set Bloom threshold to `1.0`, reserving HDR > `1.0` emission colours exclusively for lasers, explosion cores, and UFO beacons.
