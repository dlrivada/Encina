# Tests stage - issue #9 (delta rules-2026-10, rule (b) only)

Worktree `D:\Proyectos\Encina\.claude\worktrees\wia-9`, branch `audit/9`, HEAD `7456d580`. Scope: `artifacts\knowledge\delta-scope.md` (the original audit's single scoped file). Each path below was traced with `git log --diff-filter=D --format="%h %ad %s" --date=short -- <path>`, run in the worktree; the deleting commits `65826302`, `22494a97` and `e9b49ac8` are all ancestors of HEAD (`git merge-base --is-ancestor`, exit 0), so the deletions are reachable from the branch under audit. The deleting commit differs per path: the test files and projects went with test consolidation `65826302` (2026-01-16), the source package with ADR-024 `22494a97` (2026-03-28), the manifest with `e9b49ac8` (2026-09-22).

## Coverage measured

not measured: delta rule (b). No scoped file survives, so there is nothing to build, run or compare against a target, and no coverage figure is claimed for any flag.

### Scoped files that do not exist (Test-Path, run in the worktree)

| Scoped path | Test-Path | Why |
| --- | --- | --- |
| `tests\Encina.Dapper.Sqlite.ContractTests\Scheduling\ScheduledMessageStoreDapperContractTests.cs` (the only file of the record and the audit result; modified by `3f20fec8`) | False | Deleted by `65826302` ("refactor: Consolidate tests and update documentation", 2026-01-16), the only commit `git log --diff-filter=D` returns for the path, together with its project |
| `tests\Encina.Dapper.Sqlite.ContractTests` (project folder) | False | Deleted by `65826302` (same commit; its deletions under `tests/Encina.Dapper.Sqlite.*` cover the Contract, Guard, Integration, Load, Property and `.Tests` projects) |
| `src\Encina.Dapper.Sqlite` (package under test) | False | Deleted by `22494a97` (ADR-024, 2026-03-28, "feat: remove SQLite provider"); that commit touches 0 paths under `tests/Encina.Dapper.Sqlite.*` |
| `tests\Encina.Dapper.Sqlite.IntegrationTests` | False | Deleted by `65826302` (2026-01-16) |
| `tests\Encina.Dapper.Sqlite.UnitTests` | False | No deleting commit: `git log --all -- tests/Encina.Dapper.Sqlite.UnitTests` returns nothing, so the path has no history in this repository (it never existed here) |
| `.github\coverage-manifest\Encina.Dapper.Sqlite.json` | False | Deleted by `e9b49ac8` ("feat(coverage): DocRef citations for coverage (SPEC-001)", 2026-09-22, #1119). No manifest exists for the package today (the only `Sqlite` hit among the manifests is `Encina.Testing.Respawn.json`, a different package) |

Two more files were touched by the same commit `3f20fec8` ("Fixes #7"): `tests/Encina.DataAnnotations.PropertyTests/DataAnnotationsValidationBehaviorPropertyTests.cs` and `tests/Encina.FluentValidation.PropertyTests/ValidationPipelineBehaviorPropertyTests.cs`. Both also return Test-Path False (the per-package test projects were consolidated). They are not issue #9's scope (the record and the audit result list one file), they belong to #7, and #7's delta is already tracked by #1850. They are listed only so a reader does not mistake them for scope.

### Surviving scoped files

None. Rule (b) has nothing to judge: there is no file in scope with per-flag targets, so no finding of the classes "no targets", "unjustified target", "target below what is demanding" or "0 without justification" can arise. I did not widen the scope to the feature's siblings.

## Findings

- none

## Informational (not findings)

- Siblings of the deleted file that do exist today and are the live counterparts of the same feature: the three `ScheduledMessageStoreDapper.cs` stores of `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL` and `Encina.Dapper.MySQL`, with unit, guard and integration test classes under `tests\Encina.UnitTests\Dapper\*\Scheduling\`, `tests\Encina.GuardTests\Dapper\*\` and `tests\Encina.IntegrationTests\Dapper\*\Scheduling\` (nine files, found with `Get-ChildItem tests -Recurse -Filter 'ScheduledMessageStoreDapper*Tests.cs'`). Their rule (b) targets (the `Scheduling/ScheduledMessageStoreDapper.cs` entry, which has no per-file targets or justifications) and the MySQL guard gap belong to the #8 delta audit and are already in open umbrella issue #1864 (items "Set per-file targets for ScheduledMessageStoreDapper in ..." and "Add the ten missing MySQL guard tests"). Not repeated here.
- Duplicate check for this stage: the open delta umbrellas #1847 (#3), #1850 (#7), #1856 (#6) and #1864 (#8) were read. None mentions Dapper.Sqlite or `ScheduledMessageStoreDapperContractTests`; #1864's only overlap is the live-provider store above. `gh issue list --state open --search "Sqlite in:title,body"` returns #1808, #1806, #1811, #1372 (docs and CI, covered by the docs stage) and #1750 (105 adjacent duplicated SqlServer lines in 16 contract and property test files left over from the SQLite removal, a test-quality issue in surviving files, not rule (b) and not in #9's scope). Nothing tracks a rule (b) gap for #9 and none is owed.
- The record's gotcha (the permanently skipped `RescheduleRecurringMessageAsync_Contract_ResetsRetryFields`, no `.md` justification) and its decision (past `ScheduledAtUtc`) cannot be re-tested: `Select-String` over every `tests\**\*.cs` for the two test names returns no file. The skip was a pre-obligations-model test-quality issue of deleted code; the original audit already judged it moot and ADR-024 records the underlying datetime incompatibility.
- Manifest schema: this delta's rule (b) refers to the #1762 schema (per-file targets with justifications). The worktree is behind `origin/main` (`ab4ec134` after fetch); for the live siblings the entries still lack per-file targets and justifications according to #1864, which is consistent with the worktree's manifest. No manifest was edited.

## CRAP

pending #1346. Not computed: no `src/` code is in scope.

## Lessons for the pipeline

- When every scoped file and its manifest are deleted, the delta rule (b) stage can finish with a Test-Path table plus one `Select-String` over `tests\` for the record's test names; the delta brief can say up front "no survivors expected" for `code-removed` verdicts, so no coverage run is attempted.
- A fix commit that touched several files for a parent issue (`3f20fec8` "Fixes #7" touched three) must be read against the record's own scope: only the file the record names is the sub-issue's scope, and the other two files belong to the parent's delta. Say so in the table to avoid a reader counting three scoped files.
- State each deletion by its own `git log --diff-filter=D -- <path>`, not by the ADR that motivated the removal: the first version of this stage attributed the scoped test file and the test projects to ADR-024 (`22494a97`), copied from the delta scope, when test consolidation `65826302` (2026-01-16) had deleted them 10 weeks earlier and `22494a97` deleted only `src\Encina.Dapper.Sqlite`. Run the command per path (source, each test project, manifest) and record one commit per path; a path with no output has no history (`tests\Encina.Dapper.Sqlite.UnitTests`) and is stated as such (audit #9 verifier FAIL).
- Duplicate checks for a `code-removed` unit go through the SQLite-related open issues (#1750, #1806, #1808, #1811, #1372) rather than the sibling delta umbrellas; the umbrellas only matter when the unit has a live counterpart (here the Dapper scheduled message stores of #1864).
