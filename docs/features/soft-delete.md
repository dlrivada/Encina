# Soft Delete Pattern

Soft delete is a pattern where records are marked as deleted rather than being physically removed from the database. This approach provides data recovery capabilities, audit trail preservation, and compliance with data retention policies.

## Overview

Encina provides a comprehensive soft delete implementation with:

- **Two interface levels**: Read-only (`ISoftDeletable`) and mutable (`ISoftDeletableEntity`)
- **Base classes**: Pre-built entities with soft delete support
- **Automatic interception**: EF Core interceptor for transparent delete-to-update conversion
- **Repository extensions**: Specialized operations for soft-deleted entities
- **Global query filters**: Automatic exclusion of soft-deleted records
- **Pipeline behavior**: Query-level soft delete filtering

## Configuration

### Entity Framework Core

```csharp
// Program.cs
builder.Services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseSoftDelete = true;
    config.SoftDeleteOptions = new SoftDeleteOptions
    {
        AutoFilterSoftDeletedQueries = true  // Enable pipeline behavior filtering
    };
});
```

### Entity Implementation

Choose one of these approaches based on your needs:

#### Option 1: Implement Interface (Maximum Control)

```csharp
public class Order : IEntity<OrderId>, ISoftDeletableEntity
{
    public OrderId Id { get; set; }
    public string CustomerName { get; set; }

    // ISoftDeletableEntity properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // IAuditableEntity properties (optional)
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
```

#### Option 2: Use Base Class (Recommended)

```csharp
// Soft delete only
public class Order : SoftDeletableEntity<OrderId>
{
    public string CustomerName { get; set; }
    // Inherits: Id, IsDeleted, DeletedAtUtc, DeletedBy
}

// Full audit trail + soft delete
public class Order : FullyAuditedEntity<OrderId>
{
    public string CustomerName { get; set; }
    // Inherits all audit + soft delete properties
}

// For aggregates
public class Order : SoftDeletableAggregateRoot<OrderId>
{
    public string CustomerName { get; set; }
    // Inherits soft delete + domain events
}
```

### DbContext Configuration

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply soft delete configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Or use the extension method
            entity.ApplySoftDeleteQueryFilter();
        });
    }
}
```

## Interface Differences

| Interface | Properties | Use Case |
|-----------|-----------|----------|
| `ISoftDeletable` | Getters only | Domain entities with encapsulated state |
| `ISoftDeletableEntity` | Getters + Setters | Infrastructure-level entities, repository operations |

```csharp
// ISoftDeletable - Read-only (for domain entities)
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAtUtc { get; }
    string? DeletedBy { get; }
}

// ISoftDeletableEntity - Mutable (for interceptor/repository)
public interface ISoftDeletableEntity : ISoftDeletable
{
    new bool IsDeleted { get; set; }
    new DateTime? DeletedAtUtc { get; set; }
    new string? DeletedBy { get; set; }
}
```

## Soft Delete Operations

### Automatic Soft Delete (via Interceptor)

When `SoftDeleteInterceptor` is registered, any `Remove()` call on an entity implementing `ISoftDeletableEntity` is automatically converted to an update:

```csharp
// This triggers soft delete automatically
context.Orders.Remove(order);
await context.SaveChangesAsync();

// Result: IsDeleted = true, DeletedAtUtc = now, DeletedBy = currentUser
```

### Manual Soft Delete (via Domain Method)

For entities implementing only `ISoftDeletable`:

```csharp
public class Order : Entity<OrderId>, ISoftDeletable
{
    private bool _isDeleted;
    private DateTime? _deletedAtUtc;
    private string? _deletedBy;

    public bool IsDeleted => _isDeleted;
    public DateTime? DeletedAtUtc => _deletedAtUtc;
    public string? DeletedBy => _deletedBy;

    public void Delete(string deletedBy, DateTime deletedAtUtc)
    {
        if (_isDeleted)
            throw new InvalidOperationException("Order is already deleted");

        _isDeleted = true;
        _deletedAtUtc = deletedAtUtc;
        _deletedBy = deletedBy;

        AddDomainEvent(new OrderDeletedEvent(Id));
    }
}

