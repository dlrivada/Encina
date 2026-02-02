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

## Security Audit Trail (Pipeline Behavior)

Starting with v0.12.0, Encina provides a comprehensive **Security Audit Trail** system that automatically logs all command and query operations through a pipeline behavior. This is separate from entity-level audit fields and provides operation-level audit logging.

### Overview

The Security Audit system captures:

| Field | Description |
|-------|-------------|
| **Action** | The operation type (Create, Update, Delete, Get, List, etc.) |
| **EntityType** | The request/command type being executed |
| **UserId** | Who performed the operation |
| **Outcome** | Success, Failure, or Denied |
| **Duration** | How long the operation took |
| **Payloads** | Request and response data (with PII redaction) |
| **Metadata** | IP address, user agent, correlation ID |

### Quick Start

```csharp
// Enable audit trail logging
services.AddEncinaAudit(options =>
{
    options.AuditAllCommands = true;    // Audit all commands
    options.AuditAllQueries = false;    // Don't audit queries by default
    options.IncludeRequestPayload = true;
    options.IncludeResponsePayload = false;
    options.EnableAutoPurge = true;     // Auto-delete old entries
    options.RetentionDays = 90;
});

// Mark specific operations for auditing
[Auditable(Action = "CreateOrder", SensitiveFields = ["CreditCard", "Password"])]
public record CreateOrderCommand(string CustomerId, string CreditCard) : ICommand<OrderId>;
```

### Payload Capture Configuration

Control what data is captured in audit entries:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `IncludeRequestPayload` | `bool` | `true` | Capture request data |
| `IncludeResponsePayload` | `bool` | `false` | Capture response data |
| `MaxPayloadSizeBytes` | `int` | `65536` | Maximum payload size (truncated if exceeded) |
| `IncludePayloadHash` | `bool` | `true` | Include SHA256 hash of original payload |

**Configuration Example (appsettings.json):**

```json
{
  "Encina": {
    "Audit": {
      "AuditAllCommands": true,
      "AuditAllQueries": false,
      "IncludeRequestPayload": true,
      "IncludeResponsePayload": false,
      "MaxPayloadSizeBytes": 65536,
      "IncludePayloadHash": true,
      "RetentionDays": 90,
      "EnableAutoPurge": true,
      "PurgeIntervalHours": 24
    }
  }
}
```

### Sensitive Data Redaction

Encina automatically redacts sensitive fields from audit payloads to protect PII:

**Default Sensitive Field Patterns:**

- `password`, `secret`, `token`, `key`, `apikey`, `api_key`
- `authorization`, `bearer`, `credential`
- `ssn`, `socialsecuritynumber`, `social_security_number`
- `creditcard`, `credit_card`, `cardnumber`, `card_number`, `cvv`, `cvc`, `pin`
- `accesstoken`, `access_token`, `refreshtoken`, `refresh_token`
- `privatekey`, `private_key`, `connectionstring`, `connection_string`

**Adding Global Sensitive Fields:**

```csharp
services.AddEncinaAudit(options =>
{
    options.GlobalSensitiveFields = ["AccountNumber", "TaxId", "DateOfBirth"];
});
```

**Per-Operation Sensitive Fields:**

```csharp
[Auditable(SensitiveFields = ["CreditCard", "CVV", "BankAccount"])]
public record ProcessPaymentCommand(string CreditCard, string CVV) : ICommand<PaymentResult>;
```

**Redacted Payload Example:**

```json
{
  "customerId": "cust-123",
  "creditCard": "[REDACTED]",
  "cvv": "[REDACTED]",
  "amount": 99.99
}
```

### IPiiMasker Interface

The `IPiiMasker` interface allows custom redaction implementations:

```csharp
public interface IPiiMasker
{
    T MaskForAudit<T>(T request) where T : notnull;
    object MaskForAudit(object request);
}
```

**Built-in Implementations:**

| Implementation | Description |
|----------------|-------------|
| `NullPiiMasker` | No redaction (default) |
| `DefaultSensitiveDataRedactor` | JSON-based field redaction |

**Using DefaultSensitiveDataRedactor:**

```csharp
services.AddEncinaAudit(options =>
{
    // Enables DefaultSensitiveDataRedactor
    options.GlobalSensitiveFields = ["Password", "ApiKey"];
});

// Or register manually
services.AddSingleton<IPiiMasker, DefaultSensitiveDataRedactor>();
```

