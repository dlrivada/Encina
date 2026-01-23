# Integration Tests - ADO.MySQL Tenancy

## Status: Not Implemented

## Justification

Integration tests for ADO.MySQL Tenancy are not implemented for the following reasons:

### 1. Requires Running MySQL Instance

MySQL integration tests require Docker infrastructure with the `databases` profile enabled. Multi-tenancy tests would add minimal validation over existing ADO.MySQL integration tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and ADOTenancyOptions
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy SQL generation (`WHERE TenantId = @TenantId` with MySqlConnector parameters) follows the same pattern as other ADO providers. The query execution path is already validated by:

- `ADO\MySQL\Health\MySqlHealthCheckIntegrationTests.cs`
- `ADO\MySQL\Inbox\InboxStoreADOTests.cs`
- `ADO\MySQL\Outbox\OutboxStoreADOTests.cs`

### 4. Recommended Alternative

Create tenant-specific integration scenarios in a new `FunctionalRepositoryADOIntegrationTests.cs` file if explicit database-level multi-tenancy testing is required.

## Related Files

- `src/Encina.ADO.MySQL/Tenancy/TenantAwareFunctionalRepositoryADO.cs`
- `src/Encina.ADO.MySQL/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/MySQL/Tenancy/`

## Date: 2026-01-23

## Issue: #282
