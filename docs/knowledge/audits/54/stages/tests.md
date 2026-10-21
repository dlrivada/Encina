## Coverage measured
Not measured: no src/ scope. Issue #54 was closed as not planned on 2025-12-27 with no PR and no commit; nothing shipped, so no flag (unit, guard, contract, property, integration) has a scoped file to measure. The archivist's scope list names only documentation files (`docs/engineering/PROJECT-HISTORY.md`, `CHANGELOG.md`, `docs/releases/v0.11.0/CHANGELOG-DETAILS.md`), which belong to `docs-reviewer`. Load and benchmark: not applicable (no code).

Checks run in `D:\Proyectos\Encina\.claude\worktrees\wia-54` (branch `audit/54`, HEAD `81cac9093`):
- Grep `Asp\.Versioning|ApiVersion|AddEncinaVersioning` over `src\`, `tests\` and `.github\`: 0 matches in 0 files. No test, test fixture or manifest entry refers to the proposed surface.
- `git log --all --grep='#54\b' -i`: only the two audit commits (`e75cf99b9`, `81cac9093`); no implementation commit exists.
- `.github\coverage-manifest\*.json` searched for `Versioning`: the only hits are `Encina.Marten.json` lines 374, 382, 390, 398 and 405 (`Versioning/ConfigureMartenEventVersioning.cs` and siblings). They concern Marten event versioning (upcasters), a different feature from HTTP API versioning, and are not in this audit's scope.
- Rule (b) (obligations): no file of the scope is under `src/`, so no manifest entry is required or missing.

## Findings
- none

## Informational (not findings)
- Rejected issue, empty diff: there is nothing to measure and no regression test is owed. The code stage (`stages\code.md`) found no bug, so step 3 (regression tests) has no input.
- Test types per `AGENTS.md` section 9: unit, guard, contract, property, integration, load and benchmark all do not apply, because the feature was never built. The 0 tests for `Asp.Versioning` are a consequence of the rejection, not negligence.
- If the open post-1.0 backlog items #192 and #348 (identical or overlapping idea, recorded by the archivist and code stages) are ever implemented, they will need their own per-flag targets in the manifest of the package that receives the code (`Encina.AspNetCore` today has a test area under `tests\Encina.UnitTests\AspNetCore` and `tests\Encina.GuardTests\AspNetCore`); that obligation belongs to those issues and to their audit, not to #54.
- CRAP: not applicable here (no changed methods).

## CRAP
Pending #1346; no changed methods in this issue.

## Lessons for the pipeline
- For a rejected issue with an empty diff the test stage is three calls: a repo-wide Grep for the proposed API names over `src\`, `tests\` and `.github\`, a `git log --all --grep` for the issue number, and a manifest search for the feature name. Check what a manifest hit means before reporting it: `Versioning` matched Marten event versioning, an unrelated feature.
