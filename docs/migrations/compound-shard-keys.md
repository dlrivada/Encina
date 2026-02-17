# Migration Guide: Compound Shard Keys

This guide covers compatibility considerations when adopting compound shard keys in an existing Encina sharding setup.

## Compatibility Summary

| Aspect | Impact | Action Required |
|--------|--------|-----------------|
| Existing `IShardable` entities | None | No changes needed |
| Existing `[ShardKey]` entities | None | No changes needed |
| `IShardRouter` interface | **Additive** | Default methods handle fallback |
| `ShardKeyAttribute` | **Additive** | New `Order` property (default 0) |
| `ShardingErrorCodes` | **Additive** | 4 new constants added |
| Existing routing configuration | None | No changes needed |

---

## No Breaking Changes

Compound shard keys are a purely **additive** feature. Existing code continues to work without modification:

### `IShardRouter` Default Interface Methods

Two new methods were added to `IShardRouter` as **default interface methods**:

```csharp
// New - has default implementation
Either<EncinaError, string> GetShardId(CompoundShardKey key);

// New - has default implementation
Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey);
```

**Default behavior**:

- `GetShardId(CompoundShardKey)` calls `key.ToString()` and delegates to `GetShardId(string)`
- `GetShardIds(CompoundShardKey)` returns `GetAllShardIds()` (all shards)

Custom `IShardRouter` implementations do **not** need to override these methods unless they want compound-specific routing logic.

### `ShardKeyAttribute.Order` Property

The `Order` property defaults to `0`, which preserves the existing behavior for single-key entities:

```csharp
// Existing code - still works, Order defaults to 0
[ShardKey]
public string CustomerId { get; init; }

// New code - explicit ordering for compound keys
[ShardKey(Order = 0)]
public string Region { get; init; }

[ShardKey(Order = 1)]
public string CustomerId { get; init; }
```

### Single-Key to Compound-Key Compatibility

Single-key values are automatically wrapped in a single-component `CompoundShardKey`:

```csharp
// Implicit conversion from string
CompoundShardKey key = "customer-123";
// Equivalent to: new CompoundShardKey("customer-123")

// key.ComponentCount == 1
// key.PrimaryComponent == "customer-123"
// key.HasSecondaryComponents == false
```

This means:

- `CompoundShardKeyExtractor.Extract()` works on both single-key and compound-key entities
- Existing routers handle compound keys through the default interface methods
- No configuration changes needed for existing single-key setups

---

## Adopting Compound Keys

### Step 1: Define Compound Keys on Entities

Choose one approach per entity:

```csharp
// Option A: Interface
public class Order : ICompoundShardable
{
    public CompoundShardKey GetCompoundShardKey()
        => new(Region, CustomerId);
}

// Option B: Attributes
public class Order
{
    [ShardKey(Order = 0)]
    public string Region { get; init; }

    [ShardKey(Order = 1)]
    public string CustomerId { get; init; }
}
```

### Step 2: Configure Compound Routing

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseCompoundRouting(compound =>
    {
        compound
            .RangeComponent(0, regionRanges)
            .HashComponent(1);
    })
    .AddShard("shard-us-0", connectionStringUs0)
    .AddShard("shard-eu-0", connectionStringEu0);
});
```

### Step 3: Update Shard Topology

Ensure your shard IDs match the combined output. The default combiner joins component shard IDs with a hyphen:

```text
Component 0 → "us"    (from RangeShardRouter)
Component 1 → "shard-3" (from HashShardRouter)
Combined    → "us-shard-3"
```

Your topology must contain the shard ID `"us-shard-3"` with a valid connection string.

---

## Provider-Specific Notes

All 13 database providers support compound shard keys without provider-specific changes. The compound routing operates at the application layer before any provider-specific connection factory is invoked.

| Provider Category | Support | Notes |
|-------------------|---------|-------|
| ADO.NET (4) | Full | Connection factory receives the combined shard ID |
| Dapper (4) | Full | Reuses ADO's `IShardedConnectionFactory` |
| EF Core (4) | Full | DbContext factory receives the combined shard ID |
| MongoDB (1) | Full | Collection factory receives the combined shard ID |

---

## New Error Codes

Four new error codes were added. These only occur when using compound key features:

| Code | When |
|------|------|
| `CompoundShardKeyEmpty` | `CompoundShardKey` created with no components |
| `CompoundShardKeyComponentEmpty` | A component value is null or empty |
| `DuplicateShardKeyOrder` | Two `[ShardKey]` attributes on the same entity have the same `Order` |
| `PartialKeyRoutingFailed` | Partial key routing couldn't resolve any shards |

---

## Related

- [Compound Shard Keys Feature Guide](../features/compound-shard-keys.md)
- [Sharding Configuration](../sharding/configuration.md)
