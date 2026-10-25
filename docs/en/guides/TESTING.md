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

The CI workflow collects coverage from all 5 measurable test types (Unit, Guard, Contract, Property, Integration) and generates a **per-flag coverage report** using `.github/scripts/coverage-report.cs`. Each source file declares in `.github/coverage-manifest/{Package}.json` which test types apply to it, and each package declares a target percentage per test type. Coverage is computed **independently per flag** (obligations model): a line covered by unit tests does not count toward the guard or contract flag, and a package is green only when every applicable flag reaches its own target. There are no category-level targets; all configuration lives in the manifests.

Results are published to the **Coverage Dashboard**: [dlrivada.github.io/Encina/coverage/](https://dlrivada.github.io/Encina/coverage/)

The dashboard shows:

- Overall weighted coverage percentage (informational only; there is no project-wide target)
- Per-category breakdown (informational only; categories have no targets of their own)
- Per-package detail with per-test-type columns (Unit, Guard, Contract, Property, Integration) — this is the only gate: each package must reach its manifest target on every applicable flag
- Coverage trend over time (historical data accumulated in `history.json`, served live on Pages)
- Interactive sunburst distribution chart

CI does not commit coverage data to `main`. `publish-coverage.yml` computes `latest.json` and `history.json` from each successful CI Full run and persists them to the orphan `dashboard-data` branch (`.github/scripts/pages-dashboard-data.ps1 -Mode Persist`); its `deploy` job then calls `docs.yml` — the only workflow that deploys GitHub Pages (#1381) — whose `deploy` job lays that branch over the live dashboard data, inside the `pages` concurrency lock, and deploys the site. Before it appends, the publisher reads the base `history.json` from the branch copy (`pages-dashboard-data.ps1 -Mode Read -Domain coverage -OutDir base/coverage`), using the branch copy when it is present and non-empty, and otherwise the larger (by entry count) of the live Pages copy (read with `-Mode Live`; a 404 means none, any other failure fails the job) and the committed copy. The copies served live after the deploy are at [dlrivada.github.io/Encina/coverage/data/latest.json](https://dlrivada.github.io/Encina/coverage/data/latest.json) and `.../history.json` (last 100 entries, appended by `.github/scripts/coverage-history.cs`); the copies tracked under `docs/coverage/data/` in the repository are only a fallback seed and are not current.

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

## Testing Time

This section is for contributors and users who write tests for code that depends on `TimeProvider`: it states the fake-clock convention and the helpers that support it. Production code takes the clock by injection (see `AGENTS.md`, section 3).

### The fake clock

Use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` from the `Microsoft.Extensions.TimeProvider.Testing` package, which `Encina.Testing` depends on. Encina has no clock of its own: the former `FakeTimeProvider`, `FakeTimer` and `FrozenTimeScope` types of `Encina.Testing` were removed in #2774 and no alias remains.

```csharp
using Microsoft.Extensions.Time.Testing;

var clock = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

clock.Advance(TimeSpan.FromMinutes(5));              // move forward and fire due timers
clock.SetLocalTimeZone(TimeZoneInfo.Utc);            // choose the zone for GetLocalNow()
```

- Advance with `Advance(TimeSpan.FromMinutes(n))`. Moving the clock backwards throws `ArgumentOutOfRangeException`.
- Set the zone with `SetLocalTimeZone(TimeZoneInfo)`.
- The test helpers `EncinaTestFixture`, `EncinaTestContext`, `ModuleTestFixture` and the `Outbox`, `Inbox`, `Saga` and `Scheduling` test helpers expose and register this same type. Their `AdvanceTimeBy*` and `SetTimeTo` methods keep working. The four messaging helpers start their clock at the current UTC time.

### Timer callbacks must not throw

In `Microsoft.Extensions.TimeProvider.Testing` 10.5.0 and 10.11.0 the clock stopped firing every timer after one callback threw. This is behaviour observed during the #2774 spike, not a guarantee of Microsoft's documentation (the package's XML docs do not mention it). Encina keeps the following convention because of it:

- Timer callbacks in code under test must not throw.
- Tests never swallow exceptions thrown from `Advance`; do not wrap the clock in a type that catches them.

### Tests of background services that create timers

A background service usually creates its timer from a background task, and advancing the clock before the timer exists does nothing. Use `TimerSignallingTimeProvider` (`tests/Encina.TestInfrastructure/Time/TimerSignallingTimeProvider.cs`, public sealed, namespace `Encina.TestInfrastructure.Time`) instead of polling or `Task.Delay`:

```csharp
var clock = new TimerSignallingTimeProvider(DateTimeOffset.UtcNow);
// ... start the service under test with `clock` ...
await clock.WaitForTimersAsync(1, TestContext.Current.CancellationToken);
clock.Advance(TimeSpan.FromMinutes(1));
```

`WaitForTimersAsync(count, cancellationToken)` waits until `count` timers have been created on the clock since the previous wait. It is bounded to five seconds and then throws `TimeoutException` with the message "expected N timers, saw M". Signals accumulate for timers created between waits, so a timer created earlier can satisfy a later `WaitForTimersAsync` call; signals consumed before a timeout are lost.

For the worked example of injecting the clock into a component, see [Audit tracking: testing](../../features/audit-tracking.md#testing).

## Property-Based Testing Notes

Property tests limit selector list sizes to keep execution time predictable. When extending the suite, prefer shrinking-aware generators and keep assertions free of side effects so FsCheck can explore counterexamples efficiently.

## Next Steps

- Watch the [mutations dashboard](https://dlrivada.github.io/Encina/mutations/) for surviving mutants in modules you contribute to. Pair new code paths with assertions that detect the mutations Stryker generates (boundaries, equality, null checks, string content). See [`MUTATION_TESTING.md`](MUTATION_TESTING.md) for the catalog of common mutation types.
- Guard the Zero Exceptions policy by verifying new send/publish scenarios return functional results (`Either`, `Option`) instead of throwing.
- Share notification/property generators with unit fixtures so cancellation and pipeline scenarios reuse the same builders.
- Monitor the tightened 15%/25% benchmark guardrails and load throughput limits so regressions from the latest baseline flow straight into the roadmap log.
- Link roadmap workstreams to requirement IDs in `docs/en/guides/REQUIREMENTS.md` for full traceability.
