---
name: agent-handoff
description: Prepare a repository for handing work to a different agent harness. Verifies the checks, commits everything to a local branch without rewriting pushed history, confirms the worktree is reachable, records the environment and open risks, writes a complete handover document and prompt, and recommends the next model and subscription against the user's budget using BenchLM. Use when the user says hand off, handover, switch agent or harness (for example Claude Code to OpenCode), stop and commit for another agent, make the worktree available, or asks which model or plan to use next.
license: MIT
compatibility: Needs git and a POSIX shell. Model advice uses the BenchLM MCP server (benchlm_recommend, benchlm_compare_models) or its REST API at https://data.benchlm.ai/v1, plus the provider's own pricing page.
metadata:
  author: unity-ai-prototypes
  version: "1.0"
allowed-tools: Bash(git:*) Read Write Edit
---

# agent-handoff

Hand a repository to a different agent harness so the next agent can continue without re-asking decided questions or rediscovering the environment. The handover is done only when the work is committed, the worktree is reachable, every check result is recorded, and the reply ends with a prompt that can be pasted into the next session.

## 1. Before you stop

- Finish the current coherent unit of work. Do not stop mid-edit; if a command is still running, let it finish or say so.
- Run the repository's own checks and keep the exact output:
  - Unity repositories here: `Tools/unity/check.sh all` (compile, EditMode, PlayMode). If one mode cannot run, say which and why.
  - Otherwise use the build/test command named in the repo rules.
- Run `scripts/handoff_check.sh` from this skill to print the branch, status, recent commits and worktrees.
- Restore files that test runs dirty but nobody asked to change (`git status`, then `git checkout -- <file>`), exactly as the repo rules say.

## 2. Commit everything to the local branch

- Stage the current work explicitly; include every Unity `.meta` file Unity generated.
- Never rewrite history that is already pushed. If the branch is pushed, add a new commit.
- Conventional commit subject, no trailers unless the repo asks for them.
- Do not push. Do not open a pull request. Both need the user's explicit yes; say what would become public first.
- Confirm afterwards: `git status` clean, `git log --oneline -3` shows the commit.

## 3. Make the worktree reachable

- Record: repository path, branch name, HEAD hash, and `git worktree list`.
- If the work lives in a linked worktree, say its exact path and that it must not be removed until the next agent is done.
- List untracked-but-intended files if any are excluded from git, and any large files that need Git LFS.

## 4. Write the handover document

Create or update the handover file the repo already uses (for example `docs/prototypes/<name>-handoff.md`), or a dated file such as `docs/handoffs/<date>-<topic>.md`. Use `references/handover-prompt-template.md`. It must contain:

1. **Branch, HEAD, worktree path** and whether anything is pushed.
2. **What is done**, with the checks that prove it.
3. **What is next**, in order, with the exact commands.
4. **Environment notes**: tools, versions, env vars, how to run each pipeline, what must not be touched.
5. **Decisions already made** that must not be re-asked.
6. **What nobody has checked** (feel, looks judged only by a person, hardware).
7. **Open risks and unverified facts**, each with its fallback.

## 5. Recommend the next model against the budget

Read `references/model-budget.md`. In short:

1. Ask BenchLM: `benchlm_recommend` with `surface: "coding"`, `tags: ["image-input"]` when the work includes judging renders or PNGs, and `maxInputPrice`/`maxOutputPrice` when the user names a cap. Use `benchlm_compare_models` to put the shortlist beside the current model.
2. Translate scores into the user's plan: a subscription allowance is a dollar budget, not a request count. Compute per-request cost from the provider's published token estimates and divide the allowance by it.
3. Name one model for the main loop and one escape hatch for hard debugging. Say which surface each recommendation uses (coding, agentic, multimodal) and its interval.
4. Say what the BenchLM numbers do not prove: benchmark-shaped work is not the user's task, and intervals can overlap.

## 6. End the reply with the handover prompt

The last block of the reply is a self-contained prompt for the next agent, written to be pasted as the first message of a new session. Include: the repo path and branch, what to read first (in order), the task and stop conditions, the rules that matter most, and the current model and session settings. Do not put the prompt behind a "you could" — state it as the next agent's task.

## Honesty

- Say which checks ran and what they showed, and which could not run.
- You may verify numbers, tests and renders; you cannot judge feel. Leave play-test and listening judgements to the person.
- If a model cannot read images, say so; never claim to have judged a render you did not see.

## Don't

- Do not push, open a PR, delete the worktree, or change shared config without asking.
- Do not batch stages; commit the coherent unit that is actually finished.
- Do not leave test scenes, captures or scratch files in the repository; captures go outside it.
