# Load Tests - Read Auditing

## Status: Not Implemented

## Justification

Read audit logging operates as a fire-and-forget side effect of repository read operations.
Load testing the audit stores directly would primarily stress the underlying database provider,
not the read auditing logic itself. The concurrency characteristics of the store implementations
are inherited from the database drivers (ADO.NET, Dapper, EF Core, MongoDB).

### 1. Fire-and-Forget Pattern Limits Concurrency Impact

The `AuditedRepository<TEntity, TId>` decorator uses `_ = LogReadAccessAsync(...)` — a
fire-and-forget pattern where audit failures never block read operations. Under high load,
the primary impact is audit entry loss (acceptable by design), not contention or deadlocks.

### 2. Thin Wrapper Over Database Providers

Each `ReadAuditStore` implementation (ADO, Dapper, EF, MongoDB) delegates directly to
the underlying database driver. Load characteristics are determined by the driver and
database engine, not by the auditing code.

### 3. Sampling Rate Reduces Effective Load

`ReadAuditOptions.GetSamplingRate()` allows per-entity sampling. High-traffic entities
typically use 10-50% sampling, meaning the actual audit write volume under load is
significantly lower than the read volume.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify fire-and-forget resilience, concurrent writes via `InMemoryReadAuditStore`
- **Integration Tests**: Verify real database persistence across providers
- **Property Tests**: Verify invariants hold for random inputs including pagination
- **Guard Tests**: Verify parameter validation under all conditions

### 5. Recommended Alternative

If load testing becomes necessary (e.g., for capacity planning), use the existing
Encina.LoadTests infrastructure with NBomber to stress-test the database providers
directly through the `IReadAuditStore` interface with realistic entry volumes.

## Related Files

- `src/Encina.Security.Audit/AuditedRepository.cs` — Fire-and-forget decorator
- `src/Encina.Security.Audit/InMemoryReadAuditStore.cs` — Thread-safe in-memory store
- `src/Encina.ADO.Sqlite/Auditing/ReadAuditStoreADO.cs` — ADO.NET implementation
- `src/Encina.Dapper.Sqlite/Auditing/ReadAuditStoreDapper.cs` — Dapper implementation
- `src/Encina.EntityFrameworkCore/Auditing/ReadAuditStoreEF.cs` — EF Core implementation
- `src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs` — MongoDB implementation
- `tests/Encina.UnitTests/Security/Audit/ReadAudit/` — Unit test coverage

## Date: 2026-03-04
## Issue: #573
