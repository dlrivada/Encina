using Encina.Caching.Sharding.Configuration;
using Encina.Caching.Sharding.Services;
using Encina.Sharding;
using Encina.Sharding.Execution;
using Encina.Sharding.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Sharding;

/// <summary>
/// Extension methods for configuring sharding cache integration services.
/// </summary>
/// <remarks>
/// <para>
/// This extension decorates existing sharding services with caching capabilities.
/// It must be called <b>after</b> both <c>AddEncinaSharding</c> and <c>AddEncinaCaching</c>
/// have been registered.
/// </para>
/// <para>
/// All three components are opt-in and disabled by default:
/// <list type="bullet">
///   <item><description><b>Directory caching</b>: Wraps <see cref="IShardDirectoryStore"/> with an L1 cache.</description></item>
///   <item><description><b>Topology refresh</b>: Periodically refreshes <see cref="ShardTopology"/> via a background service.</description></item>
///   <item><description><b>Scatter-gather caching</b>: Wraps <see cref="IShardedQueryExecutor"/> with result caching.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 1. Register sharding
/// services.AddEncinaSharding&lt;Order&gt;(options => { ... });
///
/// // 2. Register caching
/// services.AddEncinaCaching(options => { ... });
/// services.AddEncinaRedisCache("localhost:6379");
///
/// // 3. Add sharding cache integration
/// services.AddEncinaShardingCaching(options =>
/// {
///     options.EnableDirectoryCaching = true;
///     options.EnableBackgroundRefresh = true;
///     options.EnableScatterGatherCaching = true;
/// });
/// </code>
/// </example>
public static class ShardingCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds sharding cache integration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for sharding cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaShardingCaching(
        this IServiceCollection services,
        Action<ShardingCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ShardingCacheOptions();
        configure?.Invoke(options);

        // Register options
        services.Configure<ShardingCacheOptions>(opt =>
        {
            opt.TopologyRefreshInterval = options.TopologyRefreshInterval;
            opt.TopologyCacheDuration = options.TopologyCacheDuration;
            opt.EnableBackgroundRefresh = options.EnableBackgroundRefresh;
            opt.EnableDirectoryCaching = options.EnableDirectoryCaching;
            opt.EnableScatterGatherCaching = options.EnableScatterGatherCaching;

            opt.DirectoryCache.CacheDuration = options.DirectoryCache.CacheDuration;
            opt.DirectoryCache.InvalidationStrategy = options.DirectoryCache.InvalidationStrategy;
            opt.DirectoryCache.KeyPrefix = options.DirectoryCache.KeyPrefix;
            opt.DirectoryCache.EnableDistributedInvalidation = options.DirectoryCache.EnableDistributedInvalidation;
            opt.DirectoryCache.InvalidationChannel = options.DirectoryCache.InvalidationChannel;

            opt.ScatterGatherCache.DefaultCacheDuration = options.ScatterGatherCache.DefaultCacheDuration;
            opt.ScatterGatherCache.MaxCachedResultSize = options.ScatterGatherCache.MaxCachedResultSize;
            opt.ScatterGatherCache.InvalidationChannel = options.ScatterGatherCache.InvalidationChannel;
            opt.ScatterGatherCache.EnableResultCaching = options.ScatterGatherCache.EnableResultCaching;
        });

        services.Configure<DirectoryCacheOptions>(opt =>
        {
            opt.CacheDuration = options.DirectoryCache.CacheDuration;
            opt.InvalidationStrategy = options.DirectoryCache.InvalidationStrategy;
            opt.KeyPrefix = options.DirectoryCache.KeyPrefix;
            opt.EnableDistributedInvalidation = options.DirectoryCache.EnableDistributedInvalidation;
            opt.InvalidationChannel = options.DirectoryCache.InvalidationChannel;
        });

        services.Configure<ScatterGatherCacheOptions>(opt =>
        {
            opt.DefaultCacheDuration = options.ScatterGatherCache.DefaultCacheDuration;
            opt.MaxCachedResultSize = options.ScatterGatherCache.MaxCachedResultSize;
            opt.InvalidationChannel = options.ScatterGatherCache.InvalidationChannel;
            opt.EnableResultCaching = options.ScatterGatherCache.EnableResultCaching;
        });

        // Component 1: Directory caching decorator
        if (options.EnableDirectoryCaching)
        {
            RegisterDirectoryCacheDecorator(services);
        }

        // Component 2: Topology background refresh
        if (options.EnableBackgroundRefresh)
        {
            RegisterTopologyRefreshServices(services);
        }

        // Component 3: Scatter-gather result caching decorator
        if (options.EnableScatterGatherCaching)
        {
            RegisterScatterGatherCacheDecorator(services);
        }

        return services;
    }

    private static void RegisterDirectoryCacheDecorator(IServiceCollection services)
    {
        // Find and replace the existing IShardDirectoryStore registration
        var existingDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IShardDirectoryStore));

        if (existingDescriptor is null)
        {
            // No directory store registered; nothing to decorate
            return;
        }

        services.Remove(existingDescriptor);

        services.AddSingleton<IShardDirectoryStore>(sp =>
        {
            var inner = ResolveFromDescriptor<IShardDirectoryStore>(sp, existingDescriptor);
            var cache = sp.GetRequiredService<ICacheProvider>();
            var opts = sp.GetRequiredService<IOptions<DirectoryCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<CachedShardDirectoryStore>>();
            var pubSub = sp.GetService<IPubSubProvider>();

            return new CachedShardDirectoryStore(inner, cache, opts, logger, pubSub);
        });

        // Also register the concrete type so the hosted service can resolve it
        services.AddSingleton(sp =>
            (CachedShardDirectoryStore)sp.GetRequiredService<IShardDirectoryStore>());
    }

    private static void RegisterTopologyRefreshServices(IServiceCollection services)
    {
        // Register topology source (default: static, can be overridden by user)
        services.TryAddSingleton<IShardTopologySource>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            return new StaticShardTopologySource(topology);
        });

        // Register cached topology provider
        services.AddSingleton<CachedShardTopologyProvider>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            var source = sp.GetRequiredService<IShardTopologySource>();
            var cache = sp.GetRequiredService<ICacheProvider>();
            var opts = sp.GetRequiredService<IOptions<ShardingCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<CachedShardTopologyProvider>>();
            var notifier = sp.GetService<IShardTopologyChangeNotifier>();

            return new CachedShardTopologyProvider(topology, source, cache, opts, logger, notifier);
        });

        // Replace the IShardTopologyProvider registration with the cached version
        var existingProvider = services.FirstOrDefault(
            d => d.ServiceType == typeof(IShardTopologyProvider));

        if (existingProvider is not null)
        {
            services.Remove(existingProvider);
        }

        services.AddSingleton<IShardTopologyProvider>(sp =>
            sp.GetRequiredService<CachedShardTopologyProvider>());

        // Register the background refresh hosted service
        services.AddHostedService<TopologyRefreshHostedService>();
    }

    private static void RegisterScatterGatherCacheDecorator(IServiceCollection services)
    {
        // Find and replace the existing IShardedQueryExecutor registration
        var existingDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IShardedQueryExecutor));

        if (existingDescriptor is null)
        {
            // No query executor registered; nothing to decorate
            return;
        }

        services.Remove(existingDescriptor);

        services.AddSingleton<IShardedQueryExecutor>(sp =>
        {
            var inner = ResolveFromDescriptor<IShardedQueryExecutor>(sp, existingDescriptor);
            var cache = sp.GetRequiredService<ICacheProvider>();
            var opts = sp.GetRequiredService<IOptions<ScatterGatherCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<CachedShardedQueryExecutor>>();
            var pubSub = sp.GetService<IPubSubProvider>();

            return new CachedShardedQueryExecutor(inner, cache, opts, logger, pubSub);
        });
    }

    private static T ResolveFromDescriptor<T>(IServiceProvider sp, ServiceDescriptor descriptor)
        where T : class
    {
        if (descriptor.ImplementationInstance is T instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (T)descriptor.ImplementationFactory(sp);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (T)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
        }

        throw new InvalidOperationException(
            $"Cannot resolve {typeof(T).Name} from existing service descriptor. " +
            "Ensure the service is registered before calling AddEncinaShardingCaching.");
    }
}
