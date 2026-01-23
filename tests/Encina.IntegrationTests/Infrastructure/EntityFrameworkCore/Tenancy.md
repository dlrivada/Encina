# Integration Tests - EntityFrameworkCore Tenancy

## Status: Not Implemented

## Justification

Integration tests for EntityFrameworkCore Tenancy are not implemented for the following reasons:

### 1. EF Core Tenancy Uses Global Query Filters

EF Core's multi-tenancy implementation uses Global Query Filters which are automatically applied to all queries. This mechanism is already tested by Microsoft as part of EF Core's test suite.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of EfCoreTenancyOptions, TenantDbContext extensions
- **Guard Tests**: Secure-by-default verification for EfCoreTenancyOptions
- **Property Tests**: Invariant verification for options
- **Contract Tests**: API consistency verification

### 3. Existing EF Core Integration Tests Cover Query Execution

The query execution path and database interactions are already validated by:

- `Infrastructure\EntityFrameworkCore\Health\EntityFrameworkCoreHealthCheckIntegrationTests.cs`
- `Infrastructure\EntityFrameworkCore\Inbox\InboxStoreEFIntegrationTests.cs`
- `Infrastructure\EntityFrameworkCore\Outbox\OutboxStoreEFIntegrationTests.cs`
- `Infrastructure\EntityFrameworkCore\Sagas\SagaStoreEFIntegrationTests.cs`
- `Infrastructure\EntityFrameworkCore\Repository\` tests

### 4. EF Core's Global Query Filters

The multi-tenancy implementation relies on EF Core's `HasQueryFilter()` which is:

- Tested by Microsoft in EF Core's own test suite
- Applied automatically at the DbContext level
- Validated through the existing repository integration tests

### 5. Recommended Alternative

To test tenant isolation at the database level, extend `RepositoryIntegrationTests.cs` with scenarios that:

1. Insert data for multiple tenants
2. Query with different tenant contexts
3. Verify isolation between tenants

## Related Files

- `src/Encina.EntityFrameworkCore/Tenancy/EfCoreTenancyOptions.cs`
- `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContextExtensions.cs`
- `tests/Encina.UnitTests/EntityFrameworkCore/Tenancy/`
- `tests/Encina.GuardTests/EntityFrameworkCore/Tenancy/`

## Date: 2026-01-23

## Issue: #282
