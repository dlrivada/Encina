# Benchmark Tests - Database Resilience (Pool Monitoring & Circuit Breaker)

## Status: Not Implemented

## Justification

### 1. Pool Monitoring Operations Are Not Hot Paths

`GetPoolStatistics()` is called on-demand (health check endpoints, diagnostic tools) rather than on every request. Typical invocation frequency is seconds or minutes, not microseconds:

- **SQLite, EF Core, Dapper-SQLite/PostgreSQL/MySQL**: Returns `ConnectionPoolStats.CreateEmpty()` — a static factory call with zero allocation beyond the record itself.
- **SQL Server (ADO + Dapper)**: Calls `SqlConnection.RetrieveStatistics()` — a dictionary read from the driver's internal counters. Single-digit microsecond operation.
- **PostgreSQL, MySQL (ADO)**: Read connection string properties (`MaxPoolSize`). No database round-trip.
- **MongoDB**: Calls `IMongoClient.Cluster.Description` — reads cached cluster metadata.

Benchmarking nanosecond-level differences in these operations provides no actionable insight.

### 2. Circuit Breaker State Is a Volatile Boolean Read

The `IsCircuitOpen` property reads a `volatile bool` field. This is a single CPU instruction (memory barrier + load). Benchmarking it would measure noise, not meaningful performance characteristics.

### 3. Health Checks Are I/O-Bound, Not CPU-Bound

`CheckHealthAsync` executes `SELECT 1` (or equivalent) against the database. The dominant cost is network I/O latency (milliseconds), not the C# code path (microseconds). BenchmarkDotNet is designed for CPU-bound micro-benchmarks, not I/O-bound operations where variance is dominated by external factors.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: 113 tests covering circuit breaker state transitions, concurrent pipeline behavior, cache invalidation via reflection
- **Guard Tests**: 22 tests verifying null argument validation across all 10 monitors
- **Contract Tests**: 20 tests verifying API consistency and naming conventions
- **Property Tests**: 19 tests verifying `ConnectionPoolStats` mathematical invariants (PoolUtilization clamping, equality, hash codes)
- **Integration Tests**: 15 tests verifying real database health checks with SQLite

### 5. Recommended Alternative

If performance measurement is needed in the future:
- Use the existing `Encina.Benchmarks` project to add `DatabaseHealthMonitorBenchmarks`
- Benchmark `GetPoolStatistics()` across SQL Server vs PostgreSQL vs MySQL to compare driver overhead
- Use `[MemoryDiagnoser]` to verify zero-allocation paths for `CreateEmpty()` and volatile reads
- For health check latency, use load testing (NBomber) rather than BenchmarkDotNet

## Related Files

- `src/Encina/Database/IDatabaseHealthMonitor.cs` - Core interface
- `src/Encina/Database/ConnectionPoolStats.cs` - Pool statistics record
- `src/Encina.Messaging/Health/DatabaseHealthMonitorBase.cs` - Base implementation
- `tests/Encina.UnitTests/Database/` - Unit tests
- `tests/Encina.IntegrationTests/ADO/Sqlite/Resilience/` - Integration tests

## Date: 2026-02-08
## Issue: #290
