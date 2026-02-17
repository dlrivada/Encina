using Encina.Sharding.Health;
using Encina.Sharding.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Sharding.Diagnostics;

/// <summary>
/// Extension methods for configuring sharding observability services.
/// </summary>
/// <remarks>
/// <para>
/// This extension adds OpenTelemetry-based metrics and tracing for sharding operations.
/// It must be called <b>after</b> <c>AddEncinaSharding</c> has been registered.
/// </para>
/// <para>
/// All components are opt-in via <see cref="ShardingMetricsOptions"/> and enabled by default:
/// <list type="bullet">
///   <item><description><b>Routing metrics</b>: Counters and histograms for shard routing decisions.</description></item>
///   <item><description><b>Scatter-gather metrics</b>: Duration histograms, partial failure counters, active query gauges.</description></item>
///   <item><description><b>Health metrics</b>: Per-shard health status and connection pool gauges.</description></item>
///   <item><description><b>Distributed tracing</b>: Activities for routing, scatter-gather, and per-shard queries.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 1. Register sharding
/// services.AddEncinaSharding&lt;Order&gt;(options => { ... });
///
/// // 2. Add sharding metrics and tracing
/// services.AddEncinaShardingMetrics();
///
/// // 3. Or with configuration
/// services.AddEncinaShardingMetrics(options =>
/// {
///     options.EnableHealthMetrics = false;
///     options.HealthCheckInterval = TimeSpan.FromSeconds(60);
/// });
/// </code>
/// </example>
public static class ShardingDiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds sharding metrics and tracing services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for sharding metrics options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaShardingMetrics(
        this IServiceCollection services,
        Action<ShardingMetricsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ShardingMetricsOptions();
        configure?.Invoke(options);

        services.Configure<ShardingMetricsOptions>(opt =>
        {
            opt.HealthCheckInterval = options.HealthCheckInterval;
            opt.EnableRoutingMetrics = options.EnableRoutingMetrics;
            opt.EnableScatterGatherMetrics = options.EnableScatterGatherMetrics;
            opt.EnableHealthMetrics = options.EnableHealthMetrics;
            opt.EnableReadWriteMetrics = options.EnableReadWriteMetrics;
            opt.EnableTracing = options.EnableTracing;
        });

        // Register routing metrics (used by both routing and scatter-gather)
        if (options.EnableRoutingMetrics || options.EnableScatterGatherMetrics)
        {
            services.TryAddSingleton<ShardRoutingMetrics>();
        }

        // Decorate IShardRouter with instrumented version for routing metrics
        if (options.EnableRoutingMetrics)
        {
            DecorateShardRouter(services);
        }

        // Register per-shard health metrics
        if (options.EnableHealthMetrics)
        {
            services.TryAddSingleton<ShardedDatabasePoolMetrics>();
        }

        // Register read/write separation metrics
        if (options.EnableReadWriteMetrics)
        {
            services.TryAddSingleton<ShardedReadWriteMetrics>();
        }

        return services;
    }

    /// <summary>
    /// Adds per-shard health metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This is a convenience method that only registers health metrics without routing
    /// or scatter-gather instrumentation. Requires <see cref="IShardedDatabaseHealthMonitor"/>
    /// to be registered.
    /// </remarks>
    public static IServiceCollection AddEncinaShardingHealthMetrics(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ShardedDatabasePoolMetrics>();

        return services;
    }

    private static void DecorateShardRouter(IServiceCollection services)
    {
        var existingDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IShardRouter));

        if (existingDescriptor is null)
        {
            // No router registered; nothing to decorate
            return;
        }

        services.Remove(existingDescriptor);

        services.AddSingleton<IShardRouter>(sp =>
        {
            var inner = ResolveFromDescriptor<IShardRouter>(sp, existingDescriptor);
            var metrics = sp.GetRequiredService<ShardRoutingMetrics>();
            var routerType = DetectRouterType(inner);

            return new InstrumentedShardRouter(inner, metrics, routerType);
        });
    }

    private static string DetectRouterType(IShardRouter router) => router switch
    {
        HashShardRouter => "hash",
        RangeShardRouter => "range",
        DirectoryShardRouter => "directory",
        GeoShardRouter => "geo",
        _ => "custom"
    };

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
            "Ensure the service is registered before calling AddEncinaShardingMetrics.");
    }
}
