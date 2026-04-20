# Testing Encina

This guide explains how to exercise the automated test suites that protect Encina and what each layer is intended to validate.

## Test Layers

| Suite | Project | Focus |
|-------|---------|-------|
| Unit tests | `tests/Encina.UnitTests` | Behavioural checks for Encinas, configuration helpers, and built-in pipeline components. |
| Integration tests | `tests/Encina.IntegrationTests` | End-to-end tests with real databases via Testcontainers. |
| Property tests | `tests/Encina.PropertyTests` | Configuration and pipeline invariants validated with FsCheck across varied inputs. |
| Contract tests | `tests/Encina.ContractTests` | Structural safeguards that assert the public surface keeps its interoperability guarantees. |
| Guard tests | `tests/Encina.GuardTests` | Guard clause verification for public APIs. |
| Load tests | `tests/Encina.LoadTests` | High-concurrency stress tests and throughput validation. |
| Benchmark tests | `tests/Encina.BenchmarkTests` | BenchmarkDotNet performance measurements. |

## Running The Tests

Execute every suite with one command from the repository root:

```pwsh
dotnet test Encina.slnx --configuration Release
```

Use the following filters for a specific layer:

```pwsh
# Unit tests only
dotnet test tests/Encina.UnitTests/Encina.UnitTests.csproj

# Integration tests only
dotnet test tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj

# Property tests only
dotnet test tests/Encina.PropertyTests/Encina.PropertyTests.csproj

# Contract tests only
dotnet test tests/Encina.ContractTests/Encina.ContractTests.csproj

# Guard tests only
dotnet test tests/Encina.GuardTests/Encina.GuardTests.csproj

# Load tests only
dotnet test tests/Encina.LoadTests/Encina.LoadTests.csproj

# Benchmark tests (run via BenchmarkDotNet)
dotnet run --project tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj -c Release
```

## Coverage

### Automated (CI)

The CI workflow collects coverage from all 5 measurable test types (Unit, Guard, Contract, Property, Integration) and generates a **weighted coverage report** using `.github/scripts/coverage-report.cs`. Each package is scored against its category target (e.g., 85% for core logic, 50% for database providers).

Results are published to the **Coverage Dashboard**: [dlrivada.github.io/Encina/coverage/](https://dlrivada.github.io/Encina/coverage/)

The dashboard shows:
- Overall weighted coverage percentage
- Per-category breakdown with pass/fail status
- Per-package detail with per-test-type columns (Unit, Guard, Contract, Property, Integration)
- Coverage trend over time (historical data accumulated in `docs/coverage/data/history.json`)
- Interactive sunburst distribution chart

Coverage data is committed automatically by CI to `docs/coverage/data/latest.json` after each successful run. Historical snapshots are appended to `history.json` (last 100 entries) by `.github/scripts/coverage-history.cs`.

### Manual (local)

Run the Release configuration with coverage collection:

```pwsh
dotnet test --configuration Release --collect "XPlat Code Coverage"
```

Then regenerate the weighted report locally:

```pwsh
dotnet run .github/scripts/coverage-report.cs -- --output artifacts/coverage --input artifacts/test-results
```

The output includes `encina-coverage-summary.json`, `encina-coverage-report.md`, and ReportGenerator HTML under `artifacts/coverage/`.

> **Note**: All test outputs are configured to go to `artifacts/` directory, never to the repository root.

## Mutation Testing

The Mutation Tests workflow runs weekly via Stryker.NET, fanning out into 17 parallel shards (one per folder of `src/Encina/`) that an `aggregate` job merges into a single dataset. Results accumulate per-file across runs into a single dashboard at <https://dlrivada.github.io/Encina/mutations/>.

For the methodology (formulas, accumulation model, citation system) see [`docs/testing/mutation-measurement-methodology.md`](../../testing/mutation-measurement-methodology.md). For the practical guide (running, interpreting, contributing) see [`MUTATION_TESTING.md`](MUTATION_TESTING.md).

Quick local run:

```pwsh
dotnet run --file .github/scripts/run-stryker.cs
```

Reports land under `artifacts/mutation/reports/` (JSON + HTML). The full `**/*.cs` scope works locally given enough time; CI runs 17 folder shards in parallel so each shard fits a 60-minute per-shard timeout.

The README badge is updated automatically by `.github/scripts/update-mutation-summary.cs`, which is invoked by the publish workflow after each Stryker run.

## Property-Based Testing Notes

Property tests limit selector list sizes to keep execution time predictable. When extending the suite, prefer shrinking-aware generators and keep assertions free of side effects so FsCheck can explore counterexamples efficiently.

## Next Steps

- Watch the [mutations dashboard](https://dlrivada.github.io/Encina/mutations/) for surviving mutants in modules you contribute to. Pair new code paths with assertions that detect the mutations Stryker generates (boundaries, equality, null checks, string content). See [`MUTATION_TESTING.md`](MUTATION_TESTING.md) for the catalog of common mutation types.
- Guard the Zero Exceptions policy by verifying new send/publish scenarios return functional results (`Either`, `Option`) instead of throwing.
- Share notification/property generators with unit fixtures so cancellation and pipeline scenarios reuse the same builders.
- Monitor the tightened 15%/25% benchmark guardrails and load throughput limits so regressions from the latest baseline flow straight into the roadmap log.
- Link roadmap workstreams to requirement IDs in `docs/en/guides/REQUIREMENTS.md` for full traceability.
