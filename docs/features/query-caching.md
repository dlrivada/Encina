# Query Caching (EF Core Second-Level Cache)

Encina provides a transparent EF Core query caching interceptor that acts as a second-level cache. It intercepts database queries at the `DbCommand` level, serves cached results when available, and automatically invalidates cached entries when entities are modified via `SaveChanges`.

## Overview

The query caching system consists of:

- **`QueryCacheInterceptor`**: EF Core `DbCommandInterceptor` + `ISaveChangesInterceptor` that intercepts query execution and manages the cache lifecycle
- **`DefaultQueryCacheKeyGenerator`**: Generates deterministic cache keys from SQL commands using SHA256 hashing and entity type resolution
- **`CachedDataReader`**: A full `DbDataReader` implementation that serves cached results to EF Core's materializer
- **`SqlTableExtractor`**: Extracts table names from SQL using compiled regex, supporting all SQL quoting styles

## Configuration

### Basic Setup

```csharp
// Program.cs

// Step 1: Register a cache provider
builder.Services.AddEncinaMemoryCache();
// Or: AddEncinaRedisCache("localhost:6379"), AddEncinaHybridCache(), etc.

// Step 2: Register query caching services
builder.Services.AddQueryCaching(options =>
{
    options.Enabled = true;
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
});

// Step 3: Add interceptor to DbContext
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.UseQueryCaching(sp); // Adds the interceptor
});
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `false` | Enables or disables the query cache interceptor |
| `DefaultExpiration` | `TimeSpan` | `5 minutes` | Default cache entry expiration |
| `KeyPrefix` | `string` | `"sm:qc"` | Prefix for all cache keys |
| `ThrowOnCacheErrors` | `bool` | `false` | Whether cache failures should throw or fall through to the database |
| `ExcludedEntityTypes` | `HashSet<string>` | Empty | Entity type names to exclude from caching |

### Excluding Entity Types

For high-churn tables or entities that should always reflect the latest data:

```csharp
builder.Services.AddQueryCaching(options =>
{
    options.Enabled = true;
    options.ExcludeType<AuditLog>()       // Fluent chaining
           .ExcludeType<NotificationQueue>()
           .ExcludeType<UserSession>();
});
```

### Custom Key Generator

Replace the default key generator with a custom implementation:

```csharp
// Register your custom implementation before AddQueryCaching
builder.Services.AddSingleton<IQueryCacheKeyGenerator, MyCustomKeyGenerator>();

// AddQueryCaching uses TryAdd — your registration takes precedence
builder.Services.AddQueryCaching(options => options.Enabled = true);
```

## How It Works

### Cache Key Format

The default key generator produces keys with the following format:

**Without tenant:**

```
{prefix}:{primaryEntityType}:{hash}
```

Example: `sm:qc:Order:a1b2c3d4e5f6g7h8`

**With tenant:**

```
{prefix}:{tenantId}:{primaryEntityType}:{hash}
```

Example: `sm:qc:tenant-42:Order:a1b2c3d4e5f6g7h8`

The hash is computed from:

- The full SQL command text
- All parameter names and values (in ordinal order)

This ensures that identical queries with different parameters produce different cache keys, while the same query with the same parameters always produces the same key.

### Query Interception Flow

```
EF Core Query
    |
    v
ReaderExecuting (sync/async)
    |
    +-- Is caching enabled?
    |   No --> Pass through to database
    |
    +-- Is entity type excluded?
    |   Yes --> Pass through to database
    |
    +-- Generate cache key
    |
    +-- Cache hit?
    |   Yes --> Return CachedDataReader (no database call)
    |
    +-- Cache miss --> Pass through to database
    |
    v
ReaderExecuted (sync/async)
    |
    +-- Was this a cache miss?
    |   Yes --> Capture result, store in cache
    |
    v
Return results to EF Core
```

### Cache Invalidation

Cache invalidation is automatic and entity-type-aware:

```
SaveChanges called
    |
    v
SavingChanges
    |-- Collect all modified entity types from ChangeTracker
    |-- Store pending invalidation set (AsyncLocal)
    |
    v
Database commit
    |
    v
SavedChanges
    |-- For each modified entity type:
    |   Remove all cached queries involving that type
    |
    v
