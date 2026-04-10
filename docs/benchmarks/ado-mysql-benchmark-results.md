# Encina.ADO.MySQL — Benchmark Results

> **Date**: 2026-04-10
> **Package**: `Encina.ADO.MySQL`
> **Project**: `tests/Encina.BenchmarkTests/Encina.ADO.MySQL.Benchmarks`
> **Backing store**: `mysql:9.1` via Testcontainers

These numbers establish the first baseline for the Encina ADO.NET MySQL provider. They were captured on a development workstation to unblock the benchmark-coverage gate on `Encina.ADO.MySQL`. CI baselines (Ubuntu runner) will replace them on the next scheduled publish.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers MySql (mysql:9.1, --local-infile=1)
```

## Connection & Repository — Per-Operation Cost

Measures the baseline connection-pool checkout cost plus one-row read/write against a live MySQL 9.1 container through `MySqlConnector`.

<!-- docref-table: bench:ado-mysql/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **OpenAndClose** (baseline) | 802.5 us | 1,576 B | `bench:ado-mysql/connection-open` | `MySqlConnection.Open` + `SELECT 1` + dispose |
| **GetByIdAsync** | 458.6 us | 6,499 B | `bench:ado-mysql/repo-get-by-id` | `FunctionalRepositoryADO.GetByIdAsync` on a pre-seeded row |
| **AddAsync** | 9.21 ms | 6,672 B | `bench:ado-mysql/repo-add` | `FunctionalRepositoryADO.AddAsync` (single INSERT + read-back) |
| **AddAsync_Single** | 7.89 ms | 6,873 B | `bench:ado-mysql/outbox-insert` | `OutboxStoreADO.AddAsync` (single message append) |
| **GetPendingMessagesAsync_Batch100** | 783.4 us | 49,625 B | `bench:ado-mysql/outbox-query-pending` | `OutboxStoreADO.GetPendingMessagesAsync` fetching 100 rows |
| **BulkInsert_100Rows** | 1.16 ms | 62,032 B | `bench:ado-mysql/bulk-insert-100` | `BulkOperationsMySQL.BulkInsertAsync` via `MySqlBulkCopy` |
<!-- /docref-table -->

**Key observations**:

- **Bulk is king** — 100 rows via `MySqlBulkCopy` take 1.16 ms total (~11.6 us per row), while the single-row repository `AddAsync` path averages 9.21 ms per row — **~800x faster per row** for batch loads.
- **Outbox insert is in the same ballpark as the repository insert** (7.89 vs 9.21 ms): both pay one round-trip for the `INSERT`, confirming there is no extra overhead from the outbox abstraction.
- **`GetPendingMessagesAsync` scales sub-linearly** in allocations: 100 rows read produce 49 KB of managed allocations, ~500 bytes per row.
- **The connection-pool baseline** (`OpenAndClose`) is ~800 us. Any repository operation under that floor would be suspicious.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.ADO.MySQL.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/ado-mysql
```

Filter to a subset:

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.ADO.MySQL.Benchmarks `
  -- --job short --filter "*Outbox*"
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.ADO.MySQL.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Shared ADO infrastructure: `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/`
