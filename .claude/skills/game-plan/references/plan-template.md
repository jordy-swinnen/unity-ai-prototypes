# <Name>: plan and tasks

Plan for `docs/prototypes/<name>.md`. Status: draft | approved | stage N done | complete

## Decisions needed first
1. **<decision>.** Why it matters, the options, the recommendation. Say if it needs approval (a new package, a file format, a deletion).
2. **Unverified facts to check.** <fact>: how and when you will check it.

## Units and layout
Axes, units and which way is up on screen.

| Object | Size (x, y, z) | Position | Why |
|---|---|---|---|

## Tuning values
Grouped. These are the fields of the settings asset (`Assets/Prototypes/<Name>/Settings/`).

## Code design
Assembly `<Name>`, namespace `Prototypes.<Name>`.
- `<Name>Settings` (ScriptableObject): <...>
- `<Name>State`, `<Name>Simulation` (plain C#, no engine physics): <...>
- `<Name>Game`, `<Name>View` (MonoBehaviours): <...>
- `Editor/<Name>SceneBuilder`: the menu command `Prototypes/<Name>/Build Scene`.

## Art and audio pipeline
Scripts, outputs, how to run them. Look items that need outside art, if any.

## Tasks
Each stage ends with a stop for review.

### Stage 1: greybox
- [ ] Create the folders and assembly definitions from `AGENTS.md`.
- [ ] Write plain-shape assets (script) and check their size and pivot.
- [ ] Write the EditMode tests from the spec, then the rules. Run `Tools/unity/check.sh editmode`.
- [ ] Deliberate-bug check: break one rule, see tests fail, restore.
- [ ] Commit: `feat(<name>): add rules with EditMode tests`.

### Stage 2: playable
- [ ] Input, view, scene builder. Build the scene twice and compare content.
- [ ] PlayMode tests. Run `Tools/unity/check.sh all`.
- [ ] Capture the scene (`Tools/unity/capture.sh camera`) and check it against the layout table.
- [ ] Commit: `feat(<name>): add playable scene`.

### Stage 3: look (game-polish)
- [ ] Baseline capture.
- [ ] One task per feel-table row or visual pillar, each with a before and after capture.
- [ ] Settings for every strength and duration. Tests for the logic (decays, timers).
- [ ] Commit per step: `feat(<name>): <effect>`.

### Stage 4: play test and tidy
- [ ] The person plays and fills in the "needs a person" criteria.
- [ ] Turn findings into tasks or open questions.
- [ ] Update the spec and tick the criteria. Remove leftovers (with approval).

## Verification
| Criterion | How it is checked |
|---|---|

## Risks
- <risk>: <fallback>
