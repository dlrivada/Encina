# Encina.Cdc

Core Change Data Capture (CDC) infrastructure for Encina. Provides provider-agnostic abstractions for streaming database changes as typed events through a handler pipeline.

## Features

- **Provider-Agnostic**: Same API for SQL Server, PostgreSQL, MySQL, MongoDB, Debezium
- **Typed Handlers**: `IChangeEventHandler<TEntity>` for Insert, Update, Delete operations
- **Position Tracking**: Resume from the last processed position after restart
- **Messaging Bridge**: Publish CDC events as `INotification` via Encina's pipeline
- **Outbox CDC**: Replace polling-based outbox with CDC-driven processing
- **Cache Invalidation**: Invalidate query cache entries across all instances
- **Sharded CDC**: Aggregate change streams from multiple database shards
- **Health Checks**: Built-in health monitoring for each connector
- **Railway-Oriented**: All operations return `Either<EncinaError, T>`

## Quick Start

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders");
});

// Register a provider-specific connector
services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = connectionString;
    opts.TrackedTables = ["dbo.Orders"];
});
```

## CDC-Driven Cache Invalidation

Automatically invalidate query cache entries when database changes are detected from any source (other app instances, direct SQL, migrations, external microservices):

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .WithCacheInvalidation(opts =>
          {
              opts.Tables = ["Orders", "Products"];
              opts.TableToEntityTypeMappings = new Dictionary<string, string>
              {
                  ["dbo.Orders"] = "Order"
              };
          });
});
```

### Required Dependencies

- **`ICacheProvider`**: Any Encina caching provider (Redis, Memory, Valkey, etc.)
- **`IPubSubProvider`** (optional): For cross-instance broadcast (Redis, Valkey, etc.)
- **`ICdcConnector`**: A CDC provider package (SQL Server, PostgreSQL, etc.)

### Multi-Instance Deployment

When running multiple application instances, enable pub/sub broadcast (default) to ensure all instances invalidate their local caches:

```csharp
config.WithCacheInvalidation(opts =>
{
    opts.UsePubSubBroadcast = true;           // default
    opts.PubSubChannel = "sm:cache:invalidate"; // default
});
```

Each instance runs a `CacheInvalidationSubscriberService` that listens for invalidation patterns and calls `RemoveByPatternAsync` on the local cache.

For full documentation, see [CDC Cache Invalidation Guide](../../docs/features/cdc-cache-invalidation.md).

## Provider Packages

| Package | CDC Mechanism |
|---------|---------------|
| `Encina.Cdc.SqlServer` | SQL Server Change Tracking |
| `Encina.Cdc.PostgreSql` | PostgreSQL Logical Replication (WAL) |
| `Encina.Cdc.MySql` | MySQL Binary Log Replication |
| `Encina.Cdc.MongoDb` | MongoDB Change Streams |
| `Encina.Cdc.Debezium` | Debezium HTTP + Kafka Consumer |

## Documentation

- [CDC Feature Guide](../../docs/features/cdc.md)
- [CDC Cache Invalidation](../../docs/features/cdc-cache-invalidation.md)
- [Sharded CDC](../../docs/features/cdc-sharding.md)
- [Provider Guides](../../docs/features/) (cdc-sqlserver.md, cdc-postgresql.md, etc.)
