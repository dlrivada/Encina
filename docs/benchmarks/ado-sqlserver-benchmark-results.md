# Encina.ADO.SqlServer — Benchmark Results

> **Date**: 2026-04-10
> **Package**: `Encina.ADO.SqlServer`
> **Project**: `tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks`
> **Backing store**: `mcr.microsoft.com/mssql/server:2022-latest` via Testcontainers

These numbers establish the first baseline for the Encina ADO.NET SQL Server provider. They were captured on a development workstation to unblock the benchmark-coverage gate on `Encina.ADO.SqlServer`. CI baselines (Ubuntu runner) will replace them on the next scheduled publish.

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

Measures the baseline connection-pool checkout cost plus one-row read/write against a live SQL Server 2022 container through `Microsoft.Data.SqlClient`.

<!-- docref-table: bench:ado-sqlserver/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **OpenAndClose** (baseline) | 488.1 us | 1,448 B | `bench:ado-sqlserver/connection-open` | `SqlConnection.Open` + `SELECT 1` + dispose |
| **GetByIdAsync** | 489.7 us | 7,176 B | `bench:ado-sqlserver/repo-get-by-id` | `FunctionalRepositoryADO.GetByIdAsync` on a pre-seeded row |
| **AddAsync** | 5.71 ms | 6,495 B | `bench:ado-sqlserver/repo-add` | `FunctionalRepositoryADO.AddAsync` (single INSERT + read-back) |
| **AddAsync_Single** | 5.66 ms | 6,556 B | `bench:ado-sqlserver/outbox-insert` | `OutboxStoreADO.AddAsync` (single message append) |
| **GetPendingMessagesAsync_Batch100** | 857.7 us | 71,697 B | `bench:ado-sqlserver/outbox-query-pending` | `OutboxStoreADO.GetPendingMessagesAsync` fetching 100 rows |
| **BulkInsert_100Rows** | 11.26 ms | 103,592 B | `bench:ado-sqlserver/bulk-insert-100` | `BulkOperationsADO.BulkInsertAsync` via `SqlBulkCopy` |
<!-- /docref-table -->

**Key observations**:

- **SQL Server single-row writes are expensive** (~5.7 ms per insert) because each `INSERT` takes a full round-trip over TDS even on localhost. This is ~14x slower than PostgreSQL for the same operation.
- **Reads are fast**: `GetByIdAsync` lands at ~490 us, comparable to the connection-open baseline — meaning the actual read is nearly free on top of the round-trip cost.
- **`SqlBulkCopy` is slower than expected here** (11.3 ms for 100 rows, vs 808 us for PostgreSQL's binary `COPY`). The fixed setup cost of `SqlBulkCopy` dominates at small batch sizes; it pays off at 10K+ rows in real workloads. For 100-row batches, the repository path is actually faster per row.
- **`GetPendingMessagesAsync`** reads 100 outbox rows in 858 us with 71 KB of managed allocations (~700 bytes per row materialized) — more allocation-heavy than PostgreSQL (41 KB) but comparable mean.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/ado-sqlserver
```

Filter to a subset:

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks `
  -- --job short --filter "*Outbox*"
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.ADO.SqlServer.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Shared ADO infrastructure: `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/`
