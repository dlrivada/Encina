using Encina.NBomber.Scenarios.Locking.Providers;

namespace Encina.NBomber.Scenarios.Locking;

/// <summary>
/// Registry for lock provider factories.
/// Supports multiple provider types for load testing.
/// </summary>
public static class LockProviderRegistry
{
    /// <summary>
    /// Provider name constants.
    /// </summary>
    public static class ProviderNames
    {
        /// <summary>In-memory lock provider.</summary>
        public const string InMemory = "inmemory";

        /// <summary>Redis lock provider.</summary>
        public const string Redis = "redis";

        /// <summary>SQL Server lock provider.</summary>
        public const string SqlServer = "sqlserver";

        /// <summary>Valkey (Redis-compatible) lock provider.</summary>
        public const string Valkey = "valkey";

        /// <summary>Garnet (Redis-compatible) lock provider.</summary>
        public const string Garnet = "garnet";

        /// <summary>Dragonfly (Redis-compatible) lock provider.</summary>
        public const string Dragonfly = "dragonfly";

        /// <summary>KeyDB (Redis-compatible) lock provider.</summary>
        public const string KeyDB = "keydb";
    }

    /// <summary>
    /// Container images for Redis-compatible providers.
    /// </summary>
    public static class ContainerImages
    {
        /// <summary>Official Redis image.</summary>
        public const string Redis = "redis:7-alpine";

        /// <summary>Valkey image (Redis-compatible).</summary>
        public const string Valkey = "valkey/valkey:8-alpine";

        /// <summary>Microsoft Garnet image (Redis-compatible).</summary>
        public const string Garnet = "ghcr.io/microsoft/garnet:latest";

        /// <summary>Dragonfly image (Redis-compatible).</summary>
        public const string Dragonfly = "docker.dragonflydb.io/dragonflydb/dragonfly:latest";

        /// <summary>KeyDB image (Redis-compatible).</summary>
        public const string KeyDB = "eqalpha/keydb:latest";

        /// <summary>SQL Server image.</summary>
        public const string SqlServer = "mcr.microsoft.com/mssql/server:2022-latest";
    }

    /// <summary>
    /// Gets all supported provider names.
    /// </summary>
    public static IReadOnlyList<string> SupportedProviders =>
    [
        ProviderNames.InMemory,
        ProviderNames.Redis,
        ProviderNames.SqlServer,
        ProviderNames.Valkey,
        ProviderNames.Garnet,
        ProviderNames.Dragonfly,
        ProviderNames.KeyDB
    ];

    /// <summary>
    /// Gets Redis-compatible provider names (support the same protocol).
    /// </summary>
    public static IReadOnlyList<string> RedisCompatibleProviders =>
    [
        ProviderNames.Redis,
        ProviderNames.Valkey,
        ProviderNames.Garnet,
        ProviderNames.Dragonfly,
        ProviderNames.KeyDB
    ];

    /// <summary>
    /// Creates a lock provider factory for the specified provider name.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    /// <returns>A configured lock provider factory.</returns>
    /// <exception cref="ArgumentException">Thrown if the provider name is not supported.</exception>
    public static ILockProviderFactory CreateFactory(
        string providerName,
        Action<LockProviderOptions>? configureOptions = null)
    {
        return providerName.ToLowerInvariant() switch
        {
            ProviderNames.InMemory => new InMemoryLockProviderFactory(configureOptions),
            ProviderNames.Redis => new RedisLockProviderFactory(configureOptions),
            ProviderNames.SqlServer => new SqlServerLockProviderFactory(configureOptions),
            ProviderNames.Valkey => new RedisLockProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.Valkey;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.Garnet => new RedisLockProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.Garnet;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.Dragonfly => new RedisLockProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.Dragonfly;
                configureOptions?.Invoke(options);
            }),
            ProviderNames.KeyDB => new RedisLockProviderFactory(options =>
            {
                options.RedisImage = ContainerImages.KeyDB;
                configureOptions?.Invoke(options);
            }),
            _ => throw new ArgumentException($"Unsupported lock provider: {providerName}. " +
                $"Supported providers: {string.Join(", ", SupportedProviders)}", nameof(providerName))
        };
    }

    /// <summary>
    /// Gets the container image for a Redis-compatible provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The container image name.</returns>
    public static string GetRedisImage(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            ProviderNames.Redis => ContainerImages.Redis,
            ProviderNames.Valkey => ContainerImages.Valkey,
            ProviderNames.Garnet => ContainerImages.Garnet,
            ProviderNames.Dragonfly => ContainerImages.Dragonfly,
            ProviderNames.KeyDB => ContainerImages.KeyDB,
            _ => ContainerImages.Redis
        };
    }

    /// <summary>
    /// Determines if a provider uses the Redis protocol.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>True if the provider uses Redis protocol.</returns>
    public static bool IsRedisCompatible(string providerName)
    {
        return RedisCompatibleProviders.Contains(providerName.ToLowerInvariant());
    }
}
