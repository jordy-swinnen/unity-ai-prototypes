---
name: game-spec
description: Turns a game idea, or a feature for an existing game, into a reviewable spec with design pillars, a researched look-and-feel brief, a feel table, mechanics, acceptance criteria and tests. Use it first, before planning or building any game, prototype or feature in this repository, and whenever someone says a game looks basic or feels flat.
---

# game-spec

Write the spec for a game or a feature. The spec is the contract for `game-plan`, `game-build` and `game-polish`, so it has to say what the game looks, sounds and feels like, and not only what the rules are. A spec with rules and no look and feel produces a game made of plain blocks.

Output: `docs/prototypes/<kebab-name>.md`, written from `docs/prototypes/TEMPLATE.md`. Research notes go in `docs/research/<kebab-name>.md`.

## Before you start
- Read `AGENTS.md` for the layout and rules, and `docs/prototypes/` for specs that already exist.
- Read `references/look-and-feel-guide.md`. It explains each part of the brief and what to research.
- Read `references/example-pong-v1-spec.md` once. It is a good example of mechanics, acceptance criteria and tests, and a bad example of everything visual.
- Everything in this repository's art must be original. References are for the feeling, never for copying names, characters, sprites, models or sounds.

## Steps
1. **Interview.** Ask in short rounds of at most four questions, with concrete options and your recommendation first. Cover the pitch, the feeling, who plays and for how long, 2D or 3D, the games they like and dislike, and how big the first version should be. Don't ask what you can research. Say what you assumed.
2. **Research.** Search the web for comparable games and for how they get their look and feel: what the player sees and hears when the main verb happens. Look at three references. Record sources in `docs/research/<name>.md` and tag every claim `[read]` (you read the page), `[secondary]` (a blog or summary) or `[unverified]`. Never present a guess as a fact.
3. **Pillars.** Write 2 to 4. Each one is a statement plus "so we ..." and "so we won't ...". If a later idea serves no pillar, cut it.
4. **Look directions.** Offer two or three distinct directions, each with a palette, shape language, material feel, lighting and post-processing mood, camera and audio mood. Say which you recommend and why. The user picks or mixes. Check that each direction is feasible with this repository's tools (primitive shapes and Blender scripts, URP materials and post-processing, particles, generated sounds). If it needs art that can't be made here, say so.
5. **Look-and-feel brief.** Fill the brief in the template for the chosen direction: references, visual pillars, palette with hex values, camera, UI, audio direction. Tie every line to a pillar. Fill in the **Surfaces** table for every material (flat, texture, baked occlusion, shader effect, dissolve on removal) by following `.claude/skills/game-polish/references/surfaces.md`. You decide this. Do not ask the person whether to add textures or shaders, and do not leave it out: a game with no surface decision ships as plain blocks. Ask only for the cases listed under "When to ask" in that file.
6. **Feel table.** One row for every player action and game event: visual, audio, camera, and time feedback (hit-stop, slow motion). Then a one-line review in each of Swink's six areas: input, response, context, polish, metaphor, rules.
7. **Mechanics and tuning.** Rules in plain sentences with starting numbers. Every number becomes a field on a settings asset.
8. **Acceptance criteria.** Split them. *Checked by tests or captures* must be observable and testable. *Needs a person* says what to look at and what "good" means. Be honest about which feel claims no tool can check.
9. **Tests, out of scope, open questions, sources.**
10. **Review stop.** Show the user the pillars, the chosen look and the feel table first, since those are the parts people disagree on. Don't plan or build until the status is `approved`.

## Adding a feature to an existing game
Don't write a whole new spec. Write a short delta spec in the same template: pitch and pillar it serves, what changes in the look and feel, new or changed mechanics and numbers, new feel-table rows, acceptance criteria and tests. Name which existing pillar it must not break. When the feature is built, update the original spec so it still describes the game.

## Quality bar
- Every material has a surface decision with a reason, and every removal or spawn has a plan for how it reads.
- Every look and feel line names the pillar it serves.
- The palette has hex values and roles.
- The feel table has no row with only a visual.
- No tuning number sits only in prose. It is in the tuning list.
- Criteria that need a person are marked as such.
- Sources have confidence tags. Claims from memory are marked `[unverified]`.
