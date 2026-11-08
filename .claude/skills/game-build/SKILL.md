---
name: game-build
description: Builds an approved game plan stage by stage in this repository's Unity project, with tests first, compile and test loops through the Unity CLI, render captures, a deliberate-bug check that the tests can fail, and a commit and review stop after every stage. Use it after game-plan, whenever a plan is approved and the next stage is ready to build.
---

# game-build

Build one stage of an approved plan at a time. Every stage ends compiled, tested, checked on a render, committed and shown to the user.

## Before you start
- Read the plan and spec. The plan's status must be `approved` or `stage N done`.
- Read `AGENTS.md` (layout, rules, checks) and `references/unity-loop.md` (the commands). Read `references/pitfalls.md`. Each pitfall cost real time once.
- The Unity Editor must be open on this project. `unity status` should show `ready`. If it isn't open, ask the user to open it.
- Ask before adding packages, changing `ProjectSettings/`, or deleting files, as `AGENTS.md` says.

## The loop for a stage
1. State the stage's tasks and what "done" means.
2. **Tests first.** Write the tests for the stage from the spec's acceptance criteria. They should fail or not compile yet.
3. **Write the code** to make them pass. Keep rules as plain C# with no engine physics so they can be tested step by step. Put numbers in the settings asset.
4. **Compile and test:** `Tools/unity/check.sh editmode` (or `playmode`, or `all`). Fix errors and re-run. Don't continue on a red result.
5. **Deliberate-bug check.** Break one rule on purpose (flip a sign, swap a side), see the matching test fail, and restore it. This shows the tests can fail. Do it once per stage and report it.
6. **Look at it.** For anything visible, capture a render with `Tools/unity/capture.sh` and read the image. Compare it to the spec's layout table and look brief. Fix what you see before you report.
7. **Scenes, models and other generated files** come from scripts and editor commands, never by hand. Don't edit `.unity` or `.prefab` files.
8. **Commit** with a conventional message, staging files explicitly. Include `.meta` files that Unity generated. Check `git status` first.
9. **Record** in the plan: tick the tasks, note anything that differed and why.
10. **Stop for review.** Report what was built, how it was verified, what could not be verified, and what a person should check by hand. Wait before the next stage.

## Honest reporting
- Say which checks ran and what they showed. Say which checks you could not run.
- You can check numbers, tests and renders. You can't judge how a game feels. Say so and hand that to the person.
- If a command fails, report the full output and the cause. Don't change the test or the command to hide a failure.

## Adding a feature to an existing game
Run the existing tests first and note the result. Then follow the same loop with the feature's tests. Reuse the settings asset, scene builder and test assemblies. Re-run `Tools/unity/check.sh all` at the end and compare with the starting result. Update the original spec and plan.

## Don't
- Push, create remotes, or change GitHub settings without asking.
- Leave test files, captures or scratch scripts in the repository. Write captures outside it (a temp folder).
- Batch several stages together.
