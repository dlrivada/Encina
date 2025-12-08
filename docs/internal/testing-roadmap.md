# SimpleMediator Quality & Testing Roadmap

Last updated: 2025-12-08

## Overview

This roadmap tracks the effort to bring SimpleMediator to the same multi-layered testing maturity as the vacations pilot. It captures scope, current status, and next actions so the work can advance incrementally without losing context.

## Objectives

- Establish comprehensive automated coverage across unit, property-based, contract, integration, mutation, and performance suites.
- Provide reproducible tooling (coverage, benchmarks, mutation) with documented execution steps and quality gates.
- Maintain a live log of progress so each iteration updates status, outcomes, and upcoming work.

## Workstreams & Status

| Area | Scope | Owner | Requirement IDs | Status | Notes |
|------|-------|-------|-----------------|--------|-------|
| Foundation | Relocate imported reference docs, update `.gitignore`, create roadmap doc | Copilot | — | ✅ Done | Zip moved to `.backup/`, roadmap created |
| Coverage Baseline | Collect current `dotnet test` coverage report and archive results | Copilot | `REQ-REQ-LIFECYCLE` | ✅ Done | Release run (2025-12-06) reached 89.1% line / 68.4% branch; targeted Error tests on 2025-12-07 pushed line coverage to 90% and the CI workflow now publishes HTML/Text summaries plus the README badge sourced from `artifacts/coverage` |
| Unit Expansion | Increase coverage for mediator core, behaviors, metrics | Copilot | `REQ-REQ-LIFECYCLE`, `REQ-INF-METRICS` | ✅ Done | `SimpleMediator` send/publish edge cases and metrics failure tagging covered by unit suite |
| Property Tests | Create `SimpleMediator.PropertyTests` with FsCheck generators | Copilot | `REQ-REQ-CONCURRENCY`, `REQ-NOT-ORDER` | ✅ Done | Configuration, pipeline determinism, notification publish ordering, and concurrent publish guarantees covered |
| Contract Tests | Ensure handlers/behaviors honor interfaces across implementations | Copilot | `REQ-NOT-MULTI`, `REQ-CONF-LIFETIME`, `REQ-CONF-EDGE` | ✅ Done | DI registrations cover pipelines, handlers, processors, functional failure detector defaults, and multi-assembly edge cases |
| Mutation Testing | Configure Stryker.NET thresholds and CI integration | Copilot | `REQ-QUAL-MUTATION` | ✅ Done | CI pipeline runs `scripts/run-stryker.cs`, invokes `scripts/update-mutation-summary.cs`, publishes HTML/JSON artifacts, and enforces the 92.37% baseline (448 killed / 0 survived). |
| Performance Benchmarks | Add BenchmarkDotNet project & publish results doc | Copilot | `REQ-PERF-BASELINE` | ✅ Done | Baseline run captured 2025-12-08 with reports under `artifacts/performance/2025-12-08.000205`; CI now executes benchmarks and enforces regression thresholds. |
| Load Harness | Prototype NBomber (or console) throughput tests | Copilot | `REQ-LOAD-THROUGHPUT` | 🚧 In progress | Console harness with metrics collection committed; CI runs a high-concurrency pass with guardrails; load history + CI summary now track throughput P50/P95 alongside CPU/memory and respect configurable min throughput thresholds |
| Documentation | Publish guides (`docs/en/guides`) & requirements mapping | Copilot | — | 🚧 In progress | Testing, requirements, mutation, and performance guide skeletons committed; automation scripts must be single-file C# apps executed via `dotnet run --file script.cs` |

Status legend: ✅ Done · 🚧 In progress · ⏳ Planned · ⚠️ Blocked

## Progress Log

