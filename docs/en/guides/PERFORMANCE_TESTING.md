# SimpleMediator Performance Testing Guide

## Objectives

- Quantify mediator send/publish overhead using BenchmarkDotNet.
- Surface allocation profiles and latency buckets for common request flows.

## Tooling Plan

- Project scaffold: `benchmarks/SimpleMediator.Benchmarks` (BenchmarkDotNet, net10.0).
- Scenarios: command send with instrumentation, notification publish across multi-handlers.

## Execution

- Restore dependencies: `dotnet restore SimpleMediator.slnx`.
- Run suites with `dotnet run -c Release --project benchmarks/SimpleMediator.Benchmarks/SimpleMediator.Benchmarks.csproj`.
- Persist results under `artifacts/performance/<timestamp>/` for historical comparison.

## Baseline Snapshot (2025-12-08)

The first Release run captured the following medians on the local dev rig (Core i9-13900KS, .NET 10.0.100):

| Scenario | Mean | Allocated |
|----------|------|-----------|
| `Send_Command_WithInstrumentation` | ~2.02 μs | ~4.82 KB |
| `Publish_Notification_WithMultipleHandlers` | ~1.01 μs | ~2.38 KB |

CSV/HTML summaries live at `artifacts/performance/2025-12-08.000205/` and should be updated whenever the mediator pipeline changes materially.

## Follow-Up

- Capture baseline numbers and attach CSV/markdown summaries alongside HTML results.
- Add perf regression thresholds to CI once baseline data exists.
- Document tuning recommendations (DI scope reuse, behavior ordering) based on findings.
