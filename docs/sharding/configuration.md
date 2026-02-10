# Database Sharding in Encina

This guide explains how to configure and use database sharding in Encina applications. Sharding distributes data across multiple database instances to achieve horizontal scalability, geographic distribution, and tenant isolation.

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Routing Strategies](#routing-strategies)
4. [Provider-Specific Setup](#provider-specific-setup)
5. [Connection String Management](#connection-string-management)
6. [Observability](#observability)
7. [Cache Configuration](#cache-configuration)
8. [Health Checks](#health-checks)
9. [FAQ](#faq)

---

## Overview

Encina sharding distributes data across multiple database instances using application-level routing:

```text
┌──────────────────────────────────────────────────────────────────┐
│                        Application                                │
├──────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                   Encina Sharding Pipeline                  │  │
│  │                                                              │  │
│  │  Entity ──► ShardKeyExtractor ──► IShardRouter               │  │
│  │                                        │                      │  │
│  │                          ┌─────────────┼───────────┐          │  │
│  │                        Hash        Range     Directory  Geo   │  │
│  │                          │             │           │      │   │  │
│  │                          └─────────────┼───────────┘      │   │  │
│  │                                        ▼                      │  │
│  │                               ShardTopology                   │  │
│  │                                        │                      │  │
│  │                          ┌─────────────┼───────────┐          │  │
│  │                          ▼             ▼           ▼          │  │
│  │                     Connection    DbContext    Collection      │  │
│  │                      Factory      Factory      Factory        │  │
│  └────────────────────────────────────────────────────────────┘  │
│         │                     │                   │               │
│         ▼                     ▼                   ▼               │
│  ┌────────────┐       ┌────────────┐       ┌────────────┐        │
│  │  Shard 1   │       │  Shard 2   │       │  Shard 3   │        │
│  │  Database  │       │  Database  │       │  Database  │        │
│  └────────────┘       └────────────┘       └────────────┘        │
└──────────────────────────────────────────────────────────────────┘
```

### Key Concepts

| Concept | Description |
|---------|-------------|
| **Shard key** | A value extracted from an entity that determines which shard stores it |
| **Shard topology** | The set of all shards with their connection strings and metadata |
| **Routing strategy** | The algorithm that maps shard keys to shard identifiers |
| **Scatter-gather** | Querying multiple shards in parallel and aggregating results |

### Shard Key Extraction

Encina extracts shard keys from entities using two mechanisms (in priority order):

**Option A — `IShardable` interface** (preferred, no reflection):

```csharp
public class Order : IShardable
{
    public string Id { get; set; }
    public string CustomerId { get; set; }

    public string GetShardKey() => CustomerId;
}
```

**Option B — `[ShardKey]` attribute** (simpler, uses cached reflection):

```csharp
public class Invoice
{
    public string Id { get; set; }

    [ShardKey]
    public string TenantId { get; set; }
}
```

> **Key Benefit**: Encina sharding works identically across ADO.NET, Dapper, EF Core, and MongoDB. Switch providers without changing your sharding logic.

---

## Quick Start

### 1. Define your entity

```csharp
public class Customer : IShardable
{
    public string Id { get; set; }
    public string Region { get; set; }
    public string Name { get; set; }

    public string GetShardKey() => Region;
}
```

### 2. Register sharding in DI

```csharp
// Step 1: Core sharding (topology + routing)
services.AddEncinaSharding<Customer>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", Configuration.GetConnectionString("Shard1")!)
        .AddShard("shard-2", Configuration.GetConnectionString("Shard2")!)
        .AddShard("shard-3", Configuration.GetConnectionString("Shard3")!);
});

// Step 2: Provider-specific registration (pick one per entity)
// ADO.NET
services.AddEncinaADOSharding<Customer, string>(mapping =>
{
    mapping.ToTable("customers")
        .HasId(c => c.Id, "id")
        .MapProperty(c => c.Region, "region")
        .MapProperty(c => c.Name, "name");
});

// OR Dapper (requires ADO registration too)
services.AddEncinaDapperSharding<Customer, string>(mapping =>
{
    mapping.ToTable("customers")
        .HasId(c => c.Id, "id")
        .MapProperty(c => c.Region, "region")
        .MapProperty(c => c.Name, "name");
});
services.AddEncinaADOSharding<Customer, string>(mapping =>
{
    mapping.ToTable("customers")
        .HasId(c => c.Id, "id")
        .MapProperty(c => c.Region, "region")
        .MapProperty(c => c.Name, "name");
});
```

### 3. Use the sharded repository

```csharp
public class CustomerService
{
    private readonly IFunctionalShardedRepository<Customer, string> _repository;

    public CustomerService(IFunctionalShardedRepository<Customer, string> repository)
    {
        _repository = repository;
    }

    public async Task<Either<EncinaError, Customer>> GetCustomerAsync(
        string id, string region)
    {
        return await _repository.GetByIdAsync(id, region);
    }

    public async Task<Either<EncinaError, Unit>> CreateCustomerAsync(Customer customer)
    {
        // Shard key is extracted automatically from the entity
        return await _repository.AddAsync(customer);
    }
}
```

---

## Routing Strategies

### Hash Routing (Recommended for most use cases)

Consistent hashing with xxHash64 and virtual nodes. Provides uniform distribution and minimal data movement when adding or removing shards.

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting(hash =>
    {
        // Default: 150 virtual nodes per shard (good for most workloads)
        hash.VirtualNodesPerShard = 200; // Increase for better uniformity
    })
    .AddShard("shard-1", connectionString1)
    .AddShard("shard-2", connectionString2)
    .AddShard("shard-3", connectionString3);
});
```

| Setting | Default | Description |
|---------|---------|-------------|
| `VirtualNodesPerShard` | 150 | Number of virtual ring positions per shard. Higher values improve uniformity but use more memory. Range: 100-500. |

### Range Routing

Maps contiguous key ranges to shards. Ideal for time-series data or alphabetical partitioning.

```csharp
services.AddEncinaSharding<TimeSeriesEvent>(options =>
{
    options.UseRangeRouting(
        ranges:
        [
            new ShardRange("2024-01", "2024-07", "shard-h1-2024"),
            new ShardRange("2024-07", "2025-01", "shard-h2-2024"),
            new ShardRange("2025-01", null, "shard-current") // null = unbounded
        ])
    .AddShard("shard-h1-2024", connectionStringArchive1)
    .AddShard("shard-h2-2024", connectionStringArchive2)
    .AddShard("shard-current", connectionStringCurrent);
});
```

> **Important**: Ranges must be non-overlapping and are compared lexicographically using ordinal string comparison by default. Pass a custom `StringComparer` as the second parameter to change comparison behavior.

### Directory Routing

Explicit key-to-shard mapping via a pluggable store. Use for VIP tenants, compliance requirements, or any scenario requiring manual placement.

```csharp
// In-memory directory (development/testing)
var store = new InMemoryShardDirectoryStore();
store.AddMapping("tenant-premium", "shard-dedicated");
store.AddMapping("tenant-enterprise", "shard-dedicated");

services.AddEncinaSharding<TenantData>(options =>
{
    options.UseDirectoryRouting(
        store: store,
        defaultShardId: "shard-shared") // Fallback for unmapped keys
    .AddShard("shard-dedicated", connectionStringDedicated)
    .AddShard("shard-shared", connectionStringShared);
});
```

For production, implement `IShardDirectoryStore` against a database or distributed cache:

```csharp
public class SqlDirectoryStore : IShardDirectoryStore
{
    private readonly IDbConnection _connection;

    public string? GetMapping(string key)
        => _connection.QuerySingleOrDefault<string>(
            "SELECT shard_id FROM shard_directory WHERE key = @Key",
            new { Key = key });

    public void AddMapping(string key, string shardId)
        => _connection.Execute(
            "INSERT INTO shard_directory (key, shard_id) VALUES (@Key, @ShardId) " +
            "ON CONFLICT (key) DO UPDATE SET shard_id = @ShardId",
            new { Key = key, ShardId = shardId });

    public bool RemoveMapping(string key)
        => _connection.Execute(
            "DELETE FROM shard_directory WHERE key = @Key",
            new { Key = key }) > 0;

    public IReadOnlyDictionary<string, string> GetAllMappings()
        => _connection.Query<(string Key, string ShardId)>(
            "SELECT key, shard_id FROM shard_directory")
            .ToDictionary(r => r.Key, r => r.ShardId);
}
```

### Geographic Routing

Routes based on geographic region with fallback chain support. Use for data residency compliance or latency optimization.

```csharp
services.AddEncinaSharding<UserProfile>(options =>
{
    options.UseGeoRouting(
        regions:
        [
            new GeoRegion("us-east", "shard-us", FallbackRegionCode: "us-west"),
            new GeoRegion("us-west", "shard-us-west"),
            new GeoRegion("eu-west", "shard-eu", FallbackRegionCode: "eu-central"),
            new GeoRegion("eu-central", "shard-eu-central"),
            new GeoRegion("ap-southeast", "shard-ap", FallbackRegionCode: "us-west")
        ],
        regionResolver: shardKey => ExtractRegionFromKey(shardKey),
        options: new GeoShardRouterOptions
        {
            RequireExactMatch = false,
            DefaultRegion = "us-east"
        })
    .AddShard("shard-us", connectionStringUs)
    .AddShard("shard-us-west", connectionStringUsWest)
    .AddShard("shard-eu", connectionStringEu)
    .AddShard("shard-eu-central", connectionStringEuCentral)
    .AddShard("shard-ap", connectionStringAp);
});
```

| Setting | Default | Description |
|---------|---------|-------------|
| `RequireExactMatch` | `false` | If `true`, fail when region is not found (no fallback) |
| `DefaultRegion` | `null` | Fallback region when all fallback chains are exhausted |

> **Warning**: Circular fallback chains (e.g., A falls back to B, B falls back to A) are detected and produce error code `encina.sharding.region_not_found`.

---

## Provider-Specific Setup

### ADO.NET (SQLite, SqlServer, PostgreSQL, MySQL)

```csharp
// Core sharding registration (required for all providers)
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "Server=db1;Database=orders;...")
        .AddShard("shard-2", "Server=db2;Database=orders;...");
});

// ADO.NET provider registration
services.AddEncinaADOSharding<Order, string>(mapping =>
{
    mapping.ToTable("orders")
        .HasId(o => o.Id, "id")
        .MapProperty(o => o.CustomerId, "customer_id")
        .MapProperty(o => o.Total, "total")
        .MapProperty(o => o.CreatedAtUtc, "created_at_utc");
});
```

**Registers**: `IShardedConnectionFactory`, `IShardedConnectionFactory<TConnection>`, `IShardedQueryExecutor`, `IFunctionalShardedRepository<Order, string>`.

### Dapper (SQLite, SqlServer, PostgreSQL, MySQL)

Dapper sharding reuses ADO.NET's `IShardedConnectionFactory`. Both registrations are required:

```csharp
// Core sharding
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "Server=db1;Database=orders;...")
        .AddShard("shard-2", "Server=db2;Database=orders;...");
});

