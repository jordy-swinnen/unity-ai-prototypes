# Unity AI Prototypes

An AI-native starting point for building Unity games with coding agents. Fork it or use it as a template, then go from a game idea to a playable, tested, good-looking prototype with Claude Code or OpenCode. Models are made with headless Blender scripts, scenes are built by code, and every stage is checked by tests and renders before a person reviews it.

It is generic. The process and the tools do not depend on any game. Two example prototypes show what it produces, and each lives in its own folder under `Assets/Prototypes/` (yours will too):

| Prototype | What it is | Spec |
|---|---|---|
| **Neon Pong** | A two-player pong on a neon table: every hit flashes, shakes the screen and rings, and the rally speeds up. The first game made with the skills. | `docs/prototypes/neon-pong.md` |
| **Medieval Chess** | Chess against a computer with three difficulties, in a torch-lit hall. Every capture is an animated fight where the attacker kills the victim. Full rules, models from Blender scripts, 22 generated sounds. The second game, and the bigger test of the process. | `docs/prototypes/medieval-chess.md` and `medieval-chess-plan.md` |

To play one, open its scene (`Assets/Prototypes/<Name>/Scenes/`) in Unity and press Play. Delete the prototypes you don't want. Code used by both (camera shake and hit-stop) lives in `Assets/Shared/`.

## Showcase
Renders from the two example prototypes, made by following the four skills. Neon Pong is flat colour, emission and Bloom by design. Medieval Chess adds procedural textures, baked occlusion and a hand-written burn shader.

### Neon Pong
| The serve | A hit |
|---|---|
| ![Neon Pong at the serve: a cyan paddle on the left, a pink paddle on the right and a glowing ball in the middle of a dark grid table.](docs/images/neon-pong-serve.png) | ![Neon Pong at a hit: the ball leaves a trail and pink sparks fly from the right paddle, which lights the table.](docs/images/neon-pong-hit.png) |

The ball is the brightest thing on the table and lights its surroundings. Each hit adds a flash, sparks, screen shake and a short hit-stop.

### Medieval Chess
![Medieval Chess: a chessboard in a torch-lit hall, with limestone and slate squares, a wooden frame and gold-trimmed red and bone-white pieces.](docs/images/medieval-chess-board.png)

| The pieces | A capture |
|---|---|
| ![Close-up of the white king, queen and bishop: turned bodies, gold trim, crowns, shields and swords.](docs/images/medieval-chess-pieces.png) | ![A white rook has struck a red piece, which burns away into glowing embers.](docs/images/medieval-chess-burn.png) |

Every capture is a fight: the attacker strikes and the victim burns away. The pieces are built by Blender scripts, with baked occlusion in their vertex colours. The board, frame and floor use generated textures.

These are stills. The board and piece images are camera renders without Bloom or Vignette. How the games feel in motion is not something a picture shows, so play them: open a scene in `Assets/Prototypes/<Name>/Scenes/` and press Play.

## What you get
- **Four skills** in `.claude/skills/` (both Claude Code and OpenCode read that folder). Together they are the process from idea to polished game:

  | Skill | Use it to |
  |---|---|
  | `game-spec` | Turn an idea or a feature into a spec: design pillars, a researched look-and-feel brief, a feel table, mechanics, acceptance criteria and tests. |
  | `game-plan` | Turn an approved spec into a staged plan: decisions first, greybox, tests before code, a polish stage, a review stop after every stage. |
  | `game-build` | Build the plan one stage at a time with compile and test loops, render captures and a check that the tests can fail. |
  | `game-polish` | Apply the look brief and feel table to a working game in small steps, each judged on a render. |

- **A Unity CLI skill** (`unity-cli`, made by Unity) for driving the Editor from the command line.
- **Scripts** in `Tools/unity/`: `check.sh` compiles and runs EditMode and PlayMode tests, and `capture.sh` saves renders of the game so an agent can look at it.
- **Conventions** in `AGENTS.md`: one folder per prototype, tests beside the code, tuning values in settings assets, scenes and models built by scripts, never by hand.
- **Research** in `docs/research/`, including a sourced note on game feel and look development.

## Use it as a starting point
1. Fork the repository. GitHub's "Use this template" option isn't available: GitHub refuses to make a repository with Git LFS content a template. If you want a clean history, clone it, delete `.git`, run `git init`, and push to your own new repository.
2. Open the folder in Unity Hub with the pinned Unity version (see `AGENTS.md`). Install Blender and Git LFS (see below).
3. Edit `AGENTS.md`: the pinned versions, and your own rules. Edit this README. Change the license if you want a different one.
4. Ask your agent to use `game-spec` for your idea, then `game-plan`, `game-build` and `game-polish`. Each ends with a stop for your review. For example: "Use game-spec to write a spec for a small 3D arcade game where..."
5. Delete what you don't need, such as the two example prototypes under `Assets/Prototypes/` and their specs, plans and tools.