// Usage
order.Delete("user-123", DateTime.UtcNow);
await repository.Update(order);
```

### Repository Operations

The `ISoftDeleteRepository<TEntity, TId>` provides specialized operations:

```csharp
public interface ISoftDeleteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>, ISoftDeletable
{
    // Include soft-deleted entities in queries
    Task<Either<RepositoryError, TEntity>> GetByIdWithDeletedAsync(TId id, CancellationToken ct = default);
    Task<Either<RepositoryError, IReadOnlyList<TEntity>>> ListWithDeletedAsync(Specification<TEntity> spec, CancellationToken ct = default);

    // Restore soft-deleted entity
    Task<Either<RepositoryError, TEntity>> RestoreAsync(TId id, CancellationToken ct = default);

    // Permanently delete (bypass soft delete)
    Task<Either<RepositoryError, Unit>> HardDeleteAsync(TId id, CancellationToken ct = default);
}
```

#### Query Including Deleted Entities

```csharp
// Get a soft-deleted entity
var result = await repository.GetByIdWithDeletedAsync(orderId);

result.Match(
    Right: order => Console.WriteLine($"Found: {order.CustomerName}, Deleted: {order.IsDeleted}"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);

// List all entities including soft-deleted
var spec = new OrdersByCustomerSpec(customerId);
var allOrders = await repository.ListWithDeletedAsync(spec);
```

#### Restore Soft-Deleted Entity

```csharp
var result = await repository.RestoreAsync(orderId);

result.Match(
    Right: order => Console.WriteLine($"Restored: {order.CustomerName}"),
    Left: error => Console.WriteLine($"Failed: {error.Message}")
);
```

#### Hard Delete (Permanent Removal)

```csharp
// Caution: This permanently deletes the entity
var result = await repository.HardDeleteAsync(orderId);

result.Match(
    Right: _ => Console.WriteLine("Permanently deleted"),
    Left: error => Console.WriteLine($"Failed: {error.Message}")
);
```

## Pipeline Behavior Integration

### IIncludeDeleted Marker Interface

For queries that need to include soft-deleted entities:

```csharp
// Query that includes deleted entities
public record GetAllOrdersQuery(Guid CustomerId)
    : IQuery<IReadOnlyList<Order>>, IIncludeDeleted;

// Normal query (excludes deleted)
public record GetActiveOrdersQuery(Guid CustomerId)
    : IQuery<IReadOnlyList<Order>>;
```

### Soft Delete Filter Context

The `SoftDeleteQueryFilterBehavior` automatically configures the filter context:

```csharp
// In your repository or query handler
public class OrderRepository : ISoftDeleteRepository<Order, OrderId>
{
    private readonly ISoftDeleteFilterContext _filterContext;

    public async Task<IReadOnlyList<Order>> GetOrdersAsync()
    {
        var query = _context.Orders.AsQueryable();

        // Respect the filter context
        if (_filterContext.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.ToListAsync();
    }
}
```

## Interceptor Configuration

```csharp
// SoftDeleteInterceptorOptions
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseSoftDelete = true;
    config.SoftDeleteInterceptorOptions = new SoftDeleteInterceptorOptions
    {
        Enabled = true,             // Enable/disable interceptor
        TrackDeletedAt = true,      // Automatically set DeletedAtUtc
        TrackDeletedBy = true,      // Automatically set DeletedBy from IRequestContext
        LogSoftDeletes = false      // Log soft delete operations
    };
});
```

## Provider Support

| Provider | Soft Delete | Notes |
|----------|:-----------:|-------|
| EF Core (SQLite) | ✅ | Full support with interceptor |
| EF Core (SQL Server) | ✅ | Full support with interceptor |
| EF Core (PostgreSQL) | ✅ | Full support with interceptor |
| EF Core (MySQL) | ✅ | Full support with interceptor |
| Dapper (all DBs) | ✅ | Manual filtering required |
| ADO.NET (all DBs) | ✅ | Manual filtering required |
| MongoDB | ✅ | Filter builder support |

## Best Practices

1. **Choose the right interface**: Use `ISoftDeletableEntity` for infrastructure, `ISoftDeletable` for domain entities
2. **Always apply query filters**: Ensure soft-deleted entities are excluded by default
3. **Use IIncludeDeleted sparingly**: Only for admin/audit views that need to see deleted records
4. **Consider data retention policies**: Implement scheduled hard deletes for compliance
5. **Test both paths**: Verify soft delete and restore operations in your tests

## Related Features

- [Temporal Tables](temporal-tables.md) - Point-in-time queries for SQL Server
- [Audit Trail](audit-tracking.md) - Track who created/modified entities
- [Patterns Guide](../architecture/patterns-guide.md) - Data access patterns
