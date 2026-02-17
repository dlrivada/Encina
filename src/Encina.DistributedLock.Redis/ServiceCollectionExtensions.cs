using Encina.DistributedLock.Redis.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.DistributedLock.Redis;

/// <summary>
/// Extension methods for configuring Encina Redis distributed lock services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Redis distributed lock services with a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="IDistributedLockProvider"/> using Redis.
    /// </para>
    /// <para>
    /// This provider is wire-compatible with Redis, Garnet, Valkey, Dragonfly, and KeyDB.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDistributedLockRedis("localhost:6379");
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDistributedLockRedis(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddEncinaDistributedLockRedisCore(
            _ => ConnectionMultiplexer.Connect(connectionString),
            null);
    }

    /// <summary>
    /// Adds Encina Redis distributed lock services with a connection string and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDistributedLockRedis(
        this IServiceCollection services,
        string connectionString,
        Action<RedisLockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddEncinaDistributedLockRedisCore(
            _ => ConnectionMultiplexer.Connect(connectionString),
            configure);
    }

    /// <summary>
    /// Adds Encina Redis distributed lock services with an existing connection multiplexer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDistributedLockRedis(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        services.TryAddSingleton(connectionMultiplexer);
        return services.AddEncinaDistributedLockRedisCore(
            _ => connectionMultiplexer,
            null);
    }

    /// <summary>
    /// Adds Encina Redis distributed lock services with an existing connection multiplexer and options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionMultiplexer">The existing connection multiplexer.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDistributedLockRedis(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        Action<RedisLockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton(connectionMultiplexer);
        return services.AddEncinaDistributedLockRedisCore(
            _ => connectionMultiplexer,
            configure);
    }

    private static IServiceCollection AddEncinaDistributedLockRedisCore(
        this IServiceCollection services,
        Func<IServiceProvider, IConnectionMultiplexer> connectionFactory,
        Action<RedisLockOptions>? configure)
    {
        // Register TimeProvider
        services.TryAddSingleton(TimeProvider.System);

        // Register the connection multiplexer
        services.TryAddSingleton(connectionFactory);

        // Configure options
        var options = new RedisLockOptions();
        if (configure is not null)
        {
            configure(options);
            services.Configure(configure);
        }
        else
        {
            services.TryAddSingleton(Options.Create(options));
        }

        // Register provider
        services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();

        // Register health check if enabled
        services.RegisterHealthCheck(options);

        return services;
    }

    /// <summary>
    /// Registers health check for Redis distributed lock if enabled in options.
    /// </summary>
    internal static IServiceCollection RegisterHealthCheck(
        this IServiceCollection services,
        RedisLockOptions options)
    {
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, RedisDistributedLockHealthCheck>();
        }

        return services;
    }
}