// ADO registration (provides IShardedConnectionFactory)
services.AddEncinaADOSharding<Order, string>(mapping =>
{
    mapping.ToTable("orders")
        .HasId(o => o.Id, "id")
        .MapProperty(o => o.CustomerId, "customer_id")
        .MapProperty(o => o.Total, "total");
});

// Dapper registration (reuses ADO connection factory)
services.AddEncinaDapperSharding<Order, string>(mapping =>
{
    mapping.ToTable("orders")
        .HasId(o => o.Id, "id")
        .MapProperty(o => o.CustomerId, "customer_id")
        .MapProperty(o => o.Total, "total");
});
```

> **Why both registrations?** Dapper is a thin extension over ADO.NET. Rather than duplicating connection management, Dapper's sharded repository injects `IShardedConnectionFactory` from the ADO registration and adds Dapper-specific query execution on top.

### EF Core (SQLite, SqlServer, PostgreSQL, MySQL)

```csharp
// Core sharding
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", "Server=db1;Database=orders;...")
        .AddShard("shard-2", "Server=db2;Database=orders;...");
});

// EF Core provider registration (example: PostgreSQL)
services.AddEncinaEFCoreShardingPostgreSQL<OrderDbContext, Order, string>(mapping =>
{
    mapping.ToTable("orders")
        .HasId(o => o.Id, "id")
        .MapProperty(o => o.CustomerId, "customer_id")
        .MapProperty(o => o.Total, "total");
});
```

**Registers**: `IShardedDbContextFactory<OrderDbContext>`, `IShardedQueryExecutor`, `IFunctionalShardedRepository<Order, string>`.

### MongoDB

MongoDB supports two modes. See [MongoDB Sharding Guide](mongodb.md) for full details.

```csharp
// Native sharding (recommended for production)
services.AddEncinaMongoDBSharding<Order, string>(options =>
{
    options.UseNativeSharding = true; // default
    options.IdProperty = o => o.Id;
    options.CollectionName = "orders";
});

