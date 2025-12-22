# Encina Load Testing Guide

## Objectives

- Stress Encina send/publish flows to observe throughput and stability under sustained concurrency.
- Capture baseline CPU and memory observations for future regression comparisons.

## Tooling

- Project: `load/Encina.LoadTests` (console harness targeting .NET 10.0).
- NBomber project: `load/Encina.NBomber` for profile-driven scenarios.
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

Each worker resolves `IEncina` per iteration and issues either a synthetic command or notification. Results are aggregated into a summary that includes total operations, success/failure counts, mean latency, and throughput estimates.

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

- **8×4 baseline**: `collect-load-metrics.cs -- --duration 00:01:00 --send-workers 8 --publish-workers 4` now averages roughly `2.6%` CPU on hosted Windows 2025 agents after the November image refresh, so the guardrail has been relaxed to `3%` while working set remains near `70 MB` (see `docs/data/load-history.md`, run `2025-12-08 16:39:48 UTC`).
- **32×16 stress (local 2025-12-08)**: `collect-load-metrics.cs -- --duration 00:01:00` yielded send throughput `7.70 M` ops/sec (P50 `7.47 M`, P95 `9.12 M`) and publish throughput `3.11 M` ops/sec (P50 `3.37 M`, P95 `3.71 M`) with peak working set `59.5 MB`. CPU sampling stayed well below the previous 2% cap even after tightening the guardrails.
- **Reference**: the aggregated history in `docs/data/load-history.md` now surfaces CPU, memory, and send/publish throughput for every run, including early smoke checks and the stabilized high-concurrency profile.
- **CPU/memory thresholds**: adjust guardrails by setting `Encina_LOAD_MAX_MEAN_CPU` and/or `Encina_LOAD_MAX_PEAK_MB` before invoking `check-load-metrics.cs` (CLI arguments still take precedence for ad-hoc runs). The default CPU ceiling is `3%` to account for the latest hosted-runner profile.
- **Throughput guardrails**: enforce minimum send/publish throughput via env vars (`Encina_LOAD_MIN_SEND_MEAN_OPS`, `Encina_LOAD_MIN_SEND_P50_OPS`, `Encina_LOAD_MIN_SEND_P95_OPS`, and the analogous `Encina_LOAD_MIN_PUBLISH_*` keys) or CLI flags (`--min-send-mean-ops`, `--min-send-p50-ops`, etc.). CI now targets the hosted-runner envelope (send mean/P50/P95 ≥ 1.80 M / 1.70 M / 2.00 M ops/sec, publish mean/P50/P95 ≥ 0.25 M / 0.20 M / 0.30 M ops/sec) to leave room for variance while still catching regressions.
- **Config file**: check the repo’s baseline thresholds at `ci/load-thresholds.json` (currently 1.80 M / 1.70 M / 2.00 M for send mean/P50/P95 and 0.25 M / 0.20 M / 0.30 M for publish mean/P50/P95) and pass them with `--config ci/load-thresholds.json`. CLI flags override env vars, which in turn override the config file defaults.
- **Percentiles**: the load harness samples per-second throughput; `aggregate-performance-history.cs` records send/publish P50 and P95 so regressions in stability (not only averages) are visible in `docs/data/load-history.md` and CI summaries.
- **CI summary**: `check-load-metrics.cs` scans the matching harness log and prints send/publish mean throughput plus P50/P95 breakdowns, making the GitHub summary actionable without chasing artifacts.
- **Error snippets**: when the harness captures sample send/publish failures they are echoed directly in the CI summary, so triage starts with the precise pipeline error(s) that surfaced under load.
- **Headroom guidance**: treat `3%` CPU and `160 MB` working set as alert thresholds on hosted Windows 2025 agents; exceeding either warrants investigation into new handlers or pipeline changes (override via env/CLI switches for different environments).
- **Fix note**: prior `AccessViolationException` crashes at 32×16 were traced to reflection dispatch; `Encina` now uses cached request handler wrappers, eliminating the issue.

