# Reference Tables (Broadcast Tables) in Encina

This guide explains how to use reference tables — small, read-heavy lookup tables that are automatically replicated from a primary shard to all other shards, enabling efficient local JOINs without cross-shard traffic.

## Table of Contents

1. [Overview](#overview)
2. [When to Use Reference Tables](#when-to-use-reference-tables)
3. [Quick Start](#quick-start)
4. [Configuration](#configuration)
5. [Refresh Strategies](#refresh-strategies)
6. [Replication Flow](#replication-flow)
7. [Provider Support](#provider-support)
8. [Health Monitoring](#health-monitoring)
9. [Observability](#observability)
10. [Error Handling](#error-handling)
11. [Troubleshooting](#troubleshooting)
12. [Related Documentation](#related-documentation)

---

## Overview

In a sharded database architecture, entities are distributed across shards using a routing strategy. However, some tables — such as countries, currencies, categories, or configuration data — are needed by queries on **every** shard. Without reference tables, querying these lookup tables requires cross-shard scatter-gather operations, adding latency and complexity.

Reference tables solve this by **replicating** the authoritative data from a designated primary shard to all other shards in the topology:

```text
                    Primary Shard (shard-0)

  +-----------------+    +------------------+
  |  Orders (100K)  |    |  Countries (250) |<-- Source
  +-----------------+    +--------+---------+
                                  |
                                  | Replication
                    +-------------+-------------+
                    |             |             |
                    v             v             v
              +-----------+ +-----------+ +-----------+
              |  shard-1  | |  shard-2  | |  shard-3  |
              | Orders    | | Orders    | | Orders    |
              | Countries | | Countries | | Countries |
              |   (250)   | |   (250)   | |   (250)   |
              +-----------+ +-----------+ +-----------+
```

After replication, every shard has a local copy of the `Countries` table, enabling local JOINs:

```sql
-- Executes entirely on shard-1, no cross-shard traffic
SELECT o.Id, o.Total, c.Name AS CountryName
FROM Orders o
JOIN Countries c ON o.CountryId = c.Id
WHERE o.CustomerId = @CustomerId
```

---

## When to Use Reference Tables

| Scenario | Reference Table? | Alternative |
|----------|:----------------:|-------------|
| Country/region lookup (250 rows) | Yes | — |
| Currency codes (180 rows) | Yes | — |
| Product categories (500 rows) | Yes | — |
| Configuration/settings (50 rows) | Yes | — |
| User profiles (1M+ rows) | No | Shard the table |
| Product catalog (100K+ rows) | No | Application caching |
| Frequently updated pricing | Caution | Consider CDC strategy |
| Static enums/constants | Yes | — |

**Use reference tables when**:

- The table is small (typically < 10K rows, max recommended ~100K)
- The data is read-heavy and rarely changes
- The table participates in JOINs with sharded entities on multiple shards
- Eventual consistency (seconds to minutes of lag) is acceptable

**Do NOT use reference tables when**:

- The table has millions of rows (shard it instead)
- The data changes very frequently (use application caching with pub/sub invalidation)
- Strong consistency is required (use scatter-gather or read from primary)

---

## Quick Start

### 1. Mark Your Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Encina.Sharding.ReferenceTables;

[ReferenceTable]
[Table("Countries")]
public class Country
{
    [Key]
    public int Id { get; set; }

    [Column("Code")]
    public string Code { get; set; } = string.Empty;

    [Column("Name")]
    public string Name { get; set; } = string.Empty;
}
```

### 2. Register in Sharding Configuration

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", "Server=shard0;Database=App;...")
        .AddShard("shard-1", "Server=shard1;Database=App;...")
        .AddShard("shard-2", "Server=shard2;Database=App;...")
        .AddReferenceTable<Country>(rt =>
        {
            rt.RefreshStrategy = RefreshStrategy.Polling;
            rt.PrimaryShardId = "shard-0";
            rt.PollingInterval = TimeSpan.FromMinutes(5);
        });
});
```

### 3. Register a Provider Store

```csharp
// ADO.NET
services.AddEncinaADOReferenceTableStoreSqlServer();

// Dapper
services.AddEncinaDapperReferenceTableStoreSqlServer();

// EF Core
services.AddEncinaEFCoreReferenceTableStoreSqlServer<AppDbContext>();

// MongoDB
services.AddEncinaMongoDBReferenceTableStore();
```

### 4. (Optional) Add Health Check

```csharp
services.AddHealthChecks()
    .AddCheck<ReferenceTableHealthCheck>("reference-tables");
```

The background replication service starts automatically and keeps all shards in sync.

---

## Configuration

### Per-Table Options (`ReferenceTableOptions`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RefreshStrategy` | `RefreshStrategy` | `Polling` | How changes are detected and propagated |
| `PrimaryShardId` | `string?` | `null` (first shard) | The shard holding the authoritative copy |
| `PollingInterval` | `TimeSpan` | 5 minutes | Interval between polling cycles |
| `BatchSize` | `int` | 1000 | Max rows per upsert batch |
| `SyncOnStartup` | `bool` | `true` | Whether to sync on application start |

### Global Options (`ReferenceTableGlobalOptions`)

```csharp
services.Configure<ReferenceTableGlobalOptions>(opts =>
{
    opts.MaxParallelShards = 4;
    opts.DefaultRefreshStrategy = RefreshStrategy.Polling;
    opts.HealthCheckUnhealthyThreshold = TimeSpan.FromMinutes(10);
    opts.HealthCheckDegradedThreshold = TimeSpan.FromMinutes(2);
});
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxParallelShards` | `int` | `Environment.ProcessorCount` | Max concurrent shard replications |
| `DefaultRefreshStrategy` | `RefreshStrategy` | `Polling` | Default strategy for tables without explicit config |
| `HealthCheckUnhealthyThreshold` | `TimeSpan` | 5 min | Lag threshold for unhealthy status |
| `HealthCheckDegradedThreshold` | `TimeSpan` | 1 min | Lag threshold for degraded status |

For detailed configuration guidance, see [Configuration Guide](../configuration/reference-tables.md).

---

## Refresh Strategies

### CdcDriven (Lowest Latency)

Changes are detected via Change Data Capture and propagated in near-real-time.

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.CdcDriven;
    rt.PrimaryShardId = "shard-0";
});
```

**Requires**: A configured `ICdcConnector` for the primary shard (see [CDC documentation](cdc.md)).

**Best for**: Data that changes occasionally but needs fast propagation (e.g., pricing tiers, feature flags).

### Polling (Default — Balanced)

Changes are detected periodically by comparing XxHash64 content hashes between the primary and replica shards.

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Polling;
    rt.PollingInterval = TimeSpan.FromMinutes(10);
});
```

**How it works**:

1. Read all data from the primary shard
2. Compute an XxHash64 content hash (sorted by PK for determinism)
3. Compare with stored hash from the last replication
4. If changed, replicate to all target shards
5. Update stored hash and timestamp

**Best for**: Static or rarely-changing data (countries, currencies, categories).

### Manual (Full Control)

No automatic change detection. Replication is triggered explicitly.

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Manual;
});

// Trigger manually (e.g., after a deployment or admin action)
var result = await replicator.ReplicateAsync<Country>(cancellationToken);
```

**Best for**: Configuration data that changes only during deployments or admin actions.

### Strategy Decision Matrix

| Factor | CdcDriven | Polling | Manual |
|--------|:---------:|:-------:|:------:|
| Latency | ~seconds | configurable (min-hours) | on-demand |
| Infrastructure | CDC connector required | None (built-in) | None |
| Resource usage | Low (event-driven) | Moderate (periodic reads) | Minimal |
| Complexity | Higher | Low | Lowest |
| Change frequency | Medium | Low | Very low |

---

## Replication Flow

### Polling-Based Replication

```text
  Replication Service (Background)

  1. Timer fires (PollingInterval)
  2. For each registered reference table:
     a. Read all data from primary shard
     b. Compute XxHash64 content hash
     c. Compare with stored hash
     d. If changed: replicate to target shards (parallel)
     e. Update stored hash and last-replication timestamp

         Read all             Upsert (batched)
Primary ----------> Engine ----------------------> Target Shards
shard-0             |                              +-- shard-1 (ok)
                    |                              +-- shard-2 (ok)
                    |                              +-- shard-3 (fail, retry next cycle)
                    |
                    +-- Store hash + timestamp
```

### CDC-Driven Replication

```text
  CDC Connector (Primary Shard)

  1. Change detected via CDC stream
  2. CdcDriven handler notifies replicator
  3. Replicator reads full table from primary
  4. Upserts to all target shards in parallel
  5. Updates stored hash and timestamp
```

---

## Provider Support

All 13 database providers are supported with provider-specific upsert SQL:

| Provider | Upsert Mechanism | Package |
|----------|------------------|---------|
| SQLite | `INSERT OR REPLACE INTO ...` | `Encina.ADO.Sqlite` / `Encina.Dapper.Sqlite` |
| SQL Server | `MERGE ... WHEN MATCHED THEN UPDATE` | `Encina.ADO.SqlServer` / `Encina.Dapper.SqlServer` |
| PostgreSQL | `INSERT ... ON CONFLICT DO UPDATE` | `Encina.ADO.PostgreSQL` / `Encina.Dapper.PostgreSQL` |
| MySQL | `INSERT ... ON DUPLICATE KEY UPDATE` | `Encina.ADO.MySQL` / `Encina.Dapper.MySQL` |
| EF Core (all 4) | Generic `DbContext`-based upsert | `Encina.EntityFrameworkCore` |
| MongoDB | `BulkWriteAsync` + `ReplaceOneModel` | `Encina.MongoDB` |

### Content Hash Consistency

All providers use the shared `ReferenceTableHashComputer` for content hashing, ensuring cross-provider hash consistency. This means you can:

- Read data via ADO.NET and verify with Dapper (same hash)
- Mix providers across shards (e.g., primary uses EF Core, replicas use Dapper)

---

## Health Monitoring

### Health Check Registration

```csharp
services.AddHealthChecks()
    .AddCheck<ReferenceTableHealthCheck>(
        "reference-tables",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "sharding"]);
```

### Health States

| State | Condition | Action |
|-------|-----------|--------|
| **Healthy** | All tables replicated within degraded threshold | Normal operation |
| **Degraded** | Some tables between degraded and unhealthy thresholds | Monitor, investigate |
| **Unhealthy** | Some tables exceed unhealthy threshold (or never replicated) | Alert, investigate |

---

## Observability

### OpenTelemetry Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `encina.reference_table.replications_total` | Counter | Total replication operations |
| `encina.reference_table.replication_duration_ms` | Histogram | Duration per replication cycle |
| `encina.reference_table.rows_synced_total` | Counter | Total rows synced across all shards |
| `encina.reference_table.errors_total` | Counter | Total replication errors |
| `encina.reference_table.active_replications` | UpDownCounter | Currently active replication operations |

### Activity Tags

| Tag | Example | Description |
|-----|---------|-------------|
| `encina.reference_table.entity_type` | `Country` | Entity type being replicated |
| `encina.reference_table.primary_shard` | `shard-0` | Primary shard ID |
| `encina.reference_table.target_shards` | `3` | Number of target shards |
| `encina.reference_table.rows_synced` | `250` | Rows replicated |
| `encina.reference_table.refresh_strategy` | `Polling` | Strategy used |
| `encina.reference_table.is_partial` | `false` | Whether result was partial |

---

## Error Handling

All operations return `Either<EncinaError, T>` following Encina's Railway Oriented Programming pattern:

```csharp
var result = await replicator.ReplicateAsync<Country>(ct);

result.Match(
    Right: rep =>
    {
        logger.LogInformation("Synced {Rows} rows in {Duration}ms",
            rep.RowsSynced, rep.Duration.TotalMilliseconds);

        if (rep.IsPartial)
        {
            logger.LogWarning("Partial failure: {Failed}/{Total} shards failed",
                rep.FailedShards.Count, rep.TotalShardsTargeted);
        }
    },
    Left: error =>
    {
        logger.LogError("Replication failed: {Code} - {Message}",
            error.GetCode().IfNone("unknown"), error.Message);
    });
```

### Error Codes

| Code | Description |
|------|-------------|
| `encina.reference_table.entity_not_registered` | Entity type not registered as reference table |
| `encina.reference_table.primary_shard_not_found` | Primary shard ID not in topology |
| `encina.reference_table.no_target_shards` | No active target shards available |
| `encina.reference_table.primary_read_failed` | Failed to read from primary shard |
| `encina.reference_table.replication_partial_failure` | Some shards failed |
| `encina.reference_table.replication_failed` | All shards failed |
| `encina.reference_table.hash_computation_failed` | Hash computation error |
| `encina.reference_table.store_not_registered` | No store provider registered |
| `encina.reference_table.upsert_failed` | Upsert to target shard failed |
| `encina.reference_table.get_all_failed` | Read from shard failed |
| `encina.reference_table.no_primary_key_found` | Entity has no discoverable PK |
| `encina.reference_table.replication_timeout` | Operation timed out |
| `encina.reference_table.invalid_batch_size` | BatchSize <= 0 |
| `encina.reference_table.invalid_polling_interval` | PollingInterval <= 0 |
| `encina.reference_table.missing_attribute` | Missing `[ReferenceTable]` and not explicitly registered |

---

## Troubleshooting

### "Entity type is not registered"

Ensure the entity is registered via `AddReferenceTable<T>()` in your sharding configuration. The `[ReferenceTable]` attribute alone is not sufficient — explicit registration is always required.

### Replication lag increasing

1. Check the `encina.reference_table.replication_duration_ms` metric for slow replications
2. Reduce `PollingInterval` if using `Polling` strategy
3. Consider switching to `CdcDriven` for lower latency
4. Check network connectivity to target shards

### Hash mismatch across providers

All providers use `ReferenceTableHashComputer` with XxHash64. If hashes differ, check:

1. Entity serialization — ensure `[Column]` names match across shard databases
2. Primary key ordering — hash computation sorts by PK value
3. Data type differences between provider databases

### Partial replication failures

When `ReplicationResult.IsPartial` is `true`:

1. Check `FailedShards` for specific shard errors
2. Failed shards will be retried on the next polling cycle
3. Monitor `encina.reference_table.errors_total` for persistent failures

---

## Related Documentation

- [Database Sharding Overview](../sharding/configuration.md) — Sharding configuration reference
- [CDC Pattern](cdc.md) — Change Data Capture infrastructure
- [CDC Per-Shard](cdc-sharding.md) — Sharded CDC connectors
- [Co-Location Groups](sharding-colocation.md) — Entity co-location for local JOINs
- [Reference Tables Configuration Guide](../configuration/reference-tables.md) — Detailed configuration reference
- [Reference Tables Scaling Guide](../guides/reference-tables-scaling.md) — Capacity planning and sizing
- [ADR-013: Reference Tables](../architecture/adr/013-reference-tables.md) — Architecture decision record
