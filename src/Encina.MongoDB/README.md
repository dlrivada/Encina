# Encina.MongoDB

MongoDB implementation for Encina messaging patterns and domain modeling, featuring native bulk operations, multi-tenancy, and replica set transaction support.

## Features

- **Outbox Pattern**: Reliable event publishing with at-least-once delivery
- **Inbox Pattern**: Idempotent message processing with exactly-once semantics
- **Saga Orchestration**: Distributed transaction coordination with compensation
- **Scheduled Messages**: Delayed and recurring command execution
- **Unit of Work**: Transaction support with MongoDB sessions (requires replica set)
- **Repository Pattern**: `IFunctionalRepository<TEntity, TId>` with ROP support
- **Bulk Operations**: Native MongoDB `BulkWrite` for high-performance data operations
- **Multi-Tenancy**: Database-per-tenant and shared collection strategies
- **Read/Write Separation**: Read preference routing for replica sets

## Installation

```bash
dotnet add package Encina.MongoDB
```

## Quick Start

### 1. Configure Services

```csharp
using Encina.MongoDB;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncinalMongoDB(config =>
{
    config.ConnectionString = "mongodb://localhost:27017";
    config.DatabaseName = "MyApp";
    config.UseOutbox = true;
    config.UseInbox = true;
    config.UseTransactions = true; // Requires replica set
});
```

### 2. Use Repository

```csharp
public class OrderService(IFunctionalRepository<Order, Guid> repository)
{
    public async Task<Either<EncinaError, Order>> GetOrderAsync(Guid id, CancellationToken ct)
        => await repository.GetByIdAsync(id, ct);

    public async Task<Either<EncinaError, Unit>> CreateOrderAsync(Order order, CancellationToken ct)
        => await repository.AddAsync(order, ct);
}
```

## Bulk Operations

MongoDB's native `BulkWrite` API provides exceptional performance for large-scale data operations.

### Performance Comparison (MongoDB 7, measured with Testcontainers, 1,000 entities)

| Operation | Loop Time | Bulk Time | Improvement |
|-----------|-----------|-----------|-------------|
| **Insert** | ~520ms | ~4ms | **130x faster** |
| **Update** | ~552ms | ~34ms | **16x faster** |
| **Delete** | ~518ms | ~25ms | **21x faster** |

MongoDB's `BulkWrite` operations batch multiple commands into single network requests, significantly reducing round-trip overhead.

| Operation | Implementation | Notes |
|-----------|----------------|-------|
| **BulkInsert** | `InsertMany` | Batches inserts into single request |
| **BulkUpdate** | `BulkWrite` with `ReplaceOneModel` | Ordered by default |
| **BulkDelete** | `BulkWrite` with `DeleteOneModel` | Matches by `_id` |
| **BulkMerge** | `BulkWrite` with `IsUpsert = true` | Atomic upsert |

> **Note**: Actual performance depends on document size, network latency, and MongoDB server configuration.

### Usage

```csharp
// Get bulk operations from Unit of Work
var bulkOps = unitOfWork.BulkOperations<Order>();

// Bulk insert 10,000 orders
var orders = GenerateOrders(10_000);
var result = await bulkOps.BulkInsertAsync(orders);

result.Match(
    Right: count => _logger.LogInformation("Inserted {Count} orders", count),
    Left: error => _logger.LogError("Bulk insert failed: {ErrorCode}", error.GetCode().IfNone("encina.unknown"))
);
```

### Available Operations

| Operation | MongoDB Implementation | Description |
|-----------|------------------------|-------------|
| `BulkInsertAsync` | `InsertMany` | Insert multiple documents |
| `BulkUpdateAsync` | `BulkWrite` with `ReplaceOneModel` | Replace multiple documents |
| `BulkDeleteAsync` | `BulkWrite` with `DeleteOneModel` | Delete multiple documents by ID |
| `BulkMergeAsync` | `BulkWrite` with `ReplaceOneModel` (upsert) | Upsert documents |
| `BulkReadAsync` | `Find` with `$in` filter | Read multiple documents by IDs |

### Configuration

```csharp
var config = BulkConfig.Default with
{
    BatchSize = 5000,              // Documents per batch
    PreserveInsertOrder = true,    // Maintain insertion order (IsOrdered = true)
    TrackingEntities = false       // Don't track entities after operation
};

await bulkOps.BulkInsertAsync(orders, config);
```

### Bulk Merge (Upsert)

```csharp
// Insert new documents, update existing ones
var ordersToSync = GetOrdersFromExternalSystem();

var result = await bulkOps.BulkMergeAsync(ordersToSync);

result.Match(
    Right: count => _logger.LogInformation("Synced {Count} orders", count),
    Left: error => _logger.LogError("Sync failed: {ErrorCode}", error.GetCode().IfNone("encina.unknown"))
);
```

### Error Handling

All operations return `Either<EncinaError, T>` with specific error codes:

```csharp
result.IfLeft(error =>
{
    var code = error.GetCode();

    // Error codes: Repository.BulkInsertFailed, Repository.BulkUpdateFailed, etc.
    if (code.IsSome && code == RepositoryErrors.BulkInsertFailedErrorCode)
    {
        // Handle duplicate key errors, write concern failures, etc.
        var details = error.GetDetails();
        _logger.LogError(
            "Bulk insert failed. Reason: {Reason}, ErrorCode: {ErrorCode}",
            details.GetValueOrDefault("Reason"),
            error.GetCode().IfNone("encina.unknown")
        );
    }
});
```

