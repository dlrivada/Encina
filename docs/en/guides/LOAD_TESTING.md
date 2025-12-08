# SimpleMediator Load Testing Guide

## Objectives

- Stress SimpleMediator send/publish flows to observe throughput and stability under sustained concurrency.
- Capture baseline CPU and memory observations for future regression comparisons.

## Tooling

- Project: `load/SimpleMediator.LoadTests` (console harness targeting .NET 10.0).
- Script launcher: `dotnet run --file scripts/run-load-harness.cs [-- <options>]`.

## Usage

Run the harness with optional overrides for duration and worker counts:

```pwsh
dotnet run --file scripts/run-load-harness.cs -- --duration 00:01:00 --send-workers 8 --publish-workers 4
```

### Arguments

- `--duration <TimeSpan>`: total execution window (default `00:00:30`).
- `--send-workers <int>`: parallel send loops (default = logical core count).
- `--publish-workers <int>`: parallel publish loops (default = half logical core count, minimum 1).

Each worker resolves `IMediator` per iteration and issues either a synthetic command or notification. Results are aggregated into a summary that includes total operations, success/failure counts, mean latency, and throughput estimates.

## Capturing CPU and Memory Metrics

Use the helper script to execute the harness while sampling CPU and memory:

```pwsh
dotnet run --file scripts/collect-load-metrics.cs -- --duration 00:01:00 --send-workers 8 --publish-workers 4
```

- Outputs land in `artifacts/load-metrics/` (`harness-<timestamp>.log` and `metrics-<timestamp>.csv`).
- CSV columns include timestamp, process CPU utilization, and private working set bytes. Empty `system_cpu_percent` values mean the script could not read system-wide counters (common on non-Windows environments).
- Import the CSV into a spreadsheet or notebook to chart trends and identify regressions.
- Run `dotnet run --file scripts/aggregate-performance-history.cs` to roll the collected CSV snapshots into Markdown tables under `docs/data/` (`load-history.md`, shared with benchmark history).

## Output Interpretation

The harness prints a summary similar to:

```text
=== Load Summary ===
Send → total: 9 764 163, success: 9 764 163, failures: 0
Publish → total: 13 247 612, success: 13 247 612, failures: 0
Send mean duration: 0.800 µs
Send throughput: 1 952 832.60 ops/sec
Publish mean duration: 0.500 µs
Publish throughput: 2 649 522.40 ops/sec
```

- **Mean duration**: average per-operation latency derived from aggregated stopwatch ticks.
- **Throughput**: total operations divided by run duration (includes successes and failures).
- **Sample errors**: if failures occur, up to 10 unique messages are displayed for triage.

## Baseline Resource Envelope (2025-12-08)

- **8×4 baseline**: `collect-load-metrics.cs -- --duration 00:01:00 --send-workers 8 --publish-workers 4` holds mean CPU under `0.20%` with peaks at `0.19%`; private working set stays around `69 MB` (see `2025-12-08 00:58:31 UTC` entry in `docs/data/load-history.md`).
- **32×16 stress**: after caching mediator dispatchers (Dec 8 fix), the harness now runs flat-out without crashes; CPU peaks remain below `0.20%`, working set stays in the `68–72 MB` range, and sustained throughput lands near `8.2 M` send ops/sec / `2.9 M` publish ops/sec with send P50≈`7.8 M` and P95≈`9.7 M` (latest run: `2025-12-08 10:19:29 UTC`).
- **Reference**: the aggregated history in `docs/data/load-history.md` now surfaces CPU, memory, and send/publish throughput for every run, including early smoke checks and the stabilized high-concurrency profile.
- **CPU/memory thresholds**: adjust guardrails by setting `SIMPLEMEDIATOR_LOAD_MAX_MEAN_CPU` and/or `SIMPLEMEDIATOR_LOAD_MAX_PEAK_MB` before invoking `check-load-metrics.cs` (CLI arguments still take precedence for ad-hoc runs).
- **Throughput guardrails**: enforce minimum send/publish throughput via env vars (`SIMPLEMEDIATOR_LOAD_MIN_SEND_MEAN_OPS`, `SIMPLEMEDIATOR_LOAD_MIN_SEND_P50_OPS`, `SIMPLEMEDIATOR_LOAD_MIN_SEND_P95_OPS`, and the analogous `SIMPLEMEDIATOR_LOAD_MIN_PUBLISH_*` keys) or CLI flags (`--min-send-mean-ops`, `--min-send-p50-ops`, etc.); missing log data now fails fast when thresholds are configured.
- **Config file**: check the repo’s baseline thresholds at `ci/load-thresholds.json` and pass them with `--config ci/load-thresholds.json`. CLI flags override env vars, which in turn override the config file defaults.
- **Percentiles**: the load harness samples per-second throughput; `aggregate-performance-history.cs` records send/publish P50 and P95 so regressions in stability (not only averages) are visible in `docs/data/load-history.md` and CI summaries.
- **CI summary**: `check-load-metrics.cs` scans the matching harness log and prints send/publish mean throughput plus P50/P95 breakdowns, making the GitHub summary actionable without chasing artifacts.
- **Error snippets**: when the harness captures sample send/publish failures they are echoed directly in the CI summary, so triage starts with the precise pipeline error(s) that surfaced under load.
- **Headroom guidance**: treat `1%` CPU and `100 MB` working set as alert thresholds on comparable hardware—exceeding either warrants investigation into new handlers or pipeline changes.
- **Fix note**: prior `AccessViolationException` crashes at 32×16 were traced to reflection dispatch; `SimpleMediator` now uses cached request handler wrappers, eliminating the issue.

## Next Steps

- Harden the harness against `AccessViolationException` when using higher worker counts (e.g., 32/16) so stress levels can scale safely.
- Evaluate integration into CI (potential nightly run with relaxed limits) once hardware variance guidelines are established.
- Consider introducing configurable request/notification handlers that simulate I/O delays for broader coverage.
