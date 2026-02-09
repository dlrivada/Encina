# Example: CDC Position Tracking

This example explains how CDC position tracking works and how to configure it for reliable change processing.

## Why Position Tracking?

Without position tracking, a restart causes the processor to start from the beginning (or current position), potentially missing or reprocessing events. Position tracking saves the last successfully processed position so the processor can resume exactly where it left off.

```text
Before restart: Events [1] [2] [3] [4] [5] ← position saved at 5
After restart:  Resumes from position 5 → [6] [7] [8] ...
```

## Default Configuration

Position tracking is enabled by default with an in-memory store:

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders");
    // EnablePositionTracking = true by default
    // Uses InMemoryCdcPositionStore by default
});
```

> **Note**: `InMemoryCdcPositionStore` loses position data on application restart. It is suitable for development and testing. For production, use a persistent store.

## Disabling Position Tracking

For scenarios where reprocessing is acceptable or the connector manages its own position:

```csharp
config.WithOptions(opts =>
{
    opts.EnablePositionTracking = false;
});
```

## How Position is Saved

The `CdcProcessor` saves position after each successfully dispatched event:

```text
Event 1 → Dispatch → Success → Save position 1
Event 2 → Dispatch → Success → Save position 2
Event 3 → Dispatch → FAILURE → Position stays at 2
Event 4 → Dispatch → Success → Save position 4
```

If the handler fails, the position is NOT saved for that event. On restart, the failed event will be reprocessed (at-least-once semantics).

## Provider-Specific Positions

Each provider tracks position differently:

| Provider | Position Type | What It Tracks |
|----------|---------------|----------------|
| SQL Server | `SqlServerCdcPosition` | Change Tracking version (`long`) |
| PostgreSQL | `PostgresCdcPosition` | WAL Log Sequence Number (LSN) |
| MySQL | `MySqlCdcPosition` | GTID set or binlog file/position |
| MongoDB | `MongoCdcPosition` | Change Stream resume token |
| Debezium | `DebeziumCdcPosition` | Source offset (JSON) |

All positions support serialization via `ToBytes()` and deserialization via `FromBytes()`.

## ICdcPositionStore Interface

```csharp
public interface ICdcPositionStore
{
    Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(
        string connectorId, CancellationToken cancellationToken = default);

    Task<Either<EncinaError, Unit>> SavePositionAsync(
        string connectorId, CdcPosition position,
        CancellationToken cancellationToken = default);

    Task<Either<EncinaError, Unit>> DeletePositionAsync(
        string connectorId, CancellationToken cancellationToken = default);
}
```

The `connectorId` allows multiple connectors to track positions independently.

## Custom Position Store

Implement `ICdcPositionStore` to persist positions in your preferred storage:

```csharp
public class DatabasePositionStore : ICdcPositionStore
{
    private readonly IDbConnection _connection;

    public async Task<Either<EncinaError, Unit>> SavePositionAsync(
        string connectorId, CdcPosition position,
        CancellationToken cancellationToken = default)
    {
        var bytes = position.ToBytes();
        var positionType = position.GetType().AssemblyQualifiedName;

        await _connection.ExecuteAsync(
            """
            MERGE INTO CdcPositions AS target
            USING (SELECT @ConnectorId AS ConnectorId) AS source
            ON target.ConnectorId = source.ConnectorId
            WHEN MATCHED THEN UPDATE SET PositionData = @Data, PositionType = @Type
            WHEN NOT MATCHED THEN INSERT (ConnectorId, PositionData, PositionType)
                VALUES (@ConnectorId, @Data, @Type);
            """,
            new { ConnectorId = connectorId, Data = bytes, Type = positionType });

        return Right(unit);
    }

    // ... GetPositionAsync, DeletePositionAsync
}
```

Register your custom store:

```csharp
services.AddSingleton<ICdcPositionStore, DatabasePositionStore>();
```

The `AddEncinaCdc()` method uses `TryAddSingleton`, so your registration takes precedence over the default in-memory store.

## Related

- [CDC Feature Guide](../features/cdc.md)
- [Basic CDC Setup Example](cdc-basic-setup.md)
