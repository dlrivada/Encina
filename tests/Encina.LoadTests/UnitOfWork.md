# Load Tests - Unit of Work Pattern

## Status: Not Implemented

## Justification

Load tests for the Unit of Work pattern are not implemented for the following reasons:

### 1. Unit of Work is a Coordination Pattern, Not a Hot Path

The Unit of Work pattern coordinates transactions across repositories:

- `BeginTransactionAsync()` - starts a database transaction (once per operation)
- `CommitAsync()` / `RollbackAsync()` - finalizes the transaction (once per operation)
- `Repository<T>()` - retrieves cached repository instances (O(1) dictionary lookup)

### 2. Database Operations are the Load-Sensitive Component

Meaningful load tests should target:

- Repository operations (`GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`)
- Database connection pool exhaustion
- Query execution under high concurrency
- Transaction isolation under concurrent access

These are Repository-level concerns tested in `Encina.LoadTests/Repository/`.

### 3. Transaction Management is Lightweight

The Unit of Work transaction management operations:

- `BeginTransaction` - single database call to start transaction
- `Commit` - single database call to commit
- `Rollback` - single database call to rollback

These operations are inherently limited by database performance, not by the Unit of Work implementation.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify transaction lifecycle behavior (begin, commit, rollback)
- **Property Tests**: Verify invariants hold (transaction uniqueness, disposal behavior)
- **Contract Tests**: Verify all 12 providers implement identical APIs
- **Integration Tests**: Test actual transaction behavior against real databases

### 5. Recommended Alternative

For load testing transactional scenarios:

```csharp
// NBomber scenario for transactional operations
var scenario = Scenario.Create("transactional-load", async context =>
{
    await using var unitOfWork = await _factory.CreateAsync();
    await unitOfWork.BeginTransactionAsync();

    var repo = unitOfWork.Repository<Order, OrderId>();
    await repo.AddAsync(new Order { ... });

    var result = await unitOfWork.CommitAsync();
    return result.IsRight ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5))
);
```

## Related Files

- `src/Encina.DomainModeling/IUnitOfWork.cs` - Interface definition
- `src/Encina.*/UnitOfWork/UnitOfWork*.cs` - Provider implementations
- `tests/Encina.UnitTests/*/UnitOfWork/` - Unit tests
- `tests/Encina.PropertyTests/Database/UnitOfWork/` - Property tests
- `tests/Encina.ContractTests/Database/UnitOfWork/` - Contract tests

## Date: 2026-01-24

## Issue: #281
