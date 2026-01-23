# Integration Tests - Dapper.SqlServer Tenancy

## Status: Not Implemented

## Justification

Integration tests for Dapper.SqlServer Tenancy are not implemented for the following reasons:

### 1. Requires Running SQL Server Instance

SQL Server integration tests require Docker infrastructure with the `databases` profile enabled. Multi-tenancy tests would add minimal validation over existing Dapper.SqlServer integration tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepository, TenantEntityMappingBuilder, and DapperTenancyOptions
- **Guard Tests**: Parameter validation for all public methods including null checks
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification across all Dapper providers

### 3. Multi-Tenancy Logic is Provider-Agnostic

The multi-tenancy SQL generation is identical across all Dapper providers. The query execution path is already validated by:

- `Dapper\SqlServer\Health\SqlServerHealthCheckIntegrationTests.cs`
- `Dapper\SqlServer\Inbox\InboxStoreDapperTests.cs`
- `Dapper\SqlServer\Outbox\OutboxStoreDapperTests.cs`
- `Dapper\SqlServer\Sagas\SagaStoreDapperTests.cs`
- `Dapper\SqlServer\Scheduling\ScheduledMessageStoreDapperTests.cs`

### 4. Recommended Alternative

Create tenant-specific integration scenarios or extend existing tests with multi-tenant data isolation verification if explicit database-level multi-tenancy testing is required.

## Related Files

- `src/Encina.Dapper.SqlServer/Tenancy/TenantAwareFunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.SqlServer/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/SqlServer/Tenancy/`
- `tests/Encina.GuardTests/Dapper/SqlServer/Tenancy/`

## Date: 2026-01-23

## Issue: #282
