using StackExchange.Redis;

namespace Encina.Caching.Valkey;

/// <summary>
/// Extension methods for configuring Encina Valkey caching services.
/// </summary>
/// <remarks>
/// <para>
/// Valkey is an open-source, high-performance key/value datastore, a fork of Redis
/// maintained by AWS, Google, and the Linux Foundation.
/// It is fully compatible with the Redis wire protocol, so it uses the same
/// <see cref="RedisCacheProvider"/>, <see cref="RedisPubSubProvider"/>, and
/// <see cref="RedisDistributedLockProvider"/> implementations.
/// </para>
/// <para>
/// Valkey offers:
/// </para>
/// <list type="bullet">
/// <item><description>Full Redis API compatibility</description></item>
/// <item><description>Open governance by Linux Foundation</description></item>
/// <item><description>Continued open-source development</description></item>
/// <item><description>Drop-in replacement for Redis</description></item>
/// </list>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Valkey caching services with a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Valkey connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaCaching(options =>
    /// {
    ///     options.EnableQueryCaching = true;
    /// });
    ///
    /// services.AddEncinaValkeyCache("localhost:6379");
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaValkeyCache(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddEncinaRedisCache(connectionString);
    }

    /// <summary>
    /// Adds Encina Valkey caching services with a connection string and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Valkey connection string.</param>
    /// <param name="configureCacheOptions">Configuration action for cache options.</param>
    /// <param name="configureLockOptions">Configuration action for lock options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaValkeyCache(
        this IServiceCollection services,
        string connectionString,
        Action<RedisCacheOptions> configureCacheOptions,
        Action<RedisLockOptions> configureLockOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);
        ArgumentNullException.ThrowIfNull(configureLockOptions);

        return services.AddEncinaRedisCache(connectionString, configureCacheOptions, configureLockOptions);
    }

    /// <summary>
    /// Adds Encina Valkey caching services with an existing connection multiplexer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaValkeyCache(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        return services.AddEncinaRedisCache(connectionMultiplexer);
    }

    /// <summary>
    /// Adds Encina Valkey caching services with an existing connection multiplexer and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <param name="configureCacheOptions">Configuration action for cache options.</param>
    /// <param name="configureLockOptions">Configuration action for lock options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaValkeyCache(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        Action<RedisCacheOptions> configureCacheOptions,
        Action<RedisLockOptions> configureLockOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);
        ArgumentNullException.ThrowIfNull(configureLockOptions);

        return services.AddEncinaRedisCache(connectionMultiplexer, configureCacheOptions, configureLockOptions);
    }
}
