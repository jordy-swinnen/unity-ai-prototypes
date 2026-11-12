# Research: PixelInvaders (2.5D classic alien wave shooter in a 3D command trench)

Researched 12 November 2026 for `docs/prototypes/pixel-invaders.md`, following the `game-spec` skill. General background is in `game-feel-and-look-development.md`.

Confidence tags: `[read]` I read the page directly, `[secondary]` a blog, review or search summary, `[unverified]` a guess or something not confirmed from primary documentation. Nothing here is copied; the references are for the feeling and mechanics lineage.

## Reference 1: Space Invaders (Taito, Tomohiro Nishikado, 1978)
- **Origins in Breakout (`[read]`):** In his 2000 interview (`shmuplations.com/nishikado`), Nishikado explains that he studied Atari's *Pong* schematics for six months and later took inspiration from Atari's *Breakout*: specifically that the player cannot advance to the next stage until every target is cleared ("all clear"), that the last remaining target is hard to hit, and that speed increases as the stage goes on.
- **Why alien sprites (`[read]`):** Nishikado initially considered tanks (rejected because horizontal movement required cannons angled sideways) and planes (rejected because the hardware could not animate smooth flight). He chose octopus/squid-like martians inspired by H.G. Wells's *The War of the Worlds* because jerky step movement and sudden row drops looked natural on alien creatures (`shmuplations.com/nishikado`).
- **Accelerating step-march (`[read]` / `[secondary]`):** Nishikado attributes the ability to speed up invaders and shoot down individual enemies to using an Intel 8080 microprocessor (`[read]`, `shmuplations.com/nishikado`). Secondary accounts explain the mechanism: the board updated one alien sprite per frame, so as aliens were destroyed, each full formation step completed in fewer frames, accelerating both the survivors and the four-note bass heartbeat (`[secondary]`, Wikipedia *Space Invaders* and *Retrocean*).
- **Four-note descending heartbeat (`[secondary]`):** Sound designer Michiyuki Kamei created a continuous four-note descending bass loop (inspired by a heartbeat and *Jaws*) that stepped in lockstep with the formation, speeding up as the formation shrank (`[secondary]`, Wikipedia *Space Invaders*, *Atari Archive*).
- **UFO spawning (`[read]`):** Nishikado wanted UFOs and their points to be random, found random generation awkward on the hardware, and tied UFO appearance/scoring to the player's shot count instead (`shmuplations.com/nishikado`).
- **Destructible bunkers and 1-shot discipline (`[secondary]`):** Stationary defense bunkers erode from enemy shots above, player shots below, and low-marching aliens scraping across them. With one active player shot on screen in the base state, a missed shot forces the player to wait until the projectile reaches the top rail (`[secondary]`, Wikipedia *Space Invaders*, *Retrocean*).
- **What to take:** The lockstep horizontal march that drops a row at the trench wall and speeds up as ranks thin, the accelerating four-note bass pulse, destructible pixel-eroded bunkers, periodic top-lane UFOs, and succeeding waves starting one row lower.
- **What not to copy:** Taito's trademarked names, exact 1978 bitmap patterns, cabinet artwork, or audio recordings.

## Reference 2: Space Invaders Extreme (Taito / Backbone Entertainment, 2008 / 2018 Steam)
- **Power-up capsules and timed weapon states (`[read]`):** The official Taito Steam manual (`taito.co.jp/en/steam/sie/help`) documents dropped power-up items that grant temporary weapon upgrades on a depleting power gauge, including a piercing beam (`LASER`), multi-projectile spread (`BROAD SHOT`), explosive area shot (`BOMB`), and defensive barrier (`SHIELD`).
- **Chain and rate feedback (`[read]`):** Successive hits before a timer expires build a chain counter beside the cannon and charge a rate multiplier, turning rhythmic accuracy into visible and audible escalation (`taito.co.jp/en/steam/sie/help`).
- **Audio-visual synesthesia (`[secondary]`):** Reviews (IGN, Eurogamer, Pocket Gamer) highlight how player shots and alien destruction chime in musical key over the driving pulse, while crisp 2D sprites pop against a deep, perspective-tilted background tunnel (`[secondary]`).
- **What to take:** Dropped colour-coded pixel-art power-up capsules from UFOs with a visible timer gauge beside the cannon (adapted for our scope as **Rapid Fire**, **Twin Shot** and **Bunker/Shield Repair**), musical hit chimes, and a perspective 3D trench stage behind crisp 2D sprites.
- **What not to copy:** Fever-mode roulette minigames, stage branching, licensed techno tracks, or Taito's UI layout.

