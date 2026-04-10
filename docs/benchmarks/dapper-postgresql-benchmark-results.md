# Encina.Dapper.PostgreSQL — Benchmark Results

> **Date**: 2026-04-10
> **Package**: `Encina.Dapper.PostgreSQL`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Dapper.PostgreSQL.Benchmarks`
> **Backing store**: `postgres:17-alpine` via Testcontainers

These numbers establish the first baseline for the Encina Dapper + PostgreSQL provider. They were captured on a development workstation to unblock the benchmark-coverage gate on `Encina.Dapper.PostgreSQL`. CI baselines (Ubuntu runner) will replace them on the next scheduled publish.

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

Measures the Dapper-over-Npgsql overhead for connection, repository, and outbox operations against a live PostgreSQL 17 container.

<!-- docref-table: bench:dapper-postgresql/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **OpenAndExecuteScalar** (baseline) | 354.3 us | 800 B | `bench:dapper-postgresql/connection-open` | `NpgsqlConnection.Open` + Dapper `ExecuteScalar<int>("SELECT 1")` + dispose |
| **GetByIdAsync** | 433.3 us | 12,611 B | `bench:dapper-postgresql/repo-get-by-id` | `FunctionalRepositoryDapper.GetByIdAsync` on a pre-seeded row |
| **AddAsync** | 423.7 us | 15,054 B | `bench:dapper-postgresql/repo-add` | `FunctionalRepositoryDapper.AddAsync` (single INSERT + read-back) |
| **AddAsync_Single** | 420.2 us | 5,085 B | `bench:dapper-postgresql/outbox-insert` | `OutboxStoreDapper.AddAsync` (single message append) |
| **GetPendingMessagesAsync_Batch100** | 630.0 us | 51,404 B | `bench:dapper-postgresql/outbox-query-pending` | `OutboxStoreDapper.GetPendingMessagesAsync` fetching 100 rows |
| **BulkInsert_100Rows** | 782.5 us | 11,016 B | `bench:dapper-postgresql/bulk-insert-100` | `BulkOperationsDapper.BulkInsertAsync` via binary `COPY` |
<!-- /docref-table -->

**Key observations**:

- **PostgreSQL + Dapper is basically free** vs raw ADO.NET: 354 us vs 334 us for the connection baseline, 433 us vs 416 us for `GetByIdAsync`, 782 us vs 808 us for `BulkInsert_100Rows`. The Dapper layer adds essentially zero observable overhead on top of Npgsql.
- **All 6 methods stay in the sub-millisecond range** — this is the fastest of all six database-benchmark combinations in the repo (ADO + Dapper across MySQL, PostgreSQL, SqlServer).
- **Outbox insert (420 us) is actually cheaper than the repository insert (424 us)**: same pattern as the ADO-PostgreSQL benchmarks — the outbox store has a narrower schema and no read-back.
- **Dapper materialization allocations are notable**: `GetByIdAsync` allocates 12.6 KB vs raw ADO's 11.0 KB — ~15% extra for the Dapper reflection cache but no mean-time penalty.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Dapper.PostgreSQL.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/dapper-postgresql
```

Filter to a subset:

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Dapper.PostgreSQL.Benchmarks `
  -- --job short --filter "*Outbox*"
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Dapper.PostgreSQL.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Shared Dapper infrastructure: `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/Infrastructure/`
- ADO.NET comparison: `docs/benchmarks/ado-postgresql-benchmark-results.md`
