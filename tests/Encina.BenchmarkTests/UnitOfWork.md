# Benchmark Tests - Unit of Work Pattern

## Status: Not Implemented

## Justification

Benchmark tests for the Unit of Work pattern are not implemented for the following reasons:

### 1. Unit of Work Operations are Database-Bound

All Unit of Work operations ultimately call the database:

- `BeginTransactionAsync()` - `IDbConnection.BeginTransaction()`
- `CommitAsync()` - `IDbTransaction.Commit()`
- `RollbackAsync()` - `IDbTransaction.Rollback()`
- `SaveChangesAsync()` - EF Core only, others return immediately

The performance is dominated by database latency, not by the thin wrapper code.

### 2. In-Memory Operations are O(1)

The only pure in-memory operations are:

- `Repository<T>()` - `ConcurrentDictionary.GetOrAdd()`, O(1)
- `HasActiveTransaction` - null check, O(1)

These operations complete in nanoseconds and benchmarking them would be noise.

### 3. Repository Caching Overhead is Negligible

```csharp
// The actual code being "benchmarked"
return (IFunctionalRepository<TEntity, TId>)_repositories.GetOrAdd(
    entityType,
    _ => new UnitOfWorkRepositoryADO<TEntity, TId>(_connection, mapping, this));
```

This is a single dictionary lookup - sub-microsecond operation.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correct behavior of all operations
- **Property Tests**: Verify invariants (transaction lifecycle, disposal)
- **Contract Tests**: Verify consistent API across all 12 providers
- **Integration Tests**: Test real transaction behavior against databases

### 5. Recommended Benchmarks (if needed)

If benchmarks are ever required, focus on end-to-end scenarios:

```csharp
[MemoryDiagnoser]
public class UnitOfWorkBenchmarks
{
    private readonly SqliteConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        // Create tables...
    }

    [Benchmark(Baseline = true)]
    public async Task<int> FullTransactionCycle()
    {
        await using var uow = new UnitOfWorkADO(_connection, _serviceProvider);

        await uow.BeginTransactionAsync();
        var repo = uow.Repository<Order, Guid>();
        await repo.AddAsync(new Order { Id = Guid.NewGuid() });
        var result = await uow.CommitAsync();

        return result.IsRight ? 1 : 0;
    }

    [Benchmark]
    public IFunctionalRepository<Order, Guid> RepositoryRetrieval()
    {
        using var uow = new UnitOfWorkADO(_connection, _serviceProvider);
        return uow.Repository<Order, Guid>();
    }
}
```

### 6. Why Not Include These Benchmarks Now

- **Transaction operations**: Dominated by database performance
- **Repository caching**: Single dictionary operation, ~10ns
- **Property access**: Direct field access, ~1ns
- **Disposal**: Cleanup operations, not hot path

## Related Files

- `src/Encina.DomainModeling/IUnitOfWork.cs` - Interface definition
- `src/Encina.*/UnitOfWork/UnitOfWork*.cs` - Provider implementations
- `tests/Encina.UnitTests/*/UnitOfWork/` - Unit tests
- `tests/Encina.PropertyTests/Database/UnitOfWork/` - Property tests
- `tests/Encina.ContractTests/Database/UnitOfWork/` - Contract tests

## Date: 2026-01-24

## Issue: #281
