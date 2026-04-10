# Encina.Cdc.SqlServer — Benchmark Results

> **Date**: 2026-04-11
> **Package**: `Encina.Cdc.SqlServer`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Cdc.SqlServer.Benchmarks`
> **Backing store**: `mcr.microsoft.com/mssql/server:2022-latest` with SQL Server **Change Tracking** enabled on a dedicated database

These numbers establish the first baseline for the Encina SQL Server CDC connector. They combine CPU-bound position round-trip benchmarks with a real end-to-end `GetCurrentPositionAsync` measurement against a live Change-Tracking-enabled SQL Server 2022 container.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers MsSql (mcr.microsoft.com/mssql/server:2022-latest, Change Tracking)
```

> **Why Change Tracking and not full CDC?** The Encina SQL Server CDC connector uses SQL Server's lightweight Change Tracking feature (`CHANGE_TRACKING_CURRENT_VERSION()`), not the heavyweight Change Data Capture feature. Change Tracking just needs `ALTER DATABASE ... SET CHANGE_TRACKING = ON` and works out of the box on the Linux Developer edition that the Testcontainers image ships. Full CDC requires the SQL Server Agent, which is not available on Linux.

## Position round-trip + connector round-trip

<!-- docref-table: bench:cdc-sqlserver/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **CreatePosition** (baseline) | 5 ns | 24 B | `bench:cdc-sqlserver/position-ctor` | `SqlServerCdcPosition` over an `int64` CT version |
| **ToBytes** | 6 ns | 32 B | `bench:cdc-sqlserver/position-tobytes` | Fixed-size 8-byte big-endian write |
| **FromBytes** | 5 ns | 24 B | `bench:cdc-sqlserver/position-frombytes` | Fixed-size 8-byte big-endian read |
| **ComparePositions** | 2 ns | 0 B | `bench:cdc-sqlserver/position-compare` | `long.CompareTo` |
| **GetCurrentPositionAsync** | 685 us | 3,896 B | `bench:cdc-sqlserver/get-current-position` | `SELECT CHANGE_TRACKING_CURRENT_VERSION()` via live SQL Server + `ICdcConnector` wiring |
<!-- /docref-table -->

**Key observations**:

- **The SQL Server position is structurally identical to the PostgreSQL one** — both wrap a fixed 8-byte value and use `BinaryPrimitives` for serialization, so the CPU numbers are practically indistinguishable (5-6 ns / 24-32 B each).
- **`GetCurrentPositionAsync` at 685 us** is only ~25% slower than PostgreSQL (541 us) despite SQL Server's reputation for a heavier connection-open path — Change Tracking is a lightweight system function that avoids the catalog lookups required by full CDC.
- **Allocation footprint (3,896 B) is larger than PostgreSQL (2,234 B)** because the `Microsoft.Data.SqlClient` connection pool keeps a richer pool entry per open connection than Npgsql does.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Cdc.SqlServer.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/cdc-sqlserver
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Cdc.SqlServer.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling connectors: `docs/benchmarks/cdc-{mysql,postgresql,mongodb,debezium}-benchmark-results.md`
