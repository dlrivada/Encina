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

builder.Services.AddEncinaMongoDB(config =>
{
    config.ConnectionString = "mongodb://localhost:27017/?replicaSet=rs0";
    config.DatabaseName = "MyApp";
    config.UseOutbox = true; // Requires a replica set or a sharded cluster
    config.UseInbox = true;
});
```

The transactional outbox needs a replica set or a sharded cluster (see [Transactional outbox](#transactional-outbox)).

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

## Transactional outbox

With `UseOutbox = true`, a request that implements `IHasNotifications` writes its outbox messages inside one MongoDB client-session transaction, owned by the scoped `IMongoSessionAccessor`, together with the unit of work and the messaging stores. See [How to write the outbox inside the request's transaction](../../docs/features/transactional-outbox.md) and [ADR-037](../../docs/architecture/adr/037-transactional-outbox.md).

- **Replica set or sharded cluster required.** On a standalone server the request fails closed with `Left` `transaction.not_supported`; nothing is written without the transaction. A single-node replica set is enough for development.
- **Tested versions.** The integration fixtures run `mongo:7` with MongoDB driver 3.12. No other server version is claimed.
- **Health check.** When `UseOutbox` is on, `MongoDbHealthCheck` reports `Unhealthy` on a standalone server and `Degraded` while the cluster type is not discovered; replica sets and sharded clusters are healthy.
- **No `UseTransactions` option.** There is no transaction behavior for MongoDB; the outbox behavior begins the session itself, and `IUnitOfWork` covers explicit use ([#2988](https://github.com/dlrivada/Encina/issues/2988)).
- **Transaction options.** Only the read preference (primary) is set; the read and write concern come from your `MongoClient`. A read concern of `available` or `linearizable` is rejected inside a transaction.
- **Commit failures.** The commit is never retried by Encina; `transaction.commit_failed` carries the error label (`TransientTransactionError` or `UnknownTransactionCommitResult`) as metadata. The server's 60 second transaction lifetime and write conflicts apply to every request with notifications.

A handler that writes with its own collection handle passes `IMongoSessionAccessor.CurrentSession` to the driver so its write commits or aborts with the request:

```csharp
public sealed class PlaceOrderHandler(IMongoClient client, IMongoSessionAccessor accessor) : IRequestHandler<PlaceOrder, int>
{
    public async Task<Either<EncinaError, int>> Handle(PlaceOrder request, CancellationToken cancellationToken)
    {
        var orders = client.GetDatabase("MyApp").GetCollection<BsonDocument>("Orders");
        var document = new BsonDocument("_id", request.OrderId);
        if (accessor.CurrentSession is { } session)
        {
            await orders.InsertOneAsync(session, document, cancellationToken: cancellationToken);
        }
        else
        {
            await orders.InsertOneAsync(document, cancellationToken: cancellationToken);
        }

        return request.OrderId;
    }
}
```

The session exists only inside the transactional outbox or a unit of work; outside them `CurrentSession` is `null` and the write is a plain call.

Stores that do not use the session (default, soft-delete and tenant repositories, bulk operations, dead-letter, audit, anonymization, ABAC) write outside the transaction without failing ([#1934](https://github.com/dlrivada/Encina/issues/1934)).

## Unit of Work with Transactions

> **Note**: MongoDB transactions require a **replica set** or a sharded cluster.

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
builder.Services.AddEncinaMongoDB(config =>
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
config.InboxOptions.EnableAutomaticPurge = true; // the default
config.InboxOptions.PurgeInterval = TimeSpan.FromHours(24); // the default
config.InboxOptions.PurgeBatchSize = 100; // the default
```

Only processed, expired documents are purged. Defaults and valid ranges: [Automatic purge of processed messages](../Encina.Messaging/README.md#automatic-purge-of-processed-messages).

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
| `outbox_messages` | Pending notifications for reliable delivery |
| `inbox_messages` | Processed messages for idempotency |
| `saga_states` | Saga orchestration state |
| `scheduled_messages` | Delayed/recurring commands |
| `dead_letter_messages` | Messages that failed for good, with replay state (when `UseDeadLetterQueue` is on) |

The names are the defaults of `MongoDbCollectionNames` (`config.Collections.Inbox` and its siblings change them).

### Indexes

Encina automatically creates optimized indexes:

```javascript
// outbox_messages
{ "ProcessedAtUtc": 1, "RetryCount": 1, "NextRetryAtUtc": 1 }

// inbox_messages: IX_Inbox_Expires (ordinary index, not a TTL index) and IX_Inbox_RequestType
{ "ExpiresAtUtc": 1 }
{ "RequestType": 1 }

// scheduled_messages
{ "ScheduledAtUtc": 1, "ProcessedAtUtc": 1 }

// saga_states
{ "Status": 1, "LastUpdatedAtUtc": 1 }
```

#### Inbox indexes and purge

The library creates an ordinary index `IX_Inbox_Expires` on `ExpiresAtUtc` and **no TTL index**. Expired processed documents are deleted by the purge service (`MessagePurgeService`, controlled by `InboxOptions.EnableAutomaticPurge`, `PurgeInterval` and `PurgeBatchSize`); an unprocessed document is never deleted. The earlier index `IX_Inbox_Expires_TTL` deleted unprocessed documents too and ignored `EnableAutomaticPurge = false`.

Pre-1.0 there is no migration code. `IX_Inbox_Expires` has the same key (`{ ExpiresAtUtc: 1 }`) as the old `IX_Inbox_Expires_TTL` under a different name and options, so MongoDB rejects creating it (`IndexOptionsConflict`, code 85) while the old index exists. `MongoDbIndexCreator` rethrows that error, so on a database created by an earlier build the host fails to start with `UseInbox = true` until the old TTL index is dropped by hand, once, in `mongosh`. Until then the old TTL index keeps deleting rows, unprocessed ones included.

```javascript
db.inbox_messages.dropIndex("IX_Inbox_Expires_TTL")
```

Use your own collection name if you changed `config.Collections.Inbox`. The purge options are documented in [Automatic purge of processed messages](../Encina.Messaging/README.md#automatic-purge-of-processed-messages).

## Configuration Reference

```csharp
builder.Services.AddEncinaMongoDB(config =>
{
    // Connection
    config.ConnectionString = "mongodb://localhost:27017/?replicaSet=rs0";
    config.DatabaseName = "MyApp";

    // Messaging patterns (all opt-in)
    config.UseOutbox = true; // Requires a replica set or a sharded cluster
    config.UseInbox = true;
    config.UseSagas = true;
    config.UseScheduling = true;

    // Outbox options
    config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(5);
    config.OutboxOptions.BatchSize = 100;
    config.OutboxOptions.MaxRetries = 3;

    // Inbox options
    config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(7);
    config.InboxOptions.EnableAutomaticPurge = true;
    config.InboxOptions.PurgeInterval = TimeSpan.FromHours(24);
    config.InboxOptions.PurgeBatchSize = 100;

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
