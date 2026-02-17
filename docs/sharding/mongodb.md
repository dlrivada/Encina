# MongoDB Sharding in Encina

This guide covers MongoDB-specific sharding configuration in Encina, including the dual-mode architecture that supports both native MongoDB sharding and application-level routing.

## Table of Contents

1. [Dual-Mode Overview](#dual-mode-overview)
2. [Native Sharding (Recommended)](#native-sharding-recommended)
3. [Application-Level Routing (Fallback)](#application-level-routing-fallback)
4. [Configuration Examples](#configuration-examples)
5. [Migration Between Modes](#migration-between-modes)
6. [Monitoring](#monitoring)
7. [Best Practices](#best-practices)
8. [FAQ](#faq)

---

## Dual-Mode Overview

Encina's MongoDB sharding supports two distinct modes controlled by `MongoDbShardingOptions.UseNativeSharding`:

```text
┌─────────────────────────────────────────────────────────────────┐
│                 Native Mode (UseNativeSharding = true)            │
│                                                                   │
│  Application ──► mongos ──┬──► shard1 (mongod/replica set)       │
│                           ├──► shard2 (mongod/replica set)       │
│                           └──► shard3 (mongod/replica set)       │
│                                                                   │
│  • MongoDB handles all routing transparently                      │
│  • Application sees a single logical database                     │
│  • Chunk balancing, migrations handled by MongoDB                 │
│  • Recommended for production                                     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│          App-Level Mode (UseNativeSharding = false)               │
│                                                                   │
│  Application ──► IShardRouter ──┬──► mongod1 (standalone/RS)     │
│                                 ├──► mongod2 (standalone/RS)     │
│                                 └──► mongod3 (standalone/RS)     │
│                                                                   │
│  • Encina routes at application level via IShardRouter            │
│  • Each shard is a separate MongoDB instance                      │
│  • No mongos required                                             │
│  • Use for dev/test or when mongos is unavailable                 │
└─────────────────────────────────────────────────────────────────┘
```

### Decision Criteria

| Factor | Native Sharding | App-Level Routing |
|--------|:--------------:|:-----------------:|
| **Production readiness** | Recommended | Acceptable |
| **Operational complexity** | Higher (mongos + config servers) | Lower (standalone mongod) |
| **Chunk balancing** | Automatic | Not available |
| **Infrastructure cost** | Higher (3+ nodes minimum) | Lower (1+ nodes) |
| **Development/testing** | Complex local setup | Simple docker-compose |
| **Query routing** | Transparent (mongos) | Application-managed |
| **Cross-shard queries** | Supported by mongos | Scatter-gather via Encina |

---

## Native Sharding (Recommended)

In native mode, the application connects to a `mongos` router. MongoDB handles shard key routing, chunk balancing, and data distribution transparently.

### When to Use

- Production deployments with high data volumes
- Workloads benefiting from MongoDB's automatic chunk balancing
- Teams with MongoDB operational expertise
- When you need cross-shard aggregation pipelines handled by mongos

### Configuration

```csharp
services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = true; // default
    options.IdProperty = o => o.Id;
    options.CollectionName = "orders";
    options.ShardKeyField = "customerId"; // MongoDB shard key field
});
```

### Connection String

Connect to the `mongos` instance (or multiple for HA):

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://mongos1:27017,mongos2:27017/myapp?readPreference=primaryPreferred"
  }
}
```

### MongoDB Cluster Setup

Native sharding requires a MongoDB sharded cluster:

```text
Config Servers (3-node replica set)
    ├── mongos router 1
    ├── mongos router 2
    ├── Shard 1 (replica set: primary + 2 secondaries)
    ├── Shard 2 (replica set: primary + 2 secondaries)
    └── Shard 3 (replica set: primary + 2 secondaries)
```

Enable sharding on the database and collection:

```javascript
// In mongos shell
sh.enableSharding("myapp")
sh.shardCollection("myapp.orders", { "customerId": "hashed" })
```

### How It Works Internally

When `UseNativeSharding = true`:

1. `IShardedMongoCollectionFactory` returns the **same collection** for all shard requests
2. The single `MongoClient` connects to `mongos`
3. `mongos` transparently routes operations based on the MongoDB shard key
4. Encina's `IShardRouter` is still used for `GetShardIdForEntity()` calls but the underlying data routing is delegated to MongoDB

---

## Application-Level Routing (Fallback)

In app-level mode, Encina manages routing using `IShardRouter` and connects to separate `mongod` instances per shard.

### When to Use

- Local development and testing (no mongos infrastructure needed)
- Small deployments where mongos overhead is not justified
- Cost-sensitive environments with standalone MongoDB instances
- When MongoDB sharded cluster setup is not feasible

### Configuration

Application-level mode requires both core sharding and MongoDB registration:

```csharp
// Step 1: Core sharding with topology and routing
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "mongodb://mongod1:27017/orders")
        .AddShard("shard-2", "mongodb://mongod2:27017/orders")
        .AddShard("shard-3", "mongodb://mongod3:27017/orders");
});

// Step 2: MongoDB registration with app-level mode
services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = false;
    options.IdProperty = o => o.Id;
    options.CollectionName = "orders";
});
```

### How It Works Internally

When `UseNativeSharding = false`:

1. `IShardedMongoCollectionFactory` creates a **separate `MongoClient`** per shard
2. Each `MongoClient` connects to a different `mongod` endpoint from the topology
3. Encina's `IShardRouter` determines which shard (and thus which `MongoClient`) handles each operation
4. Scatter-gather queries execute against multiple `MongoClient` instances in parallel

### Limitations

- **No automatic chunk balancing** — Data stays where Encina routes it
- **No cross-shard aggregation via mongos** — Use Encina scatter-gather instead
- **Manual capacity management** — You monitor and rebalance manually
- **No automatic failover** — If a shard goes down, operations to that shard fail

---

## Configuration Examples

### Development: Single Instance with App-Level Routing

Simplest setup for local development:

```csharp
// Development: single MongoDB with 3 logical "shards" (same instance, different DBs)
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "mongodb://localhost:27017/orders_shard1")
        .AddShard("shard-2", "mongodb://localhost:27017/orders_shard2")
        .AddShard("shard-3", "mongodb://localhost:27017/orders_shard3");
});

