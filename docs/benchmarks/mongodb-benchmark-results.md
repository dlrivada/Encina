# Encina.MongoDB — Benchmark Results

> **Date**: 2026-04-10
> **Package**: `Encina.MongoDB`
> **Project**: `tests/Encina.BenchmarkTests/Encina.MongoDB.Benchmarks`
> **Backing store**: `mongo:7` via Testcontainers (standalone, no replica set)

These numbers establish the first baseline for the Encina MongoDB provider. They were captured on a development workstation to unblock the benchmark-coverage gate on `Encina.MongoDB`. CI baselines (Ubuntu runner) will replace them on the next scheduled publish.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers MongoDb (mongo:7, standalone)
  Driver   : MongoDB.Driver
```

## Connection & Repository — Per-Operation Cost

Measures the MongoDB.Driver overhead for connection, repository, and outbox operations against a live MongoDB 7 container.

<!-- docref-table: bench:mongodb/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **PingCommand** (baseline) | 342.3 us | 11,824 B | `bench:mongodb/connection-ping` | Reused `MongoClient` + `runCommand({ ping: 1 })` round-trip |
| **GetByIdAsync** | 574.4 us | 22,992 B | `bench:mongodb/repo-get-by-id` | `FunctionalRepositoryMongoDB.GetByIdAsync` on a pre-seeded document |
| **AddAsync** | 487.3 us | 20,329 B | `bench:mongodb/repo-add` | `FunctionalRepositoryMongoDB.AddAsync` (single `InsertOne`) |
| **AddAsync_Single** | 519.3 us | 20,585 B | `bench:mongodb/outbox-insert` | `OutboxStoreMongoDB.AddAsync` (single outbox document) |
| **GetPendingMessagesAsync_Batch100** | 859.0 us | 95,634 B | `bench:mongodb/outbox-query-pending` | `OutboxStoreMongoDB.GetPendingMessagesAsync` fetching 100 documents |
| **BulkInsert_100Rows** | 1.62 ms | 73,456 B | `bench:mongodb/bulk-insert-100` | `BulkOperationsMongoDB.BulkInsertAsync` via native `BulkWrite` |
<!-- /docref-table -->

**Key observations**:

- **Sub-millisecond round-trips for every single-document operation**: MongoDB lands between 342 us (ping) and 574 us (repo read), comparable to PostgreSQL (334-808 us) and faster than MySQL/SQL Server for writes.
- **Inserts cost roughly the same as reads** (~490-520 us): unlike SQL Server where single-row INSERT takes 5-6 ms, MongoDB's write path doesn't pay an extra round-trip penalty — the driver pipelines the operation.
- **`GetByIdAsync` is slightly slower than `AddAsync`** (574 us vs 487 us) because the repository materializes the full document into a typed entity, whereas the insert path only serializes one way. The extra allocation (23 KB vs 20 KB) confirms this.
- **`BulkInsert_100Rows` at 1.62 ms** (~16 us/doc) is competitive with MySQL's `MySqlBulkCopy` (1.16 ms ADO / 1.67 ms Dapper) and much faster than SQL Server's `SqlBulkCopy` path for small batches.
- **Allocation profile is notably higher than the SQL providers** (e.g., repo `GetByIdAsync` allocates 23 KB vs ~11 KB on Dapper-PostgreSQL): BSON serialization overhead plus the driver's command builder account for the delta. If allocation pressure becomes a concern, this is the hottest target for optimization.
- **`GetPendingMessagesAsync` allocates 96 KB** to materialize 100 outbox documents — almost twice the ADO-PostgreSQL equivalent (41 KB) but mean time (859 us) stays close.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.MongoDB.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/mongodb
```

Filter to a subset:

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.MongoDB.Benchmarks `
  -- --job short --filter "*Outbox*"
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.MongoDB.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- ADO/Dapper comparisons: `docs/benchmarks/ado-*-benchmark-results.md`, `docs/benchmarks/dapper-*-benchmark-results.md`
