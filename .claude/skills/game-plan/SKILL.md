---
name: game-plan
description: Turns an approved game or feature spec into a staged plan and task list with decisions first, a greybox vertical slice, tests before code, a polish stage, and a review stop after every stage. Use it after game-spec and before game-build, whenever a spec is approved and nothing is built yet.
---

# game-plan

Turn an approved spec into a plan that someone can approve before any code exists. The plan is how the work is split, ordered and checked. It does not change what the spec promises.

Output: `docs/prototypes/<kebab-name>-plan.md`, written from `references/plan-template.md`.

## Before you start
- The spec's status must be `approved`. If it isn't, run `game-spec` or ask the user to approve it.
- Read `AGENTS.md` for the folder structure and rules. Read the spec's pillars, look-and-feel brief, feel table and acceptance criteria.
- Read `references/example-pong-v1-plan.md` once. Note that it has no polish stage, which is why the game it built looked plain.

## Steps
1. **Decisions needed first.** List everything that blocks the work or needs approval before code is written: packages to add (adding one needs approval), file formats (for example Unity 6 has no built-in `.glb` importer), facts you could not verify, and anything in the spec that conflicts with the repository's rules. Check unverified facts against the tool's own help or docs now, not halfway through.
2. **Units and layout.** A table of every object with its size, position and the reason. State the axes, units and which way is up on screen.
3. **Tuning values.** The starting values from the spec, grouped, as fields of the settings asset.
4. **Code design.** The small set of types: settings, state, rules (plain C# with no engine physics where possible, so tests run them step by step), input, view, scene builder. Say what each owns.
5. **Art and audio pipeline.** Which scripts make which assets, where they write, and how to run them. Feasibility for each look item from the spec.
6. **Stages.** Use this order. After each stage the work stops for review.
   - **Stage 1, greybox:** folders, assembly definitions, plain-shape assets, the rules with tests written first.
   - **Stage 2, playable:** input, view, a scene built by an editor command, PlayMode tests. Plain shapes are fine.
   - **Stage 3, look:** run `game-polish` on the look brief and feel table, in small steps, each judged on a render.
   - **Stage 4, play test and tidy:** the person plays, the "needs a person" criteria are ticked or turned into new tasks, leftovers are removed.
7. **Tasks.** A checkbox list per stage, each task small enough to finish and check in one go. Each stage ends with a commit task.
8. **Verification.** For each acceptance criterion, name the test, the capture or the person who checks it.
9. **Risks.** What could fail and the fallback.
10. **Review stop.** Show the decisions list first. Don't build until the plan is approved.

## Adding a feature to an existing game
Write a short plan: decisions, the files touched, the tests to add first, the polish rows from the feel table, and a stop for review. Reuse the existing settings asset, scene builder and test assemblies instead of creating new ones. Make sure the first task is to run the existing tests, so a regression is visible.

## Rules
- Tests come before the code they test.
- Every stage ends in something that compiles and passes its tests, so it can be committed.
- Don't schedule polish before the rules are tested and the game is playable.
- Keep the plan honest: when reality differs, update the plan and say why.