services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = false;
    options.IdProperty = o => o.Id;
});
```

### Production: Native Sharding with mongos

```csharp
services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = true;
    options.IdProperty = o => o.Id;
    options.CollectionName = "orders";
    options.ShardKeyField = "customerId";
    options.DatabaseName = "production_orders";
});
```

### Staging: App-Level with Replica Sets

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "mongodb://rs1-primary:27017,rs1-secondary:27017/orders?replicaSet=rs1")
        .AddShard("shard-2", "mongodb://rs2-primary:27017,rs2-secondary:27017/orders?replicaSet=rs2");
});

services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = false;
    options.IdProperty = o => o.Id;
});
```

---

## Migration Between Modes

### App-Level to Native (Recommended Upgrade Path)

When scaling from app-level to native MongoDB sharding:

1. **Set up MongoDB sharded cluster** — Config servers, mongos, shard replica sets
2. **Export data from each app-level shard** — Use `mongodump` per shard
3. **Import to sharded cluster** — `mongorestore` to the sharded collection
4. **Enable MongoDB sharding** — `sh.enableSharding()` and `sh.shardCollection()`
5. **Update Encina configuration** — Switch `UseNativeSharding = true` and update connection string to point to mongos

```csharp
// Before (app-level)
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "mongodb://mongod1:27017/orders")
        .AddShard("shard-2", "mongodb://mongod2:27017/orders");
});
services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = false;
    options.IdProperty = o => o.Id;
});

// After (native)
services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = true;
    options.IdProperty = o => o.Id;
    options.ShardKeyField = "customerId";
});
```

