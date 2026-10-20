## Coverage measured
Issue #21 changed no code and no test: no PR, and its only referencing commit `2b50a1ec` touched `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md` (`git show --stat 2b50a1ec`). `584a712b` (#36), which delivered the feature and its tests, is an ancestor of the audited HEAD `77a1951e` (`git merge-base --is-ancestor 584a712b HEAD` returned 0). So the scope of #21 has nothing to measure, and any coverage gap belongs to #36's audit. Following the lesson from audit #17, I ran only a narrow filtered check of the delivered implementation the archivist named, to confirm its tests exist, pass and are not vacuous. It is not a pass/fail against the manifest.

Build configuration: `-c Release` for every run. Cobertura reports are under `artifacts\audit\coverage\{unit,guard,contract}\`. The filters are the bare feature namespaces (`Encina.UnitTests.Marten.Projections`, `Encina.GuardTests.Marten.Projections`) and `ProjectionRegistryContractTests` for contract. Measured line coverage per file is the union of hits across classes of the cobertura file, restricted to the filtered tests.

Manifest (`.github/coverage-manifest/Encina.Marten.json`): `targets` is `unit 38`, `guard 7`, `contract 8`, a package-wide aggregate that a filtered run cannot validate. The `files{}` map only says which flags apply per file. So the figures below are informational, not pass/fail.

| File (`src/Encina.Marten/Projections/`) | Manifest `defaultTests` | unit (135 passed) | guard (35 passed) | contract (18 passed) |
| --- | --- | --- | --- | --- |
| InlineProjectionDispatcher.cs | unit, guard | 97/101 = 96.0% | 17/101 = 16.8% | 0/101 |
| InlineProjectionRelay.cs | unit, guard | 44/45 = 97.8% | 0/45 | 0/45 |
| IProjectionManager.cs | none (interface) | 6/6 = 100% | 2/6 | 0/6 |
| MartenProjectionManager.cs | unit, guard | 107/283 = 37.8% | 63/283 = 22.3% | 0/283 |
| MartenReadModelRepository.cs | unit, guard, contract | 121/150 = 80.7% | 13/150 = 8.7% | 0/150 |
| ProjectionContext.cs | unit, guard | 21/21 = 100% | 4/21 = 19.0% | 0/21 |
| ProjectionContextFactory.cs | unit, guard | 14/17 = 82.4% | 0/17 | 0/17 |
| ProjectionOptions.cs | unit | 5/5 = 100% | 0/5 | 0/5 |
| ProjectionRegistry.cs | unit, guard | 84/85 = 98.8% | 12/85 = 14.1% | 78/85 = 91.8% |
| ProjectionStatus.cs | unit, guard | 10/10 = 100% | 0/10 | 0/10 |
| IProjection.cs, IReadModel.cs, IReadModelRepository.cs, ProjectionErrorCodes.cs, ProjectionLog.cs | see manifest | not in the cobertura report (interfaces, constants or generated code, no coverable lines) | same | same |

All 188 tests across the three flags passed. Integration: not measured. The manifest has no `integration` target for `Encina.Marten`, and the Docker/Testcontainers integration test `tests\Encina.IntegrationTests\Infrastructure\Marten\Projections\MartenInlineProjectionIntegrationTests.cs` was not run (not part of #21's scope). Property: not measured (no `property` target in the manifest). Load and benchmark: not applicable to a feature #21 never built.

Observation for #36's audit, not a finding here: in this filtered run `MartenProjectionManager.cs` is the least covered implementation file in the unit flag (107/283 = 37.8%), and no test outside `Marten\Projections\` references it apart from one contract file (`IAggregateRepositoryContractTests.cs`).

## Findings
- none

## CRAP
pending #1346

## Lessons for the pipeline
- For an issue closed "created in error" with no diff, the test stage can end at confirming the empty diff (`git show --stat`, ancestry of the successor's commit) and a narrow filtered run of the successor's tests; the coverage figures are informational and any gap goes to the successor's audit, not to the empty issue.
