# Neon Pong

Status: approved
Kind: new game

## One-page design
- **Pitch:** Two players hit a glowing ball across a dark neon table, and every hit lands harder than the last.
- **Feeling:** tense and punchy. A rally should make your pulse rise, and a point should feel like a small event.
- **Pillars:**
  1. **Every hit lands.** So we put the strongest feedback (flash, shake, hit-stop, sound) on contacts. We won't spend effects on things that aren't contacts.
  2. **Pressure builds.** So the rally state is visible and audible (speed shows in the trail, glow and pitch), and a miss resets it. We won't let the first hit and the tenth hit look the same.
  3. **Readable at a glance.** So only gameplay objects glow, and the background stays quiet. We won't add an effect that hides the ball or a paddle.
- **Core loop:** serve, rally while the ball speeds up, someone misses, a point lands, serve again, first to 5 wins, then restart.
- **Verbs:** move the paddle up and down, and hit the ball.
- **Players, session length, platform, 2D or 3D:** two players on one keyboard, about 3 to 5 minutes a match, desktop, 3D table seen from above at an angle.

## Look and feel brief
### References
- **Pong (1972):** take the rally pressure (speed climbs until a miss), bare short tones, and the idea that the hit position changes the angle. Don't copy its name branding or sounds.
- **Geometry Wars:** take simple geometric shapes, saturated neon on a dark base, a quiet background and bright bursts on events. Don't copy its grid, shapes or colour roles.
- **General neon-on-dark art direction:** take the small palette, layered glow, and glow reserved for what the player acts on. Heuristics only (secondary sources).

Details and sources: `docs/research/neon-pong.md`. All art and sounds here are original.

### Visual pillars
- **Shape language:** straight lines and circles only. Paddles are slim rounded bars, the ball is a sphere, the table is a flat slab with thin rails. Round means the ball, straight means everything the ball hits. (pillar 3)
- **Colour philosophy:** near-black blue base, three saturated neon colours with fixed roles, and neutral white for text. Colour marks sides and the ball, nothing else. (pillar 3)
- **Lighting:** almost no scene lighting. The glow comes from emissive materials and bloom, with a faint point light on the ball tinting the table, so the ball visibly lights its surroundings. (pillars 1, 2)
- **Materials:** table matte near-black with a faint grid; paddles and ball emissive; rails dimly emissive. The ball is the brightest object, then paddles, then rails, then the grid. (pillar 3)
- **Rendering and post-processing:** Bloom is the main effect, with a mild Vignette to focus the centre. Tonemapping Neutral, because ACES shifts hue and saturation and could dull the neon. Bloom strength has a global setting. (pillars 1, 3)
- **Camera:** perspective, pitched down about 62 degrees, narrow field of view (about 35), so the table has depth but left and right stay clear. Judged on a render; tilt is a setting. (pillar 3)
- **UI and text:** large scores in each player's colour above the table, a short win message in white. Text stays neutral or the player's colour at full brightness. (pillar 3)

### Palette
| Role | Colour (hex) | Used for |
|---|---|---|
| Base | `#05060D` | background and table surface |
| Grid and rails | `#12305A` | faint table grid, dim rails |
| Player 1 | `#22D3EE` | left paddle, left score, left impact bursts |
| Player 2 | `#FF2E93` | right paddle, right score, right impact bursts |
| Ball | `#FFE27A` | ball, trail, serve cue (the hottest colour) |
| Text | `#E6F0FF` | win message, any neutral text |

Sides are also told apart by position (left and right) and by score placement, so colour is not the only cue. Emission strengths and bloom values are starting points to tune on renders.

### Audio direction
Short synthesised tones, like the original, but cleaner: square or triangle waves with a fast attack and a short decay. Pitch climbs with rally speed. Sounds are made by a script, so they are original. No music in this version.

### Feel table
| Event or action | Visual | Audio | Camera | Time | Pillar |
|---|---|---|---|---|---|
| Serve countdown | ball pulses at centre, dim trail off | rising blip | none | none | 2 |
| Ball in flight | trail whose length and brightness grow with speed; point light on ball | none | none | none | 2, 3 |
| Wall hit | small spark in ball colour, rail flashes | short low blip | tiny shake | none | 1 |
| Paddle hit | paddle flashes bright, burst in the paddle's colour, ball briefly scales up | pitched blip, higher with speed | shake away from the hit, scaled by speed | hit-stop about 40 ms | 1, 2 |
| Speed step | trail and glow step up | pitch step | none | none | 2 |
| Paddle moving | slight lean or stretch along movement, soft glow trail | none | none | none | 3 |
| Point scored | big burst at the end line in the scorer's colour, rails flash, score number pops | descending tone and low thump | strong shake, short | hit-stop about 80 ms | 1 |
| Win | colour wash toward the winner's colour, message fades in | three-note rising phrase | slow push-in | brief slow motion | 1 |
| Restart | everything resets calmly, effects fade out | rising blip | ease back | none | 2 |

### Six-area feel review
- **Input:** W/S and Up/Down. Risk: paddle starts and stops too abruptly or too floaty. Starting speed is a setting, with a short ease at start and stop.
- **Response:** the hit-stop and shake must start on the same frame as the contact. Risk: effects that lag the hit by a frame or more.
- **Context:** a narrow table with walls makes the angles matter. Risk: a perspective camera hiding the far side of the table.
- **Polish:** the feel table above. Risk: too much glow smearing the ball at high speed.
- **Metaphor:** a glowing ball hitting glowing bars should sound and flash like hard contact. Risk: soft sounds on hard hits.
- **Rules:** speed climbs per hit and resets on a point, edges send the ball at sharper angles. Risk: rallies that end too fast at top speed. Top speed is a setting.

