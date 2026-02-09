# Example: CDC Messaging Bridge

This example shows how to use the CDC messaging bridge to publish database changes as `INotification` through Encina's notification pipeline.

## Overview

The messaging bridge converts CDC `ChangeEvent` instances into `CdcChangeNotification` objects and publishes them via `IEncina.Publish()`. This enables you to handle database changes using the same notification handlers used for other domain events.

```text
┌───────────┐  CDC  ┌──────────────┐  intercept  ┌───────────────────┐  publish  ┌────────────────┐
│ Database  │─────▶│ CdcProcessor │────────────▶│ CdcMessagingBridge│─────────▶│ INotification  │
│ (changes) │       │ + Dispatcher │             │ (interceptor)     │           │ Handler(s)     │
└───────────┘       └──────────────┘             └───────────────────┘           └────────────────┘
```

## Configuration

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders")
          .WithTableMapping<Customer>("dbo.Customers")
          .WithMessagingBridge(opts =>
          {
              // Topic pattern with placeholders
              opts.TopicPattern = "cdc.{tableName}.{operation}";

              // Filter which tables are published
              opts.IncludeTables = ["dbo.Orders", "dbo.Customers"];
              opts.ExcludeTables = ["dbo.__EFMigrationsHistory"];

              // Filter which operations are published
              opts.IncludeOperations = [ChangeOperation.Insert, ChangeOperation.Update];
          });
});
```

## Handling CdcChangeNotification

Register a notification handler for `CdcChangeNotification`:

```csharp
public class CdcAuditHandler : INotificationHandler<CdcChangeNotification>
{
    private readonly IAuditService _auditService;

    public CdcAuditHandler(IAuditService auditService)
        => _auditService = auditService;

    public async ValueTask<Either<EncinaError, Unit>> HandleAsync(
        CdcChangeNotification notification,
        CancellationToken cancellationToken)
    {
        // Log the change for audit
        await _auditService.LogChangeAsync(new AuditEntry
        {
            Table = notification.TableName,
            Operation = notification.Operation.ToString(),
            Topic = notification.TopicName,         // e.g., "cdc.dbo.Orders.insert"
            Timestamp = notification.Metadata.CapturedAtUtc,
            Before = notification.Before,
            After = notification.After
        });

        return Right(unit);
    }
}
```

## Topic Pattern

The `TopicPattern` supports two placeholders:

| Placeholder | Replaced With | Example |
|-------------|---------------|---------|
| `{tableName}` | `ChangeEvent.TableName` | `dbo.Orders` |
| `{operation}` | `ChangeEvent.Operation` (lowercase) | `insert`, `update`, `delete` |

**Examples**:

| Pattern | Result |
|---------|--------|
| `{tableName}.{operation}` | `dbo.Orders.insert` |
| `cdc.{tableName}.{operation}` | `cdc.dbo.Orders.update` |
| `changes.{operation}` | `changes.delete` |

## Filter Behavior

| Scenario | IncludeTables | ExcludeTables | Result |
|----------|---------------|---------------|--------|
| Both empty | `[]` | `[]` | All tables published |
| Include only | `["A", "B"]` | `[]` | Only A, B published |
| Exclude only | `[]` | `["C"]` | All except C published |
| Both set | `["A", "B"]` | `["B"]` | Only A published (exclude wins) |

`IncludeOperations` works the same way: empty = all operations, non-empty = only listed operations.

## Combining with Entity Handlers

The messaging bridge runs as an `ICdcEventInterceptor` **after** the entity-specific `IChangeEventHandler<T>` completes successfully. Both run on every event:

1. `IChangeEventHandler<Order>.HandleInsertAsync(...)` — your typed handler
2. `CdcMessagingBridge.OnEventDispatchedAsync(...)` — publishes `CdcChangeNotification`
3. `INotificationHandler<CdcChangeNotification>.HandleAsync(...)` — your notification handler

This means you can have entity-specific logic in the typed handler and cross-cutting logic in the notification handler.

## Related

- [CDC Feature Guide](../features/cdc.md)
- [Outbox CDC Integration Example](cdc-outbox-integration.md)
- [Position Tracking Example](cdc-position-tracking.md)
