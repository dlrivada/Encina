---
name: worker-brief
description: Write the brief for an issue-worker or docs-writer - the fixed protocol part (worktree, absolute paths, Edit tool only, no push/PR/issues, stay-out paths, changelog fragment, verification, self-review, delegation, report with issue files, model choice) plus the task-specific goal, scope, decisions and acceptance. Use every time the orchestrator hands an issue to a worker.
---

# Worker brief

The orchestrator writes only the variable part; the fixed part below is copied as is, with the `<...>` slots filled. Keep the brief closed: every design decision the worker needs is made here, or the worker stops and reports.

The orchestrator itself does not edit `src/` or `tests/` (the `guard-orchestrator-writes` hook in `.claude/settings.json` denies the edits it can see, in the main checkout and in every worktree): any change there goes through a brief, and so does resolving a rebase conflict there (the worker resolves the conflicts in the worktree, continues the rebase, verifies and reports).

## 1. Before writing

1. Create the worktree: `git worktree add D:\Proyectos\Encina\.claude\worktrees\<name> -b <branch> <base>`.
2. Choose the agent: `docs-writer` when the issue is documentation (pages under `docs/`, package READMEs, `CONTRIBUTING.md`); `issue-worker` for everything else, including code changes that also need documentation (the issue-worker spawns `docs-writer` for that part).
3. Choose the model. Workers run on Sonnet. Pass `model: opus` to the Agent call only when the brief states why: the root cause is unknown, or the task is design-heavy. Write the reason into the brief.
4. List what the worker must not touch: the shared hot spots of the moment (always `.github/workflows/*`; anything another open PR edits).

## 2. Fixed part (copy it)

`<wt>` is the worktree's absolute path. Every command in the brief names it: the hooks and several scripts resolve relative paths against the current directory, and the worker's shell starts in the main checkout.

```text
Issue #<n> (read it: gh issue view <n> --repo dlrivada/Encina). Worktree <wt>, branch <branch>, base <base>.

Rules:
- Use ABSOLUTE paths for every read and write, inside the worktree only. Your shell starts in the main
  checkout: prefix every command whose output or inputs are cwd-relative with "Set-Location <wt>;", and use
  git -C <wt> for git. The block-main-checkout-writes hook denies writes it resolves to the main checkout.
- Edit repo files (.cs, .csproj, .props, .json, .yml, .md, .txt, .sql, .ps1, .xml, ...) only with the
  Edit/Write tools; never PowerShell -replace, Set-Content, Out-File, Tee-Object or [IO.File] writes on them.
- Commit locally, English messages, no AI attribution. Never push, open or edit PRs, open or comment on issues.
- Stay out of: .github/workflows/*<, other paths>.
- Changelog: no edits to CHANGELOG.md. A user-visible change adds changelog.d/<n>-<slug>.<section>.md
  (rules in changelog.d/README.md; an issue-worker has mechanical-fixer write it) and passes:
  Set-Location <wt>; dotnet run --file <wt>/.github/scripts/changelog-fragments.cs -- --check
- Verify: <commands from §3 for this kind of change, with <wt> filled in>. Paste the actual output in the report.
- Self-review before reporting: an issue-worker whose diff touches production code (src/, .github/scripts/,
  .claude/hooks/) spawns adversarial-reviewer on git -C <wt> diff origin/main...HEAD with this brief's
  acceptance criteria; a docs-writer spawns docs-reviewer on its pages. Fix blockers and majors; list the rest.
- Delegation is mandatory: every step that belongs to a specialist goes to it (the table in your agent
  definition). Never do a specialist's step yourself. Hooks enforce part of it: path ownership on Write/Edit,
  the spawn allowlist, and a check at stop time that the specialists your diff requires were spawned.
- When the task changes nature (scope grows, the cause is elsewhere, a decision this brief does not make,
  tests you cannot make pass), stop and report with evidence.
- Report: files changed; verification commands and output; self-review result; delegation (what went to whom);
  follow-ups as issue files in <wt>/artifacts/issues/<slug>.md (template headers verbatim, header block per
  your agent definition, first draft by the local model), listed by path; token usage of every nested spawn
  and each line of <wt>/artifacts/local-ai/ledger.csv, verbatim.
```

