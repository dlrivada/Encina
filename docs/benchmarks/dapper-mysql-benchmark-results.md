# Encina.Dapper.MySQL — Benchmark Results

> **Date**: 2026-04-10
> **Package**: `Encina.Dapper.MySQL`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Dapper.MySQL.Benchmarks`
> **Backing store**: `mysql:9.1` via Testcontainers

These numbers establish the first baseline for the Encina Dapper + MySQL provider. They were captured on a development workstation to unblock the benchmark-coverage gate on `Encina.Dapper.MySQL`. CI baselines (Ubuntu runner) will replace them on the next scheduled publish.

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

Measures the Dapper-over-MySqlConnector overhead for connection, repository, and outbox operations against a live MySQL 9.1 container.

<!-- docref-table: bench:dapper-mysql/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **OpenAndExecuteScalar** (baseline) | 897.2 us | 1,744 B | `bench:dapper-mysql/connection-open` | `MySqlConnection.Open` + Dapper `ExecuteScalar<int>("SELECT 1")` + dispose |
| **GetByIdAsync** | 550.0 us | 6,697 B | `bench:dapper-mysql/repo-get-by-id` | `FunctionalRepositoryDapper.GetByIdAsync` on a pre-seeded row |
| **AddAsync** | 9.99 ms | 9,286 B | `bench:dapper-mysql/repo-add` | `FunctionalRepositoryDapper.AddAsync` (single INSERT + read-back) |
| **AddAsync_Single** | 9.01 ms | 6,616 B | `bench:dapper-mysql/outbox-insert` | `OutboxStoreDapper.AddAsync` (single message append) |
| **GetPendingMessagesAsync_Batch100** | 752.9 us | 52,186 B | `bench:dapper-mysql/outbox-query-pending` | `OutboxStoreDapper.GetPendingMessagesAsync` fetching 100 rows |
| **BulkInsert_100Rows** | 1.67 ms | 62,256 B | `bench:dapper-mysql/bulk-insert-100` | `BulkOperationsDapper.BulkInsertAsync` via `MySqlBulkCopy` |
<!-- /docref-table -->

**Key observations**:

- **Dapper overhead vs raw ADO.NET is small** — compare with `bench:ado-mysql/*`: the `GetPendingMessagesAsync` path (753 us Dapper vs 783 us ADO) and `GetByIdAsync` (550 us Dapper vs 459 us ADO) are within a few hundred microseconds. The Dapper materialization pays a modest per-row allocation tax.
- **Single-row INSERT is dominated by the round-trip**: both outbox and repository `AddAsync` land at ~9-10 ms, matching what the ADO MySQL benchmarks show. The bottleneck is MySQL's write latency, not Dapper.
- **`BulkInsert_100Rows` is slower than the ADO equivalent** (1.67 ms Dapper vs 1.16 ms ADO) — Dapper's reflection/materialization path adds overhead on top of `MySqlBulkCopy`, but it's still ~6x faster per row than the single-row `AddAsync` path.
- **`OpenAndExecuteScalar`** at 897 us is notably higher than the raw-ADO 802 us baseline — Dapper's pre-cached `SqlMapper` lookup plus boxed scalar result accounts for most of the delta.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Dapper.MySQL.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/dapper-mysql
```

Filter to a subset:

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Dapper.MySQL.Benchmarks `
  -- --job short --filter "*Outbox*"
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Dapper.MySQL.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Shared Dapper infrastructure: `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/Infrastructure/`
- ADO.NET comparison: `docs/benchmarks/ado-mysql-benchmark-results.md`