// Application-level sharding (dev/test or when mongos unavailable)
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
    options.CollectionName = "orders";
});
```

---

## Connection String Management

### appsettings.json Pattern

```json
{
  "ConnectionStrings": {
    "Shard1": "Server=db1.example.com;Database=app;User Id=app;Password=...",
    "Shard2": "Server=db2.example.com;Database=app;User Id=app;Password=...",
    "Shard3": "Server=db3.example.com;Database=app;User Id=app;Password=..."
  }
}
```

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", Configuration.GetConnectionString("Shard1")!)
        .AddShard("shard-2", Configuration.GetConnectionString("Shard2")!)
        .AddShard("shard-3", Configuration.GetConnectionString("Shard3")!);
});
```

### Environment-Specific Configuration

Use `appsettings.{Environment}.json` to vary shard topology per environment:

```json
// appsettings.Development.json — single shard for local dev
{
  "ConnectionStrings": {
    "Shard1": "Server=localhost;Database=app_dev;Trusted_Connection=true"
  }
}

// appsettings.Production.json — multiple shards
{
  "ConnectionStrings": {
    "Shard1": "Server=db1.prod.internal;Database=app;...",
    "Shard2": "Server=db2.prod.internal;Database=app;...",
    "Shard3": "Server=db3.prod.internal;Database=app;..."
  }
}
```

