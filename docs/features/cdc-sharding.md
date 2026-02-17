# Sharded CDC Capture in Encina

This guide explains how to capture change events across a sharded database topology using Encina's Sharded CDC infrastructure. Sharded CDC builds on the existing [CDC abstractions](cdc.md) and integrates with the [Database Sharding](sharding.md) module for shard topology discovery.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Quick Start](#quick-start)
4. [Configuration](#configuration)
5. [Position Tracking](#position-tracking)
6. [Topology Changes](#topology-changes)
7. [Processing Modes](#processing-modes)
8. [Health Checks](#health-checks)
9. [Observability](#observability)
10. [Error Handling](#error-handling)
11. [Testing](#testing)
12. [Best Practices](#best-practices)
13. [FAQ](#faq)

---

## Overview

In a sharded database architecture, each shard has its own independent change stream with its own position sequence. Sharded CDC aggregates these per-shard streams into a unified event pipeline while preserving per-shard position tracking for reliable resume after restart.

| Feature | Description |
|---------|-------------|
| **Multi-Shard Streaming** | Aggregate change events from all shards into a single stream |
| **Per-Shard Positions** | Track CDC position independently per `(shardId, connectorId)` composite key |
| **Topology Integration** | Auto-discover shards from `IShardTopologyProvider` at startup |
| **Runtime Changes** | Add or remove shards at runtime without restart |
| **Processing Modes** | Aggregated (cross-shard ordering) or PerShardParallel (independent) |
| **Health Monitoring** | Shard-aware health checks with lag threshold detection |
| **Railway-Oriented** | All operations return `Either<EncinaError, T>` |

### When to Use Sharded CDC

| Scenario | Use Sharded CDC? |
|----------|-----------------|
| Single database with CDC | No - use standard `ICdcConnector` directly |
| Multiple shards, same schema | **Yes** - aggregate changes across shards |
| Shard rebalancing / topology changes | **Yes** - supports runtime add/remove |
| Cross-shard event ordering required | **Yes** - use `Aggregated` mode |
| Maximum throughput per shard | **Yes** - use `PerShardParallel` mode |
| Different databases, different schemas | No - use separate CDC connectors |

---

## Architecture

Sharded CDC uses a **wrapper pattern**: it wraps multiple per-shard `ICdcConnector` instances and exposes a unified `IShardedCdcConnector` interface.

```text
                    ┌──────────────────────────┐
                    │   ShardedCdcProcessor    │
                    │   (BackgroundService)    │
                    └────────────┬─────────────┘
                                 │
                    ┌────────────▼─────────────┐
                    │  IShardedCdcConnector    │
                    │  (ShardedCdcConnector)   │
                    └────────────┬─────────────┘
                                 │
              ┌──────────────────┼───────────────────┐
              │                  │                   │
     ┌────────▼─────────┐ ┌──────▼────────┐ ┌────────▼─────────┐
     │  ICdcConnector   │ │ ICdcConnector │ │  ICdcConnector   │
     │  (Shard A)       │ │ (Shard B)     │ │  (Shard C)       │
     └────────┬─────────┘ └──────┬────────┘ └────────┬─────────┘
              │                  │                   │
     ┌────────▼─────────┐ ┌──────▼────────┐ ┌────────▼─────────┐
     │   Database A     │ │  Database B   │ │   Database C     │
     └──────────────────┘ └───────────────┘ └──────────────────┘
```

**Key components:**

| Component | Responsibility |
|-----------|---------------|
| `IShardedCdcConnector` | Aggregates per-shard connectors into unified streams |
| `ShardedCdcConnector` | Internal implementation using `Channel<T>` for stream merging |
| `ShardedCdcProcessor` | `BackgroundService` that consumes aggregated stream and dispatches events |
| `IShardedCdcPositionStore` | Persists positions per `(shardId, connectorId)` composite key |
| `InMemoryShardedCdcPositionStore` | Default `ConcurrentDictionary`-based implementation |
| `ShardedCdcHealthCheck` | Reports health based on shard lag and connector status |
| `ShardedCdcMetrics` | OpenTelemetry metrics for shard-level monitoring |

---

## Quick Start

### 1. Register CDC with Sharded Capture

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders")
          .WithShardedCapture(opts =>
          {
              opts.AutoDiscoverShards = true;
              opts.ProcessingMode = ShardedProcessingMode.Aggregated;
              opts.ConnectorId = "orders-sharded-cdc";
          });
});

// Register a CDC provider for each shard (e.g., SQL Server)
services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = connectionString;
    opts.TrackedTables = ["dbo.Orders"];
});
```

### 2. Ensure Shard Topology Is Registered

Sharded CDC requires an `IShardTopologyProvider` from the [Database Sharding](sharding.md) module:

```csharp
services.AddEncinaSharding(config =>
{
    config.AddShardTopology(topology =>
    {
        topology.AddShard("shard-1", "Server=shard1;...");
        topology.AddShard("shard-2", "Server=shard2;...");
        topology.AddShard("shard-3", "Server=shard3;...");
    });
});
```

### 3. Implement Your Change Event Handler

```csharp
public class OrderChangeHandler : IChangeEventHandler<Order>
{
    public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
        Order entity, ChangeMetadata metadata, CancellationToken ct)
    {
        // Process new order from any shard
        return Right(unit).AsValueTask();
    }

    public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
        Order before, Order after, ChangeMetadata metadata, CancellationToken ct)
    {
        return Right(unit).AsValueTask();
    }

    public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
        Order entity, ChangeMetadata metadata, CancellationToken ct)
    {
        return Right(unit).AsValueTask();
    }
}
```

---

## Configuration

### ShardedCaptureOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AutoDiscoverShards` | `bool` | `true` | Discover shards from `IShardTopologyProvider` at startup |
| `ProcessingMode` | `ShardedProcessingMode` | `Aggregated` | How events are streamed and processed |
| `OnShardAdded` | `Action<ShardInfo>?` | `null` | Callback when a shard is added |
| `OnShardRemoved` | `Action<string>?` | `null` | Callback when a shard is removed |
| `PositionStoreType` | `Type?` | `null` | Custom `IShardedCdcPositionStore` implementation |
| `MaxLagThreshold` | `TimeSpan` | 5 minutes | Lag threshold before health degrades |
| `ConnectorId` | `string` | `"sharded-cdc"` | Unique connector identifier |

### Full Configuration Example

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders")
          .WithOptions(opts =>
          {
              opts.BatchSize = 50;
              opts.PollingInterval = TimeSpan.FromMilliseconds(100);
              opts.MaxRetries = 5;
              opts.BaseRetryDelay = TimeSpan.FromMilliseconds(500);
              opts.EnablePositionTracking = true;
          })
          .WithShardedCapture(opts =>
          {
              opts.AutoDiscoverShards = true;
              opts.ProcessingMode = ShardedProcessingMode.Aggregated;
              opts.MaxLagThreshold = TimeSpan.FromMinutes(5);
              opts.ConnectorId = "orders-sharded-cdc";
              opts.OnShardAdded = shard =>
                  logger.LogInformation("Shard added: {ShardId}", shard.ShardId);
              opts.OnShardRemoved = shardId =>
                  logger.LogInformation("Shard removed: {ShardId}", shardId);
          });
});
```

---

## Position Tracking

### Composite Key Strategy

Unlike standard CDC which tracks a single position per connector, sharded CDC uses a `(shardId, connectorId)` composite key. This allows each shard to maintain its own independent position in the change stream.

```
Standard CDC:      connectorId → position
Sharded CDC:       (shardId, connectorId) → position
```

### IShardedCdcPositionStore

```csharp
public interface IShardedCdcPositionStore
{
    Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(
        string shardId, string connectorId, CancellationToken ct);

    Task<Either<EncinaError, Unit>> SavePositionAsync(
        string shardId, string connectorId, CdcPosition position, CancellationToken ct);

    Task<Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>> GetAllPositionsAsync(
        string connectorId, CancellationToken ct);

    Task<Either<EncinaError, Unit>> DeletePositionAsync(
        string shardId, string connectorId, CancellationToken ct);
}
```

### Default In-Memory Store

The default `InMemoryShardedCdcPositionStore` uses `ConcurrentDictionary` with case-insensitive keys via `ToUpperInvariant()`:

- Thread-safe for concurrent saves from multiple shards
- Data is lost on process restart (suitable for development)
- For production, implement a persistent `IShardedCdcPositionStore`

### Custom Position Store

Register a custom persistent implementation:

```csharp
config.WithShardedCapture(opts =>
{
    opts.PositionStoreType = typeof(SqlServerShardedCdcPositionStore);
});
```

---

## Topology Changes

### Runtime Shard Addition

```csharp
// The IShardedCdcConnector supports runtime topology changes
shardedConnector.AddConnector("shard-4", newShardConnector);
```

When a shard is added:

1. A new `ICdcConnector` is created for the shard
2. Position is loaded from the store (resumes from last checkpoint)
3. The shard's change stream is merged into the aggregated stream
4. The `OnShardAdded` callback is invoked

### Runtime Shard Removal

```csharp
shardedConnector.RemoveConnector("shard-4");
```

When a shard is removed:

1. The shard's stream is stopped
2. The shard connector is disposed
3. Saved positions are preserved (for potential re-addition)
4. The `OnShardRemoved` callback is invoked

### Auto-Discovery

When `AutoDiscoverShards = true` (default), the sharded connector queries the `IShardTopologyProvider` at startup to discover all available shards and creates connectors for each.

---

## Processing Modes

### Aggregated Mode (Default)

```csharp
opts.ProcessingMode = ShardedProcessingMode.Aggregated;
```

- Events from all shards are merged into a **single ordered stream**
- Ordering is by `ChangeMetadata.CapturedAtUtc` with `ShardId` as tiebreaker
- Uses `Channel<T>` internally for efficient stream merging
- Best for scenarios requiring cross-shard event ordering

### PerShardParallel Mode

```csharp
opts.ProcessingMode = ShardedProcessingMode.PerShardParallel;
```

- Each shard is processed **independently in parallel**
- No cross-shard ordering guarantees
- Higher throughput for independent shard processing
- Best for scenarios where shard isolation is sufficient

---

## Health Checks

`ShardedCdcHealthCheck` extends `EncinaHealthCheck` and reports health based on:

| State | Condition |
|-------|-----------|
| **Healthy** | All shards active, lag below threshold |
| **Degraded** | One or more shards lagging beyond `MaxLagThreshold` |
| **Unhealthy** | No active shards, or connector errors |

Health check data includes:

- `active_shard_count`: Number of active shard connectors
- `shard_ids`: Comma-separated list of active shard identifiers
- `connector_id`: The sharded connector identifier

---

## Observability

### Metrics (via `ShardedCdcMetrics`)

| Instrument | Type | Tags | Description |
|------------|------|------|-------------|
| `encina.cdc.sharded.events_total` | Counter | `shard.id`, `cdc.operation` | Events processed per shard |
| `encina.cdc.sharded.position_saves_total` | Counter | `shard.id` | Position saves per shard |
| `encina.cdc.sharded.errors_total` | Counter | `shard.id`, `error.type` | Errors per shard |
| `encina.cdc.sharded.active_connectors` | ObservableGauge | — | Active connector count |
| `encina.cdc.sharded.lag_ms` | ObservableGauge | `shard.id` | Replication lag per shard |

### Trace Attributes

| Attribute | Description |
|-----------|-------------|
| `encina.cdc.shard.id` | Shard identifier for the current operation |
| `encina.cdc.operation` | CDC operation type (insert, update, delete) |

### Registering Metrics

Metrics are automatically registered when using `AddEncinaOpenTelemetry()`:

```csharp
services.AddEncinaOpenTelemetry();
```

---

## Error Handling

### Error Codes

| Code | Description |
|------|-------------|
| `encina.cdc.shard_not_found` | Referenced shard not found in the connector |
| `encina.cdc.shard_stream_failed` | A per-shard CDC stream failed during aggregation |

These extend the existing CDC error codes. See [CDC Error Handling](cdc.md#error-handling) for the full list.

### Retry Behavior

The `ShardedCdcProcessor` uses the same retry logic as the standard `CdcProcessor`:

- Exponential backoff with configurable `BaseRetryDelay`
- Maximum retries controlled by `MaxRetries`
- Position is only saved after successful dispatch (at-least-once delivery)

---

## Testing

### Test Coverage (132 tests)

| Test Type | Count | Location |
|-----------|-------|----------|
| Unit Tests | 86 | `tests/Encina.UnitTests/Cdc/Sharding/` |
| Guard Tests | 23 | `tests/Encina.GuardTests/Cdc/Sharding/` |
| Contract Tests | 14 | `tests/Encina.ContractTests/Cdc/Sharding/` |
| Property Tests | 9 | `tests/Encina.PropertyTests/Cdc/Sharding/` |

Integration tests are not needed because sharded CDC is a pure abstraction layer that delegates all database interaction to per-shard `ICdcConnector` instances, which have their own integration tests. See `tests/Encina.IntegrationTests/Cdc/Sharding/ShardedCdcIntegrationTests.md` for the full justification.

---

## Best Practices

### 1. Use Meaningful Connector IDs

```csharp
opts.ConnectorId = "orders-sharded-cdc";  // Descriptive
opts.ConnectorId = "cdc-1";               // Avoid generic names
```

### 2. Set Appropriate Lag Thresholds

```csharp
// For latency-sensitive workloads
opts.MaxLagThreshold = TimeSpan.FromMinutes(1);

// For batch-oriented workloads
opts.MaxLagThreshold = TimeSpan.FromMinutes(15);
```

### 3. Choose the Right Processing Mode

- Use `Aggregated` when you need events ordered across shards (e.g., building a unified event log)
- Use `PerShardParallel` when shards are independent and throughput matters more than ordering

### 4. Implement a Persistent Position Store for Production

The default `InMemoryShardedCdcPositionStore` is suitable for development. For production, implement `IShardedCdcPositionStore` backed by a durable store.

### 5. Monitor Per-Shard Lag

Use the `encina.cdc.sharded.lag_ms` gauge to detect shards falling behind and investigate root causes (slow queries, network issues, lock contention).

---

## FAQ

### Can I use sharded CDC with the messaging bridge?

Yes. The standard CDC messaging bridge works with sharded CDC. Events dispatched from the sharded processor pass through the same `ICdcDispatcher` and `ICdcEventInterceptor` pipeline.

### What happens when a shard is temporarily unreachable?

The per-shard `ICdcConnector` for that shard will produce `Left(error)` values. These are counted as failures and logged, but do not stop other shards from processing. When the shard recovers, it resumes from its last saved position.

### Can I use different CDC providers for different shards?

Yes. Each shard gets its own `ICdcConnector` instance, which can be any provider (SQL Server, PostgreSQL, etc.). The sharded connector only requires the `ICdcConnector` interface.

### Is the in-memory position store thread-safe?

Yes. `InMemoryShardedCdcPositionStore` uses `ConcurrentDictionary` with `ToUpperInvariant()` keys for thread-safe, case-insensitive composite key lookups.

### How does sharded CDC relate to the existing CdcProcessor?

`ShardedCdcProcessor` and `CdcProcessor` are mutually exclusive. When `WithShardedCapture()` is enabled, `ShardedCdcProcessor` is registered instead of `CdcProcessor`. Both use the same `ICdcDispatcher`, handlers, and interceptors.

## Related Documentation

- [Change Data Capture (CDC)](cdc.md) - Core CDC infrastructure
- [CDC for SQL Server](cdc-sqlserver.md) - SQL Server Change Tracking connector
- [CDC for PostgreSQL](cdc-postgresql.md) - PostgreSQL Logical Replication connector
- [CDC for MySQL](cdc-mysql.md) - MySQL Binary Log connector
- [CDC for MongoDB](cdc-mongodb.md) - MongoDB Change Streams connector
- [CDC for Debezium](cdc-debezium.md) - Debezium HTTP/Kafka connector
- [Database Sharding](sharding.md) - Shard topology and routing
