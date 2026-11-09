# Polish order

Do these roughly in order. Each step is its own change with its own before and after render. Skip a step if the spec doesn't ask for it.

1. **Palette and materials.** Apply the palette from the brief. Choose materials per object: base colour, smoothness, emission. Decide which objects are brightest, since that is where the eye goes. Keep gameplay objects clearly readable against the background.
2. **Lighting.** Key light direction and colour, ambient colour or skybox, shadows if the brief wants them. Check readability in a render.
3. **Camera.** Angle and field of view from the brief. A slight tilt or perspective often adds depth for little work.
4. **Post-processing.** A global Volume with a profile (see `urp-effects.md`). Typical first choices: Bloom for emissive objects, Tonemapping for contrast, Vignette for focus, Color Adjustments for mood. Keep each effect modest. Compare with the effect switched off.
5. **Motion feedback.** Easing instead of snapping, scale pop or squash and stretch on contact, smooth score changes, small idle motion where the brief wants life.
6. **Effects.** Trails and particles tied to events from the feel table (hit, score, serve, win). Keep their count and lifetime small.
7. **Camera and time feedback.** Screen shake with strength and decay (a few lines of own code, away from the hit), a brief hit-stop on big events, an optional slow-motion beat on the winning moment. Add the global strength setting.
8. **Sound.** One short sound per feel-table row, made by a script (see `audio.md`). Vary pitch with intensity.
9. **UI.** Text style, placement and animation matching the brief. Keep it readable on every background.
10. **Final pass.** Capture the whole game in several states (start, mid-play, score, win). Check each against the palette and pillars. Check performance by eye (no stutter) and note anything heavy.

Rule of thumb: when two changes fight, remove the weaker one.
