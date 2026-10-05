## Coverage measured

Issue #24 shipped no code (closed "created in error", duplicate of #35). Confirmed empty diff: `git show --stat 2b50a1ec -- src tests` (the only commit referencing #24) lists no `src/` or `tests/` file. The successor's commit 924ca2e5 (#35) is an ancestor of the audit worktree HEAD (`e14704bf`; `git merge-base --is-ancestor 924ca2e5 HEAD` exit 0), so the files below are the ones present in this branch. There is no per-flag target to evaluate for #24 itself: the manifest `targets` block is a package-wide aggregate and `files{}` is flag applicability, so a filtered run cannot pass or fail it. The figures below are informational for the #35 audit.

Narrow filtered runs, build configuration Release (as CI), `--collect "XPlat Code Coverage"`, results under `artifacts\audit\coverage\<flag>`:

- unit: `dotnet test tests\Encina.UnitTests\Encina.UnitTests.csproj -c Release --filter "FullyQualifiedName~Encina.UnitTests.Messaging.Health|FullyQualifiedName~Encina.UnitTests.AspNetCore.Health"`: 198 passed, 0 failed, 0 skipped.
- guard: `dotnet test tests\Encina.GuardTests\Encina.GuardTests.csproj -c Release --filter "FullyQualifiedName~Encina.GuardTests.Messaging.Health|FullyQualifiedName~Encina.GuardTests.AspNetCore"`: 77 passed, 0 failed, 0 skipped.
- contract, property, integration: not measured: the issue has no code scope and #35's health files have no contract, property or integration test files (searched `tests\` for the class names; hits are only in UnitTests and GuardTests). Any gap belongs to #35's audit.

Covered / coverable lines (cobertura, line coverage) of the files the archivist named, from each filtered run:

| File (src\...) | unit | guard |
| --- | --- | --- |
| Encina.Messaging\Health\EncinaHealthCheck.cs | 19/23 | 11/23 |
| Encina.Messaging\Health\HealthCheckResult.cs | 10/10 | 0/10 |
| Encina.Messaging\Health\OutboxHealthCheck.cs | 53/53 | 3/53 |
| Encina.Messaging\Health\InboxHealthCheck.cs | 15/20 | 2/20 |
| Encina.Messaging\Health\SagaHealthCheck.cs | 40/40 | 2/40 |
| Encina.Messaging\Health\SchedulingHealthCheck.cs | 43/43 | 2/43 |
| Encina.Messaging\Health\DatabaseHealthCheck.cs | 26/34 | 8/34 |
| Encina.Messaging\Health\ProviderHealthCheckOptions.cs | 5/5 | 0/5 |
| Encina.AspNetCore\Health\EncinaHealthCheckAdapter.cs | 25/25 | 0/25 |
| Encina.AspNetCore\Health\CompositeEncinaHealthCheck.cs | 34/35 | 0/35 |
| Encina.AspNetCore\Health\HealthCheckBuilderExtensions.cs | 117/221 | 95/221 |

`IEncinaHealthCheck.cs`: manifest `defaultTests: []` (interface), nothing to measure. Guard percentages are low by design: guard tests cover only null-check lines, so a per-file guard figure is meaningful only for the aggregate package run, which this narrow run is not. The unit gaps (`InboxHealthCheck.cs` 5 uncovered lines, `HealthCheckBuilderExtensions.cs` 104 uncovered lines, `EmptyHealthCheck.cs` 0/2, `EncinaHealthCheck.cs` 4 uncovered lines) are informational for #35.

Test files found for #35's surface (answering the archivist's open point): `tests\Encina.UnitTests\Messaging\Health\` (`HealthChecksTests.cs`, `SagaHealthCheckTests.cs`, `SchedulingHealthCheckTests.cs`, `HealthCheckOptionsTests.cs`, `HealthCheckExceptionMessageLeakStaticScanTests.cs`), `tests\Encina.UnitTests\AspNetCore\Health\` (`CompositeEncinaHealthCheckTests.cs`, `EncinaHealthCheckAdapterTests.cs`, `HealthCheckBuilderExtensionsTests.cs`), `tests\Encina.GuardTests\Messaging\Health\HealthChecksGuardTests.cs`, `tests\Encina.GuardTests\AspNetCore\AspNetCoreGuardTests.cs`. The original `tests\Encina.Tests\Health\` is gone (consolidated).

## Findings
- none

## CRAP
Pending #1346.

## Lessons for the pipeline
- For a closed-in-error issue with an empty diff, the archivist's "tests not located by file name" note is the one useful input: a `Select-String -List` over `tests\` for the successor's class names answers it in one call, and a narrow filtered run of the successor's namespaces confirms the tests pass; per-file gaps go to the successor's audit (#35), not to the empty issue.
