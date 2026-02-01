# Audit Trail Tracking in Encina

This guide explains how to automatically track audit information (who created/modified an entity and when) across all database providers. Encina provides both automatic tracking via EF Core interceptors and explicit helpers for non-EF Core providers.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [API Reference](#api-reference)
6. [Configuration Options](#configuration-options)
7. [Best Practices](#best-practices)
8. [Provider Support](#provider-support)
9. [Testing](#testing)
10. [FAQ](#faq)

---

## Overview

Audit tracking captures essential metadata about entity lifecycle:

| Field | Description | Interface |
|-------|-------------|-----------|
| **CreatedAtUtc** | UTC timestamp when entity was created | `ICreatedAtUtc` |
| **CreatedBy** | User ID who created the entity | `ICreatedBy` |
| **ModifiedAtUtc** | UTC timestamp of last modification | `IModifiedAtUtc` |
| **ModifiedBy** | User ID who last modified the entity | `IModifiedBy` |

### Why Audit Tracking?

| Benefit | Description |
|---------|-------------|
| **Compliance** | Meet regulatory requirements (GDPR, SOX, HIPAA) |
| **Debugging** | Know when and who made changes |
| **Analytics** | Track user activity patterns |
| **Security** | Detect unauthorized modifications |
| **Accountability** | Clear ownership of changes |

---

## The Problem

Manual audit field management leads to several issues:

### Challenge 1: Repetitive Code

Every create/update operation requires setting audit fields:

```csharp
// Without Encina - repetitive and error-prone
public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
{
    var order = new Order
    {
        // ... business fields
        CreatedAtUtc = DateTime.UtcNow,
        CreatedBy = _currentUser.Id
    };

    await _context.Orders.AddAsync(order);
    return order;
}

public async Task UpdateOrderAsync(Order order)
{
    order.ModifiedAtUtc = DateTime.UtcNow;  // Easy to forget!
    order.ModifiedBy = _currentUser.Id;     // Inconsistent naming?

    await _context.SaveChangesAsync();
}
```

### Challenge 2: Inconsistency Across Providers

Different data access patterns require different approaches:

```csharp
// EF Core
context.Entry(entity).State = EntityState.Modified;

// Dapper
await connection.ExecuteAsync("UPDATE...", entity);

// ADO.NET
command.Parameters.AddWithValue("@ModifiedAtUtc", DateTime.UtcNow);

// MongoDB
var update = Builders<Order>.Update.Set(o => o.ModifiedAtUtc, DateTime.UtcNow);
```

### Challenge 3: Testing Difficulty

Tests become dependent on system time:

```csharp
// Flaky test - depends on system clock
[Fact]
public void Order_WhenCreated_HasCreatedAtUtc()
{
    var order = new Order();
    order.CreatedAtUtc.ShouldBe(DateTime.UtcNow); // Might fail by milliseconds!
}
```

---

## The Solution

Encina provides two complementary approaches:

### 1. Automatic Interception (EF Core)

For EF Core, the `AuditInterceptor` automatically populates audit fields during `SaveChanges`:

```text
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Application    │────►│ AuditInterceptor │────►│   Database      │
│  SaveChanges()  │     │ Populates fields │     │   Persists      │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                               │
                               ▼
                        ┌──────────────────┐
                        │ Detects:         │
                        │ - ICreatedAtUtc  │
                        │ - ICreatedBy     │
                        │ - IModifiedAtUtc │
                        │ - IModifiedBy    │
                        └──────────────────┘
```

### 2. Automatic Population via Repository (Dapper/ADO.NET/MongoDB)

Starting with v0.12.0, Dapper, ADO.NET, and MongoDB repositories **automatically populate audit fields** when the repository is configured with `IRequestContext` and/or `TimeProvider`:

```text
┌─────────────────┐     ┌──────────────────────┐     ┌─────────────────┐
│  Application    │────►│ Repository.AddAsync  │────►│   Database      │
│  AddAsync()     │     │ Calls Populator      │     │   Persists      │
└─────────────────┘     └──────────────────────┘     └─────────────────┘
                               │
                               ▼
                        ┌─────────────────────────────┐
                        │ AuditFieldPopulator:        │
                        │ - PopulateForCreate(entity) │
                        │ - PopulateForUpdate(entity) │
                        └─────────────────────────────┘
```

This provides feature parity with EF Core's `AuditInterceptor` for non-EF Core providers.

### 3. Explicit Helpers (Manual Usage)

For scenarios requiring manual control, use `AuditFieldPopulator` or extension methods:

```csharp
// Static helper
AuditFieldPopulator.PopulateForCreate(entity, userId, timeProvider);

// Extension method
entity.WithAuditCreate(userId, timeProvider);
```

---

## Quick Start

### EF Core Approach

**Step 1: Configure services**

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseAuditing = true;
    config.AuditingOptions.TrackCreatedBy = true;
    config.AuditingOptions.TrackModifiedBy = true;
});
```

**Step 2: Implement the interface on your entity**

```csharp
// Option A: Implement interface directly
public class Order : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;

    // IAuditableEntity implementation
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}

// Option B: Extend base class (recommended for DDD)
public class Order : AuditedAggregateRoot<Guid>
{
    public string Description { get; set; } = string.Empty;

    public Order(Guid id) : base(id) { }
}
```

**Step 3: Use normally - fields are auto-populated**

```csharp
public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
{
    var order = new Order(Guid.NewGuid())
    {
        Description = request.Description
    };

    await _context.Orders.AddAsync(order);
    await _context.SaveChangesAsync();

    // order.CreatedAtUtc and order.CreatedBy are automatically set!
    return order;
}
```

### Non-EF Core Approach (Dapper/ADO.NET/MongoDB)

**Option A: Automatic via Repository (Recommended)**

Configure your repository with `IRequestContext` and `TimeProvider` for automatic audit field population:

**Step 1: Register IRequestContext and TimeProvider**

```csharp
// In your DI configuration
services.AddScoped<IRequestContext, HttpRequestContext>();
services.TryAddSingleton(TimeProvider.System);

// Add Dapper repository
services.AddEncinaDapper(config =>
{
    config.ConnectionString = connectionString;
    // IRequestContext and TimeProvider are automatically resolved
});

// Or ADO.NET repository
services.AddEncinaADO(config =>
{
    config.ConnectionString = connectionString;
});

// Or MongoDB repository
services.AddEncinaMongoDB(config =>
{
    config.ConnectionString = connectionString;
    config.DatabaseName = "mydb";
});
```

**Step 2: Implement the interface on your entity**

```csharp
public class Order : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
```

**Step 3: Use the repository - fields are auto-populated!**

```csharp
public class OrderService
{
    private readonly IFunctionalRepository<Order, Guid> _repository;

    public OrderService(IFunctionalRepository<Order, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Description = request.Description
        };

        // Audit fields are automatically populated!
        await _repository.AddAsync(order);

        // order.CreatedAtUtc and order.CreatedBy are set
        return order;
    }

    public async Task UpdateOrderAsync(Order order)
    {
        order.Description = "Updated";

        // Modified fields are automatically populated!
        await _repository.UpdateAsync(order);

        // order.ModifiedAtUtc and order.ModifiedBy are set
    }
}
```

**Option B: Manual with Helpers**

For scenarios requiring manual control:

```csharp
// Using static helper
public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
{
    var order = new Order
    {
        Id = Guid.NewGuid(),
        Description = request.Description
    };

    // Manually populate audit fields
    AuditFieldPopulator.PopulateForCreate(order, _currentUser.Id, _timeProvider);

    // Persist with Dapper
    await _connection.ExecuteAsync(
        "INSERT INTO Orders (Id, Description, CreatedAtUtc, CreatedBy) VALUES (@Id, @Description, @CreatedAtUtc, @CreatedBy)",
        order);

    return order;
}

// Using extension methods
public async Task UpdateOrderAsync(Order order)
{
    order.WithAuditUpdate(_currentUser.Id, _timeProvider);

    await _connection.ExecuteAsync(
        "UPDATE Orders SET Description = @Description, ModifiedAtUtc = @ModifiedAtUtc, ModifiedBy = @ModifiedBy WHERE Id = @Id",
        order);
}
```

---

## API Reference

### Interfaces

#### Granular Interfaces

| Interface | Property | Type | Description |
|-----------|----------|------|-------------|
| `ICreatedAtUtc` | `CreatedAtUtc` | `DateTime` | Creation timestamp |
| `ICreatedBy` | `CreatedBy` | `string?` | Creator user ID |
| `IModifiedAtUtc` | `ModifiedAtUtc` | `DateTime?` | Last modification timestamp |
| `IModifiedBy` | `ModifiedBy` | `string?` | Last modifier user ID |

#### Composite Interfaces

| Interface | Inherits From | Use Case |
|-----------|---------------|----------|
| `IAuditableEntity` | All four granular interfaces | EF Core interceptor pattern |
| `IAuditable` | All four (read-only) | Immutable records pattern |

#### Soft Delete Interface

| Interface | Properties | Description |
|-----------|------------|-------------|
| `ISoftDeletable` | `IsDeleted`, `DeletedAtUtc`, `DeletedBy` | Soft delete tracking |

### Base Classes

| Class | Description | Use Case |
|-------|-------------|----------|
| `AuditedEntity<TId>` | Entity with audit fields | Simple audited entities |
| `AuditedAggregateRoot<TId>` | Aggregate with audit + concurrency | DDD aggregates |
| `FullyAuditedAggregateRoot<TId>` | Audit + soft delete | Full audit trail |
| `AuditableAggregateRoot<TId>` | Immutable audit pattern | Records |
| `SoftDeletableAggregateRoot<TId>` | Immutable soft delete | Records with soft delete |

### AuditFieldPopulator

Static utility for populating audit fields:

```csharp
public static class AuditFieldPopulator
{
    // For entity creation
    public static T PopulateForCreate<T>(T entity, string? userId, TimeProvider timeProvider);

    // For entity modification
    public static T PopulateForUpdate<T>(T entity, string? userId, TimeProvider timeProvider);

    // For soft delete
    public static T PopulateForDelete<T>(T entity, string? userId, TimeProvider timeProvider);

    // For restore from soft delete
    public static T RestoreFromDelete<T>(T entity);
}
```

### Extension Methods

Available in provider-specific packages:

```csharp
// Creation
entity.WithAuditCreate(userId, timeProvider);

// Update
entity.WithAuditUpdate(userId, timeProvider);

// Soft delete
entity.WithAuditDelete(userId, timeProvider);

// Restore
entity.WithAuditRestore();
```

---

## Configuration Options

### AuditInterceptorOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable/disable auditing |
| `TrackCreatedAt` | `bool` | `true` | Track creation timestamps |
| `TrackCreatedBy` | `bool` | `true` | Track creator user ID |
| `TrackModifiedAt` | `bool` | `true` | Track modification timestamps |
| `TrackModifiedBy` | `bool` | `true` | Track modifier user ID |
| `LogAuditChanges` | `bool` | `false` | Log audit changes (debug) |
| `LogChangesToStore` | `bool` | `false` | Persist audit entries to IAuditLogStore |

### Example Configuration

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseAuditing = true;
    config.AuditingOptions = new AuditInterceptorOptions
    {
        Enabled = true,
        TrackCreatedAt = true,
        TrackCreatedBy = true,
        TrackModifiedAt = true,
        TrackModifiedBy = true,
        LogAuditChanges = false,  // Enable for debugging
        LogChangesToStore = true  // Enable for full audit trail
    };
});

// Register audit log store for detailed history
// Option 1: Use built-in database-backed store (recommended for production)
config.UseAuditLogStore = true;  // Auto-registers AuditLogStoreEF

// Option 2: Use InMemoryAuditLogStore for TESTING ONLY
// services.AddSingleton<IAuditLogStore, InMemoryAuditLogStore>();
```

---

## Best Practices

### When to Use Automatic Interceptor (EF Core)

- Standard CRUD operations with DbContext
- Need transparent audit tracking
- Want consistent behavior across all entities

### When to Use Manual Helpers

- Dapper raw SQL queries
- ADO.NET operations
- MongoDB driver
- Bulk operations bypassing EF Core
- Import/migration scripts

### Interface Selection

| Scenario | Recommended Interface |
|----------|----------------------|
| Standard mutable entity | `IAuditableEntity` |
| DDD aggregate root | `AuditedAggregateRoot<TId>` |
| With soft delete | `FullyAuditedAggregateRoot<TId>` |
| Immutable record | `IAuditable` + `AuditableAggregateRoot<TId>` |

### Soft Delete Pattern

```csharp
// Entity with soft delete
public class Order : FullyAuditedAggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { }

    public void Cancel(string userId)
    {
        // Business logic
        Delete(userId);  // Sets IsDeleted, DeletedAtUtc, DeletedBy
    }

    public void Reinstate()
    {
        // Business logic
        Restore();  // Clears deletion fields
    }
}
```

---

## Provider Support

Audit tracking is supported across all 13 database providers:

| Provider | Automatic (Interceptor/Repository) | Manual (Helpers) |
|----------|-----------------------------------|------------------|
| EF Core SQLite | ✅ via `AuditInterceptor` | ✅ |
| EF Core SQL Server | ✅ via `AuditInterceptor` | ✅ |
| EF Core PostgreSQL | ✅ via `AuditInterceptor` | ✅ |
| EF Core MySQL | ✅ via `AuditInterceptor` | ✅ |
| Dapper SQLite | ✅ via Repository (v0.12.0+) | ✅ |
| Dapper SQL Server | ✅ via Repository (v0.12.0+) | ✅ |
| Dapper PostgreSQL | ✅ via Repository (v0.12.0+) | ✅ |
| Dapper MySQL | ✅ via Repository (v0.12.0+) | ✅ |
| ADO.NET SQLite | ✅ via Repository (v0.12.0+) | ✅ |
| ADO.NET SQL Server | ✅ via Repository (v0.12.0+) | ✅ |
| ADO.NET PostgreSQL | ✅ via Repository (v0.12.0+) | ✅ |
| ADO.NET MySQL | ✅ via Repository (v0.12.0+) | ✅ |
| MongoDB | ✅ via Repository (v0.12.0+) | ✅ |

### Graceful Degradation

When `IRequestContext` is not available (null), audit operations continue normally:

- **Timestamps** (`CreatedAtUtc`, `ModifiedAtUtc`) are still populated using `TimeProvider`
- **User IDs** (`CreatedBy`, `ModifiedBy`) remain `null`
- **No exceptions** are thrown - operations complete successfully

```csharp
// Repository without IRequestContext - still works!
var repository = new FunctionalRepositoryDapper<Order, Guid>(
    connection,
    mapping,
    requestContext: null,  // No user context available
    TimeProvider.System);

var order = new Order { Id = Guid.NewGuid() };
await repository.AddAsync(order);

// order.CreatedAtUtc is set to current time
// order.CreatedBy is null (no user context)
```

---

## Testing

### Using FakeTimeProvider

For deterministic tests, inject `FakeTimeProvider`:

```csharp
[Fact]
public async Task Order_WhenCreated_SetsCreatedAtUtc()
{
    // Arrange
    var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
    var order = new Order(Guid.NewGuid());

    // Act
    AuditFieldPopulator.PopulateForCreate(order, "user-123", fakeTime);

    // Assert
    order.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    order.CreatedBy.ShouldBe("user-123");
}
```

### Testing EF Core Interceptor

```csharp
[Fact]
public async Task SaveChangesAsync_AddedEntity_SetsCreatedFields()
{
    // Arrange
    var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
    var serviceProvider = CreateServiceProviderWithUser("test-user");
    var options = new AuditInterceptorOptions { Enabled = true };
    var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

    await using var context = CreateInMemoryContext(interceptor);
    var entity = new Order(Guid.NewGuid());
    context.Orders.Add(entity);

    // Act
    await context.SaveChangesAsync();

    // Assert
    entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    entity.CreatedBy.ShouldBe("test-user");
}
```

---

## FAQ

### How do I handle system operations without a user?

Pass `null` as the user ID - timestamps are still set:

```csharp
// Background job with no user context
AuditFieldPopulator.PopulateForCreate(entity, userId: null, timeProvider);
// entity.CreatedAtUtc is set
// entity.CreatedBy is null
```

### Can I use different interfaces on different entities?

Yes! Implement only the interfaces you need:

```csharp
// Only track creation time (no user tracking)
public class SimpleEntity : ICreatedAtUtc
{
    public DateTime CreatedAtUtc { get; set; }
}

// Only track who modified (no timestamps)
public class ModifierOnly : IModifiedBy
{
    public string? ModifiedBy { get; set; }
}
```

### What about performance?

- **EF Core interceptor**: Minimal overhead - runs during SaveChanges
- **Manual helpers**: Zero overhead - just property assignment
- **Audit log store**: Consider async logging for high-throughput scenarios

### What about persistent audit log storage?

Encina provides persistent database-backed `IAuditLogStore` implementations for all 13 database providers:

- **EF Core**: `AuditLogStoreEF` - works with SQLite, SQL Server, PostgreSQL, MySQL
- **Dapper**: `AuditLogStoreDapper` - provider-specific implementations for all 4 databases
- **ADO.NET**: `AuditLogStoreADO` - provider-specific implementations for all 4 databases
- **MongoDB**: `AuditLogStoreMongoDB` - with optimized indexes for efficient history lookups

Enable persistent audit logging:

```csharp
// EF Core
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseAuditing = true;
    config.AuditingOptions.LogChangesToStore = true;
    config.UseAuditLogStore = true;  // Registers AuditLogStoreEF
});

// MongoDB
services.AddEncinaMongoDB(config =>
{
    config.UseAuditLogStore = true;  // Registers AuditLogStoreMongoDB
    config.CreateIndexes = true;     // Creates optimized indexes
});
```

### How do I query soft-deleted entities?

Use global query filters in EF Core:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Automatically filter out deleted entities
    modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
}

// To include deleted entities
var allOrders = await context.Orders
    .IgnoreQueryFilters()
    .ToListAsync();
```

### Can I customize the user ID source?

Yes, implement `IRequestContext` to provide user ID:

```csharp
public class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpRequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? UserId => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? TenantId => _accessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
    public string? CorrelationId => _accessor.HttpContext?.TraceIdentifier;
}

