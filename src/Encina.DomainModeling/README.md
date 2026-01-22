# Encina.DomainModeling

Core domain modeling abstractions and patterns for Encina, providing the foundational building blocks for Domain-Driven Design with Railway Oriented Programming.

## Features

- **Repository Pattern**: Provider-agnostic `IFunctionalRepository<TEntity, TId>` with ROP support
- **Unit of Work Pattern**: `IUnitOfWork` for coordinated transactional operations
- **Bulk Operations**: High-performance `IBulkOperations<TEntity>` for large datasets (up to 459x faster)
- **Specification Pattern**: `QuerySpecification<T>` for composable query logic
- **Error Handling**: Structured `RepositoryErrors` and `UnitOfWorkErrors` factory methods
- **Value Objects**: Base classes for immutable domain primitives

## Installation

```bash
dotnet add package Encina.DomainModeling
```

## Quick Start

### Repository Pattern

```csharp
// Define your entity
public class Order
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
}

// Use the functional repository
public class OrderService(IFunctionalRepository<Order, Guid> repository)
{
    public async Task<Either<EncinaError, Order>> GetOrderAsync(Guid id, CancellationToken ct)
        => await repository.GetByIdAsync(id, ct);

    public async Task<Either<EncinaError, Unit>> CreateOrderAsync(Order order, CancellationToken ct)
        => await repository.AddAsync(order, ct);
}
```

### Bulk Operations

Bulk operations provide **significant performance improvements** over standard ORM operations when working with large datasets.

#### Performance Comparison (measured with Testcontainers, 1,000 entities)

| Provider | Database | Insert | Update | Delete |
|----------|----------|--------|--------|--------|
| **Dapper** | SQL Server 2022 | **30x** faster | **125x** faster | **370x** faster |
| **EF Core** | SQL Server 2022 | **112x** faster | **178x** faster | **200x** faster |
| **ADO.NET** | SQL Server 2022 | **104x** faster | **187x** faster | **459x** faster |
| **MongoDB** | MongoDB 7 | **130x** faster | **16x** faster | **21x** faster |

> **Note**: Performance varies based on hardware, network latency, and entity complexity. All benchmarks measured using Testcontainers with Docker.

#### Interface

```csharp
public interface IBulkOperations<TEntity> where TEntity : class
{
    // Insert thousands of entities in seconds
    Task<Either<EncinaError, int>> BulkInsertAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default);

    // Update multiple entities efficiently
    Task<Either<EncinaError, int>> BulkUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default);

    // Delete by primary key in bulk
    Task<Either<EncinaError, int>> BulkDeleteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    // Upsert (insert or update) in bulk
    Task<Either<EncinaError, int>> BulkMergeAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default);

    // Read multiple entities by IDs efficiently
    Task<Either<EncinaError, IReadOnlyList<TEntity>>> BulkReadAsync(
        IEnumerable<object> ids,
        CancellationToken cancellationToken = default);
}
```

#### Usage Examples

```csharp
// Get bulk operations from Unit of Work
var bulkOps = unitOfWork.BulkOperations<Order>();

// Bulk insert 10,000 orders
var orders = GenerateOrders(10_000);
var result = await bulkOps.BulkInsertAsync(orders);

result.Match(
    Right: count => Console.WriteLine($"Inserted {count} orders"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);

// Bulk update with custom configuration
var config = BulkConfig.Default with
{
    BatchSize = 5000,
    PropertiesToInclude = ["Status", "UpdatedAt"]
};
await bulkOps.BulkUpdateAsync(ordersToUpdate, config);

// Bulk merge (upsert)
await bulkOps.BulkMergeAsync(ordersToUpsert);

// Bulk read by IDs
var ids = new List<object> { guid1, guid2, guid3 };
var foundOrders = await bulkOps.BulkReadAsync(ids);
```

### BulkConfig Options

```csharp
// BulkConfig is an immutable record with sensible defaults
var config = BulkConfig.Default; // BatchSize = 2000, PreserveInsertOrder = true

// Customize using with-expressions
var customConfig = BulkConfig.Default with
{
    BatchSize = 5000,              // Entities per batch
    BulkCopyTimeout = 300,         // Timeout in seconds (null = provider default)
    SetOutputIdentity = true,      // Get generated IDs back after insert
    PreserveInsertOrder = true,    // Maintain entity order
    UseTempDB = true,              // Use tempdb for staging (SQL Server)
    TrackingEntities = false,      // Don't track entities after operation
    PropertiesToInclude = ["Status", "UpdatedAt"],  // Only update these
    PropertiesToExclude = ["CreatedAt", "CreatedBy"] // Exclude these
};
```

