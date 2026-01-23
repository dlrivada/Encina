# Integration Tests - ADO.Sqlite Tenancy

## Status: Not Implemented

## Justification

Integration tests for ADO.Sqlite Tenancy are not implemented for the following reasons:

### 1. In-Memory Database Nature

SQLite is primarily used as an in-memory or file-based database for testing purposes. The Tenancy implementation uses the same underlying connection and SQL generation as the standard ADO.Sqlite provider, which is already covered by existing integration tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and ADOTenancyOptions
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy filtering logic (`WHERE TenantId = @TenantId`) is generated identically across all ADO providers. Testing this with one real database (e.g., SqlServer) validates the pattern for all providers.

### 4. Recommended Alternative

If database-level isolation testing is needed, the existing `Encina.IntegrationTests\ADO\Sqlite\Repository\FunctionalRepositoryADOIntegrationTests.cs` can be extended with tenant-specific scenarios.

## Related Files

- `src/Encina.ADO.Sqlite/Tenancy/TenantAwareFunctionalRepositoryADO.cs`
- `src/Encina.ADO.Sqlite/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/Sqlite/Tenancy/`

## Date: 2026-01-23

## Issue: #282
