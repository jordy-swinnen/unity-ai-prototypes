# Game feel and look development: research notes

Researched 8 November 2026 to design the spec, plan and polish skills for this repository. The aim is to turn "it works but it looks like blocks" into a repeatable process.

Confidence tags:
- **[read]**: read on the named page during this research.
- **[secondary]**: from a blog, forum or search summary. Treat as one person's view.
- **[unverified]**: I could not confirm it. Don't rely on it.

## 1. What "feel" is

- **[read]** Steve Swink defines game feel as real-time control of virtual objects in a simulated space, with interactions emphasised by polish. ([Wikipedia: Game feel](https://en.wikipedia.org/wiki/Game_feel))
- **[read]** The same article lists six areas for judging feel: input, response, context, polish, metaphor and rules.
  - Input is the means of control. Response is how the game reacts, including delay and how a change speeds up and settles. Context is the environment that gives actions meaning.
  - Polish is extra detail on interactions that doesn't change the mechanics. Metaphor is how mechanics relate to the theme. Rules are the defined interactions between elements.
- **[read]** The article splits polish into five kinds: animation, visual effects, sound effects, cinematic effects and tactile effects. Cinematic effects include screen shake, view-angle changes, motion blur and slow motion.
- **[unverified]** The article doesn't say how the six areas rank. The book's exact wording should be checked against Swink's 2008 book.

Use for the skill: a feel review can go through the six areas one by one. Most "it feels flat" problems sit in response and polish, and those are the cheapest to fix.

## 2. Juice: feedback layered on top of mechanics

- **[secondary]** "Juice it or lose it" (Martin Jonasson and Petri Purho, 2012) takes a plain block-breaking game and adds feedback in layers without changing the mechanics. Sources disagree about the venue (GDC Europe or Nordic Game). I could not read the slides or a transcript, so **the exact list and order of effects in that talk is [unverified]**. ([GDC Vault listing](https://gdcvault.com/play/1016487))
- **[secondary]** The commonly cited toolkit from summaries is colour, easing (tweening) instead of snapping, screen flash, particles, sound, screen shake and hit-stop.
- **[secondary]** A counterpoint exists: a talk titled "Don't Juice It or Lose It" argues heavy polish can cost immersion. Juice should serve the pillars, not decorate everything.
- **[read]** Jan Willem Nijman (Vlambeer) gave "The Art of Screenshake" at Control Conference in Amsterdam. He starts from a dull side-scrolling shooter and shows 30 small tweaks that each improve it. The write-up names only two: shake the screen slightly opposite to the direction of fire, and give enemies a 33% chance of exploding harmlessly on death. The other 28 tweaks are not in the article. ([Game Developer](https://gamedeveloper.com/design/vlambeer-co-founder-shares-advice-on-building-better-action-games))
- **[unverified]** Specific numbers such as camera offsets and shake amounts from the Nuclear Throne blog posts came from an interview, not that talk.

Use for the skill: build a **feel table**. One row per player action or game event, with columns for visual, audio, camera and time feedback. Add feedback row by row, one at a time, and judge each in play. Keep every strength and duration in a settings asset so it can be tuned.

## 3. From idea to a spec

- **[read]** An article on pre-production argues it must validate what is fun by prototyping, define pipelines, and keep living documentation. Its listed deliverables are a timeboxed phase, a vertical slice (a small playable piece of the core art, gameplay and systems, built around the game's pillars), several prototypes iterated until each system is fun, and pipeline definitions. It warns against starting by building the real game, and against documents nobody reads or that go stale. The article says itself that much of it is the author's experience and that "vertical slice" has no single definition. ([Game Developer: What you should take out of pre-production](https://www.gamedeveloper.com/game-platforms/what-you-should-take-out-of-pre-production))
- **[secondary]** Design pillars are 2 to 4 guiding principles that every later decision is checked against. They also filter scope: a feature that serves no pillar is cut.
- **[secondary]** Stone Librande's "One-Page Designs" (GDC 2010) argues for replacing long design documents with one-page diagrams that people actually read. I could not confirm the section layout of his template. ([GDC Vault](https://gdcvault.com/play/1012356))
- **[secondary]** The greybox approach: build with plain shapes first to find out whether the core is fun, then polish one element fully. The Super Mario 64 example given in the sources is an anecdote, not checked.

Use for the skill: the spec starts with a one-page design (pillars, core loop, the main verbs), then the look and feel brief, then the mechanics, acceptance criteria and tests.

## 4. Art direction

- **[read]** One art-direction guide gives these steps: set an artistic vision early (genre, audience, mood), write a style guide (colour palette, lighting principles, proportions, detail density), put cohesion ahead of individual asset quality, work within technical limits, collect references and mood boards, define a primary palette and a complementary secondary one, plan lighting and atmosphere, and iterate with feedback. It is generic, unsourced advice and has promotional content. It doesn't mention shape language. ([Wayline](https://www.wayline.io/blog/art-direction-games-best-practices))
- **[secondary]** Other guides add shape language (giving meaning to families of shapes such as squares, circles and triangles), colour that carries meaning and guides attention, a separate UI palette, and a non-colour backup (shape, icon or sound) for any colour that carries meaning.
- **[secondary]** A workable process: collect references that share a feeling, pull out the rules they have in common, write those rules down, then check every asset against them.

Use for the skill: the look brief is a short **style guide** with visual pillars: shape language, colour philosophy, lighting, material treatment and rendering style. Every item has a one-line reason tied to a pillar.

## 5. Making it real in Unity 6 URP

All of this is from Unity's own documentation, read through the pages named.

- **[read]** URP 6.3 ships 17 post-processing overrides, including Bloom, Vignette, Color Adjustments, Tonemapping, Chromatic Aberration, Film Grain, Lens Distortion, Depth of Field, Motion Blur and colour-grading tools. ([URP effect list, 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/post-processing-and-full-screen-effects/urp/effect-list.md))
- **[read]** Bloom has Threshold (default 0.9), Intensity (default 0, so off), Scatter (default 0.7), Tint, Clamp, quality filtering and a Filter choice (Gaussian, Dual, Kawase). Vignette has Color, Center, Intensity, Smoothness and Rounded. Color Adjustments has Post Exposure, Contrast, Color Filter, Hue Shift and Saturation. Tonemapping has None, Neutral and ACES modes. (same section)
- **[read]** To use effects: create a Volume (GameObject > Volume), assign a Volume Profile (New creates one), and add overrides with Add Override. A global Volume affects the camera everywhere. ([Unity 6.1: set up a volume](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/set-up-a-volume.html))
- **[read]** The camera needs **Post Processing** enabled, and the volume's layer must be in the camera's Volume Mask. The page says new URP scenes do not use post-processing by default. ([Unity 6.1: add post-processing](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/add-post-processing.html))
- **[unverified]** The C# property that turns on a camera's Post Processing from code. Whether the Unity 6 URP template scene already has a global Volume. Any Universal Renderer Data setting for post-processing. The scene builder must therefore set all of this itself and then check it in a render.
- **[read]** Cinemachine 3.1 is a free package that must be installed separately. Shake uses a Cinemachine Impulse Source (call `GenerateImpulseWithForce(float)`, where 1 is normal, or `GenerateImpulseWithVelocity(Vector3)`), and a CinemachineImpulseListener on the camera. The no-argument call uses a default velocity of (0, -1, 0). ([Impulse Source manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineImpulseSource.html), [scripting API](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineImpulseSource.html))
- **[unverified]** Whether a Cinemachine Brain is required, and which package name the Package Manager uses (the docs path suggests `com.unity.cinemachine`).

Use for the skill: for small games, prefer a few lines of own camera shake code (settings-driven, testable, no package) over adding Cinemachine. Adding a package needs approval anyway.

## 6. What this means for the skills

1. Look and feel is **specified**, not left to taste: pillars, a style guide, and a feel table sit in the spec before any art or polish work starts.
2. Polish happens **after** the rules are tested, in small steps, each judged on a render or play session. Greybox first, then polish one element fully.
3. Everything tunable lives in settings assets, so feel can be changed without touching code, and agents can't hide a bad number inside a method.
4. Agents can check numbers, tests and renders, but **not how a game feels**. Every polish stage ends with a short human play test and a list of what to look at.
5. Real references are the input. The skill researches comparable games and techniques on the web and records its sources, with the same confidence tags used here.

## Not found

- The full list of effects in "Juice it or lose it" and the other 28 tweaks in "The Art of Screenshake". The talk videos are the primary sources.
- Librande's one-page template layout.
- How audio is best produced by an agent. This is an open gap, so the first polish pass will test generating simple sounds with a script.
