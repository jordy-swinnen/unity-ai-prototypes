# Look and feel guide

How to fill in the look-and-feel part of a spec. The background and sources are in `docs/research/game-feel-and-look-development.md`. Read the confidence tags there before repeating a claim.

## What "feel" means
Steve Swink defines game feel as real-time control of virtual objects in a simulated space, with interactions emphasised by polish. He judges it on six areas. Use them as a checklist, one line each.

| Area | Ask |
|---|---|
| Input | How does the player control it? Is the mapping natural? |
| Response | How fast and how smoothly does the game react? Does a change start, peak and settle, or snap? |
| Context | Does the space give the actions meaning (walls to bounce off, things to avoid)? |
| Polish | What extra animation, effects, sound, camera and tactile feedback make hits feel real? |
| Metaphor | Do the visuals and sounds match what the mechanic is? A paddle should look and sound like something that hits. |
| Rules | Do the rules create interesting moments (risk, comebacks, speed ramps)? |

Most "it feels flat" problems are in response and polish. They are also the cheapest to fix.

## Research steps
1. Pick three references that share the feeling. For each, write what the player sees and hears when the main verb happens, and which one thing makes it feel good.
2. Search for how those games were made (talks, postmortems, art direction posts). Prefer primary sources: talks by the developers, official documentation. Mark everything else `[secondary]`.
3. Pull out rules the references share, and write them down as the style guide. Check every asset against them later.
4. Check feasibility. Mark each look item as buildable with this repository's tools, or as needing outside art.

## Visual pillars
Keep each to one or two lines and tie it to a design pillar.
- **Shape language:** which shape families mean what (round is friendly, sharp is dangerous). Keep the set small.
- **Colour philosophy:** a primary palette and a complementary secondary one, with a role for each colour. A separate palette for the UI if it differs. A non-colour backup for any colour that carries meaning.
- **Lighting:** the key light direction and mood, and whether the game is lit, emissive or both.
- **Materials:** glossy or matte, flat or textured, emissive or not. Say which objects are the brightest, since brightness decides what draws the eye.
- **Surfaces:** for each material, one of flat, emission only, procedural texture, baked occlusion, shader effect. Decide it yourself with `../../game-polish/references/surfaces.md` and write the reason in the spec. Real materials (stone, wood, metal, cloth, bone) get textures and occlusion. Neon and flat-stylised looks get none, on purpose.
- **Rendering and post-processing:** which effects (bloom, vignette, colour grading) and how strong. Post-processing is a mood tool, not a fix for weak shapes.
- **Camera:** angle, field of view, how it moves.

## Feel table rows to consider
Start from the main verbs, then add: contact or hit, miss or fail, score or reward, win and lose, start and restart, pause or waiting, and any state change the player must notice.

## Menu of feedback
Choose what serves the pillars. More is not better, and some games lose immersion when they are over-polished.
- **Visual:** colour flash, scale pop or squash and stretch, easing (smooth movement instead of snapping), trails, particles, light pulses, emissive changes, post-processing pulses, screen flash.
- **Audio:** a short sound per event, pitch that rises with intensity or combos, a low thud under big events, music layers.
- **Camera:** shake (strength and decay, often away from the hit), small pushes, zoom pulses, field-of-view kicks.
- **Time:** hit-stop (freeze a few frames on a big hit) and brief slow motion. Keep them short.
- **Tactile (if a controller exists):** rumble.

## Accessibility and comfort
- Give strong effects a global strength setting, including zero (for shake, flashes and camera motion).
- Don't rely on colour alone.
- Avoid rapid full-screen flashing.

## Tuning
Every strength, duration, colour and speed sits in a settings asset so feel can be changed without touching code. A test can cover the logic (a shake decays to zero, a hit-stop ends) even when no test can judge the result.

## What no tool can judge
Whether it feels good. The spec lists the criteria that need a person and says what to look at.
