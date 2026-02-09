# Change Data Capture (CDC) in Encina

This guide explains how to capture and react to database changes in real-time using Encina's CDC infrastructure. Encina provides a provider-agnostic abstraction layer with connectors for SQL Server, PostgreSQL, MySQL, MongoDB, and Debezium.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Architecture](#architecture)
6. [Configuration Options](#configuration-options)
7. [Core Abstractions](#core-abstractions)
8. [Messaging Integration](#messaging-integration)
9. [Health Checks](#health-checks)
10. [Error Handling](#error-handling)
11. [Provider Support](#provider-support)
12. [Testing](#testing)
13. [Best Practices](#best-practices)
14. [FAQ](#faq)

---

## Overview

Change Data Capture (CDC) streams database changes as events, enabling reactive architectures without modifying application code.

| Feature | Description |
|---------|-------------|
| **Real-time Streaming** | Capture inserts, updates, and deletes as they happen |
| **Provider-Agnostic** | Same API for SQL Server, PostgreSQL, MySQL, MongoDB, Debezium |
| **Position Tracking** | Resume from the last processed position after restart |
| **Messaging Bridge** | Publish CDC events as `INotification` via Encina's pipeline |
| **Outbox CDC** | Replace polling-based outbox with CDC-driven processing |
| **Health Checks** | Built-in health monitoring for each connector |
| **Railway-Oriented** | All operations return `Either<EncinaError, T>` |

### Why CDC?

| Benefit | Description |
|---------|-------------|
| **Decoupling** | React to changes without modifying the writing application |
| **Event Sourcing** | Build event logs from database mutations |
| **Cache Invalidation** | Invalidate caches when underlying data changes |
| **Replication** | Synchronize data across systems in near real-time |
| **Audit Trail** | Track all changes for compliance and debugging |
| **Microservices** | Propagate state changes across service boundaries |

---

## The Problem

Traditional approaches to detecting database changes have significant drawbacks:

### Challenge 1: Polling is Wasteful

```csharp
// Without CDC - polling for changes every N seconds
while (!cancellationToken.IsCancellationRequested)
{
    var changes = await db.Orders
        .Where(o => o.ModifiedAtUtc > lastCheck)
        .ToListAsync();

    foreach (var order in changes)
    {
        await ProcessChangeAsync(order);
    }

    lastCheck = DateTime.UtcNow;
    await Task.Delay(TimeSpan.FromSeconds(5));
}
// Problems: missed deletes, clock skew, database load, latency
```

### Challenge 2: Trigger-Based Approaches are Fragile

```sql
-- Database triggers couple schema to application logic
CREATE TRIGGER trg_OrderChanged ON Orders
AFTER INSERT, UPDATE, DELETE
AS BEGIN
    -- Tightly coupled, hard to test, vendor-specific
    INSERT INTO ChangeLog (TableName, Operation, Data)
    VALUES ('Orders', 'UPDATE', (SELECT * FROM inserted FOR JSON AUTO));
END
```

### Challenge 3: Provider Lock-In

Each database has a different CDC mechanism (Change Tracking, WAL, binlog, Change Streams), requiring provider-specific code that is hard to swap.

---

## The Solution

Encina provides a unified CDC abstraction layer:

```csharp
// With Encina CDC - provider-agnostic, type-safe
public class OrderChangeHandler : IChangeEventHandler<Order>
{
    public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
        Order entity, ChangeContext context)
    {
        Console.WriteLine($"New order created: {entity.Id}");
        return new(Right(unit));
    }

    public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
        Order before, Order after, ChangeContext context)
    {
        Console.WriteLine($"Order {after.Id} updated");
        return new(Right(unit));
    }

    public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
        Order entity, ChangeContext context)
    {
        Console.WriteLine($"Order {entity.Id} deleted");
        return new(Right(unit));
    }
}
```

Switch providers without changing handler code:

```csharp
// SQL Server
services.AddEncinaCdcSqlServer(opts => { opts.ConnectionString = "..."; });

// PostgreSQL
services.AddEncinaCdcPostgreSql(opts => { opts.ConnectionString = "..."; });

// MongoDB
services.AddEncinaCdcMongoDb(opts => { opts.ConnectionString = "..."; });
```

---

## Quick Start

### 1. Install NuGet packages

```bash
dotnet add package Encina.Cdc
dotnet add package Encina.Cdc.SqlServer  # or PostgreSql, MySql, MongoDb, Debezium
```

### 2. Create a change handler

```csharp
public class OrderChangeHandler : IChangeEventHandler<Order>
{
    private readonly ILogger<OrderChangeHandler> _logger;

    public OrderChangeHandler(ILogger<OrderChangeHandler> logger) => _logger = logger;

    public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
        Order entity, ChangeContext context)
    {
        _logger.LogInformation("Order {OrderId} created in {Table}",
            entity.Id, context.TableName);
        return new(Right(unit));
    }

    public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
        Order before, Order after, ChangeContext context)
    {
        _logger.LogInformation("Order {OrderId} updated", after.Id);
        return new(Right(unit));
    }

    public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
        Order entity, ChangeContext context)
    {
        _logger.LogInformation("Order {OrderId} deleted", entity.Id);
        return new(Right(unit));
    }
}
```

### 3. Configure services

```csharp
// Register CDC core services
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders")
          .WithOptions(opts =>
          {
              opts.PollingInterval = TimeSpan.FromSeconds(5);
              opts.BatchSize = 100;
          });
});

// Register a provider (SQL Server example)
services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = connectionString;
    opts.TrackedTables = ["dbo.Orders"];
});
```

### 4. Run

The `CdcProcessor` runs as a `BackgroundService` and starts automatically with your host. It streams changes from the connector, dispatches them to handlers, and tracks position.

---

## Architecture

```text
┌──────────────┐      ┌──────────────┐      ┌──────────────┐      ┌───────────────────────┐
│  Database    │────▶│ ICdcConnector │────▶│ CdcProcessor │────▶│ ICdcDispatcher        │
│  (changes)   │      │ (provider)   │      │ (background) │      │ (routes by table)     │
└──────────────┘      └──────────────┘      └──────────────┘      └───────┬───────────────┘
                                                                          │
                                              ┌───────────────────────────┼────────────────┐
                                              ▼                           ▼                ▼
                                    ┌────────────────────┐  ┌─────────────────────┐  ┌──────────────────┐
                                    │ IChangeEventHandler│  │ ICdcEventInterceptor│  │ ICdcPositionStore│
                                    │ <Order>            │  │ (MessagingBridge)   │  │ (saves progress) │
                                    └────────────────────┘  └─────────────────────┘  └──────────────────┘
```

### Processing Loop

1. **CdcProcessor** (BackgroundService) polls the connector at `PollingInterval`
2. **ICdcConnector** streams `ChangeEvent` instances from the database
3. **ICdcDispatcher** routes events to typed `IChangeEventHandler<T>` based on table mappings
4. **ICdcEventInterceptor** runs after successful dispatch (e.g., messaging bridge)
5. **ICdcPositionStore** saves the position after each successful event

### Error Handling

- Individual handler failures are tracked but don't stop the stream
- Transient errors use exponential backoff: `BaseRetryDelay * 2^(retryCount-1)`
- After `MaxRetries` consecutive failures, the processor resets and continues

---

## Configuration Options

### CdcOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `false` | Enable CDC processing |
| `PollingInterval` | `TimeSpan` | `5s` | Interval between polling cycles |
| `BatchSize` | `int` | `100` | Max events per batch |
| `MaxRetries` | `int` | `3` | Max consecutive retries |
| `BaseRetryDelay` | `TimeSpan` | `1s` | Base delay for exponential backoff |
| `TableFilters` | `string[]` | `[]` | Table name filters (empty = all) |
| `EnablePositionTracking` | `bool` | `true` | Enable position persistence |
| `UseMessagingBridge` | `bool` | `false` | Enable CDC-to-messaging bridge |
| `UseOutboxCdc` | `bool` | `false` | Enable CDC-driven outbox processing |

### CdcMessagingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TopicPattern` | `string` | `{tableName}.{operation}` | Topic name pattern for notifications |
| `IncludeTables` | `string[]` | `[]` | Tables to include (empty = all) |
| `ExcludeTables` | `string[]` | `[]` | Tables to exclude (takes precedence) |
| `IncludeOperations` | `ChangeOperation[]` | `[]` | Operations to include (empty = all) |

---

## Core Abstractions

### ICdcConnector

The connector streams database changes and tracks position:

```csharp
public interface ICdcConnector
{
    string ConnectorId { get; }

    IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        CancellationToken cancellationToken = default);

    Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default);
}
```

### IChangeEventHandler\<TEntity\>

Typed handler for reacting to changes:

```csharp
public interface IChangeEventHandler<in TEntity>
{
    ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(TEntity entity, ChangeContext context);
    ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(TEntity before, TEntity after, ChangeContext context);
    ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(TEntity entity, ChangeContext context);
}
```

### ChangeEvent

Represents a single captured change:

```csharp
public sealed record ChangeEvent(
    string TableName,
    ChangeOperation Operation,   // Insert, Update, Delete, Snapshot
    object? Before,              // null for Insert/Snapshot
    object? After,               // null for Delete
    ChangeMetadata Metadata);
```

### ChangeContext

Context provided to handlers:

```csharp
public sealed record ChangeContext(
    string TableName,
    ChangeMetadata Metadata,
    CancellationToken CancellationToken);
```

### ChangeMetadata

Metadata about the captured change:

```csharp
public sealed record ChangeMetadata(
    CdcPosition Position,
    DateTime CapturedAtUtc,
    string? TransactionId,
    string? SourceDatabase,
    string? SourceSchema);
```

### CdcPosition

Abstract base for provider-specific position tracking:

```csharp
public abstract class CdcPosition : IComparable<CdcPosition>
{
    public abstract byte[] ToBytes();
    public abstract int CompareTo(CdcPosition? other);
    public abstract override string ToString();
}
```

### ICdcPositionStore

Persistent storage for resume positions:

```csharp
public interface ICdcPositionStore
{
    Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(string connectorId, ...);
    Task<Either<EncinaError, Unit>> SavePositionAsync(string connectorId, CdcPosition position, ...);
    Task<Either<EncinaError, Unit>> DeletePositionAsync(string connectorId, ...);
}
```

### ICdcEventInterceptor

Cross-cutting concern invoked after successful dispatch:

```csharp
public interface ICdcEventInterceptor
{
    ValueTask<Either<EncinaError, Unit>> OnEventDispatchedAsync(
        ChangeEvent changeEvent,
        CancellationToken cancellationToken = default);
}
```

### CdcConfiguration

Fluent builder for configuring CDC services:

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()                                              // Enable processing
          .AddHandler<Order, OrderChangeHandler>()               // Register handler
          .WithTableMapping<Order>("dbo.Orders")                 // Map table → entity
          .WithOptions(opts => { opts.BatchSize = 50; })         // Configure options
          .WithMessagingBridge(opts => { ... })                  // Enable messaging bridge
          .UseOutboxCdc("OutboxMessages");                       // Enable outbox CDC
});
```

---

## Messaging Integration

CDC events can be published as `INotification` through Encina's notification pipeline.

### Messaging Bridge

Enable the bridge to automatically publish `CdcChangeNotification` for all captured changes:

```csharp
config.WithMessagingBridge(opts =>
{
    opts.TopicPattern = "cdc.{tableName}.{operation}";
    opts.IncludeTables = ["Orders", "Customers"];
    opts.ExcludeTables = ["__EFMigrationsHistory"];
    opts.IncludeOperations = [ChangeOperation.Insert, ChangeOperation.Update];
});
```

Handle notifications downstream:

```csharp
public class CdcNotificationHandler : INotificationHandler<CdcChangeNotification>
{
    public ValueTask<Either<EncinaError, Unit>> HandleAsync(
        CdcChangeNotification notification, CancellationToken ct)
    {
        // notification.TableName, notification.Operation, notification.TopicName
        // notification.Before, notification.After, notification.Metadata
        return new(Right(unit));
    }
}
```

### CdcChangeNotification

```csharp
public sealed record CdcChangeNotification(
    string TableName,
    ChangeOperation Operation,
    object? Before,
    object? After,
    ChangeMetadata Metadata,
    string TopicName) : INotification;
```

Factory method: `CdcChangeNotification.FromChangeEvent(changeEvent, topicPattern)`.

### Outbox CDC Integration

Replace polling-based outbox processing with CDC-driven processing:

```csharp
config.UseOutboxCdc("OutboxMessages");
```

The `OutboxCdcHandler` monitors the outbox table via CDC and immediately publishes stored notifications when new rows are inserted. It skips already-processed rows (`ProcessedAtUtc != null`) for safe coexistence with the traditional `OutboxProcessor`.

See [Outbox CDC Integration Example](../examples/cdc-outbox-integration.md) for details.

---

## Health Checks

Each provider includes a health check that verifies connector connectivity:

| Provider | Health Check Class |
|----------|-------------------|
| SQL Server | `SqlServerCdcHealthCheck` |
| PostgreSQL | `PostgresCdcHealthCheck` |
| MySQL | `MySqlCdcHealthCheck` |
| MongoDB | `MongoCdcHealthCheck` |
| Debezium | `DebeziumCdcHealthCheck` |

---

## Error Handling

### Error Codes

| Code | Description |
|------|-------------|
| `encina.cdc.connection_failed` | Failed to connect to the CDC source |
| `encina.cdc.position_invalid` | Stored position is invalid or corrupted |
| `encina.cdc.stream_interrupted` | Change stream was interrupted unexpectedly |
| `encina.cdc.handler_failed` | Handler threw an exception |
| `encina.cdc.deserialization_failed` | Failed to deserialize change event payload |
| `encina.cdc.position_store_failed` | Failed to read/write position store |

### Error Factories

```csharp
CdcErrors.ConnectionFailed("Server unreachable");
CdcErrors.ConnectionFailed("Timeout", exception);
CdcErrors.PositionInvalid(position);
CdcErrors.StreamInterrupted(exception);
CdcErrors.HandlerFailed("dbo.Orders", exception);
CdcErrors.DeserializationFailed("dbo.Orders", typeof(Order), exception);
CdcErrors.PositionStoreFailed("my-connector", exception);
```

---

## Provider Support

| Provider | Package | CDC Mechanism | Position Type |
|----------|---------|---------------|---------------|
| SQL Server | `Encina.Cdc.SqlServer` | Change Tracking | `SqlServerCdcPosition` (version long) |
| PostgreSQL | `Encina.Cdc.PostgreSql` | Logical Replication (WAL) | `PostgresCdcPosition` (LSN) |
| MySQL | `Encina.Cdc.MySql` | Binary Log Replication | `MySqlCdcPosition` (GTID or file/pos) |
| MongoDB | `Encina.Cdc.MongoDb` | Change Streams | `MongoCdcPosition` (resume token) |
| Debezium | `Encina.Cdc.Debezium` | HTTP Consumer | `DebeziumCdcPosition` (offset JSON) |

See individual provider documentation:

- [SQL Server CDC](cdc-sqlserver.md)
- [PostgreSQL CDC](cdc-postgresql.md)
- [MySQL CDC](cdc-mysql.md)
- [MongoDB CDC](cdc-mongodb.md)
- [Debezium CDC](cdc-debezium.md)

---

## Testing

### Unit Testing with Test Helpers

Use `IChangeEventHandler<T>` implementations directly in unit tests:

```csharp
var handler = new OrderChangeHandler(logger);
var context = new ChangeContext("dbo.Orders", metadata, CancellationToken.None);

var result = await handler.HandleInsertAsync(new Order { Id = 1 }, context);

result.IsRight.ShouldBeTrue();
```

### Integration Testing with TestCdcConnector

For integration tests, create in-memory connectors with pre-loaded events:

```csharp
var connector = new TestCdcConnector("test");
connector.AddEvent(new ChangeEvent(
    "dbo.Orders",
    ChangeOperation.Insert,
    Before: null,
    After: new Order { Id = 1, Name = "Test" },
    metadata));

services.AddSingleton<ICdcConnector>(connector);
```

### Test Coverage

The CDC feature includes 355+ tests:

- ~156 unit tests
- ~55 integration tests
- ~50 guard tests
- ~47 contract tests
- ~47 property tests

---

## Best Practices

### 1. Keep Handlers Fast

Handlers run in the processing loop. Offload heavy work to background jobs or queues:

```csharp
public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
    Order entity, ChangeContext context)
{
    // Fast: enqueue for async processing
    _backgroundQueue.Enqueue(new ProcessOrderCommand(entity.Id));
    return new(Right(unit));
}
```

### 2. Use Position Tracking

Always enable position tracking in production to avoid reprocessing on restart:

```csharp
opts.EnablePositionTracking = true;
```

### 3. Set Appropriate Batch Sizes

Balance latency vs throughput:

```csharp
opts.BatchSize = 100;          // Default: good for most cases
opts.PollingInterval = TimeSpan.FromSeconds(2);  // Lower for near real-time
```

### 4. Filter Tables

Only capture tables you need to reduce overhead:

```csharp
opts.TableFilters = ["dbo.Orders", "dbo.Customers"];
```

### 5. Handle Errors Gracefully

Return `Left(error)` instead of throwing exceptions:

```csharp
try
{
    await ProcessAsync(entity);
    return Right(unit);
}
catch (Exception ex)
{
    return Left(CdcErrors.HandlerFailed(context.TableName, ex));
}
```

### 6. Use Messaging Bridge for Cross-Cutting Concerns

Instead of duplicating logic across handlers, use the messaging bridge to publish `CdcChangeNotification` and handle them centrally with `INotificationHandler<CdcChangeNotification>`.

---

## FAQ

### Can I use multiple providers simultaneously?

No. Each application registers one `ICdcConnector`. If you need to capture changes from multiple databases, run separate instances or use Debezium to aggregate multiple sources.

### What happens if a handler fails?

The failure is logged and counted, but processing continues. The position is only saved after successful dispatch. On restart, failed events will be reprocessed.

### Does CDC capture schema changes?

No. CDC captures data changes (INSERT, UPDATE, DELETE). Schema migrations (ALTER TABLE) are not captured as change events.

### Can I use CDC without the messaging bridge?

Yes. The messaging bridge is optional. You can use `IChangeEventHandler<T>` directly without enabling `WithMessagingBridge()`.

### How does Outbox CDC differ from the traditional Outbox Processor?

The traditional `OutboxProcessor` polls the outbox table at regular intervals. `UseOutboxCdc()` replaces polling with CDC, publishing notifications immediately when new outbox rows are inserted. Both can coexist safely.

### What is the `Snapshot` operation?

`ChangeOperation.Snapshot` represents an initial load of existing data. Some providers emit snapshot events when first connecting to capture the current state before streaming live changes.
