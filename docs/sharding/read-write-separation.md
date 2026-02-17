# Read/Write Separation for Sharded Topologies

This guide explains how to configure per-shard read/write separation in Encina sharded applications. Each shard can have its own set of read replicas with independent selection strategies, health tracking, and staleness tolerance.

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Architecture](#architecture)
4. [Configuration](#configuration)
5. [Replica Selection Strategies](#replica-selection-strategies)
6. [Staleness Tolerance](#staleness-tolerance)
7. [Health Tracking](#health-tracking)
8. [Provider-Specific Setup](#provider-specific-setup)
9. [Observability](#observability)
10. [Failover Behavior](#failover-behavior)
11. [Combined with Other Patterns](#combined-with-other-patterns)
12. [FAQ](#faq)

---

## Overview

Sharded read/write separation extends Encina's sharding infrastructure to route read operations to replicas within each shard:

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│                              Application                                         │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  Request ──► ShardRouter ──► ShardId ──► ReadWrite Intent ──► Replica Selector   │
│                                                │                   │             │
│                                         ┌──────┴──────┐    ┌───────┴──────┐      │
│                                         │   Write?    │    │    Read?     │      │
│                                         │  → Primary  │    │ → Strategy   │      │
│                                         └──────┬──────┘    └───────┬──────┘      │
│                                                │                   │             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌───────────────────────────────┐     │
│  │    Shard 1      │  │    Shard 2      │  │           Shard 3             │     │
│  │  ┌─────────┐    │  │  ┌─────────┐    │  │  ┌─────────┐                  │     │
│  │  │ Primary │    │  │  │ Primary │    │  │  │ Primary │                  │     │
│  │  └─────────┘    │  │  └─────────┘    │  │  └─────────┘                  │     │
│  │  ┌─────────┐    │  │  ┌─────────┐    │  │  ┌─────────┐ ┌─────────┐      │     │
│  │  │Replica A│    │  │  │Replica A│    │  │  │Replica A│ │Replica B│      │     │
│  │  └─────────┘    │  │  └─────────┘    │  │  └─────────┘ └─────────┘      │     │
│  └─────────────────┘  └─────────────────┘  └───────────────────────────────┘     │
│                                                                                  │
│  Strategy: RoundRobin      Strategy: LeastLatency    Strategy: WeightedRandom    │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### Key Capabilities

| Capability | Description |
|-----------|-------------|
| **Per-shard replicas** | Each shard can have 0..N read replicas with its own connection strings |
| **5 selection strategies** | RoundRobin, Random, LeastLatency, LeastConnections, WeightedRandom |
| **Per-shard strategy override** | Different shards can use different selection strategies |
| **Health tracking** | Automatic health monitoring with recovery delay and unhealthy replica exclusion |
| **Staleness tolerance** | Global and per-query replication lag thresholds |
| **Automatic fallback** | Routes to primary when no healthy replicas are available |
| **Full observability** | OpenTelemetry metrics for routing decisions, latency, and fallback events |

### When to Use

| Scenario | Recommendation |
|----------|----------------|
| Single shard, single primary | Basic read/write separation (`Encina.Messaging.ReadWriteSeparation`) |
| Multiple shards, no replicas | Standard sharding (`AddEncinaSharding<T>`) |
| **Multiple shards with replicas** | **Sharded read/write separation (this guide)** |
| Geographic distribution per shard | This guide with `LeastLatency` strategy |

---

## Quick Start

### 1. Configure sharded read/write options

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-us", Configuration.GetConnectionString("ShardUS")!)
        .AddShard("shard-eu", Configuration.GetConnectionString("ShardEU")!);
});

// Configure read/write separation for the sharded topology
services.Configure<ShardedReadWriteOptions>(options =>
{
    options.DefaultReplicaStrategy = ReplicaSelectionStrategy.RoundRobin;
    options.FallbackToPrimaryWhenNoReplicas = true;
    options.ReplicaHealthCheckInterval = TimeSpan.FromSeconds(30);

    // US shard: 2 replicas, default strategy
    options.AddShard("shard-us", "Server=us-primary;...",
        replicaConnectionStrings: ["Server=us-replica1;...", "Server=us-replica2;..."]);

    // EU shard: 3 replicas, prefer lowest latency
    options.AddShard("shard-eu", "Server=eu-primary;...",
        replicaConnectionStrings: ["Server=eu-replica1;...", "Server=eu-replica2;...", "Server=eu-replica3;..."],
        replicaStrategy: ReplicaSelectionStrategy.LeastLatency);
});
```

### 2. Use the connection factory

```csharp
// Inject IShardedReadWriteConnectionFactory
public class OrderRepository
{
    private readonly IShardedReadWriteConnectionFactory _factory;

    public OrderRepository(IShardedReadWriteConnectionFactory factory)
        => _factory = factory;

    // Read — automatically routes to a replica
    public async Task<Order?> GetByIdAsync(string shardId, Guid id, CancellationToken ct)
    {
        var result = await _factory.GetReadConnectionAsync(shardId, ct);
        return result.Match(
            connection => /* query using connection */,
            error => /* handle error */);
    }

    // Write — always routes to primary
    public async Task SaveAsync(string shardId, Order order, CancellationToken ct)
    {
        var result = await _factory.GetWriteConnectionAsync(shardId, ct);
        result.IfRight(connection => /* insert/update using connection */);
    }

    // Context-aware — uses DatabaseRoutingContext intent
    public async Task<Order?> GetOrWriteAsync(string shardId, CancellationToken ct)
    {
        var result = await _factory.GetConnectionAsync(shardId, ct);
        // Routes to replica for Read intent, primary for Write intent
        return result.Match(
            connection => /* use connection */,
            error => /* handle error */);
    }
}
```

---

## Architecture

### Request Flow

The complete request flow from application code to database connection:

```text
┌────────────────────────────────────────────────────────────────────────────────┐
│                          Request Processing Pipeline                           │
│                                                                                │
│  ① ICommand<T> / IQuery<T>                                                     │
│         │                                                                      │
│  ② ShardKeyExtractor  ──────────────────►  ShardKey                            │
│         │                                                                      │
│  ③ IShardRouter.GetShardId(key)  ────────►  ShardId  (e.g., "shard-eu")        │
│         │                                                                      │
│  ④ DatabaseRoutingContext  ──────────────►  Intent  (Read / Write / ForceWrite)│
│         │                                                                      │
│  ⑤ Intent == Write?  ──── YES ──────────►  Primary connection string           │
│         │                                                                      │
│     NO (Read)                                                                  │
│         │                                                                      │
│  ⑥ IReplicaHealthTracker                                                       │
│     .GetAvailableReplicas(shardId)  ────►  Healthy replicas (filtered by lag)  │
│         │                                                                      │
│     No healthy replicas?                                                       │
│         │── YES ── FallbackToPrimary? ──►  Primary connection string           │
│         │                                  (fallback counter incremented)      │
│         │                                                                      │
│     Has healthy replicas                                                       │
│         │                                                                      │
│  ⑦ IShardReplicaSelector                                                       │
│     .SelectReplica(healthyReplicas)  ───►  Selected replica connection string  │
│         │                                                                      │
│  ⑧ Create IDbConnection / DbContext  ───►  Return to caller                    │
└────────────────────────────────────────────────────────────────────────────────┘
```

### Core Types

| Type | Responsibility |
|------|---------------|
| `IShardedReadWriteConnectionFactory` | Creates `IDbConnection` instances for a given shard with read/write routing |
| `IShardedReadWriteDbContextFactory<TContext>` | Creates `DbContext` instances with read/write routing (EF Core) |
| `IShardReplicaSelector` | Selects one replica from available replicas using a strategy |
| `ShardReplicaSelectorFactory` | Creates `IShardReplicaSelector` from `ReplicaSelectionStrategy` enum |
| `IReplicaHealthTracker` | Tracks health state and replication lag per replica per shard |
| `ShardReplicaHealthCheck` | Evaluates aggregate health across all shards and replicas |
| `ShardedReadWriteOptions` | Configuration for replica topology, strategies, and health thresholds |
| `ShardedReadWriteMetrics` | OpenTelemetry metrics for routing decisions and replica selection |

---

## Configuration

### ShardedReadWriteOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultReplicaStrategy` | `ReplicaSelectionStrategy` | `RoundRobin` | Default strategy for shards without an explicit override |
| `ReplicaHealthCheckInterval` | `TimeSpan` | 30 seconds | How often to check replica health |
| `UnhealthyReplicaRecoveryDelay` | `TimeSpan` | 30 seconds | Wait time before an unhealthy replica is retried |
| `MaxAcceptableReplicationLag` | `TimeSpan?` | `null` | Global replication lag threshold (null = no filtering) |
| `FallbackToPrimaryWhenNoReplicas` | `bool` | `true` | Whether reads fall back to primary when no replicas are available |

### Adding Shards

```csharp
options.AddShard(
    shardId: "shard-us",
    primaryConnectionString: "Server=us-primary;Database=Orders;...",
    replicaConnectionStrings: new[]
    {
        "Server=us-replica1;Database=Orders;...",
        "Server=us-replica2;Database=Orders;..."
    },
    replicaStrategy: ReplicaSelectionStrategy.RoundRobin);  // Optional override
```

The `AddShard` method supports fluent chaining:

```csharp
options
    .AddShard("shard-1", "Server=primary1;...",
        ["Server=replica1a;...", "Server=replica1b;..."])
    .AddShard("shard-2", "Server=primary2;...",
        ["Server=replica2a;..."],
        ReplicaSelectionStrategy.LeastLatency)
    .AddShard("shard-3", "Server=primary3;...");  // No replicas — reads use primary
```

### Per-Shard Strategy Overrides

Different shards can use different strategies based on their topology:

```csharp
// US data center: equal-capacity replicas → RoundRobin
options.AddShard("shard-us", "Server=us-primary;...",
    ["Server=us-r1;...", "Server=us-r2;...", "Server=us-r3;..."],
    ReplicaSelectionStrategy.RoundRobin);

// EU data center: varying network latency → LeastLatency
options.AddShard("shard-eu", "Server=eu-primary;...",
    ["Server=eu-r1;...", "Server=eu-r2;..."],
    ReplicaSelectionStrategy.LeastLatency);

// AP data center: unequal hardware → WeightedRandom
options.AddShard("shard-ap", "Server=ap-primary;...",
    ["Server=ap-r1;...", "Server=ap-r2;..."],
    ReplicaSelectionStrategy.WeightedRandom);
```

---

## Replica Selection Strategies

### Strategy Comparison

| Strategy | Algorithm | Overhead | Best For |
|----------|-----------|----------|----------|
| **RoundRobin** | `Interlocked.Increment` + modulo | <50ns | Equal-capacity replicas |
| **Random** | `Random.Shared.Next` | <100ns | Simple distribution, hot spot avoidance |
| **LeastLatency** | EMA smoothing (α=0.3) + min search | <200ns | Geographic distribution, varying network |
| **LeastConnections** | `ConcurrentDictionary` + min search | <200ns | Variable query complexity |
| **WeightedRandom** | Cumulative weight + binary search | <200ns | Heterogeneous hardware |

### RoundRobin (Default)

Distributes requests evenly in strict circular order:

```text
Request 1 → Replica A
Request 2 → Replica B
Request 3 → Replica C
Request 4 → Replica A  (wraps around)
```

Thread-safe via `Interlocked.Increment`. Guarantees perfectly even distribution.

### Random

Randomly selects a replica using `Random.Shared` (thread-safe):

```text
Request 1 → Replica B
Request 2 → Replica A
Request 3 → Replica A
Request 4 → Replica C
```

Approximately even distribution over time. Good default when strict fairness is not required.

### LeastLatency

Routes to the replica with the lowest observed latency, using exponential moving average (EMA) smoothing:

```text
Replica A: 5ms EMA   ← Selected
Replica B: 12ms EMA
Replica C: 8ms EMA
```

- **EMA alpha**: 0.3 (balances recent vs historical measurements)
- **Fallback**: Round-robin when no latency data has been reported
- Requires calling `ReportLatency()` after each operation

### LeastConnections

Routes to the replica with the fewest active connections:

```text
Replica A: 5 connections
Replica B: 2 connections  ← Selected
Replica C: 7 connections
```

- Tracks connections via `IncrementConnections()` / `DecrementConnections()`
- Adapts naturally to varying query execution times

### WeightedRandom

Randomly selects replicas proportional to configured weights:

```text
Weights: [5, 3, 1]
Replica A: ~56% traffic (weight 5)
Replica B: ~33% traffic (weight 3)
Replica C: ~11% traffic (weight 1)
```

- Weights passed to `WeightedRandomShardReplicaSelector(weights)`
- O(log n) selection via cumulative weight binary search
- Useful when replicas have different hardware capacity

---

## Staleness Tolerance

### Global Replication Lag Threshold

Configure a maximum acceptable replication lag across all shards:

```csharp
options.MaxAcceptableReplicationLag = TimeSpan.FromSeconds(5);
```

Replicas with replication lag exceeding this threshold are excluded from selection. If all replicas exceed the threshold and `FallbackToPrimaryWhenNoReplicas` is `true`, reads fall back to the primary.

### Per-Query Staleness via Attribute

Override the global threshold for specific queries using `[AcceptStaleReads]`:

```csharp
// This query tolerates up to 30 seconds of replication lag
[AcceptStaleReads(maxLagMilliseconds: 30_000)]
public sealed record GetDashboardStatsQuery : IQuery<DashboardStats>;

// This query accepts any staleness (never filtered by lag)
[AcceptStaleReads(maxLagMilliseconds: int.MaxValue)]
public sealed record GetHistoricalReportQuery : IQuery<Report>;
```

### StalenessOptions Configuration

```csharp
var stalenessOptions = new StalenessOptions
{
    Enabled = true,
    MaxAcceptableReplicationLag = TimeSpan.FromSeconds(5),
    FallbackToPrimaryWhenStale = true
};
```

| Property | Default | Description |
|----------|---------|-------------|
| `Enabled` | `false` | Whether staleness checking is active |
| `MaxAcceptableReplicationLag` | `null` | Global lag threshold (null = no limit) |
| `FallbackToPrimaryWhenStale` | `true` | Route to primary when all replicas are stale |

### Staleness Decision Matrix

| Scenario | Global Lag | Attribute | Result |
|----------|-----------|-----------|--------|
| No lag configured | null | None | All replicas eligible |
| Global 5s, replica at 3s | 5s | None | Replica eligible |
| Global 5s, replica at 8s | 5s | None | Replica excluded |
| Global 5s, replica at 8s | 5s | `[AcceptStaleReads(30000)]` | Replica eligible (attribute overrides) |
| All replicas stale | 5s | None | Fallback to primary |

---

## Health Tracking

### IReplicaHealthTracker

The health tracker monitors replica availability per shard:

```csharp
// Mark a replica as unhealthy (e.g., connection failure)
tracker.MarkUnhealthy("shard-us", "Server=us-replica1;...");

// Mark as healthy again (e.g., successful connection)
tracker.MarkHealthy("shard-us", "Server=us-replica1;...");

// Report replication lag
tracker.ReportReplicationLag("shard-us", "Server=us-replica1;...",
    TimeSpan.FromMilliseconds(150));

// Get available replicas (filters unhealthy and stale)
IReadOnlyList<string> available = tracker.GetAvailableReplicas(
    "shard-us",
    allReplicaConnectionStrings,
    maxAcceptableLag: TimeSpan.FromSeconds(5));
```

### Recovery Delay

Unhealthy replicas are not immediately retried. The `UnhealthyReplicaRecoveryDelay` (default: 30s) prevents flapping:

```text
T=0s:   Replica fails  →  MarkUnhealthy()  →  Excluded from selection
T=15s:  Still excluded (within 30s recovery window)
T=30s:  Recovery delay elapsed  →  Eligible again if MarkHealthy() called
```

### ShardReplicaHealthCheck

Evaluates aggregate replica health across all shards:

```csharp
var healthCheck = new ShardReplicaHealthCheck(topology, tracker, options);
healthCheck.MinimumHealthyReplicasPerShard = 1;  // Default

var summary = await healthCheck.CheckReplicaHealthAsync(ct);

// summary.OverallStatus: Healthy | Degraded | Unhealthy
// summary.AllHealthy: true if all shards are Healthy
// summary.ShardResults: per-shard breakdown
```

| Status | Condition |
|--------|-----------|
| **Healthy** | All shards have ≥ MinimumHealthyReplicasPerShard healthy replicas |
| **Degraded** | Some shards have fewer healthy replicas than minimum, but fallback to primary is available |
| **Unhealthy** | Some shards have no healthy replicas AND `FallbackToPrimaryWhenNoReplicas` is `false` |

---

## Provider-Specific Setup

### ADO.NET (SqlServer, PostgreSQL, MySQL, SQLite)

```csharp
// ADO.NET: IShardedReadWriteConnectionFactory returns IDbConnection
var result = await factory.GetReadConnectionAsync("shard-us", ct);
result.IfRight(connection =>
{
    // connection is SqlConnection, NpgsqlConnection, etc.
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM Orders WHERE Id = @Id";
    // ...
});
```

ADO.NET providers also implement the generic `IShardedReadWriteConnectionFactory<TConnection>` for strongly-typed connections:

```csharp
// Strongly-typed: returns SqlConnection directly
IShardedReadWriteConnectionFactory<SqlConnection> typedFactory = ...;
var result = await typedFactory.GetReadConnectionAsync("shard-us", ct);
result.IfRight(sqlConnection =>
{
    // sqlConnection is already SqlConnection, no cast needed
});
```

### Dapper (SqlServer, PostgreSQL, MySQL, SQLite)

```csharp
// Dapper: IShardedReadWriteConnectionFactory returns IDbConnection
var result = await factory.GetReadConnectionAsync("shard-eu", ct);
result.IfRight(async connection =>
{
    var orders = await connection.QueryAsync<Order>(
        "SELECT * FROM Orders WHERE CustomerId = @CustomerId",
        new { CustomerId = customerId });
});
```

Dapper providers implement `IShardedReadWriteConnectionFactory` (non-generic), since Dapper works with `IDbConnection` directly.

### EF Core (SqlServer, PostgreSQL, MySQL, SQLite)

```csharp
// EF Core: IShardedReadWriteDbContextFactory<TContext>
IShardedReadWriteDbContextFactory<AppDbContext> contextFactory = ...;

// Context-aware: uses DatabaseRoutingContext intent
var result = await contextFactory.CreateContextForShardAsync("shard-eu", ct);
result.IfRight(async context =>
{
    var orders = await context.Orders
        .Where(o => o.Status == OrderStatus.Active)
        .ToListAsync(ct);
});

// Explicit read: always uses a replica
var readResult = contextFactory.CreateReadContextForShard("shard-eu");
readResult.IfRight(readContext =>
{
    // readContext is configured with a replica connection string
});

// Scatter-gather: read from all shards
var allResults = contextFactory.CreateAllReadContexts();
// Returns Either<EncinaError, IReadOnlyList<(string ShardId, TContext Context)>>
```

### MongoDB

MongoDB uses its native read preference system rather than separate connection strings:

```csharp
services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    // MongoDB handles replica routing via read preferences
    options.ReadPreference = MongoReadPreference.SecondaryPreferred;
    options.MaxStaleness = TimeSpan.FromMinutes(2);
});
```

> **Note**: For MongoDB, the `IShardedReadWriteConnectionFactory` pattern does not apply. MongoDB's driver handles replica selection internally via `ReadPreference`. See the [MongoDB Sharding Guide](mongodb.md) for details.

---

## Observability

### OpenTelemetry Metrics

All metrics are emitted under the `Encina` meter (version `1.0`):

| Instrument | Type | Unit | Tags | Description |
|-----------|------|------|------|-------------|
| `encina.sharding.rw.routing_total` | Counter | `{decisions}` | `shard_id`, `rw_intent`, `replica_id` | Total read/write routing decisions |
| `encina.sharding.rw.replica_selection_duration` | Histogram | `ms` | `shard_id`, `strategy` | Time to select a replica |
| `encina.sharding.rw.replica_latency` | Histogram | `ms` | `shard_id`, `replica_id` | Observed replica latency |
| `encina.sharding.rw.fallback_to_primary_total` | Counter | `{fallbacks}` | `shard_id`, `reason` | Fallback events (no_replicas, all_unhealthy, all_stale) |
| `encina.sharding.rw.unhealthy_replicas` | ObservableGauge | `{replicas}` | `shard_id` | Current unhealthy replica count |
| `encina.sharding.rw.replication_lag` | ObservableGauge | `ms` | `shard_id`, `replica_id` | Current replication lag per replica |

### Enabling Metrics

```csharp
services.Configure<ShardingMetricsOptions>(options =>
{
    options.EnableReadWriteMetrics = true;  // Default: true
});
```

### Structured Logging

Debug-level logging for routing decisions:

```
[shard-us] Routing read to replica Server=us-replica1;... (strategy: RoundRobin)
[shard-eu] Falling back to primary: all replicas unhealthy
[shard-us] Replica Server=us-replica2;... marked unhealthy
[shard-us] Replica Server=us-replica2;... recovered after 30s
```

---

## Failover Behavior

### Automatic Fallback Chain

```text
① Attempt to select a healthy replica
    │
    ├── Success → Return replica connection
    │
    └── No healthy replicas?
         │
         ├── FallbackToPrimaryWhenNoReplicas = true
         │       → Return primary connection
         │       → Increment fallback counter
         │       → Log warning
         │
         └── FallbackToPrimaryWhenNoReplicas = false
                 → Return EncinaError
                 → Caller handles error via Either<EncinaError, T>
```

### Failover Scenarios

| Scenario | FallbackEnabled | Behavior |
|----------|----------------|----------|
| All replicas healthy | N/A | Normal selection via strategy |
| Some replicas unhealthy | N/A | Select from remaining healthy replicas |
| All replicas unhealthy | `true` | Route to primary + log warning |
| All replicas unhealthy | `false` | Return error via `Either.Left` |
| Shard has no replicas configured | `true` | Route to primary (no warning) |
| Shard has no replicas configured | `false` | Return error |

---

## Combined with Other Patterns

### With Compound Shard Keys

Compound shard keys work seamlessly with per-shard replicas:

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseCompoundRouting(builder =>
    {
        builder
            .HashComponent("tenant")
            .GeoComponent("region");
    })
    .AddShard("shard-us-t1", "Server=us-t1-primary;...");
});

// Replicas configured independently per resulting shard
options.AddShard("shard-us-t1", "Server=us-t1-primary;...",
    ["Server=us-t1-r1;...", "Server=us-t1-r2;..."]);
```

### With Entity Co-Location

Co-located entities share the same shard and therefore the same replica topology:

```csharp
// Order and OrderItem are co-located on the same shard
// Both benefit from the same replica configuration
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "Server=primary1;...");

    options.UseColocation(builder =>
    {
        builder.WithRootEntity<Order>()
            .AddColocatedEntity<OrderItem>();
    });
});
```

### With Scatter-Gather

Cross-shard queries use read replicas when the intent is `Read`:

```csharp
// Scatter-gather reads from replicas across all shards
var allContexts = await factory.GetAllReadConnectionsAsync(ct);
allContexts.IfRight(async connections =>
{
    var tasks = connections.Select(async c =>
        await c.Connection.QueryAsync<Order>("SELECT * FROM Orders"));
    var results = await Task.WhenAll(tasks);
});
```

---

## FAQ

### General

**Q: Does sharded read/write separation require code changes beyond configuration?**

A: No. If you already use `IShardedReadWriteConnectionFactory` or `IShardedReadWriteDbContextFactory`, adding replicas is purely a configuration change. The factory automatically routes based on the `DatabaseRoutingContext` intent.

**Q: What happens if I configure replicas for some shards but not others?**

A: Shards without replicas always use the primary for both reads and writes. No error is raised.

**Q: Can I change the replica strategy at runtime?**

A: The strategy is set at configuration time per shard. To change strategies, update the configuration and restart the application. The `ShardReplicaSelectorFactory` creates new selector instances based on the configured strategy.

### Performance

**Q: What's the overhead of replica selection?**

A: All strategies complete in under 200ns. The fastest (RoundRobin) is under 50ns. This is negligible compared to network round-trip times.

**Q: Should I use LeastLatency for all shards?**

A: Not necessarily. LeastLatency requires reporting latency measurements after each operation. If all replicas are in the same data center with similar latency, RoundRobin is simpler and equally effective.

### Health & Failover

**Q: How quickly does the system detect an unhealthy replica?**

A: Detection happens immediately when `MarkUnhealthy()` is called (typically on connection failure). Recovery is delayed by `UnhealthyReplicaRecoveryDelay` (default: 30s) to prevent flapping.

**Q: Can I disable fallback to primary?**

A: Yes. Set `FallbackToPrimaryWhenNoReplicas = false`. This causes read operations to return an `EncinaError` when no healthy replicas are available, allowing the caller to decide how to handle the situation.

---

## Related Documentation

- [Sharding Configuration](configuration.md) — Complete sharding setup
- [Read/Write Database Separation](../features/read-write-separation.md) — Non-sharded read/write separation
- [Sharding Scaling Guidance](scaling-guidance.md) — Capacity planning and shard key selection
- [Cross-Shard Operations](cross-shard-operations.md) — Scatter-gather queries
- [Health Checks Integration](../guides/health-checks.md) — Health monitoring setup

---

## Related Issues

- [#644 — Read/Write Separation + Sharding](https://github.com/dlrivada/Encina/issues/644)
- [#289 — Database Sharding](https://github.com/dlrivada/Encina/issues/289)
- [#283 — Read/Write Database Separation](https://github.com/dlrivada/Encina/issues/283)
