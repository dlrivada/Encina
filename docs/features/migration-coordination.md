# Schema Migration Coordination for Shards

Coordinated schema migration across all shards in a topology, with four deployment strategies, automatic rollback, schema drift detection, and full observability.

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Migration Strategies](#migration-strategies)
4. [Applying Migrations](#applying-migrations)
5. [Rollback Support](#rollback-support)
6. [Schema Drift Detection](#schema-drift-detection)
7. [Progress Tracking](#progress-tracking)
8. [Builder API](#builder-api)
9. [DI Registration](#di-registration)
10. [Provider Abstractions](#provider-abstractions)
11. [Observability](#observability)
12. [Health Checks](#health-checks)
13. [Error Codes](#error-codes)
14. [Best Practices](#best-practices)
15. [FAQ](#faq)

---

## Overview

When data is distributed across multiple shards, applying DDL changes consistently requires coordination. A manual approach (running scripts shard-by-shard) is error-prone and slow. Encina provides `IShardedMigrationCoordinator` to orchestrate migrations across the entire topology with configurable strategies, per-shard isolation, and automatic progress tracking.

```text
+---------------------------------------------------------------------+
|               Migration Coordination Flow                           |
|                                                                     |
|  coordinator.ApplyToAllShardsAsync(script, options, ct)             |
|                          |                                          |
|              +-----------+-----------+                              |
|              v           v           v                              |
|          Shard-0      Shard-1      Shard-2                          |
|         CREATE IDX   CREATE IDX   CREATE IDX                        |
|         (UpSql)      (UpSql)      (UpSql)                           |
|           OK           OK           OK                              |
|              |           |           |                              |
|              +-----------+-----------+                              |
|                          v                                          |
|                  MigrationResult                                    |
|                  AllSucceeded = true                                |
|                  SucceededCount = 3                                 |
+---------------------------------------------------------------------+
```

All operations return `Either<EncinaError, T>` following Encina's Railway Oriented Programming pattern.

---

## Quick Start

```csharp
using Encina.Sharding.Migrations;

// 1. Register migration coordination
services.AddEncinaShardMigrationCoordination(migration =>
{
    migration
        .UseStrategy(MigrationStrategy.CanaryFirst)
        .WithMaxParallelism(4)
        .StopOnFirstFailure();
});

// 2. Define a migration script
var script = new MigrationScript(
    Id: "20260216_add_status_index",
    UpSql: "CREATE INDEX idx_orders_status ON orders (status);",
    DownSql: "DROP INDEX idx_orders_status;",
    Description: "Add index on orders.status for faster filtering",
    Checksum: "sha256:a1b2c3d4...");

// 3. Apply to all shards
var options = new MigrationOptions
{
    Strategy = MigrationStrategy.CanaryFirst,
    MaxParallelism = 4,
    StopOnFirstFailure = true
};

var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);

result.Match(
    Right: r =>
    {
        if (r.AllSucceeded)
            logger.LogInformation("Migration applied to all {Count} shards", r.PerShardStatus.Count);
        else
            logger.LogWarning("{Failed} shards failed", r.FailedCount);
    },
    Left: error => logger.LogError("Coordination error: {Error}", error.Message));
```

---

## Migration Strategies

Four strategies offer different trade-offs between safety and speed:

| Strategy | Execution | Best For | Risk |
|----------|-----------|----------|------|
| **Sequential** | One shard at a time, in order | First-time rollouts, destructive DDL | Lowest |
| **Parallel** | All shards simultaneously (throttled) | Additive, non-breaking changes | Highest |
| **RollingUpdate** | Batches of N shards | Balanced approach for medium topologies | Medium |
| **CanaryFirst** | One canary, then parallel | High-risk changes requiring validation | Low |

### Sequential

```text
Shard-0 --> Shard-1 --> Shard-2 --> Shard-3
  OK          OK          OK          OK
```

Applies the migration to one shard at a time. If any shard fails (and `StopOnFirstFailure` is enabled), remaining shards are left untouched.

### Parallel

```text
Shard-0 --+
Shard-1 --+--> All at once (throttled by MaxParallelism)
Shard-2 --+
Shard-3 --+
```

Applies to all shards simultaneously, limited by `MaxParallelism`. Best for safe, additive changes like `CREATE INDEX`.

### RollingUpdate

```text
Batch 1: [Shard-0, Shard-1] --> OK
Batch 2: [Shard-2, Shard-3] --> OK
```

Applies in batches of `MaxParallelism` shards. Each batch must complete before the next starts. Balanced approach for medium-to-large topologies.

### CanaryFirst

```text
Canary: Shard-0 --> OK
Then:   Shard-1 --+
        Shard-2 --+--> Parallel
        Shard-3 --+
```

Applies to a single canary shard first. If the canary succeeds, the remaining shards are migrated in parallel. Recommended for high-risk schema changes.

---

## Applying Migrations

### MigrationScript

An immutable record containing the forward and reverse DDL:

```csharp
var script = new MigrationScript(
    Id: "20260216_add_status_index",        // Unique identifier (idempotency key)
    UpSql: "CREATE INDEX idx_orders_status ON orders (status);",
    DownSql: "DROP INDEX idx_orders_status;",
    Description: "Add index on orders.status",
    Checksum: "sha256:a1b2c3d4...");        // Integrity verification
```

All fields are validated at construction (no null or whitespace allowed).

### MigrationOptions

Per-migration configuration:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Strategy` | `MigrationStrategy` | `Sequential` | Execution strategy |
| `MaxParallelism` | `int` | `4` | Max concurrent shard migrations |
| `StopOnFirstFailure` | `bool` | `true` | Stop remaining shards on first failure |
| `PerShardTimeout` | `TimeSpan` | `5 minutes` | Timeout per individual shard |
| `ValidateBeforeApply` | `bool` | `true` | Validate script before applying |

### MigrationResult

The result contains per-shard outcomes:

```csharp
result.Match(
    Right: r =>
    {
        // Aggregate checks
        Console.WriteLine($"AllSucceeded: {r.AllSucceeded}");
        Console.WriteLine($"Succeeded: {r.SucceededCount}, Failed: {r.FailedCount}");
        Console.WriteLine($"Duration: {r.TotalDuration}");

        // Per-shard inspection
        foreach (var (shardId, status) in r.PerShardStatus)
        {
            Console.WriteLine($"  {shardId}: {status.Outcome} ({status.Duration})");
            if (status.Outcome == MigrationOutcome.Failed)
                Console.WriteLine($"    Error: {status.ErrorMessage}");
        }
    },
    Left: error => Console.WriteLine($"Coordination failed: {error.Message}"));
```

---

## Rollback Support

Roll back a migration using the `DownSql` on all shards that were successfully migrated:

```csharp
// Apply migration
var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);

// If something went wrong, rollback succeeded shards
result.IfRight(async r =>
{
    if (!r.AllSucceeded)
    {
        var rollback = await coordinator.RollbackAsync(r, ct);
        rollback.Match(
            Right: _ => logger.LogInformation("Rollback completed"),
            Left: error => logger.LogError("Rollback failed: {Error}", error.Message));
    }
});
```

Only shards with `MigrationOutcome.Succeeded` are rolled back. Shards that failed, were skipped, or are still pending are left untouched.

---

## Schema Drift Detection

Detect structural differences between shards by comparing each shard's schema against a baseline:

```csharp
var driftOptions = new DriftDetectionOptions
{
    BaselineShardId = "shard-0",           // null = auto-select first shard
    ComparisonDepth = SchemaComparisonDepth.Full,
    IncludeColumnDiffs = true,
    IncludeIndexes = true,
    IncludeConstraints = true,
    CriticalTables = ["orders", "customers", "payments"]
};

var report = await coordinator.DetectDriftAsync(driftOptions, ct);

report.IfRight(r =>
{
    if (r.HasDrift)
    {
        foreach (var diff in r.Diffs)
        {
            Console.WriteLine($"Shard {diff.ShardId} vs {diff.BaselineShardId}:");
            foreach (var table in diff.TableDiffs)
                Console.WriteLine($"  Table '{table.TableName}': {table.Type}");
        }
    }
});
```

### Comparison Depths

| Depth | What's Compared | Performance |
|-------|----------------|-------------|
| `TablesOnly` | Table names only | Fastest |
| `TablesAndColumns` | Tables + column names, types, nullability | Balanced |
| `Full` | Tables + columns + indexes + constraints | Thorough |

### Drift Types

| Type | Meaning |
|------|---------|
| `Missing` | Table exists in baseline but not in the shard |
| `Extra` | Table exists in the shard but not in baseline |
| `Modified` | Table exists in both but columns differ |

---

## Progress Tracking

Monitor long-running migrations in real-time:

```csharp
var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);

// The migration ID is returned in the result
result.IfRight(async r =>
{
    var progress = await coordinator.GetProgressAsync(r.Id, ct);

    progress.IfRight(p =>
    {
        Console.WriteLine($"Phase: {p.CurrentPhase}");
        Console.WriteLine($"Completed: {p.CompletedShards}/{p.TotalShards}");
        Console.WriteLine($"Failed: {p.FailedShards}");
        Console.WriteLine($"Remaining: {p.RemainingShards}");
        Console.WriteLine($"Finished: {p.IsFinished}");
    });
});
```

### MigrationProgress Properties

| Property | Type | Description |
|----------|------|-------------|
| `MigrationId` | `Guid` | Execution identifier |
| `TotalShards` | `int` | Total shards targeted |
| `CompletedShards` | `int` | Successfully migrated |
| `FailedShards` | `int` | Failed migrations |
| `CurrentPhase` | `string` | e.g., "Canary", "RollingBatch2", "Completed" |
| `RemainingShards` | `int` | Computed: `Total - Completed - Failed` |
| `IsFinished` | `bool` | Computed: `Completed + Failed >= Total` |

---

## Builder API

The fluent builder provides a discoverable API for configuring migration coordination:

```csharp
services.AddEncinaShardMigrationCoordination(migration =>
{
    migration
        .UseStrategy(MigrationStrategy.RollingUpdate)
        .WithMaxParallelism(8)
        .StopOnFirstFailure()
        .WithPerShardTimeout(TimeSpan.FromMinutes(10))
        .ValidateBeforeApply()
        .OnShardMigrated((shardId, outcome) =>
            logger.LogInformation("Shard {Shard}: {Outcome}", shardId, outcome))
        .WithDriftDetection(drift =>
        {
            drift.ComparisonDepth = SchemaComparisonDepth.Full;
            drift.CriticalTables = ["orders", "payments"];
        });
});
```

### Builder Methods

| Method | Description |
|--------|-------------|
| `UseStrategy(MigrationStrategy)` | Set default strategy |
| `WithMaxParallelism(int)` | Set max concurrent shards (min: 1) |
| `StopOnFirstFailure(bool)` | Stop on first shard failure |
| `WithPerShardTimeout(TimeSpan)` | Per-shard timeout (must be positive) |
| `ValidateBeforeApply(bool)` | Pre-validate scripts |
| `OnShardMigrated(Action<string, MigrationOutcome>)` | Per-shard callback |
| `WithDriftDetection(Action<DriftDetectionOptions>)` | Configure drift detection |

---

## DI Registration

```csharp
services.AddEncinaShardMigrationCoordination(migration =>
{
    migration.UseStrategy(MigrationStrategy.CanaryFirst);
});
```

This registers:

- `MigrationCoordinationOptions` as **Singleton**
- `DriftDetectionOptions` as **Singleton**
- `IShardedMigrationCoordinator` as **Scoped**

Provider-specific services must be registered separately:

- `IMigrationExecutor` — DDL execution
- `ISchemaIntrospector` — schema inspection
- `IMigrationHistoryStore` — migration history tracking

---

## Provider Abstractions

Three interfaces define the provider contract. Implementations are database-specific:

### IMigrationExecutor

Executes DDL statements against a single shard:

```csharp
public interface IMigrationExecutor
{
    Task<Either<EncinaError, Unit>> ExecuteSqlAsync(
        ShardInfo shardInfo,
        string sql,
        CancellationToken cancellationToken = default);
}
```

### IMigrationHistoryStore

Tracks migration history per shard in a `__EncinaMigrationHistory` table:

| Method | Purpose |
|--------|---------|
| `GetAppliedAsync` | List applied migrations for a shard |
| `RecordAppliedAsync` | Record a successful migration |
| `RecordRolledBackAsync` | Record a rollback |
| `EnsureHistoryTableExistsAsync` | Create history table if missing |
| `ApplyHistoricalMigrationsAsync` | Onboard new shards with history |

### ISchemaIntrospector

Reads schema metadata from a shard for drift comparison:

```csharp
public interface ISchemaIntrospector
{
    Task<Either<EncinaError, ShardSchema>> GetSchemaAsync(
        ShardInfo shardInfo,
        SchemaIntrospectionOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

### Provider Implementation Status

| Provider | IMigrationExecutor | IMigrationHistoryStore | ISchemaIntrospector |
|----------|:-:|:-:|:-:|
| ADO.NET SQLite | Planned | Planned | Planned |
| ADO.NET SqlServer | Planned | Planned | Planned |
| ADO.NET PostgreSQL | Planned | Planned | Planned |
| ADO.NET MySQL | Planned | Planned | Planned |
| Dapper (4 DBs) | Planned | Planned | Planned |
| EF Core (4 DBs) | Planned | Planned | Planned |
| MongoDB | Planned | Planned | Planned |

---

## Observability

### Metrics

Six metric instruments under the "Encina" meter:

| Metric | Type | Description |
|--------|------|-------------|
| `encina.migration.shards_migrated_total` | Counter | Shards successfully migrated |
| `encina.migration.shards_failed_total` | Counter | Shards that failed |
| `encina.migration.duration_per_shard_ms` | Histogram | Per-shard duration |
| `encina.migration.total_duration_ms` | Histogram | Total coordination duration |
| `encina.migration.drift_detected_count` | ObservableGauge | Drifted shards count |
| `encina.migration.rollbacks_total` | Counter | Rollback operations |

### Traces

Three activities via `MigrationActivitySource` ("Encina.Migration"):

- **MigrationCoordination**: Parent span for the entire operation
- **ShardMigration**: Per-shard child span
- **Complete**: Enriches parent with final results

14 activity tags are automatically added (e.g., `migration.id`, `migration.strategy`, `migration.shard.id`, `migration.shard.outcome`, `migration.duration_ms`).

---

## Health Checks

`SchemaDriftHealthCheck` periodically checks for schema drift and reports health status:

```csharp
services.AddHealthChecks()
    .AddCheck<SchemaDriftHealthCheck>("schema-drift");

services.Configure<SchemaDriftHealthCheckOptions>(options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
    options.CriticalTables = ["orders", "customers"];
});
```

| Condition | Status |
|-----------|--------|
| No drift detected | **Healthy** |
| Drift in non-critical tables | **Degraded** |
| Drift in critical tables | **Unhealthy** |

### SchemaDriftHealthCheckOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Timeout` | `TimeSpan` | `30s` | Health check timeout |
| `CriticalTables` | `IList<string>` | `[]` | Tables that trigger Unhealthy |

---

## Error Codes

All error codes follow the `encina.sharding.migration.*` convention:

| Code | Constant | Description |
|------|----------|-------------|
| `migration_failed` | `MigrationErrorCodes.MigrationFailed` | Script failed on one or more shards |
| `migration_timeout` | `MigrationErrorCodes.MigrationTimeout` | Per-shard timeout exceeded |
| `rollback_failed` | `MigrationErrorCodes.RollbackFailed` | Rollback failed on one or more shards |
| `drift_detected` | `MigrationErrorCodes.DriftDetected` | Schema drift detected during validation |
| `invalid_script` | `MigrationErrorCodes.InvalidScript` | Invalid script (checksum, empty SQL) |
| `schema_comparison_failed` | `MigrationErrorCodes.SchemaComparisonFailed` | Schema introspection failed |
| `migration_not_found` | `MigrationErrorCodes.MigrationNotFound` | Unknown migration ID |

Error codes are stable across releases and suitable for alerting rules and log filters.

---

## Best Practices

1. **Start with Sequential**: Use `MigrationStrategy.Sequential` for the first rollout, then switch to `CanaryFirst` or `RollingUpdate` once you have confidence.

2. **Always provide DownSql**: Every `MigrationScript` should have a working reverse DDL. Test rollback in a staging environment before production.

3. **Use unique migration IDs**: Include a timestamp prefix (e.g., `20260216_add_status_index`) to ensure chronological ordering and prevent collisions.

4. **Set appropriate timeouts**: The default 5-minute per-shard timeout works for most DDL. Increase for large table alterations (e.g., adding columns with defaults).

5. **Monitor with health checks**: Register `SchemaDriftHealthCheck` and configure critical tables. Run drift detection periodically, not just during migrations.

6. **Use shadow sharding for testing**: Apply migrations to shadow topology first using [Shadow Sharding](shadow-sharding.md) to validate DDL before production.

7. **Handle partial failures**: When `AllSucceeded` is false, inspect `PerShardStatus` to identify failed shards. Use `RollbackAsync` to revert succeeded shards, then investigate and retry.

---

## FAQ

**Q: Can I use this with EF Core migrations (`Database.Migrate()`)?**

Provider-specific implementations will support EF Core's migration infrastructure. The `IMigrationExecutor` abstraction allows EF Core providers to call `Database.Migrate()` per shard context.

**Q: What happens if a shard is unreachable during migration?**

The executor returns a Left error for that shard. With `StopOnFirstFailure = true`, remaining shards are skipped. With `StopOnFirstFailure = false`, the coordinator continues with other shards.

**Q: How do I onboard a new shard with existing migrations?**

Use `IMigrationHistoryStore.ApplyHistoricalMigrationsAsync()` to record all previous migrations without re-executing them. This assumes the new shard was created from a snapshot with the schema already applied.

**Q: Is drift detection expensive?**

It depends on `ComparisonDepth`. `TablesOnly` is fast (one query per shard). `Full` (with indexes and constraints) requires additional introspection queries. Use `TablesAndColumns` (default) for regular checks and `Full` for audits.

**Q: Can I run migrations across different database engines?**

Each shard in the topology can use a different provider, but the `MigrationScript.UpSql` must be compatible with all target engines. For heterogeneous topologies, consider provider-specific scripts or use an abstraction layer.

---

## Related Documentation

- [Shadow Sharding](shadow-sharding.md) — Test migrations risk-free against shadow topology
- [Scaling Guidance](../sharding/scaling-guidance.md) — Shard key selection and capacity planning
- [Database Sharding](../sharding/configuration.md) — Topology configuration reference
