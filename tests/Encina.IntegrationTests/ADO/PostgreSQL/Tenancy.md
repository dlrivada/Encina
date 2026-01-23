# Integration Tests - ADO.PostgreSQL Tenancy

## Status: Not Implemented

## Justification

Integration tests for ADO.PostgreSQL Tenancy are not implemented for the following reasons:

### 1. Requires Running PostgreSQL Instance

PostgreSQL integration tests require Docker infrastructure with the `databases` profile enabled. Multi-tenancy tests would add minimal validation over existing ADO.PostgreSQL integration tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and ADOTenancyOptions
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy SQL generation (`WHERE TenantId = @TenantId` with Npgsql parameters) follows the same pattern as other ADO providers. The query execution path is already validated by:

- `ADO\PostgreSQL\Health\PostgreSqlHealthCheckIntegrationTests.cs`
- `ADO\PostgreSQL\Inbox\InboxStoreADOTests.cs`
- `ADO\PostgreSQL\Outbox\OutboxStoreADOTests.cs`

### 4. Recommended Alternative

Create tenant-specific integration scenarios in a new `FunctionalRepositoryADOIntegrationTests.cs` file if explicit database-level multi-tenancy testing is required.

## Related Files

- `src/Encina.ADO.PostgreSQL/Tenancy/TenantAwareFunctionalRepositoryADO.cs`
- `src/Encina.ADO.PostgreSQL/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/PostgreSQL/Tenancy/`

## Date: 2026-01-23

## Issue: #282
