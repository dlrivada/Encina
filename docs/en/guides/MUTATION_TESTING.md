# SimpleMediator Mutation Testing Guide

## Goals

- Establish Stryker.NET as the baseline mutation engine for the mediator library.
- Track actionable metrics (high 85, low 70, break 60) aligned with roadmap thresholds.

## Prerequisites

- .NET 10 SDK installed.
- Install Stryker CLI (`dotnet tool install --global dotnet-stryker`) if not already available.
- Restore dependencies with `dotnet restore SimpleMediator.slnx` prior to running Stryker.

## Running The Suite

- Execute `dotnet tool run dotnet-stryker --config-file stryker-config.json --solution SimpleMediator.slnx` from the repository root.
- Use the C# helper script for convenience: `dotnet run --file scripts/run-stryker.cs`
- Prefer Release builds to mirror CI behavior (`--configuration Release`).
- The repository config pins `concurrency: 1` to avoid vstest runner hangs on Windows; adjust once the suite stabilizes.

## Reporting

- HTML report: `artifacts/mutation/StrykerOutput/<timestamp>/reports/mutation-report.html` (generated automatically).
- Raw console output remains the primary log; redirect the helper script if a persistent log file is required.
- Console summary highlights surviving mutants; treat anything above the break threshold as a failure condition.

### Current Baseline (2025-12-08)

- Mutation score: **93.74%** (449 killed, 2 survived, 0 timeout mutants) using `dotnet tool run dotnet-stryker` with the repo configuration.
- CI runs `scripts/run-stryker.cs` and enforces the baseline; refresh the README badge via `scripts/update-mutation-summary.cs` right after local runs.
- Survivors mainly sit in historical reports; the live suite is green. Keep future contributions paired with targeted tests so surviving mutants stay at zero.

### Paused Hardening Tasks

The dedicated mutation-hardening initiative is paused until new feature work settles. When ready to resume:

1. Re-run `dotnet stryker --project src/SimpleMediator/SimpleMediator.csproj --test-projects tests/SimpleMediator.Tests/SimpleMediator.Tests.csproj` to focus on the mediator core mutants (previously IDs 280–366).
2. Investigate any survivors and add unit or property tests around metrics pipeline integration and handler result validation.
3. Update the mutation badge and dashboard via `scripts/update-mutation-summary.cs` once the new score is recorded.

## Next Steps

- Keep the 93.74% baseline intact by refreshing the badge after every Stryker run.
- Resume the hardening plan when feature development stabilises, starting with the paused tasks above.
- Only introduce ignore patterns after confirming mutants are either equivalent or unreachable by design.
