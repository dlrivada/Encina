# Testing SimpleMediator

This guide explains how to exercise the automated test suites that protect SimpleMediator and what each layer is intended to validate.

## Test Layers

| Suite | Project | Focus |
|-------|---------|-------|
| Unit tests | `tests/SimpleMediator.Tests` | Behavioural checks for mediators, configuration helpers, and built-in pipeline components. |
| Property tests | `tests/SimpleMediator.PropertyTests` | Configuration and pipeline invariants validated with FsCheck across varied inputs. |
| Contract tests | `tests/SimpleMediator.ContractTests` | Structural safeguards that assert the public surface keeps its interoperability guarantees. |

## Running The Tests

Execute every suite with one command from the repository root:

```pwsh
dotnet test SimpleMediator.slnx --configuration Release
```

Use the following filters for a specific layer:

```pwsh
# Unit tests only
dotnet test tests/SimpleMediator.Tests/SimpleMediator.Tests.csproj

# Property tests only
dotnet test tests/SimpleMediator.PropertyTests/SimpleMediator.PropertyTests.csproj

# Contract tests only
dotnet test tests/SimpleMediator.ContractTests/SimpleMediator.ContractTests.csproj
```

## Coverage Snapshot

Run the Release configuration with coverage collection to refresh the stored baseline:

```pwsh
dotnet test --configuration Release --collect "XPlat Code Coverage"
```

Then regenerate the aggregated report:

```pwsh
dotnet tool run reportgenerator -reports:"TestResults/**/*.xml" -targetdir:"artifacts/coverage/latest" -reporttypes:HtmlInline_AzurePipelines;TextSummary
```

The latest HTML and summary output lives under `artifacts/coverage/latest/`.

## Mutation Testing

Run the Stryker.NET sweep through the single-file helper to keep tooling consistent across environments:

```pwsh
dotnet run --file scripts/run-stryker.cs
```

Reports land under the timestamped folder in `StrykerOutput/` alongside HTML and JSON summaries. The latest full run (2025-12-07) killed all mutants and reached a 92.37% score, raising the README badge to 90% line coverage in tandem.

Refresh the mutation badge and surface the latest totals right after Stryker completes:

```pwsh
dotnet run --file scripts/update-mutation-summary.cs
```

The helper consumes the newest `mutation-report.json`, writes a concise summary to stdout, and updates the README badge in place when the canonical pattern is present. If the badge section has been customized, it prints the Markdown snippet so you can paste it manually.

## Property-Based Testing Notes

Property tests limit selector list sizes to keep execution time predictable. When extending the suite, prefer shrinking-aware generators and keep assertions free of side effects so FsCheck can explore counterexamples efficiently.

## Next Steps

- Keep the Stryker baseline green by pairing new unit/property scenarios with badge refreshes through `scripts/update-mutation-summary.cs`.
- Share notification/property generators with unit fixtures so cancellation and pipeline scenarios reuse the same builders.
- Wire CI benchmarking thresholds so regressions beyond the documented limits fail fast and flow into the roadmap log.
- Link roadmap workstreams to requirement IDs in `docs/en/guides/REQUIREMENTS.md` for full traceability.