> **Important**: Ensure the MongoDB shard key (`ShardKeyField`) matches the Encina shard key (`IShardable.GetShardKey()`) for consistent routing.

---

## Monitoring

### Native Mode Monitoring

With native sharding, use MongoDB's built-in monitoring tools:

- **MongoDB Atlas** — Built-in dashboards for sharded clusters
- **MongoDB Cloud Manager** — Self-hosted monitoring
- **`sh.status()`** — Cluster status in mongos shell
- **`db.collection.getShardDistribution()`** — Data distribution per shard

Encina metrics are still emitted for application-level operations (routing decisions, scatter-gather timing).

### App-Level Mode Monitoring

With app-level routing, Encina metrics are the primary monitoring source:

| Metric | Description |
|--------|-------------|
| `encina.sharding.route.decisions` | Routing decisions per shard |
| `encina.sharding.route.duration_ns` | Routing latency |
| `encina.sharding.scatter.duration_ms` | Cross-shard query time |
| `encina.sharding.scatter.partial_failures` | Failed shard queries |

Additionally monitor each MongoDB instance independently:

- Connection count and pool utilization
- Storage size and oplog window
- Replication lag (if using replica sets)
- Query execution time

---

## Best Practices

### 1. Start Native in Production

For production workloads, always prefer native MongoDB sharding:

- MongoDB handles chunk balancing automatically
- mongos provides transparent cross-shard queries
- Built-in monitoring and management tools

### 2. Use App-Level for Local Development

App-level mode is ideal for development because:

- No mongos or config server infrastructure needed
- A single MongoDB instance can host multiple "shards" as different databases
- Fast startup and teardown for integration tests

### 3. Match Shard Keys

Ensure the Encina shard key and MongoDB shard key are consistent:

```csharp
// Encina shard key
public class Order : IShardable
{
    public string CustomerId { get; set; }
    public string GetShardKey() => CustomerId; // Encina routes by this
}

// MongoDB shard key must match
sh.shardCollection("myapp.orders", { "customerId": "hashed" })
```

### 4. Use Hashed Shard Keys in MongoDB

MongoDB supports ranged and hashed shard keys. For most workloads, use hashed:

```javascript
// Hashed — uniform distribution (matches Encina HashShardRouter behavior)
sh.shardCollection("myapp.orders", { "customerId": "hashed" })

// Ranged — use only when range queries on shard key are critical
sh.shardCollection("myapp.events", { "timestamp": 1 })
```

### 5. Configure Collection Name Explicitly

```csharp
options.CollectionName = "orders"; // Explicit
// vs
// Defaults to entity type name + "s" (e.g., "Orders")
```

Explicit names prevent surprises from naming convention differences.

---

## FAQ

### Can I use native sharding without Encina's routing?

Yes. If `UseNativeSharding = true`, mongos handles all routing. Encina's `IShardRouter` is only used for `GetShardIdForEntity()` metadata calls, not for actual data routing.

### What happens if mongos is down?

With native sharding, if all mongos routers are unreachable, the application cannot access MongoDB. Ensure you have multiple mongos instances and your connection string includes all of them.

### Can I mix native and app-level for different collections?

This is not recommended. Choose one mode per application. Mixing modes adds complexity and makes it harder to reason about data placement.

### How does the `IShardedMongoCollectionFactory` work in native mode?

It returns the same `IMongoCollection<TEntity>` regardless of the shard ID passed. The collection is backed by a single `MongoClient` connected to mongos, which handles the actual routing.

### Can I use MongoDB transactions across shards?

With native sharding, MongoDB 4.2+ supports multi-shard transactions. With app-level routing, cross-shard transactions are not supported; use the Saga pattern instead. See [Cross-Shard Operations](cross-shard-operations.md) for details.
