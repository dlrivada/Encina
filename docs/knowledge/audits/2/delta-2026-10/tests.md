## Coverage measured
not measured: delta rule (b). The scope of #2 is ten `.sql` scripts (`src/Encina.ADO.Oracle/Scripts/*.sql`, `src/Encina.ADO.Sqlite/Scripts/*.sql`), none of which exists in this worktree: `src/` has no `Encina.ADO.Oracle` or `Encina.ADO.Sqlite` directory and `.backup/` is absent (checked with `Get-ChildItem src -Directory` and `Test-Path .backup` in `wia-2`). There is no C# source and no coverage flag to measure.

## Findings
- none

## Informational (not findings)
- Rule (b) check: `.github/coverage-manifest/` has no `Encina.ADO.Oracle.json` or `Encina.ADO.Sqlite.json` (listing filtered on `Oracle|Sqlite` returned nothing), so no scoped file lacks targets, has an unjustified target or has a 0 target without justification; there is no scoped file to hold an entry. SQL scripts are not coverable code in any case.
- The only manifest hit for `Oracle|Sqlite` is `SqliteRespawner.cs` in `.github/coverage-manifest/Encina.Testing.Respawn.json:49` (`defaultTests` unit and guard, `reason` "Default: assume testable with mocks"). It belongs to `Encina.Testing.Respawn`, was not touched by the commit that closed #2 (`cf4d3287`), and is not in the delta scope, so it is not judged here. Whether it should exist at all after the SQLite removal (ADR-024) is a question for the audit of the Respawn package.
- `tests\` has no directory named `*Oracle*` or `*Sqlite*` (recursive directory search), consistent with the removal of both providers (ADR-009, ADR-024).

## CRAP
pending #1346

## Lessons for the pipeline
- none
