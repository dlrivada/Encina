# Reference Tables — Scaling Guide

This guide provides recommendations for sizing, capacity planning, and monitoring reference table replication in production sharded deployments.

## Table of Contents

1. [Table Size Recommendations](#table-size-recommendations)
2. [Refresh Frequency Guidelines](#refresh-frequency-guidelines)
3. [Capacity Planning](#capacity-planning)
4. [Monitoring and Alerting](#monitoring-and-alerting)
5. [When NOT to Use Reference Tables](#when-not-to-use-reference-tables)
6. [Performance Optimization](#performance-optimization)

---

## Table Size Recommendations

Reference tables should be **small** relative to your sharded data. The replication mechanism reads and writes the **entire table** on each sync cycle, so table size directly impacts replication duration and network usage.

### Size Tiers

| Tier | Row Count | Row Size | Total Size | Recommended Strategy |
|------|-----------|----------|------------|---------------------|
| **Ideal** | < 1,000 | < 1 KB | < 1 MB | Any strategy |
| **Good** | 1K - 10K | < 1 KB | 1 - 10 MB | Polling (5-15 min) |
| **Acceptable** | 10K - 100K | < 500 B | 10 - 50 MB | Polling (15-60 min) or Manual |
| **Caution** | > 100K | Any | > 50 MB | Consider alternatives |

### Why These Limits?

Each replication cycle for a reference table:

1. **Reads** all rows from the primary shard
2. **Serializes** to JSON for hash computation (XxHash64)
3. **Transfers** data to each target shard over the network
4. **Upserts** all rows via provider-specific bulk operations

For a 10K-row table with 500-byte rows (~5 MB):

- **Primary read**: ~50ms (depends on database)
- **Hash computation**: ~5ms (XxHash64 is very fast)
- **Network transfer per shard**: ~10-50ms (depends on network)
- **Upsert per shard**: ~100-500ms (depends on provider and batch size)
- **Total per cycle (3 shards)**: ~350-1600ms

For a 100K-row table (~50 MB):

- **Total per cycle (3 shards)**: ~3-15 seconds
- This is acceptable for infrequent polling but would strain CDC-driven replication.

---

## Refresh Frequency Guidelines

### Data Volatility Matrix

| Data Volatility | Examples | Recommended Strategy | Interval |
|----------------|----------|---------------------|----------|
| **Static** | Countries, ISO codes, enums | `Manual` | On deployment |
| **Rare changes** | Categories, regions, tax rates | `Polling` | 15-60 min |
| **Daily changes** | Exchange rates, pricing tiers | `Polling` | 1-5 min |
| **Hourly changes** | Feature flags, promotions | `CdcDriven` | Real-time |
| **Frequent changes** | Session data, caches | Not a reference table | — |

### Polling Interval Recommendations

| Table Size | Change Frequency | Suggested Interval |
|-----------|-----------------|-------------------|
| < 1K rows | Rarely | 30-60 min |
| < 1K rows | Daily | 5-10 min |
| 1K-10K rows | Rarely | 15-30 min |
| 1K-10K rows | Daily | 5-15 min |
| 10K-100K rows | Rarely | 30-60 min |
| 10K-100K rows | Daily | 15-30 min |

**Key insight**: When data hasn't changed, the polling cycle is cheap — it only reads, hashes, and compares. The expensive upsert step is skipped.

---

## Capacity Planning

### Storage Impact

Each reference table is fully replicated to every shard:

```
Total storage = table_size x number_of_shards
```

| Table Size | 3 Shards | 10 Shards | 50 Shards |
|-----------|----------|-----------|-----------|
| 1 MB | 3 MB | 10 MB | 50 MB |
| 10 MB | 30 MB | 100 MB | 500 MB |
| 50 MB | 150 MB | 500 MB | 2.5 GB |

**Rule of thumb**: Keep total reference table storage < 1% of each shard's total data.

### Network Bandwidth During Sync

During a replication cycle, data flows from the primary shard to all target shards:

```
Bandwidth per cycle = table_size x (number_of_shards - 1)
```

| Table Size | 3 Shards (2 targets) | 10 Shards (9 targets) |
|-----------|---------------------|----------------------|
| 1 MB | 2 MB | 9 MB |
| 10 MB | 20 MB | 90 MB |
| 50 MB | 100 MB | 450 MB |

With `MaxParallelShards = 4`, bandwidth is spread across parallel streams but concentrated in time.

### Connection Usage

During replication, the engine opens one connection per target shard (up to `MaxParallelShards` concurrent connections):

```
Peak connections = min(MaxParallelShards, target_shard_count) x reference_table_count
```

If replicating 3 reference tables across 10 shards with `MaxParallelShards = 4`:

- Peak connections: 4 x 3 = 12 concurrent connections (overlapping unlikely, typically < 8)

### CPU Impact

- **Hash computation**: Negligible — XxHash64 processes ~10 GB/s on modern CPUs
- **JSON serialization**: Moderate for large tables — ~100 MB/s throughput
- **Upsert preparation**: Low — parameter binding is lightweight

---

## Monitoring and Alerting

### Key Metrics to Watch

| Metric | Alert Threshold | Meaning |
|--------|----------------|---------|
| `encina.reference_table.replication_duration_ms` | p99 > 5s | Replication is slow |
| `encina.reference_table.errors_total` | > 0 per 5 min | Shards are failing |
| `encina.reference_table.active_replications` | > `MaxParallelShards` | Backlog building |
| Health check status | Degraded/Unhealthy | Lag exceeds thresholds |

### Recommended Alert Rules

```yaml
# Alert: Replication lag exceeds degraded threshold
- alert: ReferenceTableLagDegraded
  expr: encina_reference_table_replication_duration_ms{quantile="0.99"} > 60000
  for: 5m
  labels:
    severity: warning

# Alert: Replication errors
- alert: ReferenceTableErrors
  expr: rate(encina_reference_table_errors_total[5m]) > 0
  for: 2m
  labels:
    severity: critical

# Alert: Health check unhealthy
- alert: ReferenceTableUnhealthy
  expr: encina_health_reference_tables == 0
  for: 3m
  labels:
    severity: critical
```

### Dashboard Panels

Recommended Grafana panels for reference table monitoring:

1. **Replication Duration** (histogram): `encina.reference_table.replication_duration_ms` by entity type
2. **Rows Synced** (counter rate): `encina.reference_table.rows_synced_total` by entity type
3. **Error Rate** (counter rate): `encina.reference_table.errors_total` by entity type and error code
4. **Active Replications** (gauge): `encina.reference_table.active_replications`
5. **Health Check Status**: ASP.NET Core health check endpoint

---

## When NOT to Use Reference Tables

### Use Application Caching Instead

If your "reference" data:

- Is read by application code, not SQL JOINs
- Changes frequently (> once per minute)
- Doesn't participate in database JOINs
- Is larger than 100K rows

Consider using Encina's caching infrastructure instead:

```csharp
// Application-level caching for reference data
services.AddEncinaCaching(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
});
```

### Use Denormalization Instead

If your reference data is:

- Needed by a specific entity (e.g., order -> country name)
- Static once assigned (country doesn't change after order creation)
- Critical for read performance

Consider denormalizing the reference data into the sharded entity:

```csharp
public class Order
{
    public int CountryId { get; set; }
    public string CountryName { get; set; } // Denormalized
    public string CountryCode { get; set; } // Denormalized
}
```

### Use Scatter-Gather Instead

If the reference data:

- Is large (> 100K rows)
- Changes frequently
- Needs strong consistency
- Is only needed for occasional cross-shard reports

Use scatter-gather queries to read from the authoritative shard:

```csharp
var countries = await repository.QueryAllShardsAsync(
    new CountryByRegionSpec("EU"), cancellationToken);
```

### Decision Tree

```text
Is the data < 100K rows?
+-- YES: Does it participate in SQL JOINs?
|   +-- YES: Is eventual consistency OK?
|   |   +-- YES: Use Reference Table
|   |   +-- NO:  Read from primary shard
|   +-- NO:  Application caching
+-- NO:  Is it shardable?
    +-- YES: Shard the table
    +-- NO:  Application caching + denormalization
```

---

## Performance Optimization

### Batch Size Tuning

The `BatchSize` option controls how many rows are sent per upsert batch:

| Batch Size | Memory | Latency | Best For |
|-----------|--------|---------|----------|
| 100 | Low | Higher (more round-trips) | Very large tables |
| 500 | Medium | Moderate | Most use cases |
| 1000 (default) | Medium | Good | Tables < 10K rows |
| 5000 | Higher | Lower (fewer round-trips) | Tables 10K-100K rows |

### Parallelism Tuning

Adjust `MaxParallelShards` based on your infrastructure:

| Deployment | Recommended | Rationale |
|-----------|-------------|-----------|
| Shared database server | 2 | Limit connection pressure |
| Dedicated database per shard | `ProcessorCount` | Maximize throughput |
| Cloud managed databases | 4-8 | Balance cost and speed |
| > 20 shards | 8-16 | Avoid thundering herd |

### Startup Sync Optimization

For applications with many reference tables, stagger startup sync:

```csharp
// Critical tables: sync on startup
options.AddReferenceTable<Country>(rt => rt.SyncOnStartup = true);
options.AddReferenceTable<Currency>(rt => rt.SyncOnStartup = true);

// Non-critical tables: skip startup sync (will sync on first poll)
options.AddReferenceTable<Category>(rt => rt.SyncOnStartup = false);
options.AddReferenceTable<Tag>(rt => rt.SyncOnStartup = false);
```

---

## Related Documentation

- [Reference Tables Feature Guide](../features/reference-tables.md) — Overview and architecture
- [Reference Tables Configuration](../configuration/reference-tables.md) — Detailed configuration
- [ADR-013: Reference Tables](../architecture/adr/013-reference-tables.md) — Design decisions
- [Sharding Scaling Guidance](../sharding/scaling-guidance.md) — General sharding capacity planning
