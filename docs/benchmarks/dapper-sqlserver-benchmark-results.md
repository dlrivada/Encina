# Encina.Dapper.SqlServer — Benchmark Results

> **Date**: 2026-04-10
> **Package**: `Encina.Dapper.SqlServer`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Dapper.SqlServer.Benchmarks`
> **Backing store**: `mcr.microsoft.com/mssql/server:2022-latest` via Testcontainers

These numbers establish the first baseline for the Encina Dapper + SQL Server provider. They were captured on a development workstation to unblock the benchmark-coverage gate on `Encina.Dapper.SqlServer`. CI baselines (Ubuntu runner) will replace them on the next scheduled publish.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers MsSql (mcr.microsoft.com/mssql/server:2022-latest)
```

## Connection & Repository — Per-Operation Cost

Measures the Dapper-over-Microsoft.Data.SqlClient overhead for connection, repository, and outbox operations against a live SQL Server 2022 container.

<!-- docref-table: bench:dapper-sqlserver/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **OpenAndExecuteScalar** (baseline) | 583.2 us | 1,536 B | `bench:dapper-sqlserver/connection-open` | `SqlConnection.Open` + Dapper `ExecuteScalar<int>("SELECT 1")` + dispose |
| **GetByIdAsync** | 536.4 us | 7,536 B | `bench:dapper-sqlserver/repo-get-by-id` | `FunctionalRepositoryDapper.GetByIdAsync` on a pre-seeded row |
| **AddAsync** | 6.07 ms | 6,689 B | `bench:dapper-sqlserver/repo-add` | `FunctionalRepositoryDapper.AddAsync` (single INSERT + read-back) |
| **AddAsync_Single** | 5.43 ms | 7,161 B | `bench:dapper-sqlserver/outbox-insert` | `OutboxStoreDapper.AddAsync` (single message append) |
| **GetPendingMessagesAsync_Batch100** | 1.25 ms | 52,961 B | `bench:dapper-sqlserver/outbox-query-pending` | `OutboxStoreDapper.GetPendingMessagesAsync` fetching 100 rows |
| **BulkInsert_100Rows** | 17.66 ms | 103,152 B | `bench:dapper-sqlserver/bulk-insert-100` | `BulkOperationsDapper.BulkInsertAsync` via `SqlBulkCopy` |
<!-- /docref-table -->

**Key observations**:

- **SQL Server single-row inserts stay expensive under Dapper too** — 5.4-6.1 ms per insert, dominated by the TDS round-trip. Compare with ADO-SqlServer (5.66 ms outbox insert): Dapper adds only a few percent overhead, the bottleneck is the protocol.
- **`SqlBulkCopy` is the slowest of all 6 database backends for 100 rows** (17.66 ms), worse than Dapper MySQL bulk (1.67 ms) by ~10x and worse than Dapper Postgres bulk (782 us) by ~22x. Dapper's mapping layer adds ~6 ms over the raw ADO-SqlServer bulk (11.26 ms). For small batches, the single-row repository path is actually faster per row.
- **`GetPendingMessagesAsync` at 1.25 ms** is the slowest of all three Dapper providers for the same workload — matches the pattern seen in the ADO benchmarks where SQL Server's 100-row materialization is consistently the heaviest (71 KB allocated on ADO, 53 KB on Dapper — Dapper is cheaper here).
- **`GetByIdAsync` is fast** (~536 us, basically the connection baseline) — SQL Server's read path on localhost is nearly free once the connection is open.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Dapper.SqlServer.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/dapper-sqlserver
```

Filter to a subset:

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Dapper.SqlServer.Benchmarks `
  -- --job short --filter "*Outbox*"
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Dapper.SqlServer.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Shared Dapper infrastructure: `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/Infrastructure/`
- ADO.NET comparison: `docs/benchmarks/ado-sqlserver-benchmark-results.md`
