# Integration Tests - MongoDB Tenancy

## Status: Not Implemented

## Justification

Integration tests for MongoDB Tenancy are not implemented for the following reasons:

### 1. Requires Running MongoDB Instance

MongoDB integration tests require Docker infrastructure with the MongoDB container running. Multi-tenancy tests would add minimal validation over existing MongoDB integration tests.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of TenantAwareFunctionalRepositoryMongoDB, TenantEntityMappingBuilder, and MongoDbTenancyOptions
- **Guard Tests**: Parameter validation for all public methods including idSelector
- **Property Tests**: Invariant verification for options and mappings
- **Contract Tests**: API consistency verification

### 3. MongoDB-Specific Tenancy Filtering

MongoDB multi-tenancy uses filter expressions that are composed with user queries:

```csharp
Builders<TEntity>.Filter.Eq(x => x.TenantId, tenantId) & userFilter
```

This composition is tested in unit tests without requiring a real MongoDB instance.

### 4. Existing MongoDB Integration Tests

The MongoDB connection and query execution are already validated by:

- `Infrastructure\MongoDB\Health\MongoDbHealthCheckIntegrationTests.cs`

### 5. Recommended Alternative

To test tenant isolation at the database level, create a new integration test file that:

1. Inserts documents for multiple tenants
2. Queries with different ITenantProvider implementations
3. Verifies documents are properly filtered by TenantId

## Related Files

- `src/Encina.MongoDB/Tenancy/TenantAwareFunctionalRepositoryMongoDB.cs`
- `src/Encina.MongoDB/Tenancy/TenantEntityMappingBuilder.cs`
- `tests/Encina.UnitTests/MongoDB/Tenancy/`
- `tests/Encina.GuardTests/MongoDB/Tenancy/`

## Date: 2026-01-23

## Issue: #282
