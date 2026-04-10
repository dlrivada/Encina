# Encina.Cdc.MySql — Benchmark Results

> **Date**: 2026-04-11
> **Package**: `Encina.Cdc.MySql`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Cdc.MySql.Benchmarks`
> **Backing store**: `mysql:8.0` with binary logging enabled (`--log-bin=mysql-bin --binlog-format=ROW --server-id=1`)

These numbers establish the first baseline for the Encina MySQL CDC connector. They combine CPU-bound position round-trip benchmarks (hit on every change event by the connector) with a real end-to-end `GetCurrentPositionAsync` measurement against a live binlog-enabled MySQL 8 container.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers MySql (mysql:8.0, binlog + ROW + server-id=1)
```

> **Why MySQL 8.0 and not 9.1?** The connector uses `SHOW MASTER STATUS`, which was removed in MySQL 9.0 in favor of `SHOW BINARY LOG STATUS`. The ADO / Dapper MySQL benchmark projects use MySQL 9.1, but the CDC benchmarks pin to 8.0 until the connector catches up.

## Position round-trip + connector round-trip

<!-- docref-table: bench:cdc-mysql/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **CreateGtidPosition** (baseline) | 6 ns | 40 B | `bench:cdc-mysql/position-ctor` | GTID `MySqlCdcPosition` constructor |
| **ToBytes** | 342 ns | 504 B | `bench:cdc-mysql/position-tobytes` | JSON serialize (GTID + file/pos fields) |
| **FromBytes** | 286 ns | 688 B | `bench:cdc-mysql/position-frombytes` | JSON deserialize via `JsonDocument.Parse` |
| **CompareFilePositions** | 2 ns | 0 B | `bench:cdc-mysql/position-compare` | File/position ordinal comparison |
| **GetCurrentPositionAsync** | 1.24 ms | 6,622 B | `bench:cdc-mysql/get-current-position` | `SHOW MASTER STATUS` via live MySQL + `ICdcConnector` wiring |
<!-- /docref-table -->

**Key observations**:

- **`CompareFilePositions` is allocation-free and sub-nanosecond** — it's just a string compare + long compare, one of the hottest operations in the saga catch-up path.
- **Position `ToBytes` / `FromBytes` are the most expensive CPU path** (~300 ns, 500-700 B) because they go through `JsonSerializer` / `JsonDocument.Parse`. This is the lower-bound on how fast the connector can persist/resume progress.
- **`GetCurrentPositionAsync` at 1.24 ms** dominates the workstation numbers — roughly 3x slower than an equivalent `SELECT 1` on the same MySQL 8 container, because the connector also pays `MySqlConnection.OpenAsync` and the DI indirection on every call.
- **Why no `StreamChangesAsync` benchmark?** Streaming is infinite by design; to measure it we would need to inject a sustained write workload on the source and then synchronise with the consumer, which is out of scope for a coverage-gate benchmark.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Cdc.MySql.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/cdc-mysql
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Cdc.MySql.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling connectors: `docs/benchmarks/cdc-{postgresql,sqlserver,mongodb,debezium}-benchmark-results.md`
