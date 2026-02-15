# Time-Based Sharding in Encina

This guide explains how to use time-based sharding to partition data by time periods with automatic tier lifecycle management. Shards progress through storage tiers (Hot, Warm, Cold, Archived) as data ages, enabling cost-optimized storage and transparent query routing.

## Table of Contents

1. [Overview](#overview)
2. [When to Use Time-Based Sharding](#when-to-use-time-based-sharding)
3. [Configuration](#configuration)
4. [Shard Periods](#shard-periods)
5. [Tier Lifecycle](#tier-lifecycle)
6. [Routing](#routing)
7. [Automatic Tier Transitions](#automatic-tier-transitions)
8. [Auto-Shard Creation](#auto-shard-creation)
9. [Archival and Retention](#archival-and-retention)
10. [Health Checks](#health-checks)
11. [Observability](#observability)
12. [Error Handling](#error-handling)
13. [Operations Guide](#operations-guide)
14. [Troubleshooting](#troubleshooting)
15. [FAQ](#faq)

---

## Overview

Time-based sharding partitions data into shards based on time periods. Each shard covers a contiguous range (daily, weekly, monthly, quarterly, or yearly) and belongs to a storage tier that reflects how actively the data is used:

```text
                           Tier Lifecycle
  ┌─────────────────────────────────────────────────────────┐
  │                                                         │
  │   HOT          WARM          COLD         ARCHIVED      │
  │   (writes +    (read-only,   (read-only,  (read-only,   │
  │    reads)      recent data)  infrequent)  long-term)    │
  │                                                         │
  │   SSD/Memory   Standard      HDD/Compressed  S3/Blob    │
  │                                                         │
  │   ──30 days──► ──90 days──► ──365 days──►               │
  │                                                         │
  └─────────────────────────────────────────────────────────┘
```

Only Hot-tier shards accept writes. The `TierTransitionScheduler` background service automatically transitions shards between tiers based on configurable age thresholds.

---

## When to Use Time-Based Sharding

| Scenario | Single Shard | Hash Sharding | Time-Based Sharding |
|----------|:------------:|:-------------:|:-------------------:|
| Small dataset, all recent | | | |
| Uniform access across all data | | | |
| Access patterns skew heavily by recency | | | |
| Compliance requires data retention policies | | | |
| Cost optimization for cold/archived data | | | |
| High-volume event/log ingestion | | | |

**Use time-based sharding when**:

- Data has a natural timestamp (events, logs, transactions, sensor readings)
- Recent data is accessed frequently, older data rarely
- You want to reduce storage costs by moving old data to cheaper tiers
- Compliance or retention policies require archiving data after a specific age
- You need to delete old data cleanly (drop an entire shard instead of row-by-row deletes)

**Common use cases**:

- **Audit logs**: Monthly shards with 1-year archive policy
- **IoT sensor data**: Daily shards with aggressive tier transitions
- **E-commerce orders**: Monthly shards, warm after 30 days, cold after 90 days
- **Financial transactions**: Quarterly shards with yearly archival

---

## Configuration

### Basic Setup

```csharp
// 1. Configure the time-based sharding options
services.Configure<TimeBasedShardingOptions>(options =>
{
    options.Enabled = true;
    options.Period = ShardPeriod.Monthly;
    options.CheckInterval = TimeSpan.FromMinutes(30);
    options.ShardIdPrefix = "orders";
    options.ConnectionStringTemplate = "Server=hot;Database=orders_{0}";
    options.Transitions =
    [
        new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
        new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
        new TierTransition(ShardTier.Cold, ShardTier.Archived, TimeSpan.FromDays(365)),
    ];
});

// 2. Register the tier store and archiver
services.AddSingleton<ITierStore, InMemoryTierStore>();
services.AddSingleton<IShardArchiver, ShardArchiver>();

// 3. Register the background scheduler
services.AddHostedService<TierTransitionScheduler>();
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Whether the scheduler runs |
| `Period` | `ShardPeriod` | `Monthly` | Time granularity for shard boundaries |
| `WeekStart` | `DayOfWeek` | `Monday` | First day of week (ISO 8601), only for `Weekly` |
| `CheckInterval` | `TimeSpan` | `1 hour` | How often the scheduler checks for transitions |
| `Transitions` | `IReadOnlyList<TierTransition>` | `[]` | Age-based tier transition rules |
| `EnableAutoShardCreation` | `bool` | `true` | Whether to pre-create shards for upcoming periods |
| `ShardCreationLeadTime` | `TimeSpan` | `1 day` | How far ahead to create the next shard |
| `ShardIdPrefix` | `string` | `"shard"` | Prefix for auto-generated shard IDs |
| `ConnectionStringTemplate` | `string?` | `null` | Template with `{0}` placeholder for period label |

### Shard ID Format

Auto-generated shard IDs follow the pattern `{ShardIdPrefix}-{PeriodLabel}`:

| Period | Example Shard ID |
|--------|-----------------|
| Daily | `orders-2026-01-15` |
| Weekly | `orders-2026-W03` |
| Monthly | `orders-2026-01` |
| Quarterly | `orders-2026-Q1` |
| Yearly | `orders-2026` |

---

## Shard Periods

The `ShardPeriod` enum controls how time is divided into shard boundaries:

```csharp
public enum ShardPeriod
{
    Daily,      // One shard per calendar day
    Weekly,     // One shard per ISO 8601 week (Mon-Sun)
    Monthly,    // One shard per calendar month
    Quarterly,  // One shard per quarter (Q1: Jan-Mar, etc.)
    Yearly      // One shard per calendar year
}
```

**Choosing a period**:

| Factor | Shorter Periods (Daily) | Longer Periods (Yearly) |
|--------|:-----------------------:|:-----------------------:|
| Number of shards | More | Fewer |
| Shard size | Smaller | Larger |
| Scatter-gather queries | More shards to query | Fewer shards to query |
| Archival granularity | Fine-grained | Coarse-grained |
| Drop-shard cleanup | Low-cost deletes | Larger deletes |

**Recommendation**: Start with `Monthly` for most workloads. Use `Daily` for high-volume data (IoT, logs) and `Quarterly` or `Yearly` for lower-volume data with long retention.

---

## Tier Lifecycle

### Tier Definitions

| Tier | Writes | Reads | Typical Storage | Use Case |
|------|:------:|:-----:|-----------------|----------|
| **Hot** | Yes | Yes | SSD, in-memory cache | Current period's active data |
| **Warm** | No | Yes | Standard SSD/HDD | Recent historical data |
| **Cold** | No | Yes | HDD, compressed | Infrequent access, compliance |
| **Archived** | No | Yes | S3, Azure Blob, tape | Long-term retention |

### Tier Transitions

Transitions are defined as `TierTransition` records with three properties:

```csharp
// Transition shards from Hot to Warm after 30 days past the period end
new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30))
```

- `FromTier`: The current tier (must be less than `ToTier`)
- `ToTier`: The target tier (must be greater than `FromTier`)
- `AgeThreshold`: Time since the shard's period ended before transitioning

**Rules**:

- Transitions must move forward: Hot -> Warm -> Cold -> Archived
- You can skip tiers (e.g., Hot -> Archived)
- Backward transitions are rejected at construction time
- Same-tier transitions are rejected at construction time
- Zero or negative age thresholds are rejected

### Tier Transition Lifecycle Diagram

```text
  Period Start    Period End     +30 days      +90 days      +365 days
       │              │             │              │              │
       ▼              ▼             ▼              ▼              ▼
  ┌─────────┐    ┌─────────┐  ┌─────────┐  ┌──────────┐  ┌──────────┐
  │   HOT   │───►│   HOT   │─►│  WARM   │─►│   COLD   │─►│ ARCHIVED │
  │ (write) │    │(read+wr)│  │  (r/o)  │  │   (r/o)  │  │   (r/o)  │
  └─────────┘    └─────────┘  └─────────┘  └──────────┘  └──────────┘
  Active shard    Still active   Read-only    Read-only     Long-term
  for writes      until period   enforcement  optimized     retention
                  ends           applied      storage
```

---

## Routing

### Timestamp-Based Routing

The `ITimeBasedShardRouter` routes operations to shards based on timestamps:

```csharp
// Route a read to the appropriate shard
var result = await router.RouteByTimestampAsync(DateTime.UtcNow);
result.Match(
    Right: shardId => logger.LogInformation("Read from {ShardId}", shardId),
    Left: error => logger.LogError("Routing failed: {Error}", error.Message));

// Route a write (enforces Hot tier)
var writeResult = await router.RouteWriteByTimestampAsync(DateTime.UtcNow);
writeResult.Match(
    Right: shardId => logger.LogInformation("Writing to {ShardId}", shardId),
    Left: error => logger.LogError("Write failed: {Error}", error.Message));
```

### Range Queries

Query across multiple shards using time ranges:

```csharp
// Get all shards covering Q1 2026
var shards = await router.GetShardsInRangeAsync(
    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

shards.Match(
    Right: ids => ids.ToList().ForEach(id =>
        logger.LogInformation("Query shard: {ShardId}", id)),
    Left: error => logger.LogError("Range query failed: {Error}", error.Message));
```

### Tier Inspection

Check a shard's current tier and metadata:

```csharp
// Get the tier
Either<EncinaError, ShardTier> tier = router.GetShardTier("orders-2026-01");

// Get full metadata
Either<EncinaError, ShardTierInfo> info = router.GetShardTierInfo("orders-2026-01");
info.Match(
    Right: ti => logger.LogInformation(
        "Shard {Id}: {Tier}, Period {Start}-{End}, ReadOnly={RO}",
        ti.ShardId, ti.CurrentTier, ti.PeriodStart, ti.PeriodEnd, ti.IsReadOnly),
    Left: error => logger.LogError("Shard not found: {Error}", error.Message));
```

### Router Implementation

The `TimeBasedShardRouter` uses a sorted array of `TimeBasedShardEntry` records with binary search for O(log n) routing. Period boundaries are pre-computed at construction time using `PeriodBoundaryCalculator`, and tier metadata is stored in a `FrozenDictionary` for fast lookup.

---

## Automatic Tier Transitions

The `TierTransitionScheduler` is a `BackgroundService` that runs on a configurable interval:

1. **Each tick**, it resolves `ITierStore` and `IShardArchiver` from a scoped `IServiceProvider`
2. For each configured `TierTransition`, it queries the tier store for shards due for transition
3. For each due shard, it calls `IShardArchiver.TransitionTierAsync()`
4. Successes and failures are logged individually, with totals per tick

**Behavior**:

- The scheduler never crashes. Exceptions are caught, logged, and retried on the next tick
- Each transition is independent: a failure in one shard does not block others
- The scheduler uses `TimeProvider` for testability (inject `FakeTimeProvider` in tests)
- Scoped dependencies are resolved fresh on each tick for database context compatibility

**Example log output**:

```
[INF] Time-based shard tier transition scheduler started (interval: 00:30:00, rules: 3)
[INF] Tier transition check started
[INF] Transitioning shard 'orders-2025-10' from Hot to Warm
[INF] Tier transition succeeded for shard 'orders-2025-10' to Warm
[INF] Tier transition check completed: 1 succeeded, 0 failed
```

---

## Auto-Shard Creation

When `EnableAutoShardCreation` is `true`, the scheduler pre-creates shards for upcoming time periods:

1. Computes the current period's end date using `PeriodBoundaryCalculator`
2. Checks if `today + ShardCreationLeadTime >= currentPeriodEnd`
3. If within the lead time window:
   - Generates the shard ID: `{ShardIdPrefix}-{PeriodLabel}`
   - Generates the connection string from `ConnectionStringTemplate`
   - Checks if the shard already exists (idempotent)
   - Creates a new Hot-tier `ShardTierInfo` in the tier store

**Requirements**:

- `ConnectionStringTemplate` must be set (uses `{0}` for period label)
- The `IShardFallbackCreator` interface is available for database-level shard provisioning

**Race condition handling**: If multiple instances attempt to create the same shard simultaneously, the `ArgumentException` from duplicate shard IDs is caught and logged as a skip, not an error.

---

## Archival and Retention

### Archival

The `IShardArchiver.ArchiveShardAsync()` method exports shard data to external storage:

```csharp
var result = await archiver.ArchiveShardAsync(
    "orders-2024-01",
    new ArchiveOptions("s3://archive-bucket/orders/2024-01"));

result.Match(
    Right: _ => logger.LogInformation("Archive complete"),
    Left: error => logger.LogError("Archive failed: {Error}", error.Message));
```

> **Note**: The default `ShardArchiver` implementation is a no-op for archival. Actual data export requires a provider-specific implementation that integrates with your storage backend (S3, Azure Blob, etc.).

### Read-Only Enforcement

Read-only state is enforced at two levels:

1. **Application level**: The `ITimeBasedShardRouter` checks the `IsReadOnly` flag in `ShardTierInfo` and returns error code `encina.sharding.shard_read_only` for write attempts
2. **Database level** (optional): Register an `IReadOnlyEnforcer` to apply database-level restrictions (e.g., `ALTER DATABASE SET READ_ONLY` on SQL Server)

```csharp
// Manually enforce read-only
await archiver.EnforceReadOnlyAsync("orders-2025-10");
```

### Retention / Data Deletion

```csharp
// Delete shard data as part of a retention policy
var result = await archiver.DeleteShardDataAsync("orders-2023-01");
```

The default implementation updates the tier store metadata only. Database-level deletion (e.g., `DROP DATABASE`) requires a provider-specific `IShardArchiver`.

---

## Health Checks

Two health checks monitor the time-based sharding subsystem:

### Shard Creation Health Check

Configured via `ShardCreationHealthCheckOptions`:

| State | Condition |
|-------|-----------|
| **Healthy** | Shards exist for both the current and next period |
| **Degraded** | Next period's shard is missing (within warning window) |
| **Unhealthy** | Current period's shard is missing |

```csharp
services.Configure<ShardCreationHealthCheckOptions>(options =>
{
    options.Period = ShardPeriod.Monthly;
    options.ShardIdPrefix = "orders";
    options.WarningWindowDays = 3; // Warn 3 days before period end
});
```

### Tier Transition Health Check

Configured via `TierTransitionHealthCheckOptions`:

| State | Condition |
|-------|-----------|
| **Healthy** | All shards are within expected tier age thresholds |
| **Degraded** | Some shards have exceeded their expected tier age |
| **Unhealthy** | Shards are significantly overdue (2x threshold) |

```csharp
services.Configure<TierTransitionHealthCheckOptions>(options =>
{
    options.MaxExpectedHotAgeDays = 35;   // 1 month + 5 day buffer
    options.MaxExpectedWarmAgeDays = 95;  // 3 months + 5 day buffer
    options.MaxExpectedColdAgeDays = 370; // 1 year + 5 day buffer
    options.UnhealthyMultiplier = 2.0;   // 2x threshold = unhealthy
});
```

---

## Observability

The `TimeBasedShardingMetrics` class (in `Encina.OpenTelemetry`) exposes six metric instruments under the `Encina` meter:

| Instrument | Type | Tags | Description |
|------------|------|------|-------------|
| `encina.sharding.tiered.shards_per_tier` | ObservableGauge | `shard.tier` | Current shard count per tier |
| `encina.sharding.tiered.oldest_hot_shard_age_days` | ObservableGauge | | Age of oldest Hot shard (days) |
| `encina.sharding.tiered.tier_transitions_total` | Counter | `tier.from`, `tier.to` | Tier transitions executed |
| `encina.sharding.tiered.auto_created_shards_total` | Counter | | Auto-created shard count |
| `encina.sharding.tiered.queries_per_tier` | Counter | `shard.tier` | Queries routed per tier |
| `encina.sharding.tiered.archival_duration_ms` | Histogram | `db.shard.id` | Archival duration (ms) |

**Key alerts to configure**:

- `shards_per_tier{shard.tier="Hot"} == 0` — No active shard for writes
- `oldest_hot_shard_age_days > 35` — Hot shard overdue for transition
- `tier_transitions_total` rate drop to 0 — Scheduler may be stuck

---

## Error Handling

All operations return `Either<EncinaError, T>` following Railway Oriented Programming. Error codes specific to time-based sharding:

| Error Code | Constant | Cause |
|------------|----------|-------|
| `encina.sharding.shard_read_only` | `ShardingErrorCodes.ShardReadOnly` | Write attempt on a non-Hot shard |
| `encina.sharding.timestamp_outside_range` | `ShardingErrorCodes.TimestampOutsideRange` | Timestamp doesn't match any shard period |
| `encina.sharding.no_time_based_shards` | `ShardingErrorCodes.NoTimeBasedShards` | No shards configured in the topology |
| `encina.sharding.tier_transition_failed` | `ShardingErrorCodes.TierTransitionFailed` | Tier transition could not be completed |
| `encina.sharding.archival_failed` | `ShardingErrorCodes.ArchivalFailed` | Archival operation failed |
| `encina.sharding.retention_policy_failed` | `ShardingErrorCodes.RetentionPolicyFailed` | Retention/deletion operation failed |
| `encina.sharding.shard_creation_failed` | `ShardingErrorCodes.ShardCreationFailed` | Auto-shard creation failed |
| `encina.sharding.partial_key_routing_failed` | `ShardingErrorCodes.PartialKeyRoutingFailed` | Partial key could not resolve shards |

```csharp
var result = await router.RouteWriteByTimestampAsync(timestamp);
result.Match(
    Right: shardId => { /* proceed with write */ },
    Left: error =>
    {
        var code = error.GetCode();
        code.IfSome(c =>
        {
            if (c == ShardingErrorCodes.ShardReadOnly)
                logger.LogWarning("Shard is read-only for timestamp {Ts}", timestamp);
            else if (c == ShardingErrorCodes.TimestampOutsideRange)
                logger.LogError("No shard covers timestamp {Ts}", timestamp);
        });
    });
```

---

## Operations Guide

### Manual Tier Transitions

Use `IShardArchiver` directly for manual tier management:

```csharp
// Manually transition a shard
var result = await archiver.TransitionTierAsync("orders-2025-06", ShardTier.Archived);

// Manually enforce read-only
await archiver.EnforceReadOnlyAsync("orders-2025-06");

// Manually delete old data
await archiver.DeleteShardDataAsync("orders-2023-01");
```

### Monitoring Tier Health

1. **Check the health endpoints**: `/health/shard-creation` and `/health/tier-transitions`
2. **Watch the metrics**:
   - `shards_per_tier` gauge shows the distribution across tiers
   - `oldest_hot_shard_age_days` alerts when transitions are overdue
   - `tier_transitions_total` counter confirms the scheduler is running
3. **Review scheduler logs**: The scheduler logs each transition attempt with shard ID, source tier, and target tier

### Interpreting Metrics

| Metric Pattern | Interpretation | Action |
|---------------|---------------|--------|
| `shards_per_tier{Hot}` increasing | New shards being created | Normal if auto-creation is on |
| `shards_per_tier{Hot}` > 2 | Multiple Hot shards | Check if transitions are running |
| `oldest_hot_shard_age_days` > threshold | Transition overdue | Check scheduler logs for errors |
| `tier_transitions_total` flat | No transitions happening | Verify scheduler is enabled and running |
| `auto_created_shards_total` flat | No new shards created | Check `ConnectionStringTemplate` is set |

### Adding a New Shard Manually

```csharp
var tierInfo = new ShardTierInfo(
    ShardId: "orders-2026-04",
    CurrentTier: ShardTier.Hot,
    PeriodStart: new DateOnly(2026, 4, 1),
    PeriodEnd: new DateOnly(2026, 5, 1),
    IsReadOnly: false,
    ConnectionString: "Server=hot;Database=orders_2026-04",
    CreatedAtUtc: DateTime.UtcNow);

await tierStore.AddShardAsync(tierInfo);
```

---

## Troubleshooting

### Missed Tier Transitions

**Symptoms**: `oldest_hot_shard_age_days` increasing, `tier_transitions_total` flat

1. **Check scheduler is enabled**: Verify `TimeBasedShardingOptions.Enabled == true`
2. **Check transitions are configured**: Verify `Transitions` list is not empty
3. **Check scheduler logs**: Look for `TransitionCheckError` log entries
4. **Check the tier store**: Verify `GetShardsDueForTransitionAsync()` returns shards
5. **Check the archiver**: Verify `TransitionTierAsync()` is not returning errors

### Shard Creation Failures

**Symptoms**: Health check reports Degraded/Unhealthy, `auto_created_shards_total` flat

1. **Check `ConnectionStringTemplate`**: Must be set and contain `{0}` placeholder
2. **Check `EnableAutoShardCreation`**: Must be `true`
3. **Check `ShardCreationLeadTime`**: If too short, the window may be missed between ticks
4. **Check scheduler logs**: Look for `AutoCreateFailed` log entries
5. **Check for duplicates**: `AutoCreateSkippedAlreadyExists` means another instance created it first (normal in multi-instance deployments)

### Read-Only Enforcement Not Working

**Symptoms**: Writes succeed on non-Hot shards

1. **Check the router**: Ensure you're using `RouteWriteByTimestampAsync()` (not `RouteByTimestampAsync()` which allows reads on all tiers)
2. **Check `IsReadOnly` flag**: Verify the `ShardTierInfo.IsReadOnly` is `true` for non-Hot shards
3. **Check `IReadOnlyEnforcer`**: If database-level enforcement is needed, register a provider-specific enforcer

### Timestamp Outside Range Errors

**Symptoms**: Error code `encina.sharding.timestamp_outside_range` on routing

1. **Check shard coverage**: Verify shards exist for the timestamp's time period
2. **Check the topology**: The `TimeBasedShardRouter` only knows about shards registered in the topology
3. **Create the missing shard**: Either enable auto-creation or add the shard manually

---

## FAQ

### Can I mix time-based sharding with hash sharding?

Yes. Use compound shard keys to combine time-based routing with hash routing. See [Compound Shard Keys](compound-shard-keys.md) for details.

### What happens if the scheduler is down during a transition window?

The scheduler retries on the next tick. Transitions are idempotent: if a shard was already transitioned, the next check skips it. No data is lost.

### Can I run multiple scheduler instances?

Yes. Auto-shard creation handles race conditions via try/catch on duplicate shard IDs. Tier transitions are idempotent. However, consider using a distributed lock to avoid redundant work in multi-instance deployments.

### How do I change a shard's period after creation?

You cannot change a shard's period boundaries after creation. Instead, create new shards with the desired period and migrate data. This is by design to maintain shard boundary consistency.

### What is the performance overhead of tier-aware routing?

The `TimeBasedShardRouter` uses binary search (O(log n)) on a sorted array of shard entries, with tier metadata in a `FrozenDictionary`. For typical deployments with fewer than 100 shards, routing is effectively O(1) with sub-microsecond latency.

### Do I need to implement `IReadOnlyEnforcer`?

No. Read-only enforcement at the application level (via `RouteWriteByTimestampAsync()`) is sufficient for most use cases. Implement `IReadOnlyEnforcer` only if you need database-level protection (e.g., `ALTER DATABASE SET READ_ONLY`).

### How does `InMemoryTierStore` handle concurrency?

`InMemoryTierStore` uses `ConcurrentDictionary` internally and is thread-safe for all operations including `AddShardAsync`, `UpdateTierAsync`, and `GetShardsDueForTransitionAsync`. It is suitable for single-process deployments and testing.

---

## Related Documentation

- [Compound Shard Keys](compound-shard-keys.md) — Multi-field routing with independent strategies
- [Database Sharding Configuration](../sharding/configuration.md) — Complete sharding configuration reference
- [Cross-Shard Operations](../sharding/cross-shard-operations.md) — Scatter-gather and partial failure handling
- [Scaling Guidance](../sharding/scaling-guidance.md) — Shard key selection and capacity planning
- [ADR-010: Database Sharding](../architecture/adr/010-database-sharding.md) — Architecture Decision Record
