# Integration Tests - ADO.SqlServer Tenancy

## Status: Not Implemented

## Justification

Integration tests for ADO.SqlServer Tenancy are not implemented for the following reasons:

### 1. Requires Running SQL Server Instance

SQL Server integration tests require Docker infrastructure with the `databases` profile enabled. Multi-tenancy tests would add minimal validation over the existing ADO.SqlServer integration tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and ADOTenancyOptions
- **Guard Tests**: Parameter validation for all public methods including null checks
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy SQL generation (`WHERE TenantId = @TenantId`) is identical across all ADO providers. The query execution path is already validated by:

- `ADO\SqlServer\Repository\FunctionalRepositoryADOIntegrationTests.cs`
- `ADO\SqlServer\Inbox\InboxStoreADOTests.cs`
- `ADO\SqlServer\Outbox\OutboxStoreADOTests.cs`

### 4. Recommended Alternative

Extend `FunctionalRepositoryADOIntegrationTests.cs` with tenant isolation scenarios if explicit database-level multi-tenancy testing is required.

## Related Files

- `src/Encina.ADO.SqlServer/Tenancy/TenantAwareFunctionalRepositoryADO.cs`
- `src/Encina.ADO.SqlServer/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/SqlServer/Tenancy/`
- `tests/Encina.GuardTests/ADO/SqlServer/Tenancy/`

## Date: 2026-01-23

## Issue: #282