## Reference 3: Octopath Traveler / HD-2D (Square Enix and Acquire, 2018)
- **Crisp pixel sprites in a lit 3D diorama (`[secondary]`):** Point-filtered, low-resolution pixel-art billboards stand upright inside a 3D stage with real-time lighting, cast shadows, bloom and vignette (Wikipedia *HD-2D*, Siliconera and Nintendo Life developer summaries; see `docs/research/diorama-turn.md`).
- **Why it works (`[secondary]`):** The producers noted that pixel sprites on a bare 3D mesh look flat until dynamic lighting, shadows and post-processing tie the two together (`[secondary]`).
- **What to take:** Upright alpha-clipped pixel-art sprites casting and receiving URP shadows on a textured 3D deck, paired with emissive laser bloom.
- **What not to copy:** JRPG characters, environments, UI, or fantasy tone.

## Rules the three share (the style guide)
1. **The march is the clock:** Every step moves the formation, advances the 2-frame alien pose, and plays one bass note. As aliens die, the step interval shrinks automatically.
2. **One plane of truth, 3D depth for atmosphere:** All gameplay collision happens on a deterministic 2D coordinate plane `(x, z)` inside the 3D trench, while the 3D trench walls, deck ribs, shadows and point lights give physical weight.
3. **Every pixel change is earned in Aseprite:** Alien ranks, marching poses, hit flashes, explosion frames, bunker erosion cells, UFOs, power-up capsules and HUD badges are authored as character-mapped Aseprite Lua scripts.
4. **Shots and hits light the trench:** The recessed metal trench stays dark and matte so glowing projectiles, UFO beacons, and pixel-art sprites read at a glance.

## Feasibility in this repository
- **Aseprite pipeline (`[read]`):** `Tools/aseprite/run.sh` runs headless Lua scripts in `Tools/aseprite/PixelInvaders/` and exports `.png` and `.json` (JSON-array format) to `Assets/Prototypes/PixelInvaders/Art/Sprites/`, with `.aseprite` sources in `Art/source/PixelInvaders/`. `Assets/Shared/Editor/PixelArtTextureImporter.cs` automatically configures every PNG under `/Art/Sprites/` as a Point-filtered, uncompressed Sprite with no mipmaps.
- **Destructible pixel bunkers (`[unverified]` until built):** Each bunker can be represented in pure C# simulation as a small boolean cell grid (for example `12 x 8` blocks) initialized from an Aseprite bunker mask and rendered either as instanced pixel-tile sprites or an updated Point-filtered `Texture2D` mask so top-down alien hits, bottom-up player hits, and low alien contact chew away exact pixel chunks deterministically.
- **3D command trench (`[read]`):** Headless Blender scripts (`Tools/blender/PixelInvaders/`) export `.glb` trench deck, bevelled side rails, and recessed ribs with vertex-colour ambient occlusion (`AO`), imported by `com.unity.cloud.gltfast` 6.20.0. Procedural brushed-metal albedo and normal maps are generated by `Tools/textures/PixelInvaders/make_textures.py` via Blender's bundled numpy.
- **Audio & music (`[read]`):** Short synthesised SFX in `Tools/audio/PixelInvaders/make_sounds.py` (including the 4-note bass step pitches, laser pew, alien pop, UFO warble, power-up chime) and a looping synthwave/electro command-trench track in `Tools/audio/PixelInvaders/music/` rendered via `Tools/audio/render_music.sh` and FluidSynth.

## Sources
- https://shmuplations.com/nishikado `[read]` (Tomohiro Nishikado 2000 Developer Interview, translated from *Game Maestro*)
- https://www.taito.co.jp/en/steam/sie/help `[read]` (Official Taito *Space Invaders Extreme* Steam manual: rules, power-ups, chains)
- https://en.wikipedia.org/wiki/Space_Invaders `[secondary]`
- https://en.wikipedia.org/wiki/Space_Invaders_Extreme `[secondary]`
- https://retrocean.com/nes/space-invaders `[secondary]`
- https://www.ign.com/articles/2008/06/13/space-invaders-extreme-review-3 `[secondary]`
- https://www.eurogamer.net/space-invaders-extreme-review `[secondary]`
- https://en.wikipedia.org/wiki/HD-2D `[secondary]`
- https://docs.unity.com/en-us/engine/6000.3/manual/render-pipelines/universal-render-pipeline/urp-reference/universalrp-asset `[read]` (via `docs/research/neon-pong.md`)
