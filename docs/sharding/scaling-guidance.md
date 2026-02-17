# Sharding Scaling Guidance

Strategic guidance for planning, implementing, and scaling database sharding in Encina applications.

## Table of Contents

1. [When to Shard](#when-to-shard)
2. [Shard Key Selection](#shard-key-selection)
3. [Routing Strategy Decision Matrix](#routing-strategy-decision-matrix)
4. [Capacity Planning](#capacity-planning)
5. [Rebalancing](#rebalancing)
6. [Monitoring and Alerting](#monitoring-and-alerting)
7. [Performance Optimization](#performance-optimization)
8. [Common Pitfalls](#common-pitfalls)

---

## When to Shard

### Decision Criteria

Sharding adds operational complexity. Consider simpler alternatives first:

| Scaling Strategy | When to Use | Complexity |
|-----------------|-------------|------------|
| **Vertical scaling** | Data < 1TB, writes < 10K/s | Low |
| **Read replicas** | Read-heavy workloads (>80% reads) | Medium |
| **Table partitioning** | Single large table, queries filter on partition key | Medium |
| **Database sharding** | Write-heavy, multi-region, or data residency requirements | High |

### Shard When You Have

- **Data volume exceeding single-node capacity** — Storage limits or query performance degradation
- **Write throughput beyond single-primary capacity** — Write-heavy workloads saturating I/O
- **Geographic distribution requirements** — Data residency laws (GDPR, CCPA) require regional storage
- **Tenant isolation needs** — Large tenants require dedicated resources for SLA guarantees
- **Cost optimization** — Many small databases can be cheaper than one large database

### Do NOT Shard When

- Read replicas would solve the scaling problem
- Query patterns frequently span multiple shard key boundaries
- The application has fewer than 10 million rows in the largest table
- Operational team cannot manage multi-database deployments

---

## Shard Key Selection

The shard key is the most critical decision in a sharding strategy. A poor shard key causes hotspots, uneven distribution, and limits query flexibility.

### Good Shard Key Properties

| Property | Why It Matters |
|----------|---------------|
| **High cardinality** | Many unique values ensure even distribution across shards |
| **Evenly distributed** | Prevents hotspots where one shard handles disproportionate traffic |
| **Frequently used in queries** | Most queries include the shard key, avoiding scatter-gather |
| **Immutable** | Changing shard keys requires cross-shard data migration |
| **Meaningful** | Maps naturally to how data is accessed (by tenant, region, etc.) |

### Shard Key Anti-Patterns

| Anti-Pattern | Problem | Better Alternative |
|-------------|---------|-------------------|
| **Low cardinality** (e.g., status enum with 3 values) | Only 3 possible shards, uneven if distribution is skewed | Combine with another field: `"{status}-{customerId}"` |
| **Monotonically increasing** (e.g., auto-increment ID) | All new writes go to the latest shard (hotspot) | Use a hash-distributed key like customer ID |
| **Timestamp** as sole shard key | Recent shard receives all writes | Combine: `"{region}-{yearMonth}"` |
| **High-entropy random** (e.g., UUID) | Distributed evenly but no query locality | Use a meaningful key with natural grouping |

### Shard Key Examples by Scenario

#### Multi-Tenant SaaS

```csharp
// Shard key: TenantId — natural grouping, all tenant data co-located
public class TenantOrder : IShardable
{
    public string TenantId { get; set; }
    public string OrderId { get; set; }
    public string GetShardKey() => TenantId;
}
```

- All data for one tenant lives on one shard
- No cross-shard queries for tenant-scoped operations
- Risk: large tenants can cause hotspots (use directory routing for VIPs)

#### E-Commerce

```csharp
// Shard key: CustomerId — orders, payments, shipping co-located per customer
public class Order : IShardable
{
    public string CustomerId { get; set; }
    public decimal Total { get; set; }
    public string GetShardKey() => CustomerId;
}
```

- Customer-facing queries (order history, account details) are single-shard
- Analytics and reporting require scatter-gather
- Good distribution if customer base is large enough

#### Time-Series / IoT

```csharp
// Shard key: compound key combining device region and time bucket
public class SensorReading : IShardable
{
    public string DeviceId { get; set; }
    public string Region { get; set; }
    public DateTime Timestamp { get; set; }
    public string GetShardKey() => $"{Region}-{Timestamp:yyyy-MM}";
}
```

- Range routing: each month or quarter gets its own shard
- Old shards become read-only archives
- New data always writes to the current shard

#### Geographic Distribution

```csharp
// Shard key: Region — data stays within geographic boundary
public class UserProfile : IShardable
{
    public string UserId { get; set; }
    public string Region { get; set; }
    public string GetShardKey() => Region;
}
```

- Use geo routing for automatic region-to-shard mapping
- Data residency compliance: EU data stays on EU shards
- Fallback chains ensure availability during regional outages

---

## Routing Strategy Decision Matrix

| Criteria | Hash | Range | Directory | Geo |
|----------|:----:|:-----:|:---------:|:---:|
| **Even distribution** | Excellent | Depends on key | Manual | Depends on users |
| **Query locality** | None | Contiguous ranges | Full control | By region |
| **Rebalancing complexity** | Low (~1/N movement) | Medium (split/merge) | Low (reassign keys) | Low (reassign regions) |
| **Add/remove shards** | Automatic redistribution | Manual range adjustment | Manual mapping update | Manual region reassignment |
| **Routing performance** | O(log V) where V=virtual nodes | O(log R) where R=ranges | O(1) lookup | O(R) fallback chain |
| **Best for** | General-purpose | Time-series, alphabetical | VIP tenants, compliance | Multi-region, data residency |

### When to Choose Each Strategy

```text
                     Do you need data residency compliance?
                                    │
                           ┌────────┴────────┐
                          Yes               No
                           │                 │
                       Use Geo         Do specific entities need
                       Routing         dedicated shard placement?
                                            │
                                   ┌────────┴────────┐
                                  Yes               No
                                   │                 │
                              Use Directory    Are queries range-based?
                              Routing          (time-series, alphabetical)
                                                    │
                                           ┌────────┴────────┐
                                          Yes               No
                                           │                 │
                                      Use Range         Use Hash
                                      Routing           Routing
```

---

## Capacity Planning

### Initial Shard Count

| Data Size | Write Rate | Recommended Shards | Notes |
|-----------|-----------|-------------------|-------|
| < 100 GB | < 1K writes/s | 3 | Minimum for redundancy |
| 100 GB - 1 TB | 1K - 10K writes/s | 3-5 | Most applications start here |
| 1 TB - 10 TB | 10K - 50K writes/s | 5-10 | Growing applications |
| > 10 TB | > 50K writes/s | 10+ | Scale based on throughput needs |

> **Rule of thumb**: Start with 3 shards. Hash routing with 150 virtual nodes handles up to ~20 shards efficiently. For more, increase `VirtualNodesPerShard`.

### Growth Projection

Plan for 2-3 years of growth before your next topology change:

```text
Current data:        500 GB across 3 shards (~167 GB each)
Growth rate:         20% per year
2-year projection:   720 GB across 3 shards (~240 GB each)
3-year projection:   864 GB across 3 shards (~288 GB each)

Action:
- If max comfortable shard size is 250 GB → add 4th shard in year 2
- With hash routing: ~25% of data migrates (1/4)
```

### Connection Pool Sizing

Each shard needs its own connection pool. Total connections = shards x pool size per shard:

| Shards | Pool Size/Shard | Total Connections | Notes |
|--------|----------------|------------------|-------|
| 3 | 20 | 60 | Small application |
| 5 | 50 | 250 | Medium application |
| 10 | 100 | 1,000 | Large application; monitor total |
| 20 | 50 | 1,000 | Reduce per-shard pool to limit total |

> **Warning**: Database servers have connection limits. Ensure the sum of all application instances' pools does not exceed the database's `max_connections`.

---

## Rebalancing

### When to Add Shards

- Individual shard storage exceeds 80% of capacity
- Query latency on a shard consistently exceeds SLA targets
- Connection pool utilization exceeds 75%
- Write throughput approaches single-shard limits

### Using IShardRebalancer

The `HashShardRouter` implements `IShardRebalancer` to calculate which key ranges move between shards:

```csharp
var oldTopology = new ShardTopology([
    new ShardInfo("shard-1", "conn1"),
    new ShardInfo("shard-2", "conn2")
]);

var newTopology = new ShardTopology([
    new ShardInfo("shard-1", "conn1"),
    new ShardInfo("shard-2", "conn2"),
    new ShardInfo("shard-3", "conn3") // new shard
]);

IShardRebalancer rebalancer = hashRouter; // HashShardRouter implements IShardRebalancer
var affected = rebalancer.CalculateAffectedKeyRanges(oldTopology, newTopology);

foreach (var range in affected)
{
    Console.WriteLine(
        $"Ring [{range.RingStart}..{range.RingEnd}): " +
        $"{range.PreviousShardId} → {range.NewShardId}");
}
```

### Migration Approaches

| Approach | Downtime | Complexity | When to Use |
|----------|----------|------------|-------------|
| **Stop-the-world** | Yes (minutes to hours) | Low | Small datasets, maintenance windows available |
| **Dual-write** | No | High | Large datasets, zero-downtime requirement |
| **Background copy + cutover** | Brief (seconds) | Medium | Most common; copy data, then switch routing |

> **Note**: Encina provides the `IShardRebalancer` for migration planning. Actual data movement is the application's responsibility. Full migration tooling is planned for a future release (see Issue [#637](https://github.com/dlrivada/Encina/issues/637)).

---

## Monitoring and Alerting

### Key Metrics to Monitor

| Metric | Alert Threshold | Action |
|--------|----------------|--------|
| `encina.sharding.route.duration_ns` P99 | > 10,000 ns (10 us) | Investigate router performance; check topology size |
| `encina.sharding.scatter.duration_ms` P99 | > 5,000 ms | Check slowest shard; reduce parallelism or shard count in query |
| `encina.sharding.scatter.partial_failures` rate | > 0.01/s | Investigate failing shards; check health |
| `encina.sharding.scatter.queries.active` | > 50 concurrent | Throttle scatter-gather; increase MaxParallelism limit |
| `encina.sharding.topology.shards.active` | < expected count | Shard went offline; investigate connectivity |
| Shard storage utilization | > 80% | Plan rebalancing; add shards |
| Connection pool utilization | > 75% | Increase pool size or add shards |

### Health Check Integration

```csharp
// Periodic health summary
var summary = new ShardedHealthSummary(
    ShardedHealthSummary.CalculateOverallStatus(results),
    results);

if (!summary.AllHealthy)
{
    logger.LogWarning(
        "Shard health: {Healthy}/{Total} healthy, {Degraded} degraded, {Unhealthy} unhealthy",
        summary.HealthyCount, summary.TotalShards,
        summary.DegradedCount, summary.UnhealthyCount);
}
```

---

## Performance Optimization

### Routing Latency

Hash and range routers achieve sub-microsecond routing through:

- **Pre-computed ring** (Hash): Binary search on `ulong[]` array
- **Sorted ranges** (Range): Binary search on sorted `ShardRange[]`
- **Direct lookup** (Directory): O(1) dictionary lookup

To maintain low latency:

- Keep virtual node count at 100-200 (default 150)
- Avoid excessive shard counts (>50 shards increases ring size)
- Use `IShardable` interface over `[ShardKey]` attribute (avoids reflection)

### Scatter-Gather Tuning

```csharp
options.ScatterGatherOptions.MaxParallelism = 4;  // Limit concurrent queries
options.ScatterGatherOptions.Timeout = TimeSpan.FromSeconds(10);
options.ScatterGatherOptions.AllowPartialResults = true;
```

- **MaxParallelism**: Set to shard count or lower if connection pools are limited
- **Timeout**: Set based on your SLA; 95th percentile of single-shard query time x 2
- **AllowPartialResults**: Keep `true` for user-facing queries; set `false` for batch operations requiring completeness

### Cache Hit Rates

For directory routing, cache hit rates directly affect latency:

- Target > 95% cache hit rate for directory lookups
- Use `CachedShardDirectoryStore` (from `Encina.Caching`) to add write-through caching
- Monitor cache evictions; increase cache size if eviction rate is high

---

## Common Pitfalls

### 1. Hotspot Shards

**Problem**: One shard handles disproportionate traffic.

**Causes**:

- Low-cardinality shard key (e.g., status with 3 values)
- Popular entity (VIP tenant with 90% of data)
- Monotonically increasing key (new data always goes to latest shard)

**Solutions**:

- Use hash routing for uniform distribution
- Move large tenants to dedicated shards using directory routing
- Combine fields for higher cardinality: `"{tenantId}-{region}"`

### 2. Cross-Shard JOINs

**Problem**: Queries need to join data across multiple shards.

**Solutions**:

- **Denormalization**: Store frequently joined data on the same shard
- **Shard key co-location**: Entities that are always queried together share the same shard key
- **Application-side JOIN**: Scatter-gather both datasets and join in memory
- **Reference tables**: Replicate small, rarely-changing tables to all shards

### 3. Schema Migration Coordination

**Problem**: ALTER TABLE must run on all shards, but they're independent databases.

**Solutions**:

- Use a migration orchestrator that applies migrations to each shard sequentially
- Tag migrations as "shard-aware" in your migration tool
- Test migrations on one shard before rolling to all
- Keep backward-compatible migrations (add columns, don't rename)

### 4. Shard Key Immutability

**Problem**: An entity's shard key changes (e.g., customer moves regions).

**Solutions**:

- Treat shard key as immutable in your domain model
- If migration is needed: delete from old shard, insert to new shard (within a Saga)
- Consider using a stable identifier (account ID) instead of a mutable attribute (region)

### 5. Connection Exhaustion

**Problem**: Total connections across all shards exceed database limits.

**Solutions**:

- Reduce per-shard pool size
- Use connection pooling proxies (PgBouncer, ProxySQL)
- Limit scatter-gather parallelism via `MaxParallelism`
- Consider fewer, larger shards instead of many small ones

### 6. Uneven Shard Growth

**Problem**: Shards grow at different rates over time.

**Solutions**:

- Monitor storage per shard
- Use `IShardRebalancer` to plan data migration when imbalance exceeds 20%
- For range routing: split the largest range into two shards
- For hash routing: add a shard (consistent hashing minimizes data movement)
