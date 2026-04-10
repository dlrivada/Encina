# Encina.Cdc — Benchmark Results

> **Date**: 2026-04-11
> **Package**: `Encina.Cdc`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Cdc.Benchmarks`
> **Type**: CPU-bound (no database required)

These numbers establish the first baseline for the Encina CDC core package. They cover the allocation/equality path of `ChangeEvent` + `ChangeMetadata` (hit on every captured row by every connector) and the host-startup cost of wiring the CDC pipeline via `AddEncinaCdc` + `CdcConfiguration`.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
```

## ChangeEvent / ChangeMetadata — per-row hot path

<!-- docref-table: bench:cdc/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **CreateChangeMetadata** (baseline) | 31 ns | 56 B | `bench:cdc/metadata-ctor` | Record ctor; fires once per event |
| **CreateChangeEvent** | 20 ns | 136 B | `bench:cdc/event-ctor` | Full `ChangeEvent` allocation |
| **ChangeEvent_Equals** | 1 ns | 0 B | `bench:cdc/event-equals` | Record-generated equality on unequal events |
| **ChangeEvent_WithExpression** | 13 ns | 56 B | `bench:cdc/event-with` | Non-destructive `with` update |
| **BuildConfigurationFluentChain** | 60 ns | 376 B | `bench:cdc/config-fluent-chain` | `CdcConfiguration` fluent builder chain |
| **AddEncinaCdc_Registration** | 191 ns | 976 B | `bench:cdc/add-encina-cdc` | Full `AddEncinaCdc` into a fresh `ServiceCollection` |
<!-- /docref-table -->

**Key observations**:

- **`ChangeEvent_Equals` compiles to a single allocation-free comparison** (1 ns, 0 bytes) — the record's auto-generated equality is effectively free, which is good news for deduplication paths.
- **`CreateChangeEvent` allocates 136 B** vs `CreateChangeMetadata`'s 56 B — the delta is the `ChangeEvent` record header plus the two boxed anonymous `before`/`after` objects used in the benchmark fixture.
- **`AddEncinaCdc` + `CdcConfiguration` together cost < 200 ns / < 1 KB**, so CDC wiring adds effectively no startup penalty to a host.

## How to reproduce

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Cdc.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/cdc-core
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Cdc.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Provider benchmarks: `docs/benchmarks/cdc-{mysql,postgresql,sqlserver,mongodb,debezium}-benchmark-results.md`