### Secrets Management

Store connection strings in secure vaults, not in source control:

```csharp
// Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri("https://myvault.vault.azure.net/"),
    new DefaultAzureCredential());

// AWS Secrets Manager
builder.Configuration.AddSecretsManager();

// Then reference normally
var conn = Configuration.GetConnectionString("Shard1");
```

### Shard Weight and Status

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", connectionString1, weight: 2)  // 2x traffic
        .AddShard("shard-2", connectionString2, weight: 1)
        .AddShard("shard-3", connectionString3, isActive: false); // Excluded from routing
});
```

---

## Observability

### Metrics Reference

Encina emits the following OpenTelemetry metrics under the `Encina` meter:

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `encina.sharding.route.decisions` | Counter | `{decision}` | Total routing decisions made |
| `encina.sharding.route.duration_ns` | Histogram | `ns` | Routing latency per decision |
| `encina.sharding.topology.shards.active` | ObservableGauge | `{shard}` | Current number of active shards |
| `encina.sharding.scatter.duration_ms` | Histogram | `ms` | Total scatter-gather operation time |
| `encina.sharding.scatter.shard.duration_ms` | Histogram | `ms` | Per-shard query time within scatter-gather |
| `encina.sharding.scatter.partial_failures` | Counter | `{failure}` | Scatter-gather operations with partial failures |
| `encina.sharding.scatter.queries.active` | UpDownCounter | `{query}` | Currently executing scatter-gather queries |

### Trace Spans

All trace activities use the `Encina.Sharding` ActivitySource:

| Activity | Kind | Tags | Description |
|----------|------|------|-------------|
| Routing | Internal | `shard.key`, `router.type`, `shard.id` | Individual routing decision |
| ScatterGather | Internal | `shard.count`, `scatter.strategy` | Cross-shard query orchestration |
| ShardQuery | Client | `shard.id` | Individual shard query within scatter-gather |

### Configuration

```csharp
services.Configure<ShardingMetricsOptions>(options =>
{
    options.HealthCheckInterval = TimeSpan.FromSeconds(30); // default
    options.EnableRoutingMetrics = true;                     // default
    options.EnableScatterGatherMetrics = true;               // default
    options.EnableHealthMetrics = true;                      // default
    options.EnableTracing = true;                             // default
});
```

### OpenTelemetry Integration

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Encina"); // Includes all sharding metrics
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource("Encina.Sharding"); // Sharding trace activities
    });
```

### Example Dashboard Queries (Prometheus/Grafana)

