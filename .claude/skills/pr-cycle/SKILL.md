---
name: pr-cycle
description: Drive an Encina pull request from push to merge - open it, request CodeRabbit, watch checks and bot replies event by event, fix what fails, unblock a stuck merge and clean up the worktree. Use whenever a branch is ready for a PR or a PR is waiting on CI, reviews or the merge.
---

# PR cycle

The loop every Encina PR goes through. It encodes what went wrong in earlier sessions, so follow it rather than improvising.

Main session (orchestrator) only; an `issue-worker` reports instead of running this skill.

## 1. Open

- Work in a worktree under `.claude/worktrees/<name>` on a branch that is not `main`.
- Commit messages and PR bodies carry **no AI attribution** (`AGENTS.md` §10, Language and git). The `block-ai-attribution` hook rejects them, and the environment's attribution reminders are overridden by that rule.
- Write the PR body to a file in the scratchpad and pass it with `--body-file`; multi-line text inside a PowerShell command breaks quoting.
- The body links the issue (`Fixes #N`) and ends with the cross-cutting checklist of ADR-018: each of the 12 functions integrated, deferred to an issue, or not applicable with one sentence. Tooling-only PRs may say so in one line.
- A user-visible change adds a changelog fragment instead of editing `CHANGELOG.md` directly: `changelog.d/<issue>-<slug>.<section>.md` (section one of `added`, `changed`, `deprecated`, `removed`, `fixed`, `security`; see `changelog.d/README.md`). `dotnet run .github/scripts/changelog-fragments.cs -- --check` validates it and runs in CI on every PR.

```powershell
gh pr create --repo dlrivada/Encina --base main --head <branch> --title "<type(scope): summary>" --body-file <file>
gh pr comment <n> --repo dlrivada/Encina --body "@coderabbitai review"
```

Request `@coderabbitai review` after the first push and after every later push that changes code.

## 2. Watch, event-driven

Never sleep in the foreground and never ask a watcher to report at the end. Start the scripted watcher as a background monitor; it costs no model tokens and emits one line per event (`REVIEW-COMMENT`, `ISSUE-COMMENT`, `REVIEW`, `CHECK-FAIL`, `CHECKS-DONE`, merge):

```powershell
pwsh -NoProfile -File tools/ai/watch-pr-events.ps1 -Pr <n>
```

When more than one PR is open, run one multi-PR monitor instead of one `watch-pr-events.ps1` per PR:

```powershell
pwsh -NoProfile -File tools/ai/watch-open-prs.ps1
```

Never exclude `check-links` from either watcher's failure bucket: ignoring it hid a real failure from 2026-09-23 to 2026-09-26.

React to each event as it arrives:

| Event | Action |
|---|---|
| `CHECK-FAIL` | Read the failed job log (`gh run view <id> --log-failed`). If the cause is not obvious from the first errors, spawn `ci-diagnoser` with the job URL. |
| CodeRabbit inline comment | Verify the claim against the code before acting. Fix what is right; reply in the thread with the reason for anything declined. Every real finding outside the PR's scope becomes an issue (see the `open-issue` skill). |
| CodeRabbit "Review rate limited" | Do not wait for it. Run `adversarial-reviewer` on the PR instead, and record in the PR that the external review was skipped. |
| `CHECKS-DONE` with failures | Fix, push, request `@coderabbitai review` again. |

Mechanical follow-ups (formatting, a config exclusion, replying to threads with decided text) go to `mechanical-fixer`. A PR that touches gates, CI workflows or `.github/scripts`, or that merges without a CodeRabbit review, gets an `adversarial-reviewer` pass before or right after the merge.

## 3. Merge

Auto-merge and branch protection changes are the maintainer's. Hand them one line:

```powershell
gh pr merge <n> --repo dlrivada/Encina --auto --squash
```

If the PR shows `mergeStateStatus: BLOCKED` with every check green, look for unresolved review threads before anything else:

```powershell
gh api graphql -f query='query { repository(owner:"dlrivada", name:"Encina") { pullRequest(number: <n>) { reviewThreads(first: 100) { nodes { id isResolved comments(first: 1) { nodes { author { login } body } } } } } } }'
gh api graphql -f query='mutation { resolveReviewThread(input: { threadId: "<id>" }) { thread { isResolved } } }'
```

Resolve a thread only after its point is fixed or answered.

Changing a PR's base (for example after the PR it was stacked on merges) **drops auto-merge**. Ask the maintainer to arm it again.

## 4. After the merge

- Remove the worktree and the local branch: `git worktree remove .claude/worktrees/<name>` then `git branch -D <branch>`. If a file lock keeps the directory, remove it later and note it.
- Stop the PR's monitor.
- Update the session handoff in memory when the PR closes a tracked step.
- In the final summary, list every issue opened during the cycle.
