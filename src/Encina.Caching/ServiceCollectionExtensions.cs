using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Caching;

/// <summary>
/// Extension methods for configuring Encina caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina caching services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the caching abstractions and pipeline behaviors.
    /// You must also register a cache provider implementation:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>AddEncinaMemoryCache()</c> - In-memory caching</description></item>
    /// <item><description><c>AddEncinaRedisCache()</c> - Redis caching</description></item>
    /// <item><description><c>AddEncinaGarnetCache()</c> - Garnet caching</description></item>
    /// <item><description><c>AddEncinaValkeyCache()</c> - Valkey caching</description></item>
    /// <item><description><c>AddEncinaDragonflyCache()</c> - Dragonfly caching</description></item>
    /// <item><description><c>AddEncinaKeyDBCache()</c> - KeyDB caching</description></item>
    /// <item><description><c>AddEncinaNCacheCache()</c> - NCache caching</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaCaching(options =>
    /// {
    ///     options.EnableQueryCaching = true;
    ///     options.EnableCacheInvalidation = true;
    ///     options.DefaultDuration = TimeSpan.FromMinutes(10);
    /// });
    ///
    /// // Then add a provider
    /// services.AddEncinaRedisCache("localhost:6379");
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaCaching(
        this IServiceCollection services,
        Action<CachingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        var options = new CachingOptions();
        configure?.Invoke(options);
        services.Configure<CachingOptions>(opt =>
        {
            opt.EnableQueryCaching = options.EnableQueryCaching;
            opt.EnableCacheInvalidation = options.EnableCacheInvalidation;
            opt.EnableDistributedIdempotency = options.EnableDistributedIdempotency;
            opt.EnableDistributedLocks = options.EnableDistributedLocks;
            opt.EnablePubSubInvalidation = options.EnablePubSubInvalidation;
            opt.DefaultDuration = options.DefaultDuration;
            opt.DefaultPriority = options.DefaultPriority;
            opt.KeyPrefix = options.KeyPrefix;
            opt.InvalidationChannel = options.InvalidationChannel;
            opt.IdempotencyKeyPrefix = options.IdempotencyKeyPrefix;
            opt.IdempotencyTtl = options.IdempotencyTtl;
            opt.LockKeyPrefix = options.LockKeyPrefix;
            opt.DefaultLockExpiry = options.DefaultLockExpiry;
            opt.DefaultLockWait = options.DefaultLockWait;
            opt.DefaultLockRetry = options.DefaultLockRetry;
            opt.ThrowOnCacheErrors = options.ThrowOnCacheErrors;
            opt.SerializerOptions = options.SerializerOptions;
        });

        // Register key generator
        services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

        // Register pipeline behaviors
        if (options.EnableQueryCaching)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueryCachingPipelineBehavior<,>));
        }

        if (options.EnableCacheInvalidation)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationPipelineBehavior<,>));
        }

        if (options.EnableDistributedIdempotency)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DistributedIdempotencyPipelineBehavior<,>));
        }

        return services;
    }

    /// <summary>
    /// Adds a custom cache key generator.
    /// </summary>
    /// <typeparam name="TGenerator">The type of the cache key generator.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCacheKeyGenerator<TGenerator>(this IServiceCollection services)
        where TGenerator : class, ICacheKeyGenerator
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<ICacheKeyGenerator>();
        services.AddSingleton<ICacheKeyGenerator, TGenerator>();

        return services;
    }

    /// <summary>
    /// Adds cache configuration for a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to configure caching for.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddCacheConfiguration&lt;GetProductQuery&gt;(config =>
    /// {
    ///     config.Duration = TimeSpan.FromMinutes(10);
    ///     config.VaryByUser = false;
    ///     config.KeyGenerator = (query, ctx) => $"product:{query.ProductId}";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddCacheConfiguration<TRequest>(
        this IServiceCollection services,
        Action<CacheConfiguration<TRequest>> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new CacheConfiguration<TRequest>();
        configure(config);
        services.AddSingleton<ICacheConfiguration<TRequest>>(config);

        return services;
    }
}