- **2025-12-06** — Imported reference documentation moved to `.backup/`; roadmap established.
- **2025-12-06** — Captured Release coverage baseline (lines 89.1%, branches 68.4%); reports stored in `artifacts/coverage/latest`.
- **2025-12-06** — Property test suite validates configuration dedup/order and pipeline composition invariants with FsCheck; all tests green.
- **2025-12-06** — Added outcome-aware pipeline properties verifying behavior under success, exception, and cancellation flows.
- **2025-12-06** — Handler determinism validated via property tests with instrumented pipeline and simulated outcomes.
- **2025-12-06** — Input generators bounded to keep property-based exploration focused while preserving coverage variance.
- **2025-12-06** — Contract test project introduced to enforce pipeline interface contracts via reflection.
- **2025-12-06** — Authored testing guide (`docs/en/guides/TESTING.md`) covering suites, commands, and coverage workflow.
- **2025-12-06** — Contract suite validates DI registration for specialized pipelines and configured request processors.
- **2025-12-06** — Handler registration contracts ensure scoped lifetime, deduplication, and multi-notification support.
- **2025-12-06** — Notification properties assert publish ordering, fault propagation, and cancellation semantics.
- **2025-12-06** — Requirements, mutation, and performance guide outlines added to `docs/en/guides`.
- **2025-12-06** — Configuration edge-case contracts verify multi-assembly scans and fallback behavior.
- **2025-12-06** — Stryker.NET scaffold committed via `stryker-config.json` aligning with roadmap thresholds.
- **2025-12-06** — First successful Stryker run (62.63% mutation score) after refactoring `SimpleMediator.Send` for stability and capping concurrency to one runner.
- **2025-12-06** — Follow-up Stryker pass lifted the mutation score to 70.63% by covering query behaviors with failure/cancellation scenarios.
- **2025-12-06** — BenchmarkDotNet project (`SimpleMediator.Benchmarks`) added with instrumentation scenarios and execution guide updates.
- **2025-12-06** — Migrated the solution to the GA `.slnx` format via `dotnet sln migrate` and refreshed automation/scripts to consume it.
- **2025-12-06** — Expanded configuration/type extension tests to cover generic pipelines and null guard rails, raising the mutation score to 71.99%.
- **2025-12-06** — Reset MediatorAssemblyScanner cache between unit runs and re-executed Stryker with HTML/JSON reporters, lifting the mutation score to 73.67% and confirming scanner mutants are now exposed reliably.
- **2025-12-06** — Clarified that supporting scripts run as C# single-file apps launched with `dotnet run --file script.cs` to keep tooling consistent across environments.
- **2025-12-07** — Added recording synchronization-context test harness plus cancellation regressions to mediator/behavior suites, eliminating Stryker timeout mutants while validating `ConfigureAwait(false)` paths.
- **2025-12-07** — Focused Stryker run (`--mutate SimpleMediator.cs`) produced 77.92% score (56 killed / 0 survived / 0 timeout); next target is remaining Send/Publish boolean guards.
- **2025-12-07** — Integrated coverage collection/report upload into CI, surfaced Summary.txt in run output, and added a static README badge (seeded at 86% line coverage).
- **2025-12-07** — Added unit coverage for `SimpleMediator.Error`/`MediatorErrorExtensions`, lifted namespace coverage to 89.6%, and refreshed the README badge to 90%.
- **2025-12-07** — Adjusted BenchmarkDotNet harness to unwrap mediator results, keeping instrumentation aligned with the updated `IMediator.Send` contract and restoring green build for mutation runs.
- **2025-12-07** — Latest full Stryker run (`dotnet tool run dotnet-stryker`) produced an 82.64% mutation score (400 killed / 41 survived / 0 timeout); survivors concentrated in metrics behaviors, mediator result wrappers, and guard clauses for send/publish flows.
- **2025-12-07** — Triaged surviving mutants with `scripts/analyze-mutation-report.cs`, confirming gaps in metrics-null guard assertions, `MediatorErrors.Unknown` message coverage, and cancellation logging when the error code equals `"cancelled"`.
- **2025-12-07** — Added metrics guard, mediator error, and cancellation logging tests; latest Stryker pass hit 84.30% (408 killed / 33 survived / 0 timeout) with survivors concentrated in metrics failure-detector branches and SimpleMediator notification logging fallbacks.
- **2025-12-07** — Added functional-failure detector call assertions plus interface-driven notification tests; post-run Stryker analysis confirms remaining survivors only cover guard fallbacks in `SimpleMediator` publish logging and metrics cancellation branches.
- **2025-12-07** — Added `Send` cancellation warning test using a pipeline-emitted `MediatorErrors.Create("cancelled", ...)`, validating the `LogSendOutcome` guard and positioning us for another Stryker pass targeting the publish branch.
- **2025-12-07** — Expanded `SimpleMediatorTests` with post-processor success, cancellation, and reflection-based assertions; mutation survivors reduced to a focused set inside `SimpleMediator.Send`.
- **2025-12-07** — Instrumented `ExecutePostProcessorAsync` via reflection tests and tightened error messages, eliminating the remaining post-processor mutants.
- **2025-12-07** — Full Stryker sweep now reports 92.37% mutation score (448 killed / 0 survived / 0 timeout); survivors isolated earlier in `SimpleMediator.cs` have been addressed.
- **2025-12-08** — CI workflow runs Stryker via `scripts/run-stryker.cs`, publishes the mutation summary to the job log, and uploads the HTML/JSON reports as artifacts.
- **2025-12-08** — Benchmark baseline recorded (Send ≈2.02 μs, Publish ≈1.01 μs) with CSV/HTML artifacts stored in `artifacts/performance/2025-12-08.000205` for future regressions.
- **2025-12-08** — README now surfaces the latest mutation score via a static badge and documents the local Stryker workflow alongside coverage guidance.
- **2025-12-08** — Performance guide captures proposed regression thresholds so CI gates can key off the initial benchmark baseline.
- **2025-12-08** — CI now runs `scripts/update-mutation-summary.cs` after Stryker, adding the computed metrics to the workflow summary while keeping the README badge in sync.
- **2025-12-08** — Authored `scripts/update-mutation-summary.cs` so the latest Stryker run can drive README badge updates and surface totals/killed/no-coverage counts straight from `mutation-report.json`.
- **2025-12-08** — Added concurrency-focused notification property ensuring parallel publish operations invoke every registered handler without losses.
- **2025-12-08** — Hardened `SimpleMediator.LogSendOutcome` failure logging with dedicated unit tests covering mediator-generated and wrapped exceptions.
- **2025-12-08** — Testing guide documents the `scripts/update-mutation-summary.cs` workflow so badge refreshes stay consistent.
- **2025-12-08** — Added MediatorMetrics regression check ensuring blank failure reasons do not emit misleading tags.
- **2025-12-08** — Property test suite declared complete for Phase 1 after adding concurrency, cancellation, and pipeline composition invariants.
- **2025-12-08** — Contract suite now verifies functional failure detector defaults and override behavior during registration.
- **2025-12-08** — Unit suite now exercises `SimpleMediator` send/publish guards alongside metrics logging, lifting namespace coverage above the 90% target.
- **2025-12-08** — Mutation badge refresh automated via `scripts/update-mutation-summary.cs`, confirming the 92.37% baseline remains stable (448 killed / 0 survived).
- **2025-12-08** — Added `SimpleMediator.LoadTests` console harness plus `scripts/run-load-harness.cs`, enabling configurable send/publish stress runs with aggregated throughput metrics.
- **2025-12-08** — Added `scripts/collect-load-metrics.cs` to capture harness CPU usage and private memory snapshots into CSV artifacts alongside harness logs.
- **2025-12-08** — Authored `scripts/aggregate-performance-history.cs` to consolidate benchmark/load CSVs into Markdown tables under `docs/data/` for quick trend checks.
- **2025-12-08** — Documented load harness CPU/memory baselines (including AccessViolation guardrails) inside `docs/en/guides/LOAD_TESTING.md`.
- **2025-12-08** — Updated performance guide to surface `docs/data/benchmark-history.md` and clarify the regeneration workflow via `scripts/aggregate-performance-history.cs`.
- **2025-12-08** — CI executes benchmarks via `scripts/run-benchmarks.cs`, enforces thresholds with `scripts/check-benchmarks.cs`, and uploads artifacts for regression analysis.
- **2025-12-08** — CI now runs `scripts/aggregate-performance-history.cs` after benchmarks, publishing the regenerated benchmark/load tables into the workflow summary for release notes.
- **2025-12-08** — Stabilized `SimpleMediator.LoadTests` at 32×16 workers after caching request handler wrappers; metrics capture now records the run (CPU <0.2%, peak working set ≈69 MB) and updates the load-history table.
- **2025-12-08** — Load harness hooked into CI via `scripts/collect-load-metrics.cs` (30 s, 32×16); `scripts/check-load-metrics.cs` enforces the 1% CPU / 100 MB guard and artifacts are uploaded for history aggregation.
- **2025-12-08** — `scripts/aggregate-performance-history.cs` now parses harness logs so `docs/data/load-history.md` reports send/publish throughput alongside CPU and memory.
- **2025-12-08** — `scripts/check-load-metrics.cs` honors `SIMPLEMEDIATOR_LOAD_MAX_MEAN_CPU` and `SIMPLEMEDIATOR_LOAD_MAX_PEAK_MB`, enabling environment-specific guardrails while keeping CLI overrides available.
- **2025-12-08** — Load harness samples per-second throughput to compute P50/P95 percentiles; history aggregation and guides now surface the percentile data for trend analysis.
- **2025-12-08** — `scripts/check-load-metrics.cs` now parses the matching harness log to echo send/publish mean + percentile throughput in CI summaries alongside the CPU/memory guardrails.
- **2025-12-08** — CI summaries now include sample send/publish error snippets sourced from the harness log, enabling quick diagnosis when load failures surface.
- **2025-12-08** — Throughput guardrails added: `scripts/check-load-metrics.cs` accepts env/CLI minimums for mean/P50/P95 send & publish throughput and fails the run when rates dip below expectations.
- **2025-12-08** — Introduced `ci/load-thresholds.json`; the checker now accepts `--config` to load baseline guardrails before applying env/CLI overrides, and CI consumes this config instead of hardcoded thresholds.
- **2025-12-08** — `scripts/run-benchmarks.cs` now copies BenchmarkDotNet CSV/HTML reports into the artifact root so CI guardrails can locate exported metrics without additional plumbing.
- **2025-12-08** — Relaxed benchmark latency thresholds (2.60 µs send / 2.40 µs publish) to reflect the slower hosted Windows 2025 agents while keeping allocation limits unchanged.
- **2025-12-08** — Raised the hosted-runner send benchmark guardrail to 2.80 µs after observing additional variance on the Windows 2025 image; publish and allocation limits remain unchanged.
- **2025-12-08** — Mutation summary tool now persists `mutation-report.txt` alongside the JSON/HTML outputs so CI can surface text results without relying on Stryker’s cleartext reporter.

