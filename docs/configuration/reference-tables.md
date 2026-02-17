# Reference Tables — Configuration Guide

This document provides a comprehensive reference for configuring reference table replication in Encina's sharding infrastructure.

## Table of Contents

1. [Basic Setup](#basic-setup)
2. [Per-Table Configuration](#per-table-configuration)
3. [Global Configuration](#global-configuration)
4. [Health Check Configuration](#health-check-configuration)
5. [Refresh Strategy Comparison](#refresh-strategy-comparison)
6. [Provider Registration](#provider-registration)
7. [Integration with Sharding](#integration-with-sharding)
8. [Complete Examples](#complete-examples)

---

## Basic Setup

### Minimum Configuration

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", connectionString0)
        .AddShard("shard-1", connectionString1)
        // Register a reference table with defaults
        .AddReferenceTable<Country>();
});

// Register the provider store
services.AddEncinaADOReferenceTableStoreSqlServer();
```

This uses all defaults: `Polling` strategy, 5-minute interval, first shard as primary, batch size 1000, sync on startup.

### Multiple Reference Tables

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Polling;
    rt.PollingInterval = TimeSpan.FromMinutes(10);
})
.AddReferenceTable<Currency>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Polling;
    rt.PollingInterval = TimeSpan.FromMinutes(30);
})
.AddReferenceTable<Category>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Manual;
});
```

---

## Per-Table Configuration

### `ReferenceTableOptions`

| Property | Type | Default | Valid Range | Description |
|----------|------|---------|-------------|-------------|
| `RefreshStrategy` | `RefreshStrategy` | `Polling` | `CdcDriven`, `Polling`, `Manual` | Change detection strategy |
| `PrimaryShardId` | `string?` | `null` | Any valid shard ID | Authoritative data source; `null` = first shard |
| `PollingInterval` | `TimeSpan` | 5 min | > `TimeSpan.Zero` | Polling cycle interval (Polling strategy only) |
| `BatchSize` | `int` | 1000 | > 0 | Max rows per upsert batch |
| `SyncOnStartup` | `bool` | `true` | — | Replicate immediately on application start |

### Refresh Strategy Details

#### CdcDriven

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.CdcDriven;
    rt.PrimaryShardId = "shard-0";
    rt.BatchSize = 500;
    rt.SyncOnStartup = true;
});
```

**Prerequisites**: A configured `ICdcConnector` for the primary shard. See [CDC documentation](../features/cdc.md).

**Latency**: Near-real-time (depends on CDC connector poll interval).

#### Polling

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Polling;
    rt.PollingInterval = TimeSpan.FromMinutes(5);
    rt.PrimaryShardId = "shard-0";
    rt.BatchSize = 1000;
    rt.SyncOnStartup = true;
});
```

**How change detection works**:

1. Reads all rows from primary shard
2. Computes XxHash64 hash (rows sorted by PK for determinism)
3. Compares with stored hash from previous cycle
4. Replicates only if hash differs

**Tuning**: Lower `PollingInterval` for fresher data at the cost of more primary reads.

#### Manual

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Manual;
    rt.SyncOnStartup = false; // Only sync when explicitly triggered
});

// Trigger replication in your code:
var result = await replicator.ReplicateAsync<Country>(ct);

// Or replicate all registered tables:
var allResult = await replicator.ReplicateAllAsync(ct);
```

---

## Global Configuration

### `ReferenceTableGlobalOptions`

Configure via `IServiceCollection.Configure<T>()`:

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
| `MaxParallelShards` | `int` | `ProcessorCount` | Maximum concurrent shard replication operations |
| `DefaultRefreshStrategy` | `RefreshStrategy` | `Polling` | Default for tables without explicit strategy |
| `HealthCheckUnhealthyThreshold` | `TimeSpan` | 5 min | Lag above which health = Unhealthy |
| `HealthCheckDegradedThreshold` | `TimeSpan` | 1 min | Lag above which health = Degraded |

### Parallelism Tuning

The `MaxParallelShards` setting controls how many target shards receive data simultaneously during a replication cycle. Consider:

- **Lower values** (2-4): Reduce database connection pressure, suitable for shared infrastructure
- **Higher values** (8-16): Faster replication cycles, suitable for dedicated shard databases
- **Default** (`ProcessorCount`): Balanced for most deployments

---

## Health Check Configuration

### Basic Registration

```csharp
services.AddHealthChecks()
    .AddCheck<ReferenceTableHealthCheck>("reference-tables");
```

### With Custom Options

```csharp
services.Configure<ReferenceTableHealthCheckOptions>(opts =>
{
    opts.UnhealthyThreshold = TimeSpan.FromMinutes(15);
    opts.DegradedThreshold = TimeSpan.FromMinutes(3);
});

services.AddHealthChecks()
    .AddCheck<ReferenceTableHealthCheck>(
        "reference-tables",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "sharding"]);
```

### Health Status Logic

| Condition | Status |
|-----------|--------|
| All tables replicated within `DegradedThreshold` | Healthy |
| Some tables between `DegradedThreshold` and `UnhealthyThreshold` | Degraded |
| Any table exceeds `UnhealthyThreshold` or never replicated | Unhealthy |

---

## Refresh Strategy Comparison

| Aspect | CdcDriven | Polling | Manual |
|--------|:---------:|:-------:|:------:|
| **Latency** | Seconds | Minutes (configurable) | On-demand |
| **Infrastructure** | CDC connector required | None | None |
| **Primary shard load** | Low (event-driven) | Moderate (periodic full reads) | Low (on-demand) |
| **Network bandwidth** | Per-change | Periodic full table | On-demand full table |
| **Complexity** | Higher | Low | Lowest |
| **Best for** | Dynamic reference data | Semi-static reference data | Deployment-time data |
| **Change detection** | Stream-based | Hash comparison | Explicit trigger |

### Decision Guide

1. **Data changes < once per day**: `Manual` (sync on deployment)
2. **Data changes a few times per day**: `Polling` with 5-15 min interval
3. **Data changes hourly or more**: `CdcDriven` or `Polling` with 1-2 min interval
4. **Data must propagate in seconds**: `CdcDriven`
5. **No CDC infrastructure available**: `Polling`

---

## Provider Registration

### ADO.NET Providers

```csharp
// SQLite
services.AddEncinaADOReferenceTableStoreSqlite();

// SQL Server
services.AddEncinaADOReferenceTableStoreSqlServer();

// PostgreSQL
services.AddEncinaADOReferenceTableStorePostgreSql();

// MySQL
services.AddEncinaADOReferenceTableStoreMySql();
```

### Dapper Providers

```csharp
// SQLite
services.AddEncinaDapperReferenceTableStoreSqlite();

// SQL Server
services.AddEncinaDapperReferenceTableStoreSqlServer();

// PostgreSQL
services.AddEncinaDapperReferenceTableStorePostgreSql();

// MySQL
services.AddEncinaDapperReferenceTableStoreMySql();
```

### EF Core Providers

```csharp
// SQLite
services.AddEncinaEFCoreReferenceTableStoreSqlite<AppDbContext>();

// SQL Server
services.AddEncinaEFCoreReferenceTableStoreSqlServer<AppDbContext>();

// PostgreSQL
services.AddEncinaEFCoreReferenceTableStorePostgreSql<AppDbContext>();

// MySQL
services.AddEncinaEFCoreReferenceTableStoreMySql<AppDbContext>();
```

### MongoDB

```csharp
services.AddEncinaMongoDBReferenceTableStore();
```

---

## Integration with Sharding

Reference tables work alongside all existing sharding features:

### With Co-Location Groups

```csharp
options.UseHashRouting()
    .AddShard("shard-0", conn0)
    .AddShard("shard-1", conn1)
    .AddColocationGroup(group => group
        .WithRootEntity<Order>()
        .AddColocatedEntity<OrderItem>()
        .WithSharedShardKeyProperty("CustomerId"))
    .AddReferenceTable<Country>()
    .AddReferenceTable<Currency>();
```

### With Compound Shard Keys

```csharp
options.UseCompoundRouting(compound => compound
    .GeoComponent("Region")
    .HashComponent("CustomerId"))
    .AddShard("us-east-1", connUsEast)
    .AddShard("eu-west-1", connEuWest)
    .AddReferenceTable<Country>(rt =>
    {
        rt.PrimaryShardId = "us-east-1";
    });
```

### With Read/Write Separation

Reference tables are replicated to **primary** connections on each shard. Read replicas are not targeted (they receive data via the shard's own replication).

---

## Complete Examples

### E-Commerce with Multiple Reference Tables

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", "Server=shard0;Database=Orders;...")
        .AddShard("shard-1", "Server=shard1;Database=Orders;...")
        .AddShard("shard-2", "Server=shard2;Database=Orders;...")

        // Static data: manual sync on deployment
        .AddReferenceTable<Country>(rt =>
        {
            rt.RefreshStrategy = RefreshStrategy.Manual;
            rt.PrimaryShardId = "shard-0";
        })

        // Semi-static: poll every 15 minutes
        .AddReferenceTable<Currency>(rt =>
        {
            rt.RefreshStrategy = RefreshStrategy.Polling;
            rt.PollingInterval = TimeSpan.FromMinutes(15);
        })

        // Dynamic pricing tiers: CDC for fast propagation
        .AddReferenceTable<PricingTier>(rt =>
        {
            rt.RefreshStrategy = RefreshStrategy.CdcDriven;
            rt.BatchSize = 200;
        });
});

// Register provider (using Dapper with SQL Server)
services.AddEncinaDapperReferenceTableStoreSqlServer();

// Health check
services.AddHealthChecks()
    .AddCheck<ReferenceTableHealthCheck>("reference-tables");

// Global tuning
services.Configure<ReferenceTableGlobalOptions>(opts =>
{
    opts.MaxParallelShards = 4;
    opts.HealthCheckDegradedThreshold = TimeSpan.FromMinutes(2);
    opts.HealthCheckUnhealthyThreshold = TimeSpan.FromMinutes(10);
});
```

### Minimal Setup (Defaults for Everything)

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", conn0)
        .AddShard("shard-1", conn1)
        .AddReferenceTable<Country>()
        .AddReferenceTable<Currency>();
});

services.AddEncinaADOReferenceTableStoreSqlServer();
```

---

## Related Documentation

- [Reference Tables Feature Guide](../features/reference-tables.md) — Overview and architecture
- [Reference Tables Scaling Guide](../guides/reference-tables-scaling.md) — Capacity planning
- [ADR-013: Reference Tables](../architecture/adr/013-reference-tables.md) — Design decisions
- [Sharding Configuration](../sharding/configuration.md) — Sharding setup reference
- [CDC Pattern](../features/cdc.md) — Change Data Capture
