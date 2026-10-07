## Coverage measured
not measured: delta rule (b). Issue #28 delivered no `src/` code (it was closed in error, duplicate of #29/#30), so there is no scoped file to measure; no coverage run was attempted.

## Findings
- none

## Informational (not findings)
Delta `rules-2026-10`, rule (b) only, branch `audit/28`.

Empty diff, confirmed:
- `git show --stat 2b50a1ec` (the only commit that references #28) lists `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md` only; no `src/`, `tests/` or workflow file.
- `git show --stat ccf83b00` ("re-enable ContractTests and PropertyTests in SonarCloud workflow", the commit that lifted the exclusion for #29 and #30) changed `.github/workflows/sonarcloud.yml` only (4 insertions, 4 deletions); no `src/`, `tests/` or manifest file. `git merge-base --is-ancestor ccf83b00 HEAD` returns 0, so it is reachable from `audit/28`.
- Symbol search: the issue proposed no symbol (it asked for a workflow change), so there is nothing to search for. `Select-String -Path .github\workflows\sonarcloud.yml -Pattern 'ContractTests|PropertyTests|dotnet test|--filter'` finds nothing; the exclusion filter is gone and the job runs no tests.
- Regression targets (`Test-Path`): `tests\Encina.ContractTests` True, `tests\Encina.PropertyTests` True, `.github\workflows\sonarcloud.yml` True, `.github\workflows\ci.yml` True. `ci.yml:303` and `ci.yml:342` run `dotnet test` on the two projects.
- Manifest search: `Select-String` over the 107 files in `.github\coverage-manifest\*.json` for `sonarcloud|.github/workflows|ci.yml` finds no entry. The coverage manifests cover only `src/` files, so a workflow file has no per-file targets and none are required; rule (b) applies to files under `src/` and none are in this scope.

Successor state, re-checked live with `gh issue view`:
- #29 (`[INFRA] Re-enable ContractTests in SonarCloud workflow`): CLOSED, COMPLETED, 2025-12-24T17:42:47Z.
- #30 (`[INFRA] Re-enable PropertyTests in SonarCloud workflow`): CLOSED, COMPLETED, 2025-12-24T17:42:48Z.
- #28 itself: CLOSED, COMPLETED, single comment "Reverted - issue created in error".
No state change against the record (it names #29 as `duplicate_of`). The tests inside the two projects are left to the audits of #29 and #30.

## CRAP
pending #1346

## Lessons for the pipeline
- none
