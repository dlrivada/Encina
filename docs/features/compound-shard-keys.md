# Compound Shard Keys in Encina

This guide explains how to use compound shard keys for multi-field routing decisions. Compound keys enable routing based on combinations of entity properties (e.g., `{region, customerId}`) with independent strategies per component.

## Table of Contents

1. [Overview](#overview)
2. [When to Use Compound Keys](#when-to-use-compound-keys)
3. [Defining Compound Keys](#defining-compound-keys)
4. [Configuring Compound Routing](#configuring-compound-routing)
5. [Key Extraction Priority](#key-extraction-priority)
6. [Partial Key Routing (Scatter-Gather)](#partial-key-routing-scatter-gather)
7. [Combining Shard IDs](#combining-shard-ids)
8. [Error Handling](#error-handling)
9. [Observability](#observability)
10. [FAQ](#faq)

---

## Overview

Standard shard keys use a single field for routing (e.g., `customerId`). Compound shard keys extend this by using **multiple ordered fields**, where each field can be routed by a different strategy:

```text
┌──────────────────────────────────────────────────────────────────┐
│                    Compound Shard Key                             │
│                {region, customerId}                               │
│                                                                   │
│  Component 0: "us-east"  ──► RangeShardRouter   ──► "shard-us"  │
│  Component 1: "cust-42"  ──► HashShardRouter    ──► "shard-3"   │
│                                                                   │
│  ShardIdCombiner: ("shard-us", "shard-3") ──► "shard-us-3"      │
└──────────────────────────────────────────────────────────────────┘
```

Each component is routed independently, and the results are combined via a configurable `ShardIdCombiner` (default: hyphen-join).

---

## When to Use Compound Keys

| Scenario | Single Key | Compound Key |
|----------|:----------:|:------------:|
| Route by customer ID | ✅ | |
| Route by region, then hash by customer | | ✅ |
| Geographic routing with tenant isolation | | ✅ |
| Date-range partitioning + hash distribution | | ✅ |
| Simple tenant-based sharding | ✅ | |

**Use compound keys when**:

- Your routing needs multiple dimensions (geography + identity, time + hash)
- Different components require different strategies (range for region, hash for customer)
- You need partial key queries (query all shards in a region without specifying customer)

---

## Defining Compound Keys

There are three ways to define compound shard keys on your entities, listed by priority:

### Option 1: `ICompoundShardable` Interface (Highest Priority)

```csharp
public class Order : ICompoundShardable
{
    public OrderId Id { get; init; }
    public string Region { get; init; }
    public string CustomerId { get; init; }

    public CompoundShardKey GetCompoundShardKey()
        => new(Region, CustomerId);
}
```

### Option 2: Multiple `[ShardKey]` Attributes with Order

```csharp
public class RegionalOrder
{
    public OrderId Id { get; init; }

    [ShardKey(Order = 0)]
    public string Region { get; init; }

    [ShardKey(Order = 1)]
    public string CustomerId { get; init; }
}
```

### Option 3: Single `[ShardKey]` Attribute (Falls Back to Single-Component Key)

```csharp
public class SimpleOrder
{
    public OrderId Id { get; init; }

    [ShardKey]
    public string CustomerId { get; init; }
}
```

> **Note**: A single `[ShardKey]` produces a `CompoundShardKey` with one component, which is fully compatible with both single and compound routing.

---

## Configuring Compound Routing

Use the `UseCompoundRouting()` method on `ShardingOptions<TEntity>` to assign a strategy per component:

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseCompoundRouting(compound =>
    {
        compound
            .RangeComponent(0, regionRanges)   // Component 0: range by region
            .HashComponent(1)                   // Component 1: hash by customer
            .CombineWith(parts => string.Join("-", parts));
    })
    .AddShard("shard-us-0", "Server=us0;Database=Orders;...")
    .AddShard("shard-us-1", "Server=us1;Database=Orders;...")
    .AddShard("shard-eu-0", "Server=eu0;Database=Orders;...");
});
```

### Available Component Helpers

| Method | Strategy | Example Use |
|--------|----------|-------------|
| `HashComponent(index)` | xxHash64 + consistent hashing | Customer ID, user ID |
| `RangeComponent(index, ranges)` | Sorted boundary ranges | Date ranges, alphabetical |
| `DirectoryComponent(index, store)` | Explicit key-to-shard mapping | VIP customers, tenant mapping |
| `GeoComponent(index, regions, resolver)` | Geographic region routing | Country, region codes |
| `Component(index, router)` | Any `IShardRouter` instance | Custom strategies |
| `Component(index, factory)` | Factory with topology access | Topology-dependent setup |

### Component Index Rules

- Indices must be 0-based and contiguous
- Index 0 corresponds to the primary component (first in `CompoundShardKey.Components`)
- Non-contiguous indices (e.g., 0, 2 without 1) will cause a build error

---

## Key Extraction Priority

The `CompoundShardKeyExtractor` resolves keys using this priority order:

```text
1. ICompoundShardable.GetCompoundShardKey()     ← Highest
2. Multiple [ShardKey] attributes (ordered)
3. IShardable.GetShardKey()                      ← Single component
4. Single [ShardKey] attribute                   ← Single component
```

- If an entity implements `ICompoundShardable`, that always wins
- Multiple `[ShardKey]` attributes with `Order` values produce a compound key
- `IShardable` and single `[ShardKey]` produce a single-component compound key
- Duplicate `Order` values cause an `EncinaError` with code `DuplicateShardKeyOrder`

### Extraction Caching

`CompoundShardKeyExtractor` caches reflection metadata per entity type using a `ConcurrentDictionary`, so repeated extractions for the same type are allocation-free after the first call.

---

## Partial Key Routing (Scatter-Gather)

When you only know some components (e.g., region but not customer), use `GetShardIds()` for scatter-gather:

```csharp
// Full key → single shard
var fullKey = new CompoundShardKey("us-east", "cust-42");
Either<EncinaError, string> shardId = router.GetShardId(fullKey);

// Partial key (region only) → multiple shards
var partialKey = new CompoundShardKey("us-east");
Either<EncinaError, IReadOnlyList<string>> shardIds = router.GetShardIds(partialKey);
// Returns all "us-east-*" shards
```

**How partial routing works**:

1. Components present in the partial key are routed normally
2. Missing components expand to all possible shards
3. The resulting shard ID list is the intersection of routed components

This is useful for queries like: "Give me all orders in the US East region" where the customer ID is not specified.

---

## Combining Shard IDs

The `ShardIdCombiner` function merges per-component shard IDs into a single final shard ID:

```csharp
// Default: hyphen-join
// ("shard-us", "shard-3") → "shard-us-shard-3"

// Custom combiner:
compound.CombineWith(parts => string.Join("_", parts));
// ("shard-us", "shard-3") → "shard-us_shard-3"

// Advanced: use only primary
compound.CombineWith(parts => parts.First());
// ("shard-us", "shard-3") → "shard-us"
```

The combiner receives shard IDs in component order. The combined result must match a shard ID in the `ShardTopology`.

---

## Error Handling

Compound shard key operations return `Either<EncinaError, T>` following the Railway Oriented Programming pattern. Four error codes are specific to compound keys:

| Error Code | Constant | Cause |
|------------|----------|-------|
| `encina.sharding.compound_shard_key_empty` | `ShardingErrorCodes.CompoundShardKeyEmpty` | Empty `CompoundShardKey` (no components) |
| `encina.sharding.compound_shard_key_component_empty` | `ShardingErrorCodes.CompoundShardKeyComponentEmpty` | A component value is null or empty |
| `encina.sharding.duplicate_shard_key_order` | `ShardingErrorCodes.DuplicateShardKeyOrder` | Two `[ShardKey]` attributes share the same `Order` |
| `encina.sharding.partial_key_routing_failed` | `ShardingErrorCodes.PartialKeyRoutingFailed` | Partial key couldn't resolve any shards |

```csharp
var result = CompoundShardKeyExtractor.Extract(order);

result.Match(
    Right: key => Console.WriteLine($"Key: {key}"),
    Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}")
);
```

---

## Observability

Compound key operations emit metrics through the `ShardRoutingMetrics` class:

- **`RecordCompoundKeyExtraction(componentCount, routerType)`**: Tracks extraction operations with the number of components and the router type used

These metrics are emitted under the standard `"Encina"` meter, alongside the existing 7 sharding metric instruments.

---

## FAQ

### Can I mix single and compound keys in the same application?

Yes. Each entity type has its own sharding configuration. Some entities can use single-key routing while others use compound routing.

### What happens if my entity implements both `IShardable` and `ICompoundShardable`?

`ICompoundShardable` takes priority. The `IShardable` implementation will be ignored by `CompoundShardKeyExtractor`.

### Do I need to change existing single-key entities?

No. Single-key entities are automatically wrapped into a single-component `CompoundShardKey`. Existing `IShardable` implementations and single `[ShardKey]` attributes continue to work without changes.

### Can component routers use different topologies?

No. All component routers share the same `ShardTopology`. The compound router combines per-component shard IDs into a final ID that must exist in the topology.

### What is the performance overhead of compound keys?

Minimal. `CompoundShardKeyExtractor` caches reflection metadata per type. The compound router routes each component independently (typically 2-3 components), then combines results with a single function call.

---

## Related Documentation

- [Database Sharding Configuration](../sharding/configuration.md) — Complete sharding configuration reference
- [Cross-Shard Operations](../sharding/cross-shard-operations.md) — Scatter-gather and partial failure handling
- [Scaling Guidance](../sharding/scaling-guidance.md) — Shard key selection and capacity planning
- [ADR-010: Database Sharding](../architecture/adr/010-database-sharding.md) — Architecture Decision Record