```promql
# Routing decisions per second by shard
rate(encina_sharding_route_decisions_total[5m])

# P99 routing latency
histogram_quantile(0.99, rate(encina_sharding_route_duration_ns_bucket[5m]))

# Active scatter-gather queries
encina_sharding_scatter_queries_active

# Partial failure rate
rate(encina_sharding_scatter_partial_failures_total[5m])
```

---

## Cache Configuration

### Scatter-Gather Options

Control cross-shard query behavior:

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", conn1)
        .AddShard("shard-2", conn2);

    options.ScatterGatherOptions.MaxParallelism = 4;                    // default: -1 (unlimited)
    options.ScatterGatherOptions.Timeout = TimeSpan.FromSeconds(10);    // default: 30s
    options.ScatterGatherOptions.AllowPartialResults = true;            // default: true
});
```

| Option | Default | Description |
|--------|---------|-------------|
| `MaxParallelism` | -1 (unlimited) | Maximum concurrent shard queries. Set to limit connection usage. |
| `Timeout` | 30 seconds | Total time for scatter-gather operation. Individual shard timeouts derive from this. |
| `AllowPartialResults` | `true` | If `true`, return results from successful shards even when some fail. If `false`, return error on any shard failure. |

---

## Health Checks

### Shard Health Monitoring

```csharp
// Health results per shard
var healthy = ShardHealthResult.Healthy("shard-1", poolStats);
var degraded = ShardHealthResult.Degraded("shard-2", "High pool usage");
var unhealthy = ShardHealthResult.Unhealthy("shard-3", "Timeout", exception);

// Aggregate health summary
var results = new[] { healthy, degraded, unhealthy };
var overallStatus = ShardedHealthSummary.CalculateOverallStatus(results);
var summary = new ShardedHealthSummary(overallStatus, results);

// Overall status rules:
// - All healthy           → Healthy
// - Majority healthy/degraded → Degraded
// - Majority unhealthy    → Unhealthy
// - No shards             → Unhealthy
```

---

## FAQ

### Can I use different routing strategies for different entity types?

Yes. Each `AddEncinaSharding<TEntity>()` call is independent. You can use hash routing for `Order` and range routing for `TimeSeriesEvent`:

```csharp
services.AddEncinaSharding<Order>(o => o.UseHashRouting().AddShard(...));
services.AddEncinaSharding<TimeSeriesEvent>(o => o.UseRangeRouting(ranges).AddShard(...));
```

### What happens if a shard goes down?

- **Single-entity operations**: Return `Left` with error code `encina.sharding.shard_not_found` or a connection-level error
- **Scatter-gather queries**: If `AllowPartialResults = true` (default), return results from healthy shards. The `ShardedQueryResult.FailedShards` list shows which shards failed
- **Health checks**: The shard will be reported as `Unhealthy` in `ShardedHealthSummary`

### Can I change an entity's shard key after it's been stored?

No. Changing a shard key would require moving the entity between shards. This must be done manually: delete from the old shard, then insert into the new shard. There is no automatic migration for individual entities.

### How many shards should I start with?

Start with 3-5 shards for most workloads. Hash routing distributes data uniformly, and consistent hashing means adding shards later only moves ~1/N of existing data. See [Scaling Guidance](scaling-guidance.md) for detailed capacity planning.

### Do I need to create the same schema on every shard?

Yes. Every shard must have identical table schemas. Schema migrations must be applied to all shards. Encina does not manage schema migrations; use your preferred migration tool (EF Core Migrations, Flyway, etc.) against each shard.

### Can I use sharding with read/write separation?

Yes. Configure read/write separation per shard independently. Each shard can have its own primary and read replicas.

### What error codes should I handle?

See `ShardingErrorCodes` for the complete list. The most common ones:

| Code | When | Action |
|------|------|--------|
| `encina.sharding.shard_key_empty` | Entity's shard key is null/empty | Validate entity before persisting |
| `encina.sharding.shard_not_found` | Shard ID not in topology | Check topology configuration |
| `encina.sharding.scatter_gather_timeout` | Cross-shard query exceeded timeout | Increase `ScatterGatherOptions.Timeout` or reduce shard count in query |
| `encina.sharding.scatter_gather_partial_failure` | Some shards failed (with `AllowPartialResults = false`) | Check individual shard health |