Done (cache is now consistent)
```

If `SaveChanges` fails, pending invalidations are cleared in `SaveChangesFailed` to avoid unnecessary cache evictions.

### Entity Type Resolution

The interceptor resolves entity types from SQL in two steps:

1. **SQL parsing**: `SqlTableExtractor` uses compiled regex to extract table names from `FROM` and `JOIN` clauses, handling bracket (`[Table]`), double-quote (`"Table"`), and backtick (`` `Table` ``) quoting
2. **Entity mapping**: Table names are mapped to CLR entity types via `DbContext.Model.GetEntityTypes()`. Unresolved table names are kept as-is for invalidation purposes

## Multi-Tenant Support

When an `IRequestContext` with a `TenantId` is available, cache keys are automatically scoped to the tenant:

```csharp
// Tenant A queries Orders → cache key: sm:qc:tenant-a:Order:abc123
// Tenant B queries Orders → cache key: sm:qc:tenant-b:Order:def456
// Each tenant has isolated cache entries
```

No additional configuration is needed. The interceptor resolves `IRequestContext` from the service provider and includes the tenant ID in the cache key when present.

## Performance Considerations

### When to Use Query Caching

| Scenario | Recommendation |
|----------|----------------|
| Read-heavy queries with stable data | Use query caching |
| Reference/lookup tables | Use query caching with longer expiration |
| High-churn transactional tables | Exclude from caching or use short expiration |
| Complex aggregation queries | Use query caching (saves DB computation) |
| Single-row lookups by ID | Handler-level `[Cache]` attribute may be simpler |
| Real-time data requirements | Do not use caching |

### Query Caching vs Handler-Level Caching

Encina supports two complementary caching strategies:

| Aspect | Query Cache (`QueryCacheInterceptor`) | Handler Cache (`[Cache]` attribute) |
|--------|---------------------------------------|-------------------------------------|
| **Level** | Database command (SQL) | Application handler (request/response) |
| **Granularity** | Per-query | Per-handler |
| **Invalidation** | Automatic on `SaveChanges` | Manual or tag-based |
| **Transparency** | Fully transparent to application code | Requires `[Cache]` attribute |
| **Scope** | All EF Core queries | Only decorated handlers |
| **Best for** | Reducing database load | Caching business logic results |

Both can be used simultaneously. Handler-level caching prevents the handler from executing at all, while query caching prevents individual SQL queries from hitting the database.

### Memory Considerations

- Each cached result stores column metadata and all row data as `object?[]` arrays
- Large result sets (thousands of rows) consume proportional memory
- Use `ExcludeType<T>()` for entities that produce large result sets
- Consider using `DefaultExpiration` to limit cache lifetime
- The `CachedAtUtc` timestamp on each entry enables monitoring of cache freshness

### Cache Key Collision Risk

The default key generator uses the first 8 bytes (16 hex characters) of a SHA256 hash, providing a 64-bit key space. For practical cache sizes (up to millions of entries), the collision probability is negligible. If absolute uniqueness is required, implement a custom `IQueryCacheKeyGenerator` with a larger hash.

### Expected Overhead

Based on BenchmarkDotNet measurements:

| Operation | Expected Overhead |
|-----------|------------------|
| Key generation (simple query) | < 1 microsecond |
| Key generation (complex JOIN) | < 2 microseconds |
| Cache hit (memory provider) | < 5 microseconds |
| CachedDataReader (5 rows) | < 1 microsecond |
| CachedDataReader (1000 rows) | < 50 microseconds |

Total interceptor overhead for a cache hit is typically **< 10 microseconds**, well below the 1ms target.

## Error Handling

By default (`ThrowOnCacheErrors = false`), cache failures are logged and the query falls through to the database:

```csharp
// Cache provider is down → query executes normally against the database
// No application errors, just slightly higher database load
```

For strict environments where cache availability is critical:

```csharp
builder.Services.AddQueryCaching(options =>
{
    options.ThrowOnCacheErrors = true; // Throws on cache failures
});
```

## Troubleshooting

### Stale Cache Data

**Symptom**: Queries return outdated data after modifications.

**Causes and Solutions**:

- **Direct SQL modifications**: The interceptor only invalidates on `SaveChanges`. Direct SQL (`ExecuteSqlRaw`) bypasses invalidation. Use `ICacheProvider.RemoveByPrefixAsync` manually.
- **Multiple DbContext instances**: Each context tracks its own changes. Modifications in one context don't invalidate cache entries created by another.
- **External data changes**: Changes made outside the application (stored procedures, other services) aren't detected.

### Memory Pressure

**Symptom**: Application memory grows with cache usage.

**Solutions**:

- Reduce `DefaultExpiration` to evict entries sooner
- Use `ExcludeType<T>()` for entities with large result sets
- Switch to a distributed cache provider (Redis) to offload memory
- Monitor cache size with the cache provider's diagnostic tools

### Invalidation Delays

**Symptom**: Cache isn't invalidated immediately after `SaveChanges`.

**Cause**: Invalidation happens in `SavedChanges` (after commit). If using distributed cache (Redis), there's network latency for cache removal.

**Solutions**:

- Use shorter `DefaultExpiration` as a safety net
- For time-critical data, exclude the entity type from caching

## Related Documentation

- [Caching Overview](../../README.md#caching) - Main caching documentation
- [Issue #291](https://github.com/dlrivada/Encina/issues/291) - Implementation tracking
- [CHANGELOG](../../CHANGELOG.md) - Release notes

## Source Files

| File | Purpose |
|------|---------|
| `src/Encina.Caching/Abstractions/IQueryCacheKeyGenerator.cs` | Interface + `QueryCacheKey` record |
| `src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs` | EF Core interceptor |
| `src/Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs` | Default key generator |
| `src/Encina.EntityFrameworkCore/Caching/CachedDataReader.cs` | DbDataReader implementation |
| `src/Encina.EntityFrameworkCore/Caching/CachedQueryResult.cs` | Cached result model |
| `src/Encina.EntityFrameworkCore/Caching/SqlTableExtractor.cs` | SQL table name extraction |
| `src/Encina.EntityFrameworkCore/Caching/QueryCacheOptions.cs` | Configuration options |
| `src/Encina.EntityFrameworkCore/Extensions/QueryCachingExtensions.cs` | DI extensions |
