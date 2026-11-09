---
name: game-polish
description: Applies a spec's look-and-feel brief and feel table to a working game in small steps, each judged on a render, covering materials, lighting, post-processing, motion, effects, camera, time feedback and sound, and ends with a list for a person to play-test. Use it when a game works but looks or feels basic, after game-build has made it playable, or when the user asks for more juice, style or visual polish.
---

# game-polish

Make a working game look and feel the way its spec says. Polish is not decoration. Each change must serve a pillar and must be seen on a render before it stays.

## Before you start
- The game must compile and its tests must pass. Run `Tools/unity/check.sh all` and note the result. Polish never goes on top of a red build.
- Read the spec's pillars, look-and-feel brief (palette, visual pillars, camera, audio direction) and **feel table**. If any of these is missing, run `game-spec` first. Don't invent a style.
- Read `references/urp-effects.md` (what is verified and what is not) and `references/audio.md`. Read `references/polish-order.md`.
- Read `Tools/unity/capture.sh` usage, and `game-build/references/pitfalls.md`.

## The loop for one step
1. **Baseline capture.** Before the first change, save a render with `capture.sh play` (or `screen` for a scene with no UI text). The `camera` mode has no post-processing, so it hides Bloom and Vignette. Look at it and write down what is off against the look brief.
2. **Pick one step** from `references/polish-order.md`: one visual pillar or one feel-table row.
3. **Make the change** through code, settings and the scene builder. Never hand-edit scenes or models. Put every strength, duration, colour and speed on the settings asset.
4. **Add tests for the logic** that tests can reach: a shake decays to zero, a hit-stop ends, an effect triggers on the right event, a setting of zero turns it off. Run `Tools/unity/check.sh all`.
5. **Capture again** and open both images. Does it match the brief? Is anything harder to read than before (the ball, the score, the player)? Fix what you see, then capture again.
6. **Keep or cut.** If the change does not serve a pillar, remove it. Say what you cut and why.
7. **Commit** the step with a conventional message such as `feat(<name>): add ball trail`.

## At the end of the stage
- Show the before and after renders.
- List what you could not check: how it feels in motion, timing, loudness, comfort. Give the person a short play-test list: what to do, what to look at, and what "good" means for each item from the spec.
- Offer a strength setting for shake, flashes and camera motion, including zero, and check it works.
- Stop for review. Don't start another round until the person has played it.

## Adding polish for a new feature
Only polish the feature's own feel-table rows, in the existing style guide. Check the old rows still look the same by capturing before and after.

## Don't
- Add an effect nobody asked for in the spec. If you think of a good one, propose it as an open question.
- Copy another game's assets, names or sounds.
- Make the state of the game harder to read (a glowing ball that hides the paddle).
- Push, change GitHub settings, or add packages without asking. Prefer a few lines of your own code over a package for small effects, and say why.
- Change global git config, or rewrite history that is already pushed.