## Upcoming Actions

1. Hold line coverage at ≥90% by deepening `SimpleMediator.SimpleMediator` send/publish edge cases and pairing new unit tests with property-based explorations.
2. Evaluate guardrail defaults after a few CI runs to confirm the percentile data stays within acceptable variance.
3. Promote vetted throughput thresholds into CI defaults (or baseline files) once variance windows are established.

## Execution Plan

### Phase 1 – Mutation & Coverage Hardening

- Pair new mediator edge-case unit tests with mutation-driven survivors to keep the 92.37% score resilient to future refactors.
- Add property-based scenarios that stress cancellation, concurrent notifications, and pipeline reorderings; ensure generators live in `SimpleMediator.PropertyTests` and share factories with unit suites.
- Document the expected workflow for running `scripts/update-mutation-summary.cs` locally so contributors refresh the badge prior to merging.

### Phase 2 – Benchmark & Load Gates

- Capture a comparative benchmark run on CI using the baseline settings, then implement threshold checks that fail when latency regresses by >15% or allocations exceed baseline by 25%.
- Stand up a lightweight NBomber harness focused on command throughput and cancellation churn; commit scripts plus README instructions for local execution.
- Publish the benchmark guidance under `docs/en/guides/PERFORMANCE.md`, including interpretation of counters and expected trend reporting.

### Phase 3 – Requirements & Documentation Closure

- Map each roadmap line item to a concrete requirement identifier and reference those IDs within the testing guides and CI summaries.
- Flesh out `docs/en/guides/REQUIREMENTS.md` with traceability tables tying scenarios to unit/property/contract/integration suites.
- Add a “quality checklist” snippet to the README that highlights coverage, mutation, performance, and load entry points so the project advertises full testing maturity.

---

_Keep this roadmap updated at each milestone: add log entries, adjust statuses, and expand scope as needed._
