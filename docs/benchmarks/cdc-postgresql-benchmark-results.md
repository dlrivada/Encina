# Encina.Cdc.PostgreSql — Benchmark Results

> **Date**: 2026-04-11
> **Package**: `Encina.Cdc.PostgreSql`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Cdc.PostgreSql.Benchmarks`
> **Backing store**: `postgres:17-alpine` with `wal_level=logical`

These numbers establish the first baseline for the Encina PostgreSQL CDC connector. They combine CPU-bound position round-trip benchmarks with a real end-to-end `GetCurrentPositionAsync` measurement against a live logical-replication-enabled PostgreSQL 17 container.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers PostgreSql (postgres:17-alpine, wal_level=logical)
```

## Position round-trip + connector round-trip

<!-- docref-table: bench:cdc-postgresql/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **CreatePosition** (baseline) | 5 ns | 24 B | `bench:cdc-postgresql/position-ctor` | `PostgresCdcPosition` over a 64-bit LSN |
| **ToBytes** | 6 ns | 32 B | `bench:cdc-postgresql/position-tobytes` | Fixed-size 8-byte big-endian write |
| **FromBytes** | 5 ns | 24 B | `bench:cdc-postgresql/position-frombytes` | Fixed-size 8-byte big-endian read |
| **ComparePositions** | 2 ns | 0 B | `bench:cdc-postgresql/position-compare` | `NpgsqlLogSequenceNumber.CompareTo` |
| **GetCurrentPositionAsync** | 541 us | 2,234 B | `bench:cdc-postgresql/get-current-position` | `SELECT pg_current_wal_lsn()` via live PostgreSQL + `ICdcConnector` wiring |
<!-- /docref-table -->

**Key observations**:

- **The PostgreSQL position is the cheapest of all five providers** — 24 bytes + 5-6 ns for every CPU operation. The LSN is a fixed 8-byte value, so both serialization directions collapse to `BinaryPrimitives.{Read,Write}UInt64BigEndian` plus a tiny allocation for the result array.
- **`GetCurrentPositionAsync` at 541 us** is the fastest connector round-trip of the four database providers: PostgreSQL + Npgsql is traditionally the lightest connection-open path in this repo, and the CDC connector inherits that floor.
- **`ComparePositions` is allocation-free and ~2 ns** — which matters because the connector uses it to decide whether a snapshot has caught up to the live WAL.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Cdc.PostgreSql.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/cdc-postgresql
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Cdc.PostgreSql.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling connectors: `docs/benchmarks/cdc-{mysql,sqlserver,mongodb,debezium}-benchmark-results.md`
