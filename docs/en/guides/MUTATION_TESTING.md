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

- HTML report: `StrykerOutput/<timestamp>/reports/mutation-report.html` (generated automatically).
- Raw console output remains the primary log; redirect the helper script if a persistent log file is required.
- Console summary highlights surviving mutants; treat anything above the break threshold as a failure condition.

### Baseline (2025-12-06)

- Mutation score: **70.63%** (429 tested mutants; 327 killed, 102 survived).
- Hotspots: `SimpleMediator` orchestration logic and `MediatorAssemblyScanner` reflection paths keep the bulk of survivors.
- Action items: strengthen mediator core tests (handler resolution, exception paths) and add targeted coverage for scanner edge cases.

## Next Steps

- Integrate Stryker runs into CI once execution time is acceptable.
- Add targeted ignore patterns once hotspots are identified.
