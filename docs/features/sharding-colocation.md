# Entity Co-Location

## Overview

Co-location groups ensure that related entities are always stored on the same shard, enabling efficient **local JOINs** and **shard-local transactions** without cross-shard coordination.

In a sharded database, a query like `SELECT * FROM Orders JOIN OrderItems ON ...` normally requires a scatter-gather across all shards. With co-location, both `Order` and `OrderItem` share the same shard key and are guaranteed to reside on the same shard, making the JOIN a local operation.

**Key benefits:**

- Eliminates cross-shard JOINs for related entities
- Enables shard-local transactions without distributed coordination
- Provides startup validation to catch misconfigurations early
- Integrates with all 5 routing strategies (Hash, Range, Directory, Geo, Compound)

## Configuration

There are two ways to define co-location groups: **declarative** (attributes) and **programmatic** (builder).

### Declarative (Attribute-Based)

Apply `[ColocatedWith]` to child entities that should share a shard with the root entity:

```csharp
// Root entity defines the shard topology
public class Order : IShardable
{
    public OrderId Id { get; init; }
    public string CustomerId { get; init; }
    public string GetShardKey() => CustomerId;
}

// Child entity co-located with Order
[ColocatedWith(typeof(Order))]
public class OrderItem : IShardable
{
    public OrderItemId Id { get; init; }
    public string CustomerId { get; init; }
    public string GetShardKey() => CustomerId;
}

// Another child entity co-located with Order
[ColocatedWith(typeof(Order))]
public class OrderPayment : IShardable
{
    public PaymentId Id { get; init; }
    public string CustomerId { get; init; }
    public string GetShardKey() => CustomerId;
}
```

### Programmatic (Builder API)

Use `ColocationGroupBuilder` for runtime or test scenarios:

```csharp
var group = new ColocationGroupBuilder()
    .WithRootEntity<Order>()
    .AddColocatedEntity<OrderItem>()
    .AddColocatedEntity<OrderPayment>()
    .WithSharedShardKeyProperty("CustomerId")
    .Build();

// Register manually
var registry = new ColocationGroupRegistry();
registry.RegisterGroup(group);
```

Or register entities incrementally:

```csharp
var registry = new ColocationGroupRegistry();
registry.RegisterColocatedEntity(typeof(Order), typeof(OrderItem));
registry.RegisterColocatedEntity(typeof(Order), typeof(OrderPayment));
```

### Router Construction

All routers accept an optional `ColocationGroupRegistry`:

```csharp
var registry = new ColocationGroupRegistry();
// ... register groups ...

var router = new HashShardRouter(topology, colocationRegistry: registry);

// Query co-location metadata
IColocationGroup? group = router.GetColocationGroup(typeof(OrderItem));
// group.RootEntity == typeof(Order)
// group.ColocatedEntities == [typeof(OrderItem), typeof(OrderPayment)]
```

## Validation Rules

Co-location constraints are validated at startup during service registration. A `ColocationViolationException` is thrown if any rule is violated.

| Rule | Error Code | Description |
|------|-----------|-------------|
| Entity must be shardable | `ColocationEntityNotShardable` | Both root and co-located entities must implement `IShardable`, `ICompoundShardable`, or use `[ShardKey]` |
| Compatible shard key types | `ColocationShardKeyMismatch` | Shard key types must be compatible between root and child entities |
| No duplicate registration | `ColocationDuplicateRegistration` | An entity can belong to at most one co-location group |
| No self-reference | `ColocationSelfReference` | An entity cannot be co-located with itself |

### Handling Violations

```csharp
try
{
    services.AddEncinaSharding<Order>(options =>
    {
        options.UseHashRouting()
            .AddShard("shard-0", "Server=shard0;...")
            .AddColocatedEntity<OrderItem>();
    });
}
catch (ColocationViolationException ex)
{
    // ex.RootEntityType, ex.FailedEntityType, ex.Reason
    // ex.ExpectedShardKeyType, ex.ActualShardKeyType (if applicable)

    // Convert to EncinaError for ROP pipelines
    EncinaError error = ex.ToEncinaError();
}
```