## Mechanics
- The table is a flat slab. Gameplay moves along x (the table's length) and z (its width).
- Each player moves one paddle along its end line. A paddle can't leave the table. Player 1 (left) uses W and S, player 2 (right) uses Up and Down. Input goes through the Input System package the project has.
- The ball starts at the centre, serves after a pause toward the player who just lost the point, and the very first serve goes in a random direction at a small random angle.
- The ball reflects off the top and bottom walls with equal angles.
- A paddle hit sends the ball back at an angle set by where it hit: centre straight back, edges up to the maximum angle.
- Ball speed rises by a step per paddle hit, up to a maximum, and resets to the base after a point.
- A ball past an end line scores for the other player. First to 5 wins. Space starts a new game after a win.
- The rules are plain C# with no engine physics, stepped at a fixed time step, so tests are repeatable. The scene only draws the state.

## Tuning values
All on one settings asset. Feel values are starting points for tuning on renders and in play.
- **Table:** half length 8, half width 5, ball radius 0.25, paddle half length 1, paddle half thickness 0.2, paddle x 7.5.
- **Rules:** base speed 9, speed step 0.8, max speed 22, max bounce angle 60 degrees, max serve angle 20 degrees, serve delay 1 s, paddle speed 10, win score 5.
- **Look:** emission intensity (ball, paddle, rails), bloom intensity and threshold, vignette intensity, trail length and width, point light intensity and range, camera pitch and field of view.
- **Feel:** shake strength (wall, paddle, score, win), shake duration and decay, hit-stop durations, burst size and count, pitch range, global effect strength (0 to 1).

## Art and audio assets
- **Models (Blender script, `.glb`):** table slab (with grid lines), rail, paddle, ball. Each under 1,500 triangles. Emission and colours are set on materials in Unity from the palette.
- **Effects (Unity):** trail on the ball, particle bursts, point light, one Volume profile.
- **Sounds (Python script, `.wav`):** wall, paddle (with pitch variants), serve, point, win, restart. Each under 1.5 s.

## Acceptance criteria
### Checked by tests or captures
- [ ] The Blender script runs headless and writes every model without errors, with the planned sizes and pivots.
- [ ] The Build Scene command builds the scene from nothing, and running it twice gives the same content.
- [ ] Walls reflect with equal angles. Paddle centre hits return straight, edge hits at the maximum angle.
- [ ] Speed rises by the step per paddle hit, never exceeds the maximum, and resets after a point.
- [ ] Each end line scores for the right player. A point resets the ball, and the next serve goes toward the player who lost it.
- [ ] The game ends at 5 points, shows the winner, and restarts.
- [ ] A paddle never leaves the table.
- [ ] A camera render shows glowing paddles, ball and rails with visible bloom on a dark table, and the ball is the brightest object.
- [ ] A render with the global effect strength at 0 shows no glow beyond the base look, and the shake offset is zero.
- [ ] Shake decays to zero, hit-stop ends, and effects trigger on the right events (tests).
- [ ] Every feel-table sound exists, is not silent, doesn't clip, and ends near zero.
- [ ] `Tools/unity/check.sh all` passes.
### Needs a person
- [ ] The look matches the brief: it reads as neon arcade, not plain blocks.
- [ ] Paddle hits feel punchy, and the shake and hit-stop feel good and not nauseating.
- [ ] Tension visibly and audibly builds across a rally.
- [ ] The ball and paddles stay easy to see at top speed.
- [ ] The keyboard keys work as described, and Space restarts after a win.
- [ ] The sounds fit the mood and the volume is balanced.

## Tests
- **EditMode (rules):** wall reflection, paddle bounce at centre and edge, speed step and cap, scoring both sides, reset, serve direction, win, restart, paddle limits, repeatability.
- **EditMode (feel logic):** shake decays to zero, shake respects the global strength, hit-stop timer ends, effect strength 0 disables feedback.
- **EditMode (builder):** the scene has every part and the models sit where the rules expect them, building twice gives the same content, the Volume has Bloom with intensity above 0 and the camera has post-processing on.
- **PlayMode:** paddles and ball follow the state, input moves the paddles, a point updates the score on screen, a win shows the winner, feedback triggers on a hit.

## Out of scope
- A computer opponent, power-ups, ball spin, menus, gamepad, online play and music.
- Settings screens. Strengths are changed on the settings asset.

## Open questions
- Camera tilt: about 62 degrees is a guess. Judge on renders, and change it in the settings if the far side is hard to see.
- Whether bloom needs HDR rendering turned on in the URP Asset or camera. Checked on a render.
- Emission intensities and bloom values. They are starting numbers, tuned on renders.
- Whether the point light on the ball is worth its cost. Keep it only if the render shows it.
- How good simple script-made sounds are. The person decides after listening.

## Sources
See `docs/research/neon-pong.md` and `docs/research/game-feel-and-look-development.md`. Pong's segmented paddle and speed rule: Wikipedia [read]. Geometry Wars look: Game Developer interview [read] and review summaries [secondary]. Neon-on-dark heuristics [secondary]. URP HDR, Emission and Bloom: Unity 6.3 docs [read].
