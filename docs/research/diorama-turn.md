# Research: DioramaTurn (rotatable diorama puzzle, pixel sprites in a 3D room)

Notes for `docs/prototypes/diorama-turn.md`. Confidence tags: `[read]` I read the page, `[secondary]` a blog or summary, `[unverified]` a guess or something I could not confirm. Nothing here is copied; the references are for the feeling.

## Reference 1: Fez (Polytron, 2012)
- What the player sees and hears: the world is shown as flat 2D. A turn rotates the whole world in 90 degree steps, and pieces at different depths now touch or cross, so a new path exists. `[secondary]` (Wikipedia and a puzzle write-up, via search results)
- The one thing that makes it feel good: a turn is the whole puzzle verb, and it is cheap and exact (quarter steps). `[secondary]`
- How it works, by one developer's reverse engineering, not by Polytron: an orthographic camera, with collision that ignores the depth axis facing the player, so the player stays in a purely 2D space. `[read]` veeenu.github.io/blog/fez-world (author's own analysis, he says so; the page does not say how Fez stores its world or sorts depth).
- Cost warning: Phil Fish is quoted as saying he had to design and draw everything four times, and that the rotation could have been faked with four parallel 2D levels. `[secondary]` Wikipedia summary. For us this means: draw each piece once, as a camera-facing sprite, and let the camera do the work.
- Do not copy: Gomez, the fez, the trixel look, the name.

## Reference 2: Octopath Traveler, HD-2D (Square Enix and Acquire, 2018)
- What the player sees: pixel-art characters standing in a lit 3D diorama. The look is described as dynamic lighting, depth of field and tilt-shift giving a miniature, diorama feel. `[secondary]` Wikipedia HD-2D article (via search results). I could not read the Unreal Engine spotlight (HTTP 403), so the exact post-processing stack is `[unverified]`.
- Built in Unreal Engine, so sprites could be lit like any modern game. `[secondary]` (via The Verge, as quoted in search results)
- The team said all-pixel art first felt "fake and low-quality" next to the mix of pixel sprites and 3D. `[secondary]` (Siliconera and Nintendo Life summaries). Producer line: pixel sprites on a 3D background alone do not capture the look, lighting and post do. `[secondary]`
- The one thing that makes it feel good: crisp sprites against soft, lit, slightly blurred 3D.
- Do not copy: any character, sprite, UI or music; the JRPG tone.

## Reference 3: Monument Valley (ustwo games, 2014)
- What the player sees: impossible geometry that joins up when the view lines up, a small character that walks to a tapped spot. `[secondary]` (Wikipedia, Game Developer and GDC 2014 talk summaries)
- Design rules worth keeping: everything needed to solve a puzzle is on one screen; it is meant as an experience, not a hard challenge; puzzles were cut or made easier when they did not fit. `[secondary]` (Game Developer and Pocket Gamer summaries; I did not read the GDC recording)
- For Monument Valley 2 a Japanese write-up says the level editor assembled levels like blocks and used teleportation to keep the 2D/3D illusion working. `[secondary]` cgworld.jp; the idea matches our plan to treat a link as a graph edge, not as physics.
- Do not copy: Ida, the totem, the Escher style, any level.

## Rules the three share (the style guide)
1. The camera and the turn are the main verb, so the turn must be exact, quick and satisfying.
2. The player can read the whole puzzle on one screen.
3. The "magic" is a projection trick, not physics: walking works on a graph that is rebuilt per view.
4. Flat crisp art sits in a space that is lit and soft.

## Feasibility in this repository
- Orthographic URP camera, four snapped views, an eased turn: buildable with C# only. `[unverified]` until built; Unity's orthographic camera and a Transform tween are standard.
- Pixel sprites as camera-facing quads or SpriteRenderers on a Point-filtered Sprite import: ready (Part 1, `Assets/Shared/Editor/PixelArtTextureImporter.cs`). Pixel-perfect scale in 3D needs a fixed pixels-per-unit and an orthographic size chosen so one sprite pixel is a whole number of screen pixels at the target resolution: to be tested with a capture.
- Blender models: room shell, platforms, pedestals, glTF into Unity through glTFast (already installed). Lit by URP lights with soft shadows.
- Bloom and Vignette: URP post-processing volumes, already used by the other two prototypes. Depth of Field is not usable: both URP 17.6 DoF shaders call `LinearEyeDepth(depth, _ZBufferParams)` with no orthographic branch `[read]` (`Library/PackageCache/.../Shaders/PostProcessing/GaussianDepthOfField.shader` line 70 and `BokehDepthOfField.shader` line 38). The tilt-shift look is a screen-space pass instead (a `FullScreenPassRendererFeature` exists in this URP `[read]`).
- Gamepad: `com.unity.inputsystem` 1.20.1 is already in `Packages/manifest.json` `[read]`, so keyboard, mouse and gamepad need no new package.

## Sources
- https://en.wikipedia.org/wiki/Fez_(video_game) `[secondary]`
- https://veeenu.github.io/blog/fez-world `[read]`
- https://en.wikipedia.org/wiki/HD-2D `[secondary]`
- https://www.unrealengine.com/en-US/spotlights/octopath-traveler-s-hd-2d-art-style-and-story-make-for-a-jrpg-dream-come-true (403, not read)
- https://www.siliconera.com/project-octopath-traveler-developers-answer-project-started-troubles-developing-hd-2d/ `[secondary]`
- https://www.nintendolife.com/news/2022/05/triangle-strategy-producers-talk-hd-2d-and-why-other-devs-havent-used-it `[secondary]`
- https://en.wikipedia.org/wiki/Monument_Valley_(video_game) `[secondary]`
- https://gdcvault.com/play/1020878/Designing-Monument-Valley-Less-Game `[secondary]` (title and abstract only)
- https://cgworld.jp/feature/201804-gdc2018pt3.html `[secondary]`
