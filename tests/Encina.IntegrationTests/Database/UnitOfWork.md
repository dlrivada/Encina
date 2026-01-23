# Integration Tests - Unit of Work Pattern

## Status: Not Implemented (Placeholder for Future)

## Justification

Integration tests for the Unit of Work pattern are documented as a future consideration. Currently:

### 1. Unit Tests Provide Adequate Coverage with Mocks

The existing Unit Tests in `Encina.UnitTests/*/UnitOfWork/` use mocked database connections to verify:

- Constructor validation
- Transaction lifecycle (begin, commit, rollback)
- Repository caching behavior
- Disposal behavior
- Error handling

### 2. Integration Tests Would Require Database Infrastructure

Full integration tests would need:

- Docker containers for each database provider (Sqlite, SqlServer, PostgreSQL, MySQL, Oracle)
- MongoDB container
- Test database initialization/teardown
- Network latency considerations

### 3. Repository Integration Tests Cover Transaction Behavior

The Repository integration tests (when implemented) will exercise:

- Real database transactions
- ACID compliance
- Concurrent transaction handling
- Rollback behavior under failures

These tests indirectly verify Unit of Work transaction management.

### 4. Property Tests Verify Behavioral Invariants

`Encina.PropertyTests/Database/UnitOfWork/UnitOfWorkPropertyTests.cs` verifies:

- Transaction uniqueness (only one active at a time)
- Transaction lifecycle consistency
- Disposal safety
- Repository caching consistency

### 5. Recommended Integration Tests (if implemented)

```csharp
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class UnitOfWorkSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly SqlConnection _connection;

    [Fact]
    public async Task FullTransactionCycle_WithRealDatabase_Succeeds()
    {
        await using var uow = new UnitOfWorkADO(_connection, _serviceProvider);

        await uow.BeginTransactionAsync();
        var repo = uow.Repository<Order, Guid>();

        var order = new Order { Id = Guid.NewGuid(), Name = "Test" };
        await repo.AddAsync(order);

        await uow.CommitAsync();

        // Verify data persisted
        var retrieved = await repo.GetByIdAsync(order.Id);
        retrieved.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Rollback_WithRealDatabase_DiscardsChanges()
    {
        await using var uow = new UnitOfWorkADO(_connection, _serviceProvider);

        await uow.BeginTransactionAsync();
        var repo = uow.Repository<Order, Guid>();

        var order = new Order { Id = Guid.NewGuid(), Name = "Test" };
        await repo.AddAsync(order);

        await uow.RollbackAsync();

        // Verify data NOT persisted
        var retrieved = await repo.GetByIdAsync(order.Id);
        retrieved.IsLeft.ShouldBeTrue();
    }
}
```

## Related Files

- `src/Encina.DomainModeling/IUnitOfWork.cs` - Interface definition
- `src/Encina.*/UnitOfWork/UnitOfWork*.cs` - Provider implementations
- `tests/Encina.UnitTests/*/UnitOfWork/` - Unit tests (with mocks)
- `tests/Encina.PropertyTests/Database/UnitOfWork/` - Property tests
- `tests/Encina.ContractTests/Database/UnitOfWork/` - Contract tests

## Date: 2026-01-24

## Issue: #281
