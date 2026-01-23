# Integration Tests - Dapper.MySQL Tenancy

## Status: Not Implemented

## Justification

Integration tests for Dapper.MySQL Tenancy are not implemented for the following reasons:

### 1. Requires Running MySQL Instance

MySQL integration tests require Docker infrastructure with the `databases` profile enabled. Multi-tenancy tests would add minimal validation over existing Dapper.MySQL integration tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and DapperTenancyOptions
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all Dapper providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy SQL generation is identical across all Dapper providers. The query execution path is already validated by:

- `Dapper\MySQL\Health\MySqlHealthCheckIntegrationTests.cs`
- `Dapper\MySQL\Inbox\InboxStoreDapperTests.cs`
- `Dapper\MySQL\Outbox\OutboxStoreDapperTests.cs`
- `Dapper\MySQL\Sagas\SagaStoreDapperTests.cs`
- `Dapper\MySQL\Scheduling\ScheduledMessageStoreDapperTests.cs`

### 4. Recommended Alternative

Create tenant-specific integration scenarios or extend existing tests with multi-tenant data isolation verification if explicit database-level multi-tenancy testing is required.

## Related Files

- `src/Encina.Dapper.MySQL/Tenancy/TenantAwareFunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.MySQL/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/MySQL/Tenancy/`

## Date: 2026-01-23

## Issue: #282
