# Encina.Cdc.MongoDb — Benchmark Results

> **Date**: 2026-04-11
> **Package**: `Encina.Cdc.MongoDb`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Cdc.MongoDb.Benchmarks`
> **Backing store**: `mongo:7` (standalone; replica set not required for `GetCurrentPositionAsync`)

These numbers establish the first baseline for the Encina MongoDB CDC connector. They combine CPU-bound position round-trip benchmarks with a real end-to-end `GetCurrentPositionAsync` measurement against a live MongoDB 7 container.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
  Database : Testcontainers MongoDb (mongo:7, standalone)
```

> **Why standalone and not a replica set?** MongoDB Change Streams (the `StreamChangesAsync` path) require a replica set, but `GetCurrentPositionAsync` only pings the server and reads the last saved position from the injected position store. Standalone is enough for this coverage-gate benchmark; a full streaming benchmark would need a replica-set container.

## Position round-trip + connector round-trip

<!-- docref-table: bench:cdc-mongodb/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **CreatePosition** (baseline) | 7 ns | 24 B | `bench:cdc-mongodb/position-ctor` | `MongoCdcPosition` wrapping a pre-built resume-token `BsonDocument` |
| **ToBytes** | 240 ns | 976 B | `bench:cdc-mongodb/position-tobytes` | BSON serialize via `BsonSerializer` + `MemoryStream` |
| **FromBytes** | 287 ns | 1,024 B | `bench:cdc-mongodb/position-frombytes` | BSON deserialize via `BsonBinaryReader` |
| **ComparePositions** | 84 ns | 96 B | `bench:cdc-mongodb/position-compare` | `BsonDocument.CompareTo` (field-by-field opaque comparison) |
| **GetCurrentPositionAsync** | 660 us | 34,051 B | `bench:cdc-mongodb/get-current-position` | `ping` round-trip + position-store lookup via `ICdcConnector` |
<!-- /docref-table -->

**Key observations**:

- **BSON round-trip dominates the CPU cost** — `ToBytes` / `FromBytes` each allocate ~1 KB and take 240-287 ns, vs. ~5 ns for the fixed-size 8-byte path used by PostgreSQL/SQL Server. This reflects the opaque shape of MongoDB resume tokens (which are schema-free `BsonDocument` instances).
- **`ComparePositions` allocates 96 B** even though it nominally only compares two documents — `BsonDocument.CompareTo` traverses both documents and materializes intermediate state. Not a concern in practice because comparisons happen once per catch-up check, not per event.
- **`GetCurrentPositionAsync` allocates 34 KB** — higher than any of the SQL providers — because the MongoDB driver sets up a fresh `MongoClient` state machine for every call inside the current connector implementation. This is a visible optimization target if the connector becomes a hot path.

## How to reproduce

```pwsh
# Requires Docker Desktop running
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Cdc.MongoDb.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/cdc-mongodb
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Cdc.MongoDb.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling connectors: `docs/benchmarks/cdc-{mysql,postgresql,sqlserver,debezium}-benchmark-results.md`
