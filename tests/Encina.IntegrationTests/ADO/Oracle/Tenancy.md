# Integration Tests - ADO.Oracle Tenancy

## Status: Not Implemented

## Justification

Integration tests for ADO.Oracle Tenancy are not implemented for the following reasons:

### 1. Requires Running Oracle Instance

Oracle integration tests require Docker infrastructure with a licensed Oracle container. This is more complex to set up than other databases and has licensing considerations.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and ADOTenancyOptions
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy SQL generation (`WHERE TenantId = :TenantId` with Oracle parameters) follows the same pattern as other ADO providers. The query execution path is already validated by:

- `ADO\Oracle\Inbox\InboxStoreADOTests.cs`
- `ADO\Oracle\Outbox\OutboxStoreADOTests.cs`

### 4. Oracle-Specific Considerations

Oracle uses `:param` syntax instead of `@param`, but this is handled by the Oracle ADO.NET provider and doesn't affect multi-tenancy logic.

### 5. Recommended Alternative

Create tenant-specific integration scenarios in a new `FunctionalRepositoryADOIntegrationTests.cs` file if explicit database-level multi-tenancy testing is required, using Oracle XE container.

## Related Files

- `src/Encina.ADO.Oracle/Tenancy/TenantAwareFunctionalRepositoryADO.cs`
- `src/Encina.ADO.Oracle/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/Oracle/Tenancy/`

## Date: 2026-01-23

## Issue: #282
