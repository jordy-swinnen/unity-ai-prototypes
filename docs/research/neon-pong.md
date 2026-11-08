# Neon Pong: research notes

Researched 8 November 2026 for `docs/prototypes/neon-pong.md`, following the `game-spec` skill. General background is in `game-feel-and-look-development.md`.

Confidence tags: **[read]** read on the named page, **[secondary]** blog, review or search summary, **[unverified]** not confirmed.

## Reference 1: the original Pong (1972)
- **[read]** Allan Alcorn split each paddle into eight segments so the hit position changes the return angle. The article gives one number: the centre segments return the ball at 90 degrees to the paddle, and the outer ones at smaller angles. It gives no other angles. [Wikipedia: Pong](https://en.wikipedia.org/wiki/Pong)
- **[secondary]** A French-language source gives different figures (180 degrees from the two centre segments and 45 from the outer ones). The sources disagree on numbers and agree on the pattern: centre goes straight back, edges go out sharper.
- **[read]** The ball accelerates the longer it stays in play, and missing resets the speed. No numbers are given. (same article)
- **[read]** The original sounds were simple tones from the sync generator, after the designers couldn't make crowd sounds with digital circuits. (same article)
- **What to take:** the rally pressure curve (speed climbs, a miss resets) and bare, short tones for events. **What not to copy:** the name's branding, the exact original sounds, the 1972 look.

## Reference 2: Geometry Wars (Bizarre Creations)
- **[read]** The look is simple geometric shapes. The creator planned the shapes on graph paper and hand-coded them. The look is colour-saturated neon. A background grid replaced a starfield, because a starfield was judged impractical for such frantic action. Glowing bursts came from simple particle effects. Significant game objects got a distinctive shadow, and everything else stayed flat, so attention stays on the game. [Game Developer: The Color and the Shape](https://www.gamedeveloper.com/game-platforms/the-color-and-the-shape-bizarre-creations-on-i-geowars-i-sensible-aesthetic)
- **[read]** The article doesn't explain how the glow was produced beyond "simple particle effects", and says nothing about flashes.
- **[secondary]** Reviews describe a faux-vector look with phosphor glow and bright flashes when enemies die. One review credits Tempest 2000 as the origin of the over-bright, neon-blurred polygon style. [Trusted Reviews](https://www.trustedreviews.com/reviews/geometry-wars-retro-evolved-2)
- **What to take:** simple shapes, saturated colour on a dark base, a quiet background, bright bursts on events. **What not to copy:** its grid layout, enemy shapes, and colour assignments.

## Reference 3: neon-on-dark art direction in general
All **[secondary]**, from design blogs and guides of mixed quality, so treat them as heuristics.
- Saturated accents read much brighter against near-black than against grey.
- Build glow in layers: a bright core, a soft glow, an outer bloom. Let the glow colour fade toward a darker saturated tone, not toward plain white.
- Keep the palette small (3 to 5 saturated colours) and reserve glow for the things the player acts on.
- Neon colours often fail contrast checks, especially greens and blues on dark. Keep text neutral or high contrast.
- Strong bloom over a long session can tire the eyes, so keep a strength setting.

## How Unity URP makes this (Unity 6.3 docs)
- **[read]** The URP Asset has an **HDR** setting under Quality that turns on HDR rendering for every camera, which the page calls useful for bloom. The page doesn't say bloom requires it, and doesn't give its default. A camera's **HDR Rendering** (Output section, Base cameras only) overrides the asset's setting. The camera's default is not stated. [URP Asset](https://docs.unity.com/en-us/engine/6000.3/manual/render-pipelines/universal-render-pipeline/urp-reference/universalrp-asset), [Camera reference](https://docs.unity.com/en-us/engine/6000.3/manual/cameras/urp/camera-components-reference/camera-component-reference)
- **[read]** URP Lit: the **Emission** property sets an emitted colour, default black (none). To glow brighter than white while keeping colour, raise the **Intensity** in the colour picker. [Lit shader](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader)
- **[read]** Bloom: Threshold (default 0.9, gamma space), Intensity (default 0, off), Scatter (default 0.7), Clamp, Tint. Tonemapping **Neutral** changes hue and saturation little, and **ACES** is more contrasted and does change hue and saturation. See `game-feel-and-look-development.md`.
- **[unverified]** Whether bloom works without HDR on, the defaults of the HDR settings, and which emission intensities give a good glow. All of these are checked on renders in the build stages.

## Not found
- A technical write-up of how Geometry Wars made its glow.
- Exact angle figures for the original Pong's segments.