// Register in DI
services.AddScoped<IRequestContext, HttpRequestContext>();
```

---

## Audit Log Storage

### Current Implementations

| Store | Package | Status | Use Case |
|-------|---------|--------|----------|
| `InMemoryAuditLogStore` | `Encina.DomainModeling` | ✅ Available | **Testing only** |
| `AuditLogStoreEF` | `Encina.EntityFrameworkCore` | ✅ Available | EF Core (all 4 providers) |
| `AuditLogStoreDapper` | `Encina.Dapper.*` | ✅ Available | Dapper (SQLite, SqlServer, PostgreSQL, MySQL) |
| `AuditLogStoreADO` | `Encina.ADO.*` | ✅ Available | ADO.NET (SQLite, SqlServer, PostgreSQL, MySQL) |
| `AuditLogStoreMongoDB` | `Encina.MongoDB` | ✅ Available | MongoDB |

### Custom Implementation

To implement your own persistent store:

```csharp
public class SqlAuditLogStore : IAuditLogStore
{
    private readonly IDbConnection _connection;

    public SqlAuditLogStore(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        await _connection.ExecuteAsync(@"
            INSERT INTO AuditLog (Id, EntityType, EntityId, Action, UserId, TimestampUtc, OldValues, NewValues)
            VALUES (@Id, @EntityType, @EntityId, @Action, @UserId, @TimestampUtc, @OldValues, @NewValues)",
            new
            {
                entry.Id,
                entry.EntityType,
                entry.EntityId,
                Action = entry.Action.ToString(),
                entry.UserId,
                entry.TimestampUtc,
                OldValues = JsonSerializer.Serialize(entry.OldValues),
                NewValues = JsonSerializer.Serialize(entry.NewValues)
            });
    }

    public async Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
    {
        return await _connection.QueryAsync<AuditLogEntry>(@"
            SELECT * FROM AuditLog
            WHERE EntityType = @EntityType AND EntityId = @EntityId
            ORDER BY TimestampUtc DESC",
            new { EntityType = entityType, EntityId = entityId });
    }
}

// Register in DI
services.AddScoped<IAuditLogStore, SqlAuditLogStore>();
```

---

## See Also

- [Immutable Domain Models](./immutable-domain-models.md) - Using records with audit tracking
- [Multi-Tenancy](./multi-tenancy.md) - Combining audit tracking with tenant isolation
- [Issue #574](https://github.com/dlrivada/Encina/issues/574) - Persistent IAuditLogStore implementations
