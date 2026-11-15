## Scope
Issue #67 produced no code. Its subject files today:
- `.github/scripts/run-stryker.cs` (the issue named `scripts/run-stryker.cs`, which no longer exists; moved under `.github/scripts/`).
- `.github/stryker-config.json`, `.github/workflows/mutation-tests.yml` (mutation scope, ADR-056).
- `.github/coverage-manifest/` (108 per-package manifests) and `.github/scripts/coverage-report.cs` (the replacement model).
- `docs/testing/mutation-measurement-methodology.md`, `docs/architecture/adr/056-mutation-testing-scope.md`.
- README.md and ROADMAP.md: grep for `79.75` and `95%` found no hits.
PR #1300 (merged 2026-09-24T12:13:29Z, 5 minutes after the close) closes #1090, touching README.md, ROADMAP.md, docs/architecture/component-diagram.md and docs/architecture/patterns-guide.md; it only mentions #67 as "reviewed separately". Not a closing PR of #67, kept as linked_prs.

## Destinations
- Per-file mutation score, no project-wide target: present in AGENTS.md section 9 ("Mutation score is per file with no project-wide target", line 132). The methodology page does not say it explicitly; open #3323 tracks that (planned).
- Per-flag coverage targets: present (`.github/coverage-manifest/`, `coverage-report.cs`, AGENTS.md section 9).
- Per-package [TEST] issues: process rule, present in AGENTS.md section 11 templates (`test_implementation.md`).

## Successor and duplicate issues
No successor or duplicate. Related open: #3323 (OPEN, "[DEBT] The mutation methodology page never states that the mutation score has no project-wide target"). #19 is CLOSED, #873 (epic) OPEN, #1090 CLOSED (only cross-references).

## Lessons for the pipeline
- The pre-draft linked PR #1300 as the delivering PR and listed Rules and Candidate destinations (spec-invariant) from it; PR #1300 closes #1090 and says #67 is reviewed separately, so it is only a linked PR. Check the PR body and closingIssuesReferences.
- The pre-draft was in the main checkout only (artifacts/ is gitignored); the worktree had none.