## Observability

### Metrics

Three instruments under the `Encina` meter (version `1.0`):

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `encina.sharding.colocation.groups_registered` | ObservableGauge | `{groups}` | Current number of co-location groups |
| `encina.sharding.colocation.validation_failures_total` | Counter | `{failures}` | Cumulative validation failures |
| `encina.sharding.colocation.local_joins_total` | Counter | `{joins}` | Cumulative local JOINs via co-location |

### Trace Attributes

Three attributes on sharding activities:

| Attribute | Type | Description |
|-----------|------|-------------|
| `encina.sharding.colocation.group` | `string` | Co-location group name (root entity type) |
| `encina.sharding.colocation.is_colocated` | `bool` | Whether the entity is co-located |
| `encina.sharding.colocation.root_entity` | `string` | Root entity type name |

Available via `Encina.OpenTelemetry.ActivityTagNames.Colocation.*`.

### Structured Logging

Five source-generated log events (EventIds 620-624):

| EventId | Level | Message |
|---------|-------|---------|
| 620 | Information | Co-location group registered |
| 621 | Error | Co-location validation failed |
| 622 | Debug | Co-location group routed |
| 623 | Warning | Co-location group not found |
| 624 | Debug | Co-location registry initialized |

## User Responsibilities

The co-location system validates structural constraints (shardability, key compatibility, uniqueness) but **does not enforce shard key value alignment at runtime**. Users must ensure:

1. **Same shard key values**: Co-located entities must produce the same shard key value for related records. For example, `Order.GetShardKey()` and `OrderItem.GetShardKey()` must return the same `CustomerId` for a given order and its items.

2. **Consistent insertion**: When inserting related entities, use the same shard key to ensure they land on the same shard. The router will route based on each entity's own shard key.

3. **No runtime cross-shard moves**: Moving an entity to a different shard key value after insertion may break co-location guarantees for existing related records.

## API Reference

### Core Types

| Type | Description |
|------|-------------|
| `IColocationGroup` | Interface: `RootEntity`, `ColocatedEntities`, `SharedShardKeyProperty` |
| `ColocationGroup` | Sealed record implementing `IColocationGroup` |
| `ColocationGroupBuilder` | Fluent builder for programmatic group construction |
| `ColocationGroupRegistry` | Thread-safe registry with O(1) bidirectional lookups |
| `ColocatedWithAttribute` | Declarative attribute for child entities |
| `ColocationViolationException` | Startup validation exception with `ToEncinaError()` |

### Router Integration

All routers expose `GetColocationGroup(Type entityType)`:

| Router | Co-location Support |
|--------|-------------------|
| `HashShardRouter` | Via optional `colocationRegistry` parameter |
| `RangeShardRouter` | Via optional `colocationRegistry` parameter |
| `DirectoryShardRouter` | Via optional `colocationRegistry` parameter |
| `GeoShardRouter` | Via optional `colocationRegistry` parameter |
| `CompoundShardRouter` | Via optional `colocationRegistry` parameter |
| `IShardRouter` (default) | Returns `null` (no co-location awareness) |

## Future Enhancements

The following enhancements are planned for future milestones:

- **Query-layer co-location awareness**: Automatic detection of co-located JOINs in specifications, routing to single shard instead of scatter-gather
- **Co-location-aware query planner**: Optimize query execution plans based on co-location metadata
- **Cross-group co-location**: Support for entities that participate in multiple co-location groups (e.g., an entity shared between two aggregates)
- **Runtime co-location validation**: Optional middleware to verify shard key alignment at insert time

## Related Documentation

- [Sharding Configuration](../sharding/configuration.md) — Complete sharding setup guide
- [Compound Shard Keys](compound-shard-keys.md) — Multi-field shard key support
- [Distributed Aggregations](distributed-aggregations.md) — Cross-shard aggregation operations
- [Specification Scatter-Gather](specification-scatter-gather.md) — Specification-based cross-shard queries
