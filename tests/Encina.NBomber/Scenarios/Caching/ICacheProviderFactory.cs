using Encina.Caching;

namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Factory interface for creating cache providers in load testing scenarios.
/// </summary>
public interface ICacheProviderFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the cache provider (e.g., "memory", "redis", "hybrid").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the category of the cache provider.
    /// </summary>
    CacheProviderCategory Category { get; }

    /// <summary>
    /// Initializes the provider and any required resources.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a configured cache provider instance.
    /// </summary>
    /// <returns>The cache provider instance.</returns>
    ICacheProvider CreateCacheProvider();

    /// <summary>
    /// Gets a value indicating whether the provider is available for testing.
    /// </summary>
    /// <remarks>
    /// Redis-based providers may return false if Docker is not available.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the provider-specific options.
    /// </summary>
    CacheProviderOptions Options { get; }
}

/// <summary>
/// Categories of cache providers for load testing.
/// </summary>
public enum CacheProviderCategory
{
    /// <summary>In-memory caching (IMemoryCache).</summary>
    Memory,

    /// <summary>Redis-compatible distributed caching.</summary>
    Redis,

    /// <summary>Hybrid L1/L2 caching (HybridCache).</summary>
    Hybrid
}

/// <summary>
/// Configuration options for cache provider load testing.
/// </summary>
public sealed class CacheProviderOptions
{
    /// <summary>
    /// Gets or sets the default expiration time for cached items.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of items for memory cache size limits.
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the value size in bytes for load testing.
    /// </summary>
    public int ValueSizeBytes { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the Redis connection string (for Redis-based providers).
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Redis container image (e.g., "redis:7-alpine", "valkey/valkey:7.2").
    /// </summary>
    public string RedisImage { get; set; } = "redis:7-alpine";

    /// <summary>
    /// Gets or sets the key prefix for cache keys.
    /// </summary>
    public string KeyPrefix { get; set; } = "loadtest";

    /// <summary>
    /// Gets or sets whether to use pipelining for Redis operations.
    /// </summary>
    public bool UsePipelining { get; set; } = true;

    /// <summary>
    /// Gets or sets the L1 cache expiration for hybrid cache.
    /// </summary>
    public TimeSpan? L1CacheExpiration { get; set; } = TimeSpan.FromSeconds(30);
}
