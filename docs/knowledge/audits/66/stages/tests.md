## Coverage measured
not measured: no src/ scope. Issue #66 ("Increase branch coverage to 80%") was closed not planned with no closing commit and delivered no code (archivist.md). The audit branch `audit/66` was created from `origin/main` at `4ba5e59b0` (`git reflog show audit/66`). `git diff --stat 4ba5e59b0 237e118f2 -- src tests` is empty, so no file under `src/` or `tests/` is in scope. There is no scoped file, so no flag (unit, guard, contract, property, integration) has a manifest target to compare, and no coverage run was attempted. A flag that was not run is "not measured", not "pass".

## Findings
- none

## Informational (not findings)
- Rule (b) (obligations): no scoped file, so there is no per-file entry to check in `.github/coverage-manifest/{Package}.json`.
- Regression tests: the code stage reported no bug (`code.md` Findings: none), so no regression-test check applies.
- Test quality and real infrastructure: nothing in scope.
- The replacement model that the issue's closing comment points to is verified by the code stage (`AGENTS.md:125`, `docs/testing/coverage-measurement-methodology.md:23` and `:213`, `.github/scripts/coverage-report.cs`). A case-insensitive search of `coverage-report.cs` for "branch" finds 0 occurrences in this worktree, so the script gates no branch figure, consistent with "branch and method coverage are informational" in AGENTS.md section 9.
- Open tracker #2225 (state OPEN, checked live with `gh issue view`): "The branch of SagaStoreMongoDB.AddAsync that copies a foreign ISagaState is executed by no test, and line coverage hides it". It is a single-branch gap, not a successor of #66. Its test gap is owned by that issue and the audit of the issue that delivered `SagaStoreMongoDB`, not by this empty issue.
- Obligation a later implementation inherits: if a single branch-coverage gate were ever proposed again, it would need its own manifest key and per-flag targets with justifications (AGENTS.md section 9); the rejected alternative recorded in the knowledge file stays the rule.

## CRAP
pending #1346

## Lessons for the pipeline
- none
