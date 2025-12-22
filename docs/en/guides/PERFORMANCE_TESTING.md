# Encina Performance Testing Guide

## Objectives

- Quantify Encina send/publish overhead using BenchmarkDotNet.
- Surface allocation profiles and latency buckets for common request flows.

## Tooling Plan

- Project scaffold: `benchmarks/Encina.Benchmarks` (BenchmarkDotNet, net10.0).
- Scenarios: command send with instrumentation, notification publish across multi-handlers.

## Execution

- Restore dependencies: `dotnet restore Encina.slnx`.
- Run suites with `dotnet run --file scripts/run-benchmarks.cs` to generate a timestamped artifact directory.
- Persist results under `artifacts/performance/<timestamp>/` for historical comparison (script handles directory creation and logs the path).

## Baseline Snapshot (2025-12-08)

The latest Release run (Core i9-13900KS, .NET 10.0.100) produced the following metrics:

| Scenario | Mean | Allocated |
|----------|------|-----------|
| `Send_Command_WithInstrumentation` | 1.352 μs | 4.50 KB |
| `Publish_Notification_WithMultipleHandlers` | 0.987 μs | 2.38 KB |

Artifacts reside at `artifacts/performance/2025-12-08.134404/`. Update this table whenever the Encina pipeline or hardware baseline changes materially.

### Trend History

- **Aggregated table**: `docs/data/benchmark-history.md` lists every captured run (timestamp, scenario, mean, allocations). Regenerate it with `dotnet run --file scripts/aggregate-performance-history.cs` after uploading fresh CSV snapshots.
- **CI linkage**: when the workflow publishes the `benchmark-report` artifact, download the CSVs or wire a follow-up job to run the aggregator so the Markdown stays current without manual intervention.
- **Annotation tip**: prepend notable environment changes (SDK bump, hardware swap) to the Markdown entry before committing so deviations remain explainable at a glance.

## Regression Thresholds & Enforcement

Thresholds now track the 2025-12-08 baseline with a 15% latency window and 25% allocation window enforced inside CI via `scripts/check-benchmarks.cs`. Any run exceeding the limits fails the pipeline and writes a Markdown summary to the job log for quick triage:

- `Send_Command_WithInstrumentation`: mean must stay < 1.56 μs and allocations < 5.63 KB (1.352 μs / 4.50 KB × 1.15 / 1.25).
- `Publish_Notification_WithMultipleHandlers`: mean must stay < 1.14 μs and allocations < 2.98 KB (0.987 μs / 2.38 KB × 1.15 / 1.25).

Both limits apply to the `Mean` and `Allocated` columns exported by BenchmarkDotNet’s CSV reporter. The check script accepts `--directory <path>` when validating a historical run.

Over time we plan to refine the limits with percentile-based thresholds once enough datapoints exist, documenting any environment-specific adjustments here.

### CI Integration

The CI workflow executes:

```pwsh
dotnet run --file scripts/run-benchmarks.cs
dotnet run --file scripts/check-benchmarks.cs -- --directory $env:BENCHMARK_DIR
```

The first command emits `benchmark-dir=<full path>` to `GITHUB_OUTPUT`. The second consumes the directory, enforces the limits, and appends per-scenario results to `GITHUB_STEP_SUMMARY`. Artifacts are uploaded under the `benchmark-report` name for later comparison.

### Capturing Trends

- Download the `benchmark-report` artifact from successive CI runs and append CSV snapshots into `artifacts/performance/`.
- Execute `dotnet run --file scripts/aggregate-performance-history.cs` (locally or as a CI post-step) to regenerate the Markdown tables under `docs/data/` for both benchmarks and load harness runs.
- Review `docs/data/benchmark-history.md` during code review to quickly spot latency/allocation drift and decide whether thresholds should tighten or a regression investigation is needed.
- Update this guide’s baseline section once the history shows sustained improvements so the documented expectations stay aligned with reality.

## Follow-Up

- Automate wiring of `scripts/aggregate-performance-history.cs` inside CI so trend Markdown refreshes without manual intervention.
- Document tuning recommendations (DI scope reuse, behavior ordering) based on findings.
- Pair benchmark data with the new load harness (see `LOAD_TESTING.md`) to correlate latency regressions with throughput drops.
