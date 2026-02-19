# Secrets Management -- Caching Strategy

This guide covers how the `CachedSecretProvider` decorator works, what gets cached and what does not, cache key formats, invalidation behavior, and how to tune the cache for your workload.

## Overview

`CachedSecretProvider` is a transparent decorator that wraps any `ISecretProvider` implementation with in-memory caching via `IMemoryCache`. It follows Encina's decorator pattern: register your provider first, then add caching on top.

```text
Caller --> CachedSecretProvider --> Inner Provider (Azure/AWS/Vault/Google)
```

All caching decisions are ROP-aware: only `Right` (success) results are cached. `Left` (error) results pass through uncached, allowing the next call to retry the operation against the real provider.

## Registration

### Default Settings

```csharp
// Register provider
builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
});

// Add caching with defaults (TTL: 5 minutes, Enabled: true)
builder.Services.AddEncinaSecretsCaching();
```

### Custom TTL

```csharp
builder.Services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(15);
    options.Enabled = true;
});
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DefaultTtl` | `TimeSpan` | 5 minutes | Time-to-live for cached entries |
| `Enabled` | `bool` | `true` | Master switch; when `false`, all calls pass through to the inner provider |

## What Gets Cached

### Cached Operations (Read)

| Operation | Cache Key Format | Notes |
|-----------|-----------------|-------|
| `GetSecretAsync(name)` | `encina:secrets:{name}` | Caches the full `Secret` record |
| `GetSecretVersionAsync(name, version)` | `encina:secrets:v:{name}:{version}` | Caches the versioned `Secret` record |
| `ExistsAsync(name)` | `encina:secrets:exists:{name}` | Caches the `bool` result |

### Never Cached

| Operation | Reason |
|-----------|--------|
| `ListSecretsAsync()` | The result set can change frequently; caching would mask new or removed secrets |
| `SetSecretAsync(name, value, ...)` | Write operation; triggers cache invalidation instead |
| `DeleteSecretAsync(name)` | Write operation; triggers cache invalidation instead |
| Any `Left` (error) result | Errors should not be cached so the next attempt can retry |

## ROP-Aware Caching

The cache layer respects the `Either<EncinaError, T>` pattern throughout:

**On read (cache miss):**

```text
1. Call inner provider
2. Result is Right?  --> Cache the value with TTL
   Result is Left?   --> Do NOT cache; return error as-is
```

**On write (set/delete):**

```text
1. Call inner provider
2. Result is Right?  --> Invalidate related cache entries
   Result is Left?   --> Do NOT invalidate; return error as-is
```

This means:

- A transient network failure does not poison the cache
- The next `GetSecretAsync` call after a failure goes straight to the provider
- A successful `SetSecretAsync` immediately evicts the old cached value

## Cache Invalidation

When `SetSecretAsync` or `DeleteSecretAsync` succeeds (`Right`), the following entries are removed:

| Invalidated Key | Example |
|-----------------|---------|
| `encina:secrets:{name}` | `encina:secrets:api-key` |
| `encina:secrets:exists:{name}` | `encina:secrets:exists:api-key` |

### What Is NOT Invalidated

Versioned entries (`encina:secrets:v:{name}:{version}`) are **not** individually invalidated on set/delete. This is by design: `IMemoryCache` does not support key enumeration, so there is no efficient way to find all cached versions of a given secret. Versioned entries expire naturally when their TTL elapses.

**Practical impact:** If you update secret `api-key` and immediately call `GetSecretVersionAsync("api-key", "2")`, you may get a stale cached result for up to `DefaultTtl`. For most applications this is acceptable. If you need immediate consistency for versioned reads after writes, either:

- Reduce `DefaultTtl` to a shorter interval
- Disable caching (`Enabled = false`) for that specific flow

## Disabled Cache Mode

When `Enabled = false`, `CachedSecretProvider` acts as a pure passthrough:

```csharp
builder.Services.AddEncinaSecretsCaching(options =>
{
    options.Enabled = false;
});
```

In this mode:

- All read operations go directly to the inner provider
- Write operations do not attempt cache invalidation
- The `IMemoryCache` instance is still registered but unused
- No cache hit/miss log messages are emitted

This is useful for development environments where you want to always see the latest vault state, or for debugging cache-related issues.

## Cache Hit/Miss Logging

`CachedSecretProvider` logs at `Debug` level:

| Event | Log Message |
|-------|-------------|
| Cache hit | `Cache hit for secret '{SecretName}'.` |
| Cache miss | `Cache miss for secret '{SecretName}'.` |
| Invalidation after set | `Cache invalidated for secret '{SecretName}' after set.` |
| Invalidation after delete | `Cache invalidated for secret '{SecretName}' after delete.` |

Enable `Debug` logging for `Encina.Secrets.CachedSecretProvider` to see these messages:

```json
{
  "Logging": {
    "LogLevel": {
      "Encina.Secrets.CachedSecretProvider": "Debug"
    }
  }
}
```

## Performance Considerations

### Trade-off: Freshness vs. Latency

| TTL | Freshness | Latency | Vault Calls |
|-----|-----------|---------|-------------|
| 1 minute | High | Higher (frequent misses) | Many |
| 5 minutes (default) | Balanced | Low | Moderate |
| 30 minutes | Lower | Lowest | Few |
| 1 hour+ | Low | Lowest | Minimal |

### Choosing the Right TTL

| Scenario | Recommended TTL | Reasoning |
|----------|-----------------|-----------|
| Connection strings | 10-30 minutes | Rarely change; high call frequency |
| API keys (rotated hourly) | 5 minutes | Balance between freshness and load |
| Feature flags | 1-5 minutes | Need relatively quick propagation |
| Encryption keys | 30-60 minutes | Change infrequently; critical path performance |
| Development/debugging | Disabled | Always see latest values |

### Memory Usage

Each cached `Secret` record occupies roughly the size of its string properties. For a typical application with 10-50 secrets, memory usage from the cache is negligible (under 100 KB).

## Combining with Instrumentation

When both caching and instrumentation are enabled, registration order determines the decoration chain:

```csharp
// Provider -> Caching -> Instrumentation
builder.Services.AddEncinaKeyVaultSecrets(options => { ... });
builder.Services.AddEncinaSecretsCaching(options => { ... });
builder.Services.AddEncinaSecretsInstrumentation(options => { ... });
```

This produces: `Caller -> Instrumented -> Cached -> KeyVault`

Instrumentation captures metrics for both cache hits (fast) and cache misses (which call the real vault). This gives you visibility into your actual cache hit ratio.

## Full Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "eu-west-1";
});

builder.Services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(15);
    options.Enabled = true;
});

var app = builder.Build();

app.MapGet("/secret/{name}", async (string name, ISecretProvider provider) =>
{
    var result = await provider.GetSecretAsync(name);
    return result.Match(
        Right: secret => Results.Ok(new { secret.Name, secret.Version }),
        Left: error => Results.NotFound(new { error.Message })
    );
});

app.Run();
```

The first request for a secret name hits AWS Secrets Manager. Subsequent requests within the 15-minute TTL are served from memory.

## Related

- [Basic Setup](secrets-basic-setup.md)
- [IConfiguration Integration](secrets-configuration-integration.md)
- [Error Handling with ROP](secrets-error-handling.md)