### Auto-Purge Configuration

Automatically delete old audit entries to manage storage:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableAutoPurge` | `bool` | `false` | Enable automatic purging |
| `RetentionDays` | `int` | `2555` | Days to keep entries (~7 years) |
| `PurgeIntervalHours` | `int` | `24` | Hours between purge runs |

```csharp
services.AddEncinaAudit(options =>
{
    options.EnableAutoPurge = true;
    options.RetentionDays = 90;        // Keep 90 days
    options.PurgeIntervalHours = 12;   // Run every 12 hours
});
```

The `AuditRetentionService` is a `BackgroundService` that runs automatically when `EnableAutoPurge` is true.

### Querying Audit Entries

Use `IAuditStore.QueryAsync` with `AuditQuery` for flexible querying:

**AuditQuery Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `UserId` | `string?` | Filter by user |
| `TenantId` | `string?` | Filter by tenant |
| `EntityType` | `string?` | Filter by entity/request type |
| `EntityId` | `string?` | Filter by specific entity |
| `Action` | `string?` | Filter by action (Create, Update, etc.) |
| `Outcome` | `AuditOutcome?` | Filter by outcome (Success, Failure, Denied) |
| `CorrelationId` | `string?` | Filter by correlation ID |
| `FromUtc` | `DateTime?` | Filter by minimum timestamp |
| `ToUtc` | `DateTime?` | Filter by maximum timestamp |
| `IpAddress` | `string?` | Filter by IP address |
| `MinDuration` | `TimeSpan?` | Filter by minimum duration |
| `MaxDuration` | `TimeSpan?` | Filter by maximum duration |
| `PageNumber` | `int` | Page number (default: 1) |
| `PageSize` | `int` | Page size (default: 50, max: 1000) |

**Query Examples:**

```csharp
// Inject the audit store
public class AuditController
{
    private readonly IAuditStore _auditStore;

    public AuditController(IAuditStore auditStore) => _auditStore = auditStore;

    // Query by user with date range
    public async Task<PagedResult<AuditEntry>> GetUserActivity(string userId)
    {
        var query = new AuditQuery
        {
            UserId = userId,
            FromUtc = DateTime.UtcNow.AddDays(-30),
            PageSize = 100
        };

        var result = await _auditStore.QueryAsync(query);
        return result.Match(
            Right: pagedResult => pagedResult,
            Left: error => throw new InvalidOperationException(error.Message));
    }

    // Query failed operations
    public async Task<PagedResult<AuditEntry>> GetFailedOperations()
    {
        var query = AuditQuery.Builder()
            .WithOutcome(AuditOutcome.Failure)
            .InDateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
            .WithPageSize(50)
            .Build();

        var result = await _auditStore.QueryAsync(query);
        return result.Match(r => r, e => throw new Exception(e.Message));
    }

    // Query slow operations
    public async Task<PagedResult<AuditEntry>> GetSlowOperations()
    {
        var query = new AuditQuery
        {
            MinDuration = TimeSpan.FromSeconds(5),
            Outcome = AuditOutcome.Success,
            PageSize = 20
        };

        var result = await _auditStore.QueryAsync(query);
        return result.Match(r => r, e => throw new Exception(e.Message));
    }
}
```

**PagedResult Structure:**

```csharp
public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
}
```

### Store-Specific Setup

#### EF Core (All 4 Databases)

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseAuditStore = true;  // Registers AuditStoreEF
});

// Run migrations to create SecurityAuditEntries table
dotnet ef migrations add AddSecurityAuditEntries
dotnet ef database update
```

**Migration adds:**

```csharp
migrationBuilder.CreateTable(
    name: "SecurityAuditEntries",
    columns: table => new
    {
        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
        CorrelationId = table.Column<string>(nullable: false),
        UserId = table.Column<string>(nullable: true),
        TenantId = table.Column<string>(nullable: true),
        Action = table.Column<string>(nullable: false),
        EntityType = table.Column<string>(nullable: false),
        EntityId = table.Column<string>(nullable: true),
        Outcome = table.Column<int>(nullable: false),
        ErrorMessage = table.Column<string>(nullable: true),
        TimestampUtc = table.Column<DateTime>(nullable: false),
        StartedAtUtc = table.Column<DateTimeOffset>(nullable: false),
        CompletedAtUtc = table.Column<DateTimeOffset>(nullable: false),
        IpAddress = table.Column<string>(nullable: true),
        UserAgent = table.Column<string>(nullable: true),
        RequestPayloadHash = table.Column<string>(nullable: true),
        RequestPayload = table.Column<string>(nullable: true),
        ResponsePayload = table.Column<string>(nullable: true),
        Metadata = table.Column<string>(nullable: true)
    });
```

