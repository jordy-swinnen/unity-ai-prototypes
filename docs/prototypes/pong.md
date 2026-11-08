# Pong

Status: draft, waiting for review. This is the first prototype. It also exists to settle the project structure for later prototypes (see Open questions).

## Goal
Two players hit a ball back and forth across a small 3D table seen from above. The first to 5 points wins. Everything you see (paddles, ball, table, walls) is a custom model made with a headless Blender script.

## Mechanics
- The play field is a flat table. The camera looks straight down at it. Gameplay moves on two axes only, and the models are 3D.
- Each player controls one paddle at the left or right end. A paddle moves along its end line only, and it can't leave the table.
- Player 1 uses W and S. Player 2 uses the Up and Down arrow keys. Input goes through the Input System package that the project already has.
- The ball starts at the centre and moves toward the player who did not score last. The first serve goes in a random direction.
- The ball bounces off the top and bottom walls. The angle of incidence equals the angle of reflection.
- The ball bounces off a paddle. The bounce angle depends on where it hits: centre of the paddle sends it straight back, and the edges send it out at up to 60 degrees from the straight line.
- The ball speed starts at a base value and rises by a fixed step on every paddle hit, up to a maximum. It resets to the base value after each point.
- A ball that passes an end line scores one point for the other player. Then the ball resets to the centre and the next serve starts after a short pause.
- At 5 points the game ends and shows the winner. A key press starts a new game.
- Speeds, angles, paddle size, win score and pause length live in one ScriptableObject, not in code.
- Game rules are plain C# with no Unity physics. The simulation steps with a fixed time step, so tests can run it deterministically. The scene only draws the current state.

## Art assets
- A Blender script makes `paddle`, `ball`, `table` and `wall`, and exports each as a `.glb` file.
- Models are in metres, with the origin at the centre of each object, and use simple flat-colour materials.
- The scene is built by an editor menu command from these assets. Nobody edits the `.unity` file by hand.

## Acceptance criteria
- [ ] The Blender script runs headless and writes all four `.glb` files without errors.
- [ ] A menu command builds the Pong scene from nothing, and running it twice gives the same scene.
- [ ] The ball reflects off the top and bottom walls with the correct angle.
- [ ] A hit at the centre of a paddle returns the ball straight back. A hit at the edge returns it at the maximum angle.
- [ ] Ball speed rises by the set step on each paddle hit and never goes above the maximum.
- [ ] A ball passing the left end line gives player 2 a point, and passing the right end line gives player 1 a point.
- [ ] After a point, the ball resets to the centre and speed returns to the base value.
- [ ] The game ends at 5 points and shows the winner, and a key press starts a new game.
- [ ] A paddle never moves outside the table.
- [ ] `unity recompile` reports no errors and `unity test` passes for EditMode and PlayMode.

## Tests
- EditMode: wall reflection angle, paddle bounce angle at centre and edge, speed step and maximum, scoring for each side, reset after a point, win at 5, paddle clamped to the table, and the same input always gives the same simulation result.
- PlayMode: the built scene contains a paddle for each player, a ball and four walls. After simulating a ball past an end line, the score on screen goes up.

## Out of scope
- A computer opponent, online play, sound, menus, and power-ups.
- Gamepad support.
- Visual polish beyond flat colours.

## Open questions
- Project structure for many prototypes. Proposal: one folder per prototype under `Assets/Prototypes/<Name>/`, with its own assembly definitions, tests, art exports and scenes, and shared code only in `Assets/Shared/`. Blender sources go in `Art/source/<Name>/` and scripts in `Tools/blender/<Name>/`.
- Should the placeholder `crate.glb` and `make_crate.py` be removed once Pong has real assets?
- Is a fixed top-down camera right, or should the camera tilt slightly for a more 3D look?
