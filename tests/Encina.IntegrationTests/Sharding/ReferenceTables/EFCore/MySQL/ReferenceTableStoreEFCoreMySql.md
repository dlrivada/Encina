# Integration Tests - EF Core MySQL Reference Table Store

## Status: Deferred to Phase 6

## Justification

EF Core reference table stores require dedicated `DbContext` subclasses with model configuration for each test entity. The `ReferenceTableStoreFactoryEF<TContext>` creates per-shard `DbContext` instances via `DbContextOptionsBuilder`, requiring:

1. A test `DbContext` with `OnModelCreating` configuration for `CountryRef`
2. Schema creation via EF Core migrations or `EnsureCreated()` per shard
3. Connection string swapping per shard at runtime

This infrastructure will be implemented in Phase 6 (Full End-to-End Testing) when the EF Core sharding DI pipeline is fully wired.

### Adequate Coverage from Other Test Types
- **Unit Tests**: `ReferenceTableStoreEF` logic tested via mocked `DbContext`
- **Contract Tests**: `ReferenceTableStoreContractTests` verifies interface compliance via reflection
- **ADO/Dapper Integration**: Same SQL operations verified against real MySQL databases

## Related Files
- `src/Encina.EntityFrameworkCore/Sharding/ReferenceTables/ReferenceTableStoreEF.cs`
- `src/Encina.EntityFrameworkCore/Sharding/ReferenceTables/ReferenceTableStoreFactoryEF.cs`

## Date: 2026-02-15
## Issue: #639