### Error Handling

All bulk operations return `Either<EncinaError, T>` for explicit error handling:

```csharp
var result = await bulkOps.BulkInsertAsync(orders);

// Pattern matching
var message = result.Match(
    Right: count => $"Success: {count} inserted",
    Left: error => $"Failed: {error.Message} (Code: {error.GetCode()})"
);

// Error codes for programmatic handling
result.IfLeft(error =>
{
    var code = error.GetCode();
    if (code.IsSome && code == RepositoryErrors.BulkInsertFailedErrorCode)
    {
        // Handle specific error type
    }
});
```

#### Error Codes

| Error Code | Description |
|------------|-------------|
| `Repository.BulkInsertFailed` | Bulk insert operation failed |
| `Repository.BulkUpdateFailed` | Bulk update operation failed |
| `Repository.BulkDeleteFailed` | Bulk delete operation failed |
| `Repository.BulkMergeFailed` | Bulk merge/upsert operation failed |
| `Repository.BulkReadFailed` | Bulk read operation failed |

### Database Provider Support

| Provider | BulkInsert | BulkUpdate | BulkDelete | BulkMerge | BulkRead |
|----------|:----------:|:----------:|:----------:|:---------:|:--------:|
| **SQL Server (EF Core)** | SqlBulkCopy | MERGE + TVP | DELETE + TVP | MERGE | SELECT + TVP |
| **SQL Server (Dapper)** | SqlBulkCopy | MERGE + TVP | DELETE + TVP | MERGE | SELECT + TVP |
| **SQL Server (ADO.NET)** | SqlBulkCopy | MERGE + TVP | DELETE + TVP | MERGE | SELECT + TVP |
| **MongoDB** | InsertMany | BulkWrite | BulkWrite | BulkWrite (upsert) | Find + $in |

### Unit of Work Integration

```csharp
public class OrderFulfillmentService(IUnitOfWork unitOfWork)
{
    public async Task<Either<EncinaError, Unit>> ProcessBatchAsync(
        List<Order> orders,
        CancellationToken ct)
    {
        var bulkOps = unitOfWork.BulkOperations<Order>();

        // Begin transaction
        var begin = await unitOfWork.BeginTransactionAsync(ct);
        if (begin.IsLeft) return begin;

        // Bulk insert orders
        var insertResult = await bulkOps.BulkInsertAsync(orders, ct: ct);
        if (insertResult.IsLeft)
        {
            await unitOfWork.RollbackAsync(ct);
            return insertResult.Map(_ => Unit.Default);
        }

        // Commit transaction
        return await unitOfWork.CommitAsync(ct);
    }
}
```

## Specification Pattern

```csharp
// Define a specification
public class PendingOrdersSpecification : QuerySpecification<Order>
{
    public PendingOrdersSpecification()
    {
        Where(o => o.Status == OrderStatus.Pending);
        OrderByDescending(o => o.CreatedAt);
        Take(100);
    }
}

// Use with repository
var pendingOrders = await repository.ListAsync(new PendingOrdersSpecification(), ct);
```

## Unit of Work Pattern

```csharp
public async Task<Either<EncinaError, Unit>> TransferFundsAsync(
    IUnitOfWork uow,
    Guid sourceId,
    Guid targetId,
    decimal amount,
    CancellationToken ct)
{
    var accounts = uow.Repository<Account, Guid>();

    var begin = await uow.BeginTransactionAsync(ct);
    if (begin.IsLeft) return begin;

    var source = await accounts.GetByIdAsync(sourceId, ct);
    var target = await accounts.GetByIdAsync(targetId, ct);

    // Modify accounts...

    var save = await uow.SaveChangesAsync(ct);
    if (save.IsLeft)
    {
        await uow.RollbackAsync(ct);
        return save.Map(_ => Unit.Default);
    }

    return await uow.CommitAsync(ct);
}
```

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.EntityFrameworkCore` | EF Core implementation of repository, UoW, and bulk operations |
| `Encina.Dapper.SqlServer` | Dapper implementation for SQL Server |
| `Encina.ADO.SqlServer` | ADO.NET implementation for SQL Server |
| `Encina.MongoDB` | MongoDB implementation with native bulk operations |

## License

MIT License - see LICENSE file for details
