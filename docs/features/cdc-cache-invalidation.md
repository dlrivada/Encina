# CDC-Driven Query Cache Invalidation in Encina

This guide explains how to use Encina's CDC infrastructure to automatically invalidate query cache entries across all application instances when database changes are detected from any source.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Architecture](#architecture)
6. [Configuration Options](#configuration-options)
7. [Table-to-Entity Mapping](#table-to-entity-mapping)
8. [Health Checks](#health-checks)
9. [Observability](#observability)
10. [Troubleshooting](#troubleshooting)
11. [Testing](#testing)
12. [Best Practices](#best-practices)
13. [FAQ](#faq)

---

## Overview

CDC-driven cache invalidation detects database changes from **any source** and invalidates matching cache entries across all application instances via pub/sub broadcast.

| Feature | Description |
|---------|-------------|
| **Any Change Source** | Detects changes from other instances, direct SQL, migrations, external services |
| **Cross-Instance** | Broadcasts invalidation patterns to all instances via `IPubSubProvider` |
| **Pattern-Based** | Uses `{prefix}:*:{entityType}:*` to match `QueryCacheInterceptor` keys |
| **Resilient** | Cache/pub/sub failures are logged but never block the CDC pipeline |
| **Configurable** | Table filtering, explicit entity mappings, custom prefixes and channels |
| **Observable** | OpenTelemetry traces, metrics, and structured logging |

### How It Complements QueryCacheInterceptor

| Scenario | QueryCacheInterceptor | CDC Cache Invalidation |
|----------|----------------------|----------------------|
| Same instance calls `SaveChanges()` | Invalidated immediately | Also invalidated (redundant, safe) |
| Another instance calls `SaveChanges()` | **Not invalidated** (stale) | Invalidated via pub/sub |
| Direct SQL update | **Not invalidated** (stale) | Invalidated via CDC detection |
| Database migration | **Not invalidated** (stale) | Invalidated via CDC detection |
| External microservice writes | **Not invalidated** (stale) | Invalidated via CDC detection |

---

## The Problem

### Challenge: Stale Cache Across Instances

```
Instance A: SaveChanges(Order) → Cache invalidated locally ✅
Instance B: Still serving cached Order data → STALE ❌
Instance C: Still serving cached Order data → STALE ❌
```

The `QueryCacheInterceptor` only invalidates cache entries on the instance that made the change. In multi-instance deployments, other instances continue serving stale data until their cache entries expire naturally.

### Challenge: Changes Outside the Application

Direct SQL updates, database migrations, and writes from external microservices are invisible to `QueryCacheInterceptor` because they bypass `SaveChanges()`. These changes leave **all** instances with stale cache data.

---

## The Solution

CDC captures every database change at the database level, translates it into a cache invalidation pattern, and broadcasts it to all instances via pub/sub:

```
Database Change (any source)
    │
    ▼
CDC Connector (detects change)
    │
    ▼
QueryCacheInvalidationCdcHandler
    ├── 1. Resolve entity type from table name
    ├── 2. Generate pattern: {prefix}:*:{entityType}:*
    ├── 3. Invalidate LOCAL cache via ICacheProvider.RemoveByPatternAsync()
    └── 4. Broadcast pattern via IPubSubProvider.PublishAsync()
              │
              ▼
    CacheInvalidationSubscriberService (all instances)
              │
              ▼
    ICacheProvider.RemoveByPatternAsync() on each instance
```

---

## Quick Start

### 1. Register CDC with Cache Invalidation

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .WithCacheInvalidation();
});
```

### 2. Register Dependencies

Cache invalidation requires an `ICacheProvider` and optionally an `IPubSubProvider`:

```csharp
// Required: cache provider (Redis, Memory, Valkey, etc.)
services.AddEncinaRedisCache(opts =>
{
    opts.ConnectionString = "localhost:6379";
});

// Required for cross-instance: pub/sub provider
services.AddEncinaRedisPubSub(opts =>
{
    opts.ConnectionString = "localhost:6379";
});

// Required: CDC connector (SQL Server, PostgreSQL, etc.)
services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = connectionString;
    opts.TrackedTables = ["dbo.Orders", "dbo.Products"];
});
```

### 3. Enable Query Caching (if not already)

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseQueryCaching();
});
```

That's it. CDC changes will now automatically invalidate cache entries across all instances.

---

## Architecture

### Components

| Component | Visibility | Purpose |
|-----------|-----------|---------|
| `QueryCacheInvalidationOptions` | **Public** | Configuration options |
| `QueryCacheInvalidationCdcHandler` | Internal | Translates CDC events to cache invalidation |
| `CdcTableNameResolver` | Internal | Resolves entity type from table name |
| `CacheInvalidationSubscriberService` | Internal | Receives pub/sub messages and invalidates local cache |
| `CacheInvalidationSubscriberHealthCheck` | Internal | Health check for subscriber connectivity |
| `CacheInvalidationActivitySource` | Internal | OpenTelemetry tracing |
| `CacheInvalidationMetrics` | Internal | Metrics counters |

### Cache Key Pattern

Cache keys generated by `QueryCacheInterceptor` follow this format:

```
{prefix}:*:{entityType}:*
```

For example, with the default prefix `sm:qc` and table `dbo.Orders`:

```
sm:qc:*:Orders:*
```

The CDC handler generates the same pattern to ensure correct invalidation.

---

## Configuration Options

```csharp
config.WithCacheInvalidation(opts =>
{
    // Cache key prefix — must match QueryCacheInterceptor's prefix
    opts.CacheKeyPrefix = "sm:qc";  // default

    // Enable cross-instance broadcast via pub/sub
    opts.UsePubSubBroadcast = true;  // default

    // Pub/sub channel name for invalidation messages
    opts.PubSubChannel = "sm:cache:invalidate";  // default

    // Filter: only invalidate for these tables (null = all tables)
    opts.Tables = ["Orders", "Products"];

    // Explicit table-to-entity mappings (case-insensitive lookup)
    opts.TableToEntityTypeMappings = new Dictionary<string, string>
    {
        ["dbo.Orders"] = "Order",
        ["public.products"] = "Product"
    };
});
```

### Configuration Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CacheKeyPrefix` | `string` | `"sm:qc"` | Must match `QueryCacheInterceptor` prefix |
| `UsePubSubBroadcast` | `bool` | `true` | Enable cross-instance invalidation |
| `PubSubChannel` | `string` | `"sm:cache:invalidate"` | Channel name for pub/sub messages |
| `Tables` | `HashSet<string>?` | `null` | Table filter (null = all tables) |
| `TableToEntityTypeMappings` | `Dictionary<string, string>?` | `null` | Explicit table → entity type mappings |

---

## Table-to-Entity Mapping

### Automatic Schema Stripping (Default)

When no explicit mapping is configured, the resolver strips the schema prefix:

| CDC Table Name | Resolved Entity Type |
|----------------|---------------------|
| `dbo.Orders` | `Orders` |
| `public.products` | `products` |
| `schema.subschema.Table` | `Table` |
| `Orders` | `Orders` |

### Explicit Mappings

For custom entity names (e.g., singular forms, renamed types):

```csharp
opts.TableToEntityTypeMappings = new Dictionary<string, string>
{
    ["dbo.Orders"] = "Order",           // Plural → singular
    ["dbo.ProductCatalog"] = "Product", // Table rename
    ["public.user_accounts"] = "User"   // Snake case → PascalCase
};
```

Explicit mappings are looked up case-insensitively and always take precedence over schema stripping.

---

## Health Checks

When `UsePubSubBroadcast` is enabled, a health check (`CacheInvalidationSubscriberHealthCheck`) is automatically registered. It verifies pub/sub connectivity by publishing a test message.

**Tags**: `encina`, `cdc`, `cache-invalidation`, `ready`

**Health check data**:

| Key | Description |
|-----|-------------|
| `channel` | The pub/sub channel name |
| `cache_key_prefix` | The configured cache key prefix |

---

## Observability

### Tracing (OpenTelemetry)

**ActivitySource**: `Encina.Cdc.CacheInvalidation` (version 1.0)

| Activity | Description |
|----------|-------------|
| `encina.cdc.cache.invalidation` | Full invalidation flow (table → cache → broadcast) |
| `encina.cdc.cache.broadcast` | Pub/sub broadcast to other instances |

**Span attributes**:

- `encina.cdc.table_name` — Source table name
- `encina.cdc.operation` — insert, update, or delete
- `encina.cdc.cache.entity_type` — Resolved entity type
- `encina.cdc.cache.pattern` — Cache key pattern used
- `encina.cdc.cache.broadcast_sent` — Whether broadcast was sent
- `encina.cdc.cache.channel` — Pub/sub channel (broadcast activity)

### Metrics

**Meter**: `Encina.Cdc.CacheInvalidation` (version 1.0)

| Instrument | Type | Tags | Description |
|------------|------|------|-------------|
| `encina.cdc.cache.invalidations` | Counter | `table_name`, `operation` | Cache invalidation operations |
| `encina.cdc.cache.broadcasts` | Counter | `table_name`, `operation` | Pub/sub broadcast operations |
| `encina.cdc.cache.errors` | Counter | `table_name`, `operation`, `error_type` | Error occurrences |

**Error types**: `cache_failure`, `broadcast_failure`

### Logging

16 structured log events (EventIds 150-165) covering:

- Cache invalidation lifecycle (filtering, resolving, invalidating)
- Pub/sub broadcast lifecycle (publishing, success, failure)
- Subscriber lifecycle (starting, receiving, invalidating, errors)

---

## Troubleshooting

### Cache Not Being Invalidated

1. **Mismatched prefix**: Ensure `CacheKeyPrefix` matches the prefix used by `QueryCacheInterceptor` (default: `"sm:qc"`)
2. **Table filtering**: If `Tables` is set, verify the CDC table name is in the filter (comparison is case-insensitive)
3. **Entity type mismatch**: If using explicit mappings, verify the mapped entity type matches what `QueryCacheInterceptor` uses

### Cross-Instance Invalidation Not Working

1. **No pub/sub provider**: Register an `IPubSubProvider` (Redis, Valkey, etc.)
2. **Different channels**: Ensure all instances use the same `PubSubChannel` value
3. **UsePubSubBroadcast disabled**: Verify `UsePubSubBroadcast = true` (default)
4. **Health check**: Check the `encina-cdc-cache-invalidation` health check for connectivity issues

### CDC Events Not Being Processed

1. **CDC not enabled**: Ensure `config.UseCdc()` is called before `WithCacheInvalidation()`
2. **No connector**: Register a CDC connector (`AddEncinaCdcSqlServer`, etc.)
3. **Table not tracked**: Verify the CDC connector is configured to track the relevant tables

---

## Testing

48 tests provide comprehensive coverage:

| Test Type | Count | Coverage |
|-----------|-------|----------|
| **Unit** | 19 | Entity resolution, pattern generation, table filtering, pub/sub, error handling |
| **Guard** | 4 | Constructor null checks, optional parameter validation |
| **Contract** | 9 | Handler return types, interface shape, cancellation handling |
| **Property** | 9 | Schema stripping invariants, mapping precedence, determinism |
| **Integration** | 7 | End-to-end flows, subscriber callback, DI registration |

---

## Best Practices

1. **Always match prefixes**: The `CacheKeyPrefix` must match between `QueryCacheInterceptor` and `QueryCacheInvalidationOptions`
2. **Use table filtering**: In high-traffic databases, filter only the tables that have cached queries to reduce noise
3. **Use explicit mappings**: When your entity types don't match table names (e.g., singular vs plural), configure explicit mappings
4. **Monitor error metrics**: Watch `encina.cdc.cache.errors` for cache or pub/sub connectivity issues
5. **Don't worry about redundancy**: When the same instance both writes and receives CDC, the double invalidation is safe and cheap

---

## FAQ

### Q: Does this replace QueryCacheInterceptor?

No. CDC cache invalidation **complements** `QueryCacheInterceptor`. The interceptor provides immediate, synchronous invalidation for local changes. CDC provides asynchronous cross-instance invalidation for all change sources. Use both together for the best cache consistency.

### Q: What happens if pub/sub is down?

Local cache invalidation still works. The broadcast to other instances fails gracefully (logged, not thrown). Other instances will serve stale data until their cache entries expire or pub/sub recovers.

### Q: What happens if the cache provider is down?

The handler logs the error and returns `Right(unit)` without blocking the CDC pipeline. Cache entries remain stale until the cache provider recovers and the next CDC event triggers invalidation.

### Q: Can I use this without pub/sub?

Yes. Set `UsePubSubBroadcast = false` or don't register an `IPubSubProvider`. The handler will only invalidate the local cache on the instance running the CDC processor.

### Q: What is the invalidation latency?

Latency depends on the CDC connector's polling interval (configurable via `CdcOptions.PollingInterval`) plus pub/sub network latency. Typical end-to-end latency is 1-5 seconds depending on configuration and infrastructure.
