# Integration Tests - Dapper.Oracle Tenancy

## Status: Not Implemented

## Justification

Integration tests for Dapper.Oracle Tenancy are not implemented for the following reasons:

### 1. Requires Running Oracle Instance

Oracle integration tests require Docker infrastructure with a licensed Oracle container. This is more complex to set up than other databases and has licensing considerations.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and DapperTenancyOptions
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all Dapper providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy SQL generation is handled by Dapper and is identical across all Dapper providers. The query execution path is already validated by:

- `Dapper\Oracle\Inbox\InboxStoreDapperTests.cs`
- `Dapper\Oracle\Outbox\OutboxStoreDapperTests.cs`
- `Dapper\Oracle\Sagas\SagaStoreDapperTests.cs`
- `Dapper\Oracle\Scheduling\ScheduledMessageStoreDapperTests.cs`

### 4. Oracle-Specific Considerations

Oracle uses `:param` syntax instead of `@param`, but this is handled transparently by the Oracle Dapper provider and doesn't affect multi-tenancy logic.

### 5. Recommended Alternative

Create tenant-specific integration scenarios using Oracle XE container if explicit database-level multi-tenancy testing is required.

## Related Files

- `src/Encina.Dapper.Oracle/Tenancy/TenantAwareFunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.Oracle/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/Oracle/Tenancy/`

## Date: 2026-01-23

## Issue: #282
