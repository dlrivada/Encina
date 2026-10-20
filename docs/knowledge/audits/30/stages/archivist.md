## Scope
- `.github/workflows/sonarcloud.yml` (exists today; the only file in commit `ccf83b00`, 4+/4-). Its test step and filter were later removed: the file now runs static analysis only and says coverage is handled by Codecov (line 60, ADR-023, #911).
- Where PropertyTests run today: `.github/workflows/ci.yml` (path filter line 60, job at lines ~342-353) and `.github/workflows/ci-full.yml` (lines ~250-253). Project `tests/Encina.PropertyTests`.
- No `src/` scope. No PR: the issue was closed by commit `ccf83b00` ("Closes #29, Closes #30"); verified with `git show --stat`. `packages` and `prs` are empty.
- The code stage's real surface is CI wiring: `ci.yml`, `ci-full.yml`, `sonarcloud.yml`.

## Destinations
- Decision "PropertyTests/ContractTests not excluded from CI": present, as separate jobs in `ci.yml` and `ci-full.yml` (grep verified).
- Decision "SonarCloud runs no tests; coverage from Codecov": present, `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md` exists.
- Pre-draft candidates "regression-test" and "spec-invariant" were not sourced by any quote; dropped. Pre-draft "Rules and lessons" were restatements of the issue's Impact text, not decisions; dropped.

## Successor and duplicate issues
- #29 (ContractTests): CLOSED, delivered by the same commit; already has `docs/knowledge/issues/29.md`.
- #28 (same scope, both tests): CLOSED, earlier the same day; not a duplicate of #30 (#30 is the delivered one).
- #7 (57 failing tests): CLOSED.
- #1721 (CI skips test jobs on path filters): CLOSED. #1845 (audit pipeline parallel audits) OPEN, unrelated process issue; #1763/#1785 are the historian/delta work that cites audit #30.

## Lessons for the pipeline
- none (the record was written from the closed sibling #29, whose commit and facts are identical; duration computed from createdAt 13:21:42Z and closedAt 17:42:48Z).