## Unit of Work with Transactions

> **Note**: MongoDB transactions require a **replica set** configuration.

```csharp
public async Task<Either<EncinaError, Unit>> ProcessBatchAsync(
    IUnitOfWork unitOfWork,
    List<Order> orders,
    CancellationToken ct)
{
    var bulkOps = unitOfWork.BulkOperations<Order>();

    // Begin transaction (uses MongoDB session)
    var begin = await unitOfWork.BeginTransactionAsync(ct);
    if (begin.IsLeft) return begin;

    // Bulk operations within transaction
    var result = await bulkOps.BulkInsertAsync(orders, ct: ct);
    if (result.IsLeft)
    {
        await unitOfWork.RollbackAsync(ct);
        return result.Map(_ => Unit.Default);
    }

    // Commit on success
    return await unitOfWork.CommitAsync(ct);
}
```

## Multi-Tenancy

### Database-per-Tenant

```csharp
builder.Services.AddEncinaMongoDBWithTenancy(
    config =>
    {
        config.ConnectionString = "mongodb://localhost:27017";
        config.DatabaseName = "MyApp"; // Base database name
    },
    tenancy =>
    {
        tenancy.EnableDatabasePerTenant = true;
        tenancy.DatabaseNamePattern = "{baseName}_{tenantId}";
        // Results in: MyApp_tenant1, MyApp_tenant2, etc.
    });
```

### Shared Collection with Tenant Filter

```csharp
builder.Services.AddEncinaMongoDBWithTenancy(
    config => { ... },
    tenancy =>
    {
        tenancy.AutoFilterTenantQueries = true;
        tenancy.AutoAssignTenantId = true;
        tenancy.TenantIdPropertyName = "TenantId";
    });
```

## Read/Write Separation

Leverage MongoDB replica sets for read scaling:

```csharp
builder.Services.AddEncinalMongoDB(config =>
{
    config.ReadWriteSeparation.Enabled = true;
    config.ReadWriteSeparation.ReadPreference = MongoReadPreference.SecondaryPreferred;
    config.ReadWriteSeparation.ReadConcern = MongoReadConcern.Majority;
});
```

## Messaging Patterns

### Outbox Pattern

```csharp
config.UseOutbox = true;
config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(5);
config.OutboxOptions.BatchSize = 100;
config.OutboxOptions.MaxRetries = 3;
```

### Inbox Pattern

```csharp
config.UseInbox = true;
config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(7);
config.InboxOptions.EnableAutomaticPurge = true;
```

### Scheduled Messages

```csharp
config.UseScheduling = true;
config.SchedulingOptions.EnableRecurringMessages = true;
```

### Dead Letter Queue

```csharp
config.UseDeadLetterQueue = true;
config.DeadLetterOptions.RetentionPeriod = TimeSpan.FromDays(14);
config.DeadLetterOptions.CleanupInterval = TimeSpan.FromHours(1);
```

The collection name is `Collections.DeadLetterMessages` (default `dead_letter_messages`). Capture is idempotent through the unique index `UX_DeadLetterMessages_Source` on `SourcePattern` and `SourceMessageId`. Expired documents are deleted by the cleanup loop, not by a TTL index. Guide: [How to enable the persistent dead letter queue](../../docs/features/dead-letter-queue.md).

## Database Schema

### Collections

| Collection | Purpose |
|------------|---------|
| `OutboxMessages` | Pending notifications for reliable delivery |
| `InboxMessages` | Processed messages for idempotency |
| `SagaStates` | Saga orchestration state |
| `ScheduledMessages` | Delayed/recurring commands |
| `dead_letter_messages` | Messages that failed for good, with replay state (when `UseDeadLetterQueue` is on) |

### Indexes

Encina automatically creates optimized indexes:

```javascript
// OutboxMessages
{ "ProcessedAtUtc": 1, "RetryCount": 1, "NextRetryAtUtc": 1 }

// InboxMessages
{ "ExpiresAtUtc": 1 }

// ScheduledMessages
{ "ScheduledAtUtc": 1, "ProcessedAtUtc": 1 }

// SagaStates
{ "Status": 1, "LastUpdatedAtUtc": 1 }
```

## Configuration Reference

```csharp
builder.Services.AddEncinalMongoDB(config =>
{
    // Connection
    config.ConnectionString = "mongodb://localhost:27017";
    config.DatabaseName = "MyApp";

    // Messaging patterns (all opt-in)
    config.UseOutbox = true;
    config.UseInbox = true;
    config.UseSagas = true;
    config.UseScheduling = true;
    config.UseTransactions = true; // Requires replica set

    // Outbox options
    config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(5);
    config.OutboxOptions.BatchSize = 100;
    config.OutboxOptions.MaxRetries = 3;

    // Inbox options
    config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(7);
    config.InboxOptions.EnableAutomaticPurge = true;

    // Health checks
    config.ProviderHealthCheck.Enabled = true;
    config.ProviderHealthCheck.Name = "mongodb";
});
```

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.DomainModeling` | Core abstractions (`IBulkOperations`, `IUnitOfWork`) |
| `Encina.Messaging` | Shared messaging pattern interfaces |
| `Encina.Tenancy` | Multi-tenancy abstractions |

## License

MIT License - see LICENSE file for details