#### Dapper (SQLite, SQL Server, PostgreSQL, MySQL)

Execute the appropriate SQL script for your database:

**SQL Server:**

```sql
CREATE TABLE SecurityAuditEntries (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CorrelationId NVARCHAR(100) NOT NULL,
    UserId NVARCHAR(256),
    TenantId NVARCHAR(100),
    Action NVARCHAR(100) NOT NULL,
    EntityType NVARCHAR(500) NOT NULL,
    EntityId NVARCHAR(256),
    Outcome INT NOT NULL,
    ErrorMessage NVARCHAR(MAX),
    TimestampUtc DATETIME2 NOT NULL,
    StartedAtUtc DATETIMEOFFSET NOT NULL,
    CompletedAtUtc DATETIMEOFFSET NOT NULL,
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    RequestPayloadHash NVARCHAR(64),
    RequestPayload NVARCHAR(MAX),
    ResponsePayload NVARCHAR(MAX),
    Metadata NVARCHAR(MAX)
);

CREATE INDEX IX_SecurityAuditEntries_UserId ON SecurityAuditEntries(UserId);
CREATE INDEX IX_SecurityAuditEntries_EntityType_EntityId ON SecurityAuditEntries(EntityType, EntityId);
CREATE INDEX IX_SecurityAuditEntries_TimestampUtc ON SecurityAuditEntries(TimestampUtc);
CREATE INDEX IX_SecurityAuditEntries_CorrelationId ON SecurityAuditEntries(CorrelationId);
```

**PostgreSQL:**

```sql
CREATE TABLE "SecurityAuditEntries" (
    "Id" UUID PRIMARY KEY,
    "CorrelationId" VARCHAR(100) NOT NULL,
    "UserId" VARCHAR(256),
    "TenantId" VARCHAR(100),
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(500) NOT NULL,
    "EntityId" VARCHAR(256),
    "Outcome" INTEGER NOT NULL,
    "ErrorMessage" TEXT,
    "TimestampUtc" TIMESTAMP NOT NULL,
    "StartedAtUtc" TIMESTAMPTZ NOT NULL,
    "CompletedAtUtc" TIMESTAMPTZ NOT NULL,
    "IpAddress" VARCHAR(45),
    "UserAgent" VARCHAR(500),
    "RequestPayloadHash" VARCHAR(64),
    "RequestPayload" TEXT,
    "ResponsePayload" TEXT,
    "Metadata" JSONB
);

CREATE INDEX "IX_SecurityAuditEntries_UserId" ON "SecurityAuditEntries"("UserId");
CREATE INDEX "IX_SecurityAuditEntries_EntityType_EntityId" ON "SecurityAuditEntries"("EntityType", "EntityId");
CREATE INDEX "IX_SecurityAuditEntries_TimestampUtc" ON "SecurityAuditEntries"("TimestampUtc");
```

**MySQL:**

```sql
CREATE TABLE `SecurityAuditEntries` (
    `Id` CHAR(36) PRIMARY KEY,
    `CorrelationId` VARCHAR(100) NOT NULL,
    `UserId` VARCHAR(256),
    `TenantId` VARCHAR(100),
    `Action` VARCHAR(100) NOT NULL,
    `EntityType` VARCHAR(500) NOT NULL,
    `EntityId` VARCHAR(256),
    `Outcome` INT NOT NULL,
    `ErrorMessage` TEXT,
    `TimestampUtc` DATETIME(6) NOT NULL,
    `StartedAtUtc` DATETIME(6) NOT NULL,
    `CompletedAtUtc` DATETIME(6) NOT NULL,
    `IpAddress` VARCHAR(45),
    `UserAgent` VARCHAR(500),
    `RequestPayloadHash` VARCHAR(64),
    `RequestPayload` LONGTEXT,
    `ResponsePayload` LONGTEXT,
    `Metadata` JSON
);

CREATE INDEX `IX_SecurityAuditEntries_UserId` ON `SecurityAuditEntries`(`UserId`);
CREATE INDEX `IX_SecurityAuditEntries_EntityType_EntityId` ON `SecurityAuditEntries`(`EntityType`, `EntityId`);
CREATE INDEX `IX_SecurityAuditEntries_TimestampUtc` ON `SecurityAuditEntries`(`TimestampUtc`);
```

