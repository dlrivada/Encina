## Scope

No `src/` or test code was changed by this issue (closed not planned, no closing commit; the timeline `closed` event has no commit_id). The issue targeted "Encina (core), all packages"; no package is scoped.

Surface of the replacement model, all present today:
- `.github/coverage-manifest/` (108 files) and `.github/scripts/coverage-report.cs`.
- `AGENTS.md` section 9 ("Coverage is per flag ... There is no project-wide percentage; branch and method coverage are informational").
- `docs/testing/mutation-measurement-methodology.md`.

PR #1300 (merged 2026-09-24T12:13:29Z, after the close at 12:08:39Z) is a timeline cross-reference only; it says "Fixes #1090" (closed) and changed README.md, ROADMAP.md, docs/architecture/component-diagram.md and docs/architecture/patterns-guide.md. It is recorded as `linked_prs`, not `prs`. The pre-draft's claim that it was the cause of this closure is wrong.

## Destinations

- Per-flag, per-package targets instead of one percentage: present (AGENTS.md section 9, coverage-manifest, coverage-report.cs).
- Per-file mutation score, no project-wide target: present (docs/testing/mutation-measurement-methodology.md).
- Per-package [TEST] issues for gaps: present as practice (for example open #2225); no single tracker, so no destination beyond AGENTS.md was claimed.
- Dropped from the pre-draft: `reviewer-checklist` destination and "Rules and lessons" about hand-typed figures (belong to #1090/#1300, not quoted in #66); the packages list (copied from the issue's "Affected Packages").

## Successor and duplicate issues

None named. The comment points to per-package [TEST] issues rather than one successor. Search `branch coverage in:title` found only #66 and open #2225 (a single-branch gap in SagaStoreMongoDB, not a successor). #1090 (CLOSED) is the issue PR #1300 fixed; #873 (OPEN epic) and #19 only cross-reference.

## Lessons for the pipeline

- Pre-draft attributed PR #1300 as the cause of the closure; it merged after the close and fixes #1090. Check the PR's "Fixes" line and merge time before using a cross-referenced PR as a closing source.
- The pre-draft's `closed_at: 09/24/2026`, `linked_prs: [1300]` and unticked "Affected Packages" were normalised (ISO date, packages empty).
