# Encina.ADO.PostgreSQL — Benchmark Results

> **Date**: 2026-04-10
> **Package**: `Encina.ADO.PostgreSQL`
> **Project**: `tests/Encina.BenchmarkTests/Encina.ADO.PostgreSQL.Benchmarks`
> **Backing store**: `postgres:17-alpine` via Testcontainers

These numbers establish the first baseline for the Encina ADO.NET PostgreSQL provider. They were captured on a development workstation to unblock the benchmark-coverage gate on `Encina.ADO.PostgreSQL`. CI baselines (Ubuntu runner) will replace them on the next scheduled publish.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers PostgreSql (postgres:17-alpine)
```

## Connection & Repository — Per-Operation Cost

Measures the baseline connection-pool checkout cost plus one-row read/write against a live PostgreSQL 17 container through Npgsql.

<!-- docref-table: bench:ado-postgresql/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **OpenAndClose** (baseline) | 334.4 us | 632 B | `bench:ado-postgresql/connection-open` | `NpgsqlConnection.Open` + `SELECT 1` + dispose |
| **GetByIdAsync** | 415.6 us | 10,988 B | `bench:ado-postgresql/repo-get-by-id` | `FunctionalRepositoryADO.GetByIdAsync` on a pre-seeded row |
| **AddAsync** | 458.0 us | 15,720 B | `bench:ado-postgresql/repo-add` | `FunctionalRepositoryADO.AddAsync` (single INSERT + read-back) |
| **AddAsync_Single** | 398.0 us | 5,359 B | `bench:ado-postgresql/outbox-insert` | `OutboxStoreADO.AddAsync` (single message append) |
| **GetPendingMessagesAsync_Batch100** | 608.1 us | 41,173 B | `bench:ado-postgresql/outbox-query-pending` | `OutboxStoreADO.GetPendingMessagesAsync` fetching 100 rows |
| **BulkInsert_100Rows** | 808.2 us | 11,528 B | `bench:ado-postgresql/bulk-insert-100` | `BulkOperationsPostgreSQL.BulkInsertAsync` via binary `COPY` |
<!-- /docref-table -->

**Key observations**:

- **PostgreSQL is the fastest of the three ADO providers by a wide margin** — every operation lands in the 300-800 us range, roughly an order of magnitude faster than SQL Server and MySQL for the equivalent paths.
- **Bulk vs single-row insert**: 100 rows via the binary `COPY` protocol cost 808 us (8 us/row) while the single-row repository `AddAsync` is 458 us — so the break-even with repository inserts sits around 2-3 rows. PostgreSQL's `COPY` is the most efficient of the three providers for small batches.
- **Outbox insert (~398 us) is cheaper than the repository insert (~458 us)**: the outbox store has a narrower schema and no read-back, which pays off here.
- **`GetPendingMessagesAsync`** reads 100 rows in 608 us (~6 us/row) with 41 KB of managed allocations — the sub-microsecond query floor on this hardware.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.ADO.PostgreSQL.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/ado-postgresql
```

Filter to a subset:

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.ADO.PostgreSQL.Benchmarks `
  -- --job short --filter "*Outbox*"
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.ADO.PostgreSQL.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Shared ADO infrastructure: `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/`