**SQLite:**

```sql
CREATE TABLE "SecurityAuditEntries" (
    "Id" TEXT PRIMARY KEY,
    "CorrelationId" TEXT NOT NULL,
    "UserId" TEXT,
    "TenantId" TEXT,
    "Action" TEXT NOT NULL,
    "EntityType" TEXT NOT NULL,
    "EntityId" TEXT,
    "Outcome" INTEGER NOT NULL,
    "ErrorMessage" TEXT,
    "TimestampUtc" TEXT NOT NULL,
    "StartedAtUtc" TEXT NOT NULL,
    "CompletedAtUtc" TEXT NOT NULL,
    "IpAddress" TEXT,
    "UserAgent" TEXT,
    "RequestPayloadHash" TEXT,
    "RequestPayload" TEXT,
    "ResponsePayload" TEXT,
    "Metadata" TEXT
);

CREATE INDEX "IX_SecurityAuditEntries_UserId" ON "SecurityAuditEntries"("UserId");
CREATE INDEX "IX_SecurityAuditEntries_EntityType_EntityId" ON "SecurityAuditEntries"("EntityType", "EntityId");
CREATE INDEX "IX_SecurityAuditEntries_TimestampUtc" ON "SecurityAuditEntries"("TimestampUtc");
```

#### ADO.NET (SQLite, SQL Server, PostgreSQL, MySQL)

Use the same SQL scripts as Dapper (above).

#### MongoDB

```csharp
services.AddEncinaMongoDB(config =>
{
    config.UseAuditStore = true;
    config.CreateIndexes = true;  // Creates optimized indexes
});
```

**Collection: `security_audit_entries`**

**Indexes created:**

```javascript
db.security_audit_entries.createIndex({ "UserId": 1 })
db.security_audit_entries.createIndex({ "EntityType": 1, "EntityId": 1 })
db.security_audit_entries.createIndex({ "TimestampUtc": -1 })
db.security_audit_entries.createIndex({ "CorrelationId": 1 })
db.security_audit_entries.createIndex({ "TenantId": 1 })
```

#### InMemoryAuditStore (Testing/Development)

```csharp
// Default - InMemoryAuditStore is registered automatically
services.AddEncinaAudit();

// For testing
var store = new InMemoryAuditStore();
await store.RecordAsync(entry);
var entries = store.GetAllEntries();  // Testing helper
store.Clear();  // Reset between tests
```

### Provider Comparison Table

| Provider | GUID Column | DateTime Column | Payload Column | Index Support |
|----------|-------------|-----------------|----------------|---------------|
| SQL Server | `UNIQUEIDENTIFIER` | `DATETIMEOFFSET` | `NVARCHAR(MAX)` | Full |
| PostgreSQL | `UUID` | `TIMESTAMPTZ` | `JSONB` | Full + JSON |
| MySQL | `CHAR(36)` | `DATETIME(6)` | `JSON` | Full + JSON |
| SQLite | `TEXT` | `TEXT` (ISO 8601) | `TEXT` | Basic |
| MongoDB | `UUID` (BSON) | `Date` | `Document` | Full + TTL |
| EF Core | Provider-dependent | Provider-dependent | Provider-dependent | Via migrations |

### Performance Recommendations

| Scenario | Recommendation |
|----------|----------------|
| High-volume writes | Use async `RecordAsync`, consider batching |
| Large payloads | Set `MaxPayloadSizeBytes` limit, disable response capture |
| Query performance | Create indexes on frequently queried fields |
| Storage management | Enable `EnableAutoPurge` with appropriate `RetentionDays` |
| PII compliance | Configure `GlobalSensitiveFields` comprehensively |

---

## See Also

- [Immutable Domain Models](./immutable-domain-models.md) - Using records with audit tracking
- [Multi-Tenancy](./multi-tenancy.md) - Combining audit tracking with tenant isolation
- [Issue #395](https://github.com/dlrivada/Encina/issues/395) - Security Audit Trail implementation