### What applies to a docs-writer

The fixed part is the same for both agents; for a `docs-writer` read it with these differences, and state them in the variable part only when the task deviates:

| Rule | issue-worker | docs-writer |
|---|---|---|
| Owns | everything except documentation, changelog fragments, PublicAPI files and coverage manifests (the site's code and data under `docs/` included) | `docs/**/*.md` (not `docs/plans/**`) and the images those pages show, `README.md` and `CONTRIBUTING.md` anywhere, its own changelog fragment, its `artifacts/` issue files |
| Must not edit | documentation (→ `docs-writer`), changelog/PublicAPI/manifests (→ `mechanical-fixer`) | anything outside its allowlist: `src/`, `tests/`, `.claude/`, build files, the site's code and data under `docs/` (→ `mechanical-fixer` if decided, else report) |
| May spawn | `ci-diagnoser`, `mechanical-fixer`, `Explore`, `adversarial-reviewer`, `docs-writer` | `mechanical-fixer`, `docs-reviewer`, `Explore` |
| Self-review | `adversarial-reviewer` when production code changed | `docs-reviewer` when documentation changed |
| Verification (§3) | the row for the kind of change | the Documentation row |
| Rules of the craft | the brief and `CLAUDE.md` | the `encina-docs` skill, read in full first |

## 3. Verification by kind of change

Fill `<wt>` in every command. Each one either starts with `Set-Location <wt>;` or names the worktree absolutely.

| Change | Commands |
|---|---|
| C# in `src/` | `Set-Location <wt>; dotnet build <wt>\Encina.slnx -c Release` (0 warnings); the affected test projects with `dotnet test <wt>\tests\<Project>\<Project>.csproj --filter <...> --results-directory <wt>\artifacts\test-results`; `Set-Location <wt>; dotnet format <wt>\Encina.slnx --verify-no-changes --include <files>` |
| Public API | the above, plus `PublicAPI.Unshipped.txt` updated by `mechanical-fixer` (RS0016/RS0017 clean) |
| Tests only | `dotnet test <wt>\tests\<Project>\<Project>.csproj --filter <new classes> --results-directory <wt>\artifacts\test-results`, run twice for determinism |
| `.github/scripts/*.cs` | `Set-Location <wt>; dotnet run --file <wt>\.github\scripts\<script>.cs -- <the mode the change touches>` against a real input |
| Hooks in `.claude/hooks` | `pwsh -NoProfile -File <wt>\.claude\hooks\tests\Test-Hooks.ps1`, plus an AST parse of each changed hook: `[System.Management.Automation.Language.Parser]::ParseFile('<wt>\.claude\hooks\<hook>.ps1', [ref]$null, [ref]$errors)` with `$errors` empty |
| Changelog fragment | `Set-Location <wt>; dotnet run --file <wt>\.github\scripts\changelog-fragments.cs -- --check` |
| Local-model draft | `Set-Location <wt>; dotnet run --file <wt>\tools\ai\local-ai-ask.cs -- --task <name> --brief <wt>\artifacts\local-ai\briefs\<name>.md --input <file> --out <wt>\artifacts\local-ai\out\<name>.md` (the script appends to `artifacts/local-ai/ledger.csv` under the current directory, hence the `Set-Location`) |
| Documentation | the `encina-docs` skill, §5 (lychee, markdownlint, citation check), each run from `Set-Location <wt>;` |

## 4. Variable part (write it)

```text
Goal: <one sentence: what is true when this is done>.
Scope: <files, packages, providers in and out>.
Decisions: <every design choice already made, with its reason; say which ones the worker may not revisit>.
Acceptance: <numbered, checkable criteria; the tests or commands that prove each one>.
```

Short is better: the fixed part carries the protocol, and the agent definition carries the rest.
