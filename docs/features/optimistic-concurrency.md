# Optimistic Concurrency in Encina

This guide explains how to implement optimistic concurrency control across all database providers in Encina. The feature provides consistent version-based conflict detection with Railway Oriented Programming (ROP) for explicit error handling.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [API Reference](#api-reference)
6. [Conflict Resolution Strategies](#conflict-resolution-strategies)
7. [Provider Support](#provider-support)
8. [Configuration Options](#configuration-options)
9. [Testing](#testing)
10. [FAQ](#faq)

---

## Overview

Optimistic concurrency control prevents lost updates when multiple users modify the same entity simultaneously. Encina provides:

| Feature | Description |
|---------|-------------|
| **Integer Versioning** | `IVersioned` / `IVersionedEntity` for explicit version tracking |
| **Row Versioning** | `IConcurrencyAwareEntity` for timestamp-based tokens (EF Core) |
| **Conflict Detection** | Returns `Either<EncinaError, T>` instead of throwing exceptions |
| **Conflict Information** | `ConcurrencyConflictInfo<T>` captures all entity states for resolution |
| **Built-in Resolvers** | `LastWriteWinsResolver`, `FirstWriteWinsResolver`, `MergeResolver` |
| **Provider Coherence** | Same interfaces work across EF Core, Dapper, ADO.NET, MongoDB, and Marten |

### Why Optimistic Concurrency?

| Benefit | Description |
|---------|-------------|
| **Data Integrity** | Prevents silent overwrites of concurrent changes |
| **User Experience** | Detect conflicts and let users decide how to proceed |
| **Scalability** | No locking overhead - better performance under high load |
| **Auditability** | Version tracking provides change history |

---

## The Problem

Without concurrency control, simultaneous updates can cause lost updates:

### The Lost Update Problem

```text
Time    User A                          User B                          Database
─────   ──────                          ──────                          ────────
T1      Read Order (v1, qty=10)                                         v1, qty=10
T2                                      Read Order (v1, qty=10)         v1, qty=10
T3      Update qty=15                                                   v1, qty=15
T4                                      Update qty=20                   v1, qty=20
T5      ← User A's change is LOST! →                                    qty=20
```

### Challenge 1: Silent Data Loss

Traditional "last write wins" silently discards changes:

```csharp
// Without concurrency control - User A's changes are lost
public async Task UpdateOrderAsync(Order order)
{
    _context.Orders.Update(order);
    await _context.SaveChangesAsync(); // No conflict detection!
}
```

### Challenge 2: Inconsistent Error Handling

Different providers handle conflicts differently:

```csharp
// EF Core throws DbUpdateConcurrencyException
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex) { ... }

// Dapper returns 0 rows affected
var rowsAffected = await connection.ExecuteAsync(sql);
if (rowsAffected == 0) { /* Is it NotFound or Conflict? */ }

// MongoDB throws MongoWriteException
```

### Challenge 3: No Context for Resolution

When a conflict occurs, you need all entity states to resolve it:

```csharp
// What was the original state?
// What did we try to save?
// What's currently in the database?
// Without this info, meaningful resolution is impossible
```

---

## The Solution

Encina provides a unified concurrency approach with ROP:

### 1. Version-Based Tracking

```csharp
// Mark entities with versioning interface
public class Order : IVersionedEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public int Version { get; set; }  // Auto-incremented on save

    // IVersioned explicit implementation
    long IVersioned.Version => Version;
}
```

### 2. Consistent Conflict Detection

```text
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  UpdateAsync()  │────►│ Version Check    │────►│   Database      │
│  version = 2    │     │ WHERE v = 1      │     │   v = 1 → 2?    │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │ rowsAffected == 0?   │
                    │                      │
                    │ EXISTS → Conflict    │
                    │ NOT EXISTS → NotFound│
                    └──────────────────────┘
```

### 3. Rich Error Information

```csharp
// Returns Either instead of throwing
var result = await repository.UpdateAsync(id, modifiedEntity);

result.Match(
    Right: updated => Console.WriteLine($"Success: v{updated.Version}"),
    Left: error =>
    {
        if (error.GetCode().IfSome(c => c == RepositoryErrors.ConcurrencyConflictErrorCode))
        {
            var details = error.GetDetails();
            var current = details["CurrentEntity"];
            var proposed = details["ProposedEntity"];
            var database = details["DatabaseEntity"];
            // Now you have all the context for resolution!
        }
    });
```

---

## Quick Start

### EF Core

```csharp
// 1. Define versioned entity
public class Order : IVersionedEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public int Version { get; set; }

    long IVersioned.Version => Version;
}

// 2. Configure EF Core model
modelBuilder.Entity<Order>(entity =>
{
    entity.Property(e => e.Version)
        .IsRequired()
        .IsConcurrencyToken();  // EF Core handles this automatically
});

// 3. Use with repository
var result = await repository.UpdateAsync(orderId, modifiedOrder);

result.Match(
    Right: order => logger.LogInformation("Updated order to version {Version}", order.Version),
    Left: error => logger.LogWarning("Update failed: {Error}", error.Message));
```

### Dapper

```csharp
// 1. Define versioned entity (same interface)
public class Order : IVersionedEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public int Version { get; set; }

    long IVersioned.Version => Version;
}

// 2. Repository automatically handles versioning
var result = await repository.UpdateAsync(orderId, modifiedOrder);

// Result contains ConcurrencyConflictInfo on conflict
result.IfLeft(error =>
{
    var details = error.GetDetails();
    if (details.ContainsKey("DatabaseEntity"))
    {
        // Conflict detected - database has different version
    }
});
```

### ADO.NET

```csharp
// Same pattern - IVersionedEntity works with ADO.NET repositories
public class Order : IVersionedEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public int Version { get; set; }

    long IVersioned.Version => Version;
}

// Repository generates versioned SQL:
// UPDATE Orders SET ..., [Version] = @NewVersion
// WHERE Id = @Id AND [Version] = @OriginalVersion
```

### MongoDB

```csharp
// MongoDB uses the same interface
public class Order : IVersionedEntity
{
    [BsonId]
    public Guid Id { get; set; }
    public string Description { get; set; }
    public int Version { get; set; }

    long IVersioned.Version => Version;
}

// MongoDB repository uses FindOneAndUpdate with version filter:
// { "_id": id, "Version": originalVersion }
```

### Marten (Event Sourcing)

```csharp
// Marten uses event stream versioning (different from entity versioning)
// Aggregates automatically track version via event stream

var result = await aggregateRepository.SaveAsync(aggregate);

result.IfLeft(error =>
{
    if (error.GetCode().IfSome(c => c == MartenErrorCodes.ConcurrencyConflict))
    {
        // Event stream version conflict
        var details = error.GetDetails();
        var expectedVersion = details["ExpectedVersion"];
        var aggregateVersion = details["AggregateVersion"];
    }
});
```

---

## API Reference

### Core Interfaces

#### `IVersioned`

Read-only interface for entities with version tracking:

```csharp
public interface IVersioned
{
    /// <summary>
    /// Gets the current version number for optimistic concurrency.
    /// </summary>
    long Version { get; }
}
```

#### `IVersionedEntity`

Mutable interface for entities that can have their version modified:

```csharp
public interface IVersionedEntity : IVersioned
{
    /// <summary>
    /// Gets or sets the current version number.
    /// </summary>
    new int Version { get; set; }
}
```

#### `IConcurrencyAwareEntity`

For timestamp-based row versioning (EF Core SQL Server):

```csharp
public interface IConcurrencyAwareEntity
{
    /// <summary>
    /// Gets or sets the row version for optimistic concurrency.
    /// </summary>
    byte[]? RowVersion { get; set; }
}
```

### ConcurrencyConflictInfo<TEntity>

Captures all entity states when a conflict occurs:

```csharp
public sealed record ConcurrencyConflictInfo<TEntity>(
    TEntity CurrentEntity,    // Entity when originally loaded
    TEntity ProposedEntity,   // Entity we tried to save
    TEntity? DatabaseEntity)  // Current database state (null if deleted)
    where TEntity : class
{
    /// <summary>
    /// True if DatabaseEntity is null (entity was deleted by another process).
    /// </summary>
    public bool WasDeleted => DatabaseEntity is null;

    /// <summary>
    /// Converts to dictionary for error details.
    /// </summary>
    public ImmutableDictionary<string, object?> ToDictionary(
        JsonSerializerOptions? serializerOptions = null);
}
```

### RepositoryErrors

Factory methods for concurrency errors:

```csharp
// With entity ID
var error = RepositoryErrors.ConcurrencyConflict<Order, Guid>(orderId, exception);

// Without ID
var error = RepositoryErrors.ConcurrencyConflict<Order>(exception);

// With full conflict information
var conflictInfo = new ConcurrencyConflictInfo<Order>(current, proposed, database);
var error = RepositoryErrors.ConcurrencyConflict(conflictInfo, exception);

// Error code constant
string code = RepositoryErrors.ConcurrencyConflictErrorCode; // "Repository.ConcurrencyConflict"
```

---

## Conflict Resolution Strategies

Encina provides three built-in conflict resolvers:

### LastWriteWinsResolver

Always uses the proposed entity (current operation wins):

```csharp
public sealed class LastWriteWinsResolver<TEntity> : IConcurrencyConflictResolver<TEntity>

// Usage
var resolver = new LastWriteWinsResolver<Order>();
var result = await resolver.ResolveAsync(current, proposed, database);
// Result: proposed entity with version = database.Version + 1
```

**Use when:** Latest value should always be preferred (settings, configuration).

**Warning:** Can cause lost updates - use carefully.

### FirstWriteWinsResolver

Always uses the database entity (first committed change wins):

```csharp
public sealed class FirstWriteWinsResolver<TEntity> : IConcurrencyConflictResolver<TEntity>

// Usage
var resolver = new FirstWriteWinsResolver<Order>();
var result = await resolver.ResolveAsync(current, proposed, database);
// Result: database entity unchanged
```

**Use when:** Idempotent operations where first value should be preserved.

**Note:** Current operation's changes are silently discarded.

### MergeResolver (Custom)

Base class for implementing domain-specific merge logic:

```csharp
public class OrderMergeResolver : MergeResolver<Order>
{
    protected override Task<Either<EncinaError, Order>> MergeAsync(
        Order current,
        Order proposed,
        Order database,
        CancellationToken cancellationToken)
    {
        // Check for conflicting changes
        var dbChangedStatus = current.Status != database.Status;
        var weChangedStatus = current.Status != proposed.Status;

        if (dbChangedStatus && weChangedStatus)
        {
            // Both changed Status - cannot merge
            var conflictInfo = new ConcurrencyConflictInfo<Order>(current, proposed, database);
            return Task.FromResult(
                Either<EncinaError, Order>.Left(
                    RepositoryErrors.ConcurrencyConflict(conflictInfo)));
        }

        // Merge non-conflicting changes
        var merged = new Order
        {
            Id = database.Id,
            Status = weChangedStatus ? proposed.Status : database.Status,
            Notes = proposed.Notes,  // Always take our notes
            Version = database.Version + 1
        };

        return Task.FromResult(Either<EncinaError, Order>.Right(merged));
    }
}

// Register in DI
services.AddSingleton<IConcurrencyConflictResolver<Order>, OrderMergeResolver>();
```

### Using Resolvers in Application Code

```csharp
public class OrderService
{
    private readonly IFunctionalRepository<Order, Guid> _repository;
    private readonly IConcurrencyConflictResolver<Order> _resolver;

    public async Task<Either<EncinaError, Order>> UpdateOrderAsync(
        Guid id,
        Order proposed,
        CancellationToken ct)
    {
        // First attempt
        var result = await _repository.UpdateAsync(id, proposed, ct);

        return await result.MatchAsync(
            Right: order => Task.FromResult(Either<EncinaError, Order>.Right(order)),
            Left: async error =>
            {
                // Check if concurrency conflict
                var code = error.GetCode();
                if (code.IsSome && code.ValueUnsafe() == RepositoryErrors.ConcurrencyConflictErrorCode)
                {
                    // Get conflict info from error details
                    var details = error.GetDetails();
                    var current = details["CurrentEntity"] as Order;
                    var database = details["DatabaseEntity"] as Order;

                    if (current != null && database != null)
                    {
                        // Try to resolve
                        var resolved = await _resolver.ResolveAsync(
                            current, proposed, database, ct);

                        // Retry with resolved entity
                        return await resolved.MatchAsync(
                            Right: async merged => await _repository.UpdateAsync(id, merged, ct),
                            Left: err => Task.FromResult(Either<EncinaError, Order>.Left(err)));
                    }
                }

                return Either<EncinaError, Order>.Left(error);
            });
    }
}
```

---

## Provider Support

### Provider Support Matrix

| Provider | Integer Version (`IVersionedEntity`) | Row Version (`IConcurrencyAwareEntity`) | Automatic Detection | Notes |
|----------|:------------------------------------:|:---------------------------------------:|:-------------------:|-------|
| **EF Core** | ✅ | ✅ | ✅ | Full support via interceptors |
| **Dapper SqlServer** | ✅ | ❌ | ✅ | Version WHERE clause |
| **Dapper PostgreSQL** | ✅ | ❌ | ✅ | Version WHERE clause |
| **Dapper MySQL** | ✅ | ❌ | ✅ | Version WHERE clause |
| **Dapper SQLite** | ✅ | ❌ | ✅ | Version WHERE clause |
| **ADO SqlServer** | ✅ | ❌ | ✅ | Version WHERE clause |
| **ADO PostgreSQL** | ✅ | ❌ | ✅ | Version WHERE clause |
| **ADO MySQL** | ✅ | ❌ | ✅ | Version WHERE clause |
| **ADO SQLite** | ✅ | ❌ | ✅ | Version WHERE clause |
| **MongoDB** | ✅ | ❌ | ✅ | Version filter |
| **Marten** | N/A | N/A | ✅ | Event stream versioning |

### Database-Specific SQL

Each provider generates appropriate SQL:

| Provider | Version Column Quoting | Example SQL |
|----------|------------------------|-------------|
| **SQL Server** | `[Version]` | `WHERE [Id] = @Id AND [Version] = @OriginalVersion` |
| **PostgreSQL** | `"Version"` | `WHERE "Id" = @Id AND "Version" = @OriginalVersion` |
| **MySQL** | `` `Version` `` | ``WHERE `Id` = @Id AND `Version` = @OriginalVersion`` |
| **SQLite** | `"Version"` | `WHERE "Id" = @Id AND "Version" = @OriginalVersion` |

### Marten Event Sourcing

Marten uses a different concurrency model based on event streams:

```csharp
// Events are appended with expected version
_session.Events.Append(aggregateId, expectedVersion, events);

// On conflict, error includes:
// - ExpectedVersion: version we expected
// - AggregateVersion: current aggregate version
// - UncommittedEventCount: events we tried to append
// - ConflictType: "EventStreamVersionConflict"
```

---

## Configuration Options

### EF Core Configuration

```csharp
// Option 1: Use IsConcurrencyToken() for integer version
modelBuilder.Entity<Order>()
    .Property(e => e.Version)
    .IsConcurrencyToken();

// Option 2: Use IsRowVersion() for SQL Server timestamp
modelBuilder.Entity<Order>()
    .Property(e => e.RowVersion)
    .IsRowVersion();
```

### Marten Configuration

```csharp
services.AddEncinaMarten(options =>
{
    // Control concurrency behavior
    options.UseOptimisticConcurrency = true;  // Default: true
    options.ThrowOnConcurrencyConflict = false; // Default: false (returns Either)
});
```

---

## Testing

### Unit Testing with Resolvers

```csharp
[Fact]
public async Task LastWriteWinsResolver_ShouldReturnProposedWithIncrementedVersion()
{
    // Arrange
    var resolver = new LastWriteWinsResolver<Order>();
    var current = new Order { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
    var proposed = new Order { Id = current.Id, Name = "Modified", Version = 2 };
    var database = new Order { Id = current.Id, Name = "Other", Version = 5 };

    // Act
    var result = await resolver.ResolveAsync(current, proposed, database);

    // Assert
    result.IsRight.ShouldBeTrue();
    result.IfRight(order =>
    {
        order.Name.ShouldBe("Modified");  // Our change
        order.Version.ShouldBe(6);        // database.Version + 1
    });
}
```

### Integration Testing with Fake Stores

```csharp
[Fact]
public async Task UpdateAsync_ConcurrentModification_ReturnsConcurrencyConflict()
{
    // Arrange
    var order = new Order { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
    await _repository.AddAsync(order);

    // Simulate concurrent modification
    await ModifyInDatabaseDirectly(order.Id, "External Change", version: 2);

    // Act - Try to update with stale version
    order.Name = "Our Change";
    var result = await _repository.UpdateAsync(order.Id, order);

    // Assert
    result.IsLeft.ShouldBeTrue();
    result.IfLeft(error =>
    {
        error.GetCode().IfSome(code =>
            code.ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode));
    });
}
```

### Testing ConcurrencyConflictInfo

```csharp
[Fact]
public void ConcurrencyConflictInfo_WhenEntityDeleted_WasDeletedIsTrue()
{
    // Arrange
    var current = new Order { Id = Guid.NewGuid(), Name = "Current" };
    var proposed = new Order { Id = current.Id, Name = "Proposed" };

    // Act
    var conflictInfo = new ConcurrencyConflictInfo<Order>(
        current, proposed, null);  // null = deleted

    // Assert
    conflictInfo.WasDeleted.ShouldBeTrue();
}
```

---

## FAQ

### Q: When should I use integer versioning vs row versioning?

**Integer versioning (`IVersionedEntity`):**

- Works across all providers
- Version is meaningful (v1, v2, v3...)
- Can be used in business logic
- Recommended for most cases

**Row versioning (`IConcurrencyAwareEntity`):**

- SQL Server-specific (uses `rowversion` type)
- Automatically updated by database
- No manual version management
- Better for high-throughput scenarios

### Q: How do I distinguish NotFound from ConcurrencyConflict?

When `UpdateAsync` returns `Left` with 0 rows affected:

```csharp
// Encina repositories automatically distinguish:
// 1. If entity doesn't exist → RepositoryErrors.NotFound
// 2. If entity exists but version mismatch → RepositoryErrors.ConcurrencyConflict

result.IfLeft(error =>
{
    var code = error.GetCode().IfNone("Unknown");

    if (code == RepositoryErrors.NotFoundErrorCode)
    {
        // Entity was deleted
    }
    else if (code == RepositoryErrors.ConcurrencyConflictErrorCode)
    {
        // Version mismatch - entity exists with different version
    }
});
```

### Q: Can I disable concurrency checking for specific updates?

Yes, by not implementing `IVersionedEntity`:

```csharp
// This entity has no concurrency control
public class Settings
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    // No Version property
}
```

### Q: What happens if I forget to increment the version?

For `IVersionedEntity`, Encina repositories automatically:

1. Capture the original version before update
2. Increment the version on the entity
3. Use the original version in the WHERE clause

You don't need to manually manage versions.

### Q: How does Marten's concurrency differ from entity versioning?

Marten uses **event stream versioning**:

| Aspect | Entity Versioning | Marten Event Sourcing |
|--------|-------------------|----------------------|
| **What's versioned** | Entity state | Event stream |
| **Version increments** | On entity update | On each event appended |
| **Conflict detection** | `WHERE Version = @expected` | `Events.Append(id, expectedVersion, ...)` |
| **Database entity available** | Yes | No (would need full replay) |

### Q: Can I use concurrency with bulk operations?

Yes, but with limitations:

```csharp
// UpdateRangeAsync handles versioned entities
var result = await repository.UpdateRangeAsync(entities);

// On partial failure (some conflicts), returns Left with details
result.IfLeft(error =>
{
    // Check if any entities had conflicts
    var details = error.GetDetails();
    // ...
});
```

### Q: Is there a performance impact?

Minimal. The version check adds a single column to the WHERE clause. For versioned entities:

```sql
-- Without versioning
UPDATE Orders SET Name = @Name WHERE Id = @Id

-- With versioning (minimal overhead)
UPDATE Orders SET Name = @Name, Version = @NewVersion
WHERE Id = @Id AND Version = @OriginalVersion
```

---

## See Also

- [Immutable Domain Models](./immutable-domain-models.md) - Works well with versioned entities
- [Audit Tracking](./audit-tracking.md) - Combine with versioning for full audit trail
- [Unit of Work](../patterns/unit-of-work.md) - Transaction boundaries with concurrency
