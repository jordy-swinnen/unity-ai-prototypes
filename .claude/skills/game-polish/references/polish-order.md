# Polish order

Do these roughly in order. Each step is its own change with its own before and after render. Skip a step if the spec doesn't ask for it.

1. **Palette and materials.** Apply the palette from the brief. Choose materials per object: base colour, smoothness, emission. Decide which objects are brightest, since that is where the eye goes. Keep gameplay objects clearly readable against the background.
2. **Surfaces.** For every material, textures, baked occlusion and shader effects as decided in the spec's Surfaces table (`surfaces.md`). Do it before lighting is final, since lights show texture and occlusion. Compare with the strength at 0.
3. **Lighting.** Key light direction and colour, ambient colour or skybox, shadows if the brief wants them. Check readability in a render.
4. **Camera.** Angle and field of view from the brief. A slight tilt or perspective often adds depth for little work.
5. **Post-processing.** A global Volume with a profile (see `urp-effects.md`). Typical first choices: Bloom for emissive objects, Tonemapping for contrast, Vignette for focus, Color Adjustments for mood. Keep each effect modest. Compare with the effect switched off.
6. **Motion feedback.** Easing instead of snapping, scale pop or squash and stretch on contact, smooth score changes, small idle motion where the brief wants life.
7. **Effects.** (A removed or destroyed object burns, dissolves or breaks up, see `surfaces.md`.) Trails and particles tied to events from the feel table (hit, score, serve, win). Keep their count and lifetime small.
8. **Camera and time feedback.** Screen shake with strength and decay (a few lines of own code, away from the hit), a brief hit-stop on big events, an optional slow-motion beat on the winning moment. Add the global strength setting.
9. **Sound.** One short sound per feel-table row, made by a script (see `audio.md`). Vary pitch with intensity.
10. **UI.** Text style, placement and animation matching the brief. Keep it readable on every background.
11. **Final pass.** Capture the whole game in several states (start, mid-play, score, win). Check each against the palette and pillars. Check performance by eye (no stutter) and note anything heavy.

Rule of thumb: when two changes fight, remove the weaker one.
