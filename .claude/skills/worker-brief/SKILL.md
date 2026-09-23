---
name: worker-brief
description: Write the brief for an issue-worker or docs-writer - the fixed protocol part (worktree, absolute paths, Edit tool only, no push/PR/issues, stay-out paths, changelog fragment, verification, self-review, delegation, report with issue files, model choice) plus the task-specific goal, scope, decisions and acceptance. Use every time the orchestrator hands an issue to a worker.
---

# Worker brief

The orchestrator writes only the variable part; the fixed part below is copied as is, with the `<...>` slots filled. Keep the brief closed: every design decision the worker needs is made here, or the worker stops and reports.

## 1. Before writing

1. Create the worktree: `git worktree add D:\Proyectos\Encina\.claude\worktrees\<name> -b <branch> <base>`.
2. Choose the model. Workers run on Sonnet. Pass `model: opus` to the Agent call only when the brief states why: the root cause is unknown, or the task is design-heavy. Write the reason into the brief.
3. List what the worker must not touch: the shared hot spots of the moment (always `.github/workflows/*`; anything another open PR edits).

## 2. Fixed part (copy it)

```text
Issue #<n> (read it: gh issue view <n> --repo dlrivada/Encina). Worktree <absolute path>, branch <branch>, base <base>.

Rules:
- Use ABSOLUTE paths for every read and write, inside the worktree only. Your shell starts in the main
  checkout; use git -C <worktree> for git. The block-main-checkout-writes hook denies writes aimed at the
  main checkout.
- Edit repo files (.cs, .csproj, .props, .json, .yml, .md, ...) only with the Edit/Write tools; never
  PowerShell -replace, Set-Content, Out-File or [IO.File] writes on them.
- Commit locally, English messages, no AI attribution. Never push, open or edit PRs, open or comment on issues.
- Stay out of: .github/workflows/*<, other paths>.
- Changelog: no edits to CHANGELOG.md. A user-visible change adds changelog.d/<n>-<slug>.<section>.md
  (rules in changelog.d/README.md) and passes: dotnet run .github/scripts/changelog-fragments.cs -- --check
- Verify: <commands from §3 for this kind of change>. Paste the actual output in the report.
- Self-review: if you touched production code, spawn adversarial-reviewer on your diff
  (git -C <worktree> diff origin/main...HEAD) with this brief's acceptance criteria; fix blockers and majors;
  list the rest.
- Delegation is mandatory: every step that belongs to a specialist goes to it (ci-diagnoser for a failure not
  obvious from the first errors, mechanical-fixer for decided mechanical edits, the local model through the
  local-ai-task skill for bulk drafts). Never do a specialist's step yourself.
- When the task changes nature (scope grows, the cause is elsewhere, a decision this brief does not make,
  tests you cannot make pass), stop and report with evidence.
- Report: files changed; verification commands and output; self-review result; delegation (what went to whom);
  follow-ups as issue files in <worktree>/artifacts/issues/<slug>.md (template headers verbatim, header block
  per .claude/agents/issue-worker.md), listed by path; token usage of every nested spawn and each line of
  artifacts/local-ai/ledger.csv, verbatim.
```

## 3. Verification by kind of change

| Change | Commands |
|---|---|
| C# in `src/` | `dotnet build Encina.slnx -c Release` (0 warnings); the affected test projects with `--filter`; `dotnet format Encina.slnx --verify-no-changes --include <files>` |
| Public API | the above, plus `PublicAPI.Unshipped.txt` updated (RS0016/RS0017 clean) |
| Tests only | the changed test project with `--filter` on the new classes, run twice for determinism |
| `.github/scripts/*.cs` | `dotnet run <script> -- <the mode the change touches>` against a real input |
| Hooks in `.claude/hooks` | `pwsh -NoProfile -File .claude/hooks/tests/Test-Hooks.ps1`, plus an AST parse of each changed hook |
| Documentation | the `encina-docs` skill, §5 (lychee, markdownlint, citation check) |

## 4. Variable part (write it)

```text
Goal: <one sentence: what is true when this is done>.
Scope: <files, packages, providers in and out>.
Decisions: <every design choice already made, with its reason; say which ones the worker may not revisit>.
Acceptance: <numbered, checkable criteria; the tests or commands that prove each one>.
```

Short is better: the fixed part carries the protocol, and the agent definition carries the rest.
