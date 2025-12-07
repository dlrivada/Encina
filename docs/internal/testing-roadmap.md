# SimpleMediator Quality & Testing Roadmap

Last updated: 2025-12-08

## Overview

This roadmap tracks the effort to bring SimpleMediator to the same multi-layered testing maturity as the vacations pilot. It captures scope, current status, and next actions so the work can advance incrementally without losing context.

## Objectives

- Establish comprehensive automated coverage across unit, property-based, contract, integration, mutation, and performance suites.
- Provide reproducible tooling (coverage, benchmarks, mutation) with documented execution steps and quality gates.
- Maintain a live log of progress so each iteration updates status, outcomes, and upcoming work.

## Workstreams & Status

| Area | Scope | Owner | Status | Notes |
|------|-------|-------|--------|-------|
| Foundation | Relocate imported reference docs, update `.gitignore`, create roadmap doc | Copilot | ✅ Done | Zip moved to `.backup/`, roadmap created |
| Coverage Baseline | Collect current `dotnet test` coverage report and archive results | Copilot | ✅ Done | Release run (2025-12-06) reached 89.1% line / 68.4% branch; targeted Error tests on 2025-12-07 pushed line coverage to 90% and the CI workflow now publishes HTML/Text summaries plus the README badge sourced from `artifacts/coverage` |
| Unit Expansion | Increase coverage for mediator core, behaviors, metrics | Copilot | ⏳ Planned | Target ≥90% lines for `SimpleMediator` namespace |
| Property Tests | Create `SimpleMediator.PropertyTests` with FsCheck generators | Copilot | 🚧 In progress | Configuration, pipeline determinism, and notification publish ordering covered |
| Contract Tests | Ensure handlers/behaviors honor interfaces across implementations | Copilot | 🚧 In progress | DI registrations cover pipelines, handlers, processors, and multi-assembly edge cases |
| Mutation Testing | Configure Stryker.NET thresholds and CI integration | Copilot | 🚧 In progress | CI pipeline now runs `scripts/run-stryker.cs`, publishes the latest report artifact, and enforces the 92.37% baseline (448 killed / 0 survived). |
| Performance Benchmarks | Add BenchmarkDotNet project & publish results doc | Copilot | 🚧 In progress | Baseline run captured 2025-12-08 with reports under `artifacts/performance/2025-12-08.000205`; next step is documenting guidance. |
| Load Harness | Prototype NBomber (or console) throughput tests | Copilot | ⏳ Planned | Document CPU/memory requirements |
| Documentation | Publish guides (`docs/en/guides`) & requirements mapping | Copilot | 🚧 In progress | Testing, requirements, mutation, and performance guide skeletons committed; automation scripts must be single-file C# apps executed via `dotnet run --file script.cs` |

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

## Upcoming Actions

1. Monitor the first few CI mutation runs and surface the latest score via README badge automation.
2. Summarise the 2025-12-08 BenchmarkDotNet baseline in `docs/en/guides/PERFORMANCE_TESTING.md` and define regression thresholds for CI adoption.
3. Extend requirements mapping with links from roadmap items to scenario identifiers.
4. Hold line coverage at ≥90% by deepening `SimpleMediator.SimpleMediator` send/publish edge cases and pairing new unit tests with property-based explorations.

---

_Keep this roadmap updated at each milestone: add log entries, adjust statuses, and expand scope as needed._
