# LoadTests - Database Resilience (Pool Monitoring & Circuit Breaker)

## Status: Not Implemented

## Justification

### 1. Pool Monitoring Operations Are Lightweight
`GetPoolStatistics()` is a synchronous method that either:
- Returns `ConnectionPoolStats.CreateEmpty()` (SQLite, EF Core, Dapper-SQLite/PostgreSQL/MySQL)
- Reads from `SqlConnection.RetrieveStatistics()` (SQL Server only)

These are single-call, non-blocking operations with no connection pool contention. They do not create, open, or hold connections.

### 2. Circuit Breaker State Is Volatile Boolean
The circuit breaker state (`_isCircuitOpen`) is a `volatile bool` field. Reading and writing a volatile boolean is an atomic, lock-free operation. Concurrent access has no contention.

### 3. Health Checks Are Already Concurrent-Safe by Design
`CheckHealthAsync` creates a new scoped connection per call and does not share state between invocations. The only shared state is the circuit breaker flag, which is volatile.

### 4. Adequate Coverage from Other Test Types
- **Unit Tests**: 113 tests covering circuit breaker state transitions, concurrent pipeline behavior, cache invalidation via reflection
- **Guard Tests**: 22 tests verifying null argument validation across all 10 monitors
- **Contract Tests**: 20 tests verifying API consistency and naming conventions
- **Property Tests**: 19 tests verifying `ConnectionPoolStats` mathematical invariants
- **Integration Tests**: 15 tests verifying real database health checks with SQLite

### 5. Recommended Alternative
If load testing is needed in the future:
- Use the existing `Encina.LoadTests` harness to add a health check worker
- Add concurrent `CheckHealthAsync` calls alongside send/publish workers
- Monitor circuit breaker state transitions under sustained database failure simulation

## Related Files
- `src/Encina/Database/IDatabaseHealthMonitor.cs` - Core interface
- `src/Encina.Messaging/Health/DatabaseHealthMonitorBase.cs` - Base implementation
- `tests/Encina.UnitTests/Database/` - Unit tests
- `tests/Encina.IntegrationTests/ADO/Sqlite/Resilience/` - Integration tests

## Date: 2026-02-08
## Issue: #290
