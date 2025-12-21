using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.Caching.Redis;

/// <summary>
/// Extension methods for configuring SimpleMediator Redis caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator Redis caching services with a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ICacheProvider"/> - Using <see cref="RedisCacheProvider"/></description></item>
    /// <item><description><see cref="IPubSubProvider"/> - Using <see cref="RedisPubSubProvider"/></description></item>
    /// <item><description><see cref="IDistributedLockProvider"/> - Using <see cref="RedisDistributedLockProvider"/></description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSimpleMediatorCaching(options =>
    /// {
    ///     options.EnableQueryCaching = true;
    ///     options.EnableCacheInvalidation = true;
    /// });
    ///
    /// services.AddSimpleMediatorRedisCache("localhost:6379");
    /// </code>
    /// </example>
    public static IServiceCollection AddSimpleMediatorRedisCache(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddSimpleMediatorRedisCacheCore(
            _ => ConnectionMultiplexer.Connect(connectionString),
            null,
            null);
    }

    /// <summary>
    /// Adds SimpleMediator Redis caching services with a connection string and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="configureCacheOptions">Configuration action for cache options.</param>
    /// <param name="configureLockOptions">Configuration action for lock options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorRedisCache(
        this IServiceCollection services,
        string connectionString,
        Action<RedisCacheOptions> configureCacheOptions,
        Action<RedisLockOptions> configureLockOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);
        ArgumentNullException.ThrowIfNull(configureLockOptions);

        return services.AddSimpleMediatorRedisCacheCore(
            _ => ConnectionMultiplexer.Connect(connectionString),
            configureCacheOptions,
            configureLockOptions);
    }

    /// <summary>
    /// Adds SimpleMediator Redis caching services with an existing connection multiplexer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorRedisCache(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        services.TryAddSingleton(connectionMultiplexer);
        return services.AddSimpleMediatorRedisCacheCore(
            _ => connectionMultiplexer,
            null,
            null);
    }

    /// <summary>
    /// Adds SimpleMediator Redis caching services with an existing connection multiplexer and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <param name="configureCacheOptions">Configuration action for cache options.</param>
    /// <param name="configureLockOptions">Configuration action for lock options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorRedisCache(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        Action<RedisCacheOptions> configureCacheOptions,
        Action<RedisLockOptions> configureLockOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(configureCacheOptions);
        ArgumentNullException.ThrowIfNull(configureLockOptions);

        services.TryAddSingleton(connectionMultiplexer);
        return services.AddSimpleMediatorRedisCacheCore(
            _ => connectionMultiplexer,
            configureCacheOptions,
            configureLockOptions);
    }

    private static IServiceCollection AddSimpleMediatorRedisCacheCore(
        this IServiceCollection services,
        Func<IServiceProvider, IConnectionMultiplexer> connectionFactory,
        Action<RedisCacheOptions>? configureCacheOptions,
        Action<RedisLockOptions>? configureLockOptions)
    {
        // Register the connection multiplexer
        services.TryAddSingleton(connectionFactory);

        // Configure options
        if (configureCacheOptions is not null)
        {
            services.Configure(configureCacheOptions);
        }
        else
        {
            services.TryAddSingleton(Options.Create(new RedisCacheOptions()));
        }

        if (configureLockOptions is not null)
        {
            services.Configure(configureLockOptions);
        }
        else
        {
            services.TryAddSingleton(Options.Create(new RedisLockOptions()));
        }

        // Register providers
        services.TryAddSingleton<ICacheProvider, RedisCacheProvider>();
        services.TryAddSingleton<IPubSubProvider, RedisPubSubProvider>();
        services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();

        return services;
    }
}