## NBomber Harness

Launch the profile-based scenarios through the same helper script:

```pwsh
dotnet run --file scripts/run-load-harness.cs -- --nbomber send-burst
```

- `--nbomber <alias>` maps to a JSON profile stored under `load/profiles/`. Use `send-burst` for the command-only sweep or `mixed-traffic` to run publish/send scenarios side-by-side. Pass `--profile <path>` to reference a custom JSON manifest directly.
- Override defaults with the same switches exposed by the harness itself (`--scenario`, `--duration`, `--send-rate`, `--publish-rate`, `--reporting-interval`, `--output`). CLI flags take precedence over profile data.
- Reports land in `artifacts/load-metrics/nbomber-<timestamp>/` by default. Reuse the path via `--output <directory>` when wiring CI steps that need deterministic artifact locations.
- Each run emits:
  - `nbomber-report.csv/html/md/txt` — raw scenario stats straight from NBomber.
  - `nbomber-summary.json` — condensed throughput/latency snapshot for automation.
  - `harness-<timestamp>.log` — formatted lines (`Send throughput`, `Send throughput P50`, etc.) consumed by `scripts/check-load-metrics.cs` and the history aggregator.
  - `metrics-<timestamp>.csv` — placeholder CPU/memory sample to keep the CSV schema consistent with the console harness (values remain empty until process counters plug in).
- NBomber prints a per-scenario summary (latency buckets, RPS, failure counts) to stdout and persists HTML/CSV artifacts; the helper script forwards the final 50 lines into the GitHub summary when run inside CI.

GitHub Actions now executes `scripts/run-load-harness.cs -- --nbomber send-burst --duration 00:00:30` after the console guardrail check, so CI uploads both harness outputs under `artifacts/load-metrics/` and the workflow summary includes the NBomber tail logs.

Use `dotnet run --file scripts/summarize-nbomber-run.cs [-- --directory <path>]` to parse the generated `nbomber-summary.json` and emit a concise throughput/latency report (the script automatically targets the most recent `nbomber-*` directory and appends to `$GITHUB_STEP_SUMMARY` when available). Pass `--thresholds <json>` to fail the run when metrics fall outside expected envelopes; CI points this to `ci/nbomber-thresholds.json`, which currently enforces ≥6.75 M ops/sec throughput and ≤0.85 ms latency for the send-burst profile.

### Profile Layout

Profiles capture duration, warm-up, throughput targets, and reporting cadence:

```json
{
  "scenario": "mixed-traffic",
  "duration": "00:01:00",
  "warmUp": "00:00:10",
  "sendRate": 180000,
  "publishRate": 90000,
  "reportingInterval": "00:00:10"
}
```

- Add new profiles under `load/profiles/nbomber.<name>.json` to keep CI-friendly baselines alongside bespoke experiment runs.
- The harness normalises edge cases (non-positive durations/rates) and will log the substituted defaults to prevent silent misconfiguration.

### Aggregation & Thresholds

- `scripts/aggregate-performance-history.cs` now picks up the NBomber directories automatically thanks to the generated `metrics-*` CSV and `harness-*` logs.
- Throughput percentiles currently reuse the mean RPS values because NBomber does not expose per-second percentile snapshots yet; the JSON summary retains latency percentiles so we can derive richer metrics once upstream support lands.

## Next Steps

- Harden the console harness against `AccessViolationException` when using higher worker counts (e.g., 32/16) so stress levels can scale safely.
- Capture NBomber baseline artefacts (send-burst and mixed-traffic) and feed their throughput/latency summaries into the history aggregator.
- Monitor the enforced send-burst thresholds (`ci/nbomber-thresholds.json`) and tighten or extend them (e.g., mixed-traffic, percentile limits) as variance data accumulates; consider mirroring the checks in `check-load-metrics.cs` if additional reports need to fail early.
- Consider introducing configurable request/notification handlers that simulate I/O delays for broader coverage.
