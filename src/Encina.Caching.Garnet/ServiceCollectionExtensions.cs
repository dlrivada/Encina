using StackExchange.Redis;

namespace Encina.Caching.Garnet;

/// <summary>
/// Extension methods for configuring Encina Garnet caching services.
/// </summary>
/// <remarks>
/// <para>
/// Microsoft Garnet is a high-performance cache-store from Microsoft Research.
/// It is fully compatible with the Redis wire protocol, so it uses the same
/// <see cref="RedisCacheProvider"/>, <see cref="RedisPubSubProvider"/>, and
/// <see cref="RedisDistributedLockProvider"/> implementations.
/// </para>
/// <para>
/// Garnet offers:
/// </para>
/// <list type="bullet">
/// <item><description>Extremely low latency and high throughput</description></item>
/// <item><description>Full Redis API compatibility</description></item>
/// <item><description>Written in C# for .NET integration</description></item>
/// <item><description>Storage tiering (memory + SSD)</description></item>
/// </list>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Garnet caching services with a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Garnet connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaCaching(options =>
    /// {
    ///     options.EnableQueryCaching = true;
    /// });
    ///
    /// // Garnet typically runs on port 6379 or 3278
    /// services.AddEncinaGarnetCache("localhost:3278");
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaGarnetCache(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddEncinaRedisCache(connectionString);
    }

    /// <summary>
    /// Adds Encina Garnet caching services with a connection string and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Garnet connection string.</param>
    /// <param name="configureCacheOptions">Configuration action for cache options.</param>
    /// <param name="configureLockOptions">Configuration action for lock options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaGarnetCache(
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
    /// Adds Encina Garnet caching services with an existing connection multiplexer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaGarnetCache(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        return services.AddEncinaRedisCache(connectionMultiplexer);
    }

    /// <summary>
    /// Adds Encina Garnet caching services with an existing connection multiplexer and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <param name="configureCacheOptions">Configuration action for cache options.</param>
    /// <param name="configureLockOptions">Configuration action for lock options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaGarnetCache(
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