## What you need
- Unity **6000.6.8f1** through Unity Hub. This is not an LTS release. It's the version Unity Hub recommended when the project was created. You must be signed in to Unity Hub to add packages.
- Blender **5.2.3 LTS**, run headless to export models.
- Git and [Git LFS](https://git-lfs.com). Run `git lfs install` once on your machine.
- The Unity CLI (`unity`), Python 3, and Claude Code or OpenCode.

## Get started
```bash
git clone https://github.com/jordy-swinnen/unity-ai-prototypes.git
cd unity-ai-prototypes
git lfs install
git lfs pull
```
Open the folder in Unity Hub with 6000.6.8f1. The first open takes a while because Unity builds its `Library/` folder, which is not committed. Keep the Editor open while an agent works: the compile and test commands talk to the running Editor.

## Repository layout
```
Assets/Prototypes/<Name>/     one folder per game or prototype (PascalCase)
  Scripts/                    runtime code, assembly <Name>
  Editor/                     editor code and the "Build Scene" menu command
  Tests/EditMode/             logic tests
  Tests/PlayMode/             scene and behaviour tests
  Art/Exports/                .glb files written by Blender scripts
  Art/Textures/               .png textures written by a script
  Audio/                      sounds written by scripts
  Materials/                  materials written by the scene builder
  Shaders/                    hand-written URP shaders
  Scenes/                     scenes written by the build command
  Settings/                   ScriptableObjects with tuning values
Assets/Shared/                code and assets used by two or more prototypes
Assets/ThirdParty/            anything we did not make, kept apart (created when needed)
Assets/Sandbox/<user>/        personal experiments, ignored by git (created when needed)
Art/source/<Name>/            .blend source files (Git LFS)
Tools/blender/<Name>/         Blender Python scripts
Tools/audio/<Name>/           sound scripts
Tools/textures/<Name>/        texture scripts (run in Blender for numpy)
Tools/unity/                  check.sh and capture.sh
docs/prototypes/              one spec and one plan per game or feature (start from TEMPLATE.md)
docs/images/                  the renders shown in this README
docs/research/                background and sourced research notes
.claude/skills/               the skills
AGENTS.md                     rules for coding agents (source of truth)
```
Unity only compiles test assemblies inside `Assets/`, and only imports files from `Assets/`. That's why tests and exported models live there.

## Checks
With the Unity Editor open on the project:
```bash
Tools/unity/check.sh compile     # compile scripts
Tools/unity/check.sh all         # compile, then EditMode and PlayMode tests
Tools/unity/capture.sh play /tmp/shot.png     # enter Play Mode and capture the Game window (glow and UI included)
```
`check.sh` prints how many tests passed and exits non-zero on a compile error or a failing test. The raw `unity` commands behind it are in `AGENTS.md` and `.claude/skills/game-build/references/unity-loop.md`. With the Editor closed, `unity test` starts its own Editor in batch mode (not run in this repository yet).

### Tests on pull requests
`.github/workflows/tests.yml` runs the EditMode and PlayMode tests on every pull request, in Unity's official editor image through [GameCI](https://game.ci). It only runs when two things are set up in your own repository, because Unity needs a licence:
1. GitHub Actions is turned on (Settings > Actions).
2. Three repository secrets exist: `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD`. [GameCI explains how to get them](https://game.ci/docs/github/activation).

Without the licence secret the job fails with a message instead of passing without testing anything. Pull requests from forks get no secrets, so the tests are skipped there with a warning.

## Working with coding agents
- `AGENTS.md` holds all the rules. Claude Code reads it through `CLAUDE.md`, which contains only `@AGENTS.md`. OpenCode reads `AGENTS.md` directly. Don't put rules in `CLAUDE.md`.
- Both tools read skills from `.claude/skills/`. The `unity-cli` skill there is by Unity and is not covered by this repository's license.
- OpenCode models are set in `opencode.json`. Planning uses `opencode-go/glm-5.3`, and building uses `opencode-go/deepseek-v4.1-flash`. Claude Code models are set in Claude Code, not in this repo.
- Agents plan multi-file changes first and wait for approval. They stop for your review after every stage, and they say what they could not check. They can check numbers, tests and renders, but not how a game feels, so playing it is your job.

## License
MIT. See `LICENSE`. The files under `.claude/skills/unity-cli/` belong to Unity and are not under this license.
