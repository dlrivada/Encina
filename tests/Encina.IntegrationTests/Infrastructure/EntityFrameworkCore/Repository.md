# Integration Tests - EntityFrameworkCore Repository

## Status: Not Implemented

## Justification

Integration tests for EntityFrameworkCore Repository are not implemented for the following reasons:

### 1. EF Core Has Built-in Testing Support

Entity Framework Core provides excellent testing support through:
- `InMemoryDatabase` provider for unit testing
- `UseInMemoryDatabase()` for integration testing
- Built-in change tracking and SaveChanges testing

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryEF using mocked DbContext
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for specifications
- **Contract Tests**: API consistency verification with other repository implementations

### 3. EF Core's Repository is a Thin Wrapper

The `FunctionalRepositoryEF<TEntity, TId>` is a thin wrapper around `DbSet<T>`. EF Core's own integration tests validate database interactions.

### 4. Recommended Alternative

For database-specific integration tests:
```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;
using var context = new AppDbContext(options);
var repository = new FunctionalRepositoryEF<Order, Guid>(context);
// Test CRUD operations
```

## Related Files

- `src/Encina.EntityFrameworkCore/Repository/FunctionalRepositoryEF.cs`
- `src/Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs`
- `tests/Encina.UnitTests/EntityFrameworkCore/Repository/`

## Date: 2026-01-24

## Issue: #279
