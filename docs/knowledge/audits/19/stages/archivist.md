## Scope

The issue touched one file: `.github/workflows/dotnet-ci.yml` (threshold 45% to 85%). That file no longer exists. Commit 59a5a867 ("fix(ci): consolidate CI workflows") replaced it with `.github/workflows/ci.yml` and `ci-full.yml`. A search of `ci.yml` and `ci-full.yml` finds no 45 or 85 coverage threshold; `ci-full.yml` runs `coverage-report.cs` and fails only when it produces no index or every entry is `noData`; `ci.yml` has the blocking `crap-gate` job (AGENTS.md section 9).

Current homes of the replacement model, all verified present in the worktree:
- `.github/coverage-manifest/*.json` (`defaults.json` plus one per package; e.g. `Encina.json` has `targets` contract 15, guard 20, unit 70).
- `.github/scripts/coverage-report.cs` (last touched by 365a7ace).
- `docs/testing/coverage-measurement-methodology.md` and `docs/testing/mutation-measurement-methodology.md` (the latter describes per-file data carried across runs).
- ADR-023 `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md`.
- PR #1300 (merged 2026-09-24) changed README.md, ROADMAP.md, `docs/architecture/component-diagram.md`, `docs/architecture/patterns-guide.md`. A search of README, ROADMAP, CLAUDE.md and AGENTS.md for "85" finds no coverage target.

Code scope for the auditor: none under `src/`. The issue is CI configuration and a coverage target, so there is no code to audit. The pre-draft's open question about CLAUDE.md still saying "85% line coverage" is answered: it does not. The pre-draft's `packages: [unknown]` is corrected to empty.

## Destinations

- Reject the single global threshold: present, `docs/testing/coverage-measurement-methodology.md` line 18 and 23 ("single percentage ... per-package targets are not optional") and ADR-023.
- Per-flag coverage against manifest targets, no project-wide percentage: present, AGENTS.md section 9 and the methodology doc.
- Mutation score per file, no project-wide target: present as per-file dashboard data in `docs/testing/mutation-measurement-methodology.md` (line 45 "per-file across runs"). The explicit words "no project-wide target" are in AGENTS.md section 9 ("Mutation score is per file with no project-wide target"), not in the methodology page itself.
- Gaps tracked as per-package `[TEST]` issues: present as practice (open example #1627, an open #901 aggregate that still carries the old 85% target).
- No hand-typed figures in docs: present (AGENTS.md section 9 and SPEC-001); PR #1300 applied it to the four files.
- Not carried over: branch 80%, method 90%, "Core and Messaging 90%". No equivalent target exists; AGENTS.md says branch and method coverage are informational.

## Successor and duplicate issues

No successor or duplicate is named. Related issues, states verified with `gh issue view` today:
- #65 `[TEST] Increase line coverage to 85%`: CLOSED.
- #1090 (the issue PR #1300 fixes): CLOSED.
- #505, #872: cross-references only; #872 (epic) is OPEN and unrelated to the decision.
- #66 and #67 are named in the issue body and in PR #1300 as "reviewed separately"; their state was not checked here (outside this issue's scope) and nothing depends on it.
- #901 `[TEST] Increase coverage for 13 low-coverage modules (1-59%) to >=85%`: OPEN. Not a successor, but it still states a global 85% target; the auditor may note it as inconsistent with the per-flag model.

## Lessons for the pipeline

- The pre-draft listed a CI workflow file as scope without checking it still exists; verify renames with `git log --follow` before listing scope. dotnet-ci.yml was consolidated away.
- A fresh wia worktree has no `artifacts/knowledge/predraft`; read it from the main checkout and create `artifacts/knowledge/issues` in the worktree.
