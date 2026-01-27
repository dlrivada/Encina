using Encina.NBomber.Scenarios.Caching.Providers;

namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Registry for cache provider factories.
/// Provides lookup and creation of provider factories by name.
/// </summary>
public static class CacheProviderRegistry
{
    /// <summary>
    /// Known provider names.
    /// </summary>
    public static class ProviderNames
    {
        /// <summary>In-memory cache provider.</summary>
        public const string Memory = "memory";

        /// <summary>Redis cache provider.</summary>
        public const string Redis = "redis";

        /// <summary>Valkey cache provider (Redis-compatible).</summary>
        public const string Valkey = "redis-valkey";

        /// <summary>Garnet cache provider (Redis-compatible).</summary>
        public const string Garnet = "redis-garnet";

        /// <summary>Dragonfly cache provider (Redis-compatible).</summary>
        public const string Dragonfly = "redis-dragonfly";

        /// <summary>KeyDB cache provider (Redis-compatible).</summary>
        public const string KeyDB = "redis-keydb";

        /// <summary>Hybrid L1/L2 cache provider.</summary>
        public const string Hybrid = "hybrid";
    }

    /// <summary>
    /// Container images for Redis-compatible providers.
    /// </summary>
    public static class ContainerImages
    {
        /// <summary>Redis 7 Alpine image.</summary>
        public const string Redis = "redis:7-alpine";

        /// <summary>Valkey 7.2 image.</summary>
        public const string Valkey = "valkey/valkey:7.2";

        /// <summary>Microsoft Garnet image.</summary>
        public const string Garnet = "ghcr.io/microsoft/garnet:latest";

        /// <summary>Dragonfly image.</summary>
        public const string Dragonfly = "docker.dragonflydb.io/dragonflydb/dragonfly:latest";

        /// <summary>KeyDB image.</summary>
        public const string KeyDB = "eqalpha/keydb:latest";
    }

    /// <summary>
    /// Gets all registered provider names.
    /// </summary>
    /// <returns>Collection of provider names.</returns>
    public static IReadOnlyCollection<string> GetAllProviderNames()
    {
        return
        [
            ProviderNames.Memory,
            ProviderNames.Redis,
            ProviderNames.Valkey,
            ProviderNames.Garnet,
            ProviderNames.Dragonfly,
            ProviderNames.KeyDB,
            ProviderNames.Hybrid
        ];
    }

    /// <summary>
    /// Creates a cache provider factory for the specified provider name.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="configureOptions">Optional configuration delegate.</param>
    /// <returns>The cache provider factory.</returns>
    /// <exception cref="ArgumentException">Thrown if the provider name is not recognized.</exception>
    public static ICacheProviderFactory CreateFactory(
        string providerName,
        Action<CacheProviderOptions>? configureOptions = null)
    {
        return providerName.ToLowerInvariant() switch
        {
            ProviderNames.Memory => new MemoryCacheProviderFactory(configureOptions),
            ProviderNames.Redis => new RedisCacheProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.Redis;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.Valkey => new RedisCacheProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.Valkey;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.Garnet => new RedisCacheProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.Garnet;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.Dragonfly => new RedisCacheProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.Dragonfly;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.KeyDB => new RedisCacheProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.KeyDB;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.Hybrid => new HybridCacheProviderFactory(configureOptions),
            _ => throw new ArgumentException($"Unknown cache provider: {providerName}", nameof(providerName))
        };
    }

    /// <summary>
    /// Checks if a provider name is recognized.
    /// </summary>
    /// <param name="providerName">The provider name to check.</param>
    /// <returns>True if the provider is recognized; otherwise, false.</returns>
    public static bool IsKnownProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            ProviderNames.Memory or
            ProviderNames.Redis or
            ProviderNames.Valkey or
            ProviderNames.Garnet or
            ProviderNames.Dragonfly or
            ProviderNames.KeyDB or
            ProviderNames.Hybrid => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the category for a provider name.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The provider category.</returns>
    public static CacheProviderCategory GetCategory(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            ProviderNames.Memory => CacheProviderCategory.Memory,
            ProviderNames.Hybrid => CacheProviderCategory.Hybrid,
            _ when providerName.StartsWith("redis", StringComparison.OrdinalIgnoreCase) => CacheProviderCategory.Redis,
            _ => throw new ArgumentException($"Unknown cache provider: {providerName}", nameof(providerName))
        };
    }
}
