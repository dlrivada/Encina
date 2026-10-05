## Coverage measured
Issue #22 has an empty diff: it was closed "created in error" with no PR and no closing commit. `git show --stat 2b50a1ec` (the only commit that references it) lists `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md`, with no `src/` or `tests/` file. `git merge-base --is-ancestor` exits 0 for both `2b50a1ec` and the successor's commit `957093c2` (#37) against this worktree's HEAD, so both are reachable from `audit/22`. The scope is therefore "not measured: no src/ scope in #22's own diff". Per the coverage flags (unit, guard, contract, property, integration) there is nothing to compare for this issue.

Informational run only, on the successor's delivery that the archivist named (`src/Encina.Marten/Versioning/`). It uses a narrow test filter on the bare namespace and `-c Release` (the configuration CI uses). The figures are not a pass/fail for #22 and go to #37's audit.

- Unit: `dotnet test tests\Encina.UnitTests -c Release --filter "FullyQualifiedName~Encina.UnitTests.Marten.Versioning" --collect "XPlat Code Coverage"` gave 68 passed, 0 failed. Line rates from the cobertura report:
  - `ConfigureMartenEventVersioning.cs` 100%
  - `EventUpcasterRegistry.cs` 98.2% (56 of 57 coverable lines)
  - `EventVersioningOptions.cs` 100%
  - `LambdaEventUpcaster.cs` 100%
  - `EventUpcasterBase.cs` 0% (0 of 5 coverable lines)
  - `EventVersioningErrorCodes.cs` and `VersioningLog.cs` are absent from the report. The first holds constants only; the second I did not inspect.
- Guard: `dotnet test tests\Encina.GuardTests -c Release --filter "FullyQualifiedName~Encina.GuardTests.Marten.Versioning" --collect "XPlat Code Coverage"` gave 25 passed, 0 failed. Line rates:
  - `EventUpcasterRegistry.cs` 50.9%
  - `LambdaEventUpcaster.cs` 50%
  - `ConfigureMartenEventVersioning.cs` 0%
  - `EventVersioningOptions.cs` 0%
  - `EventUpcasterBase.cs` 0%
  Of these, the manifest lists guard as applicable only for `ConfigureMartenEventVersioning.cs`, `EventUpcasterBase.cs`, `EventUpcasterRegistry.cs`, `LambdaEventUpcaster.cs` and `VersioningLog.cs`. `EventVersioningOptions.cs` has `defaultTests: ["unit"]`.
- Manifest: the `Encina.Marten` `targets` block is a package-wide aggregate (unit 38, guard 7, contract 8). A filtered run cannot validate it, so I report per-file rates and no pass/fail.
- Contract, property, integration: not measured. They are outside the narrow run, and no scoped file of #22 exists. Test files are present for reference: `tests\Encina.IntegrationTests\Infrastructure\Marten\Versioning\EventVersioningIntegrationTests.cs` (151 lines, with `TestProductUpcasters.cs`/`TestVersionedEvents.cs` support types). I did not run them because they need Docker and belong to #37's audit.

## Findings
- none

## Informational (not findings)
The gaps seen in the informational run are not findings of #22, because the issue changed no code. They are handed to #37's audit.

- `EventUpcasterBase.cs` has 0% unit and guard coverage: no test derives from `EventUpcasterBase<,>`, and the one test-tree reference is `TestProductUpcasters.cs:8`/`:14` in the integration project.
- The guard rates of 50.9% (`EventUpcasterRegistry.cs`) and 50% (`LambdaEventUpcaster.cs`) are also low.

## CRAP
pending #1346

## Lessons for the pipeline
- For a closed-in-error issue with an empty diff and a same-titled successor, the test stage reduces to confirming the empty diff, confirming the successor's commit is an ancestor of the audit branch, and a narrow filtered run of the successor's tests. The coverage gaps it surfaces (here `EventUpcasterBase.cs` at 0%) should be handed to the successor's audit rather than filed against the empty issue.
- A `## Findings` section with no findings contains only `- none`. Any prose after it, even an explanation that the gaps are informational, is parsed by the remediation drafter as a finding. Put informational gaps in a separate section such as `## Informational (not findings)`.
