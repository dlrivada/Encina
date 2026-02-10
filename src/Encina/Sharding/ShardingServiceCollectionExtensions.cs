using Encina.Sharding.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Sharding;

/// <summary>
/// Extension methods for configuring Encina database sharding services.
/// </summary>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina sharding services for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for sharding options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><see cref="ShardingOptions{TEntity}"/> - Configuration via IOptions pattern</item>
    /// <item><see cref="ShardTopology"/> - The shard topology for this entity type</item>
    /// <item><see cref="IShardRouter"/> - The configured routing strategy</item>
    /// <item><see cref="IShardRouter{TEntity}"/> - Entity-aware router with shard key extraction</item>
    /// </list>
    /// </para>
    /// <para>
    /// Startup validation ensures all shard IDs have connection strings and a routing strategy is configured.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =>
    /// {
    ///     options.UseHashRouting(hash => hash.VirtualNodesPerShard = 200)
    ///         .AddShard("shard-0", "Server=shard0;Database=Orders;...")
    ///         .AddShard("shard-1", "Server=shard1;Database=Orders;...")
    ///         .AddShard("shard-2", "Server=shard2;Database=Orders;...")
    ///         .AddShard("shard-3", "Server=shard3;Database=Orders;...");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaSharding<TEntity>(
        this IServiceCollection services,
        Action<ShardingOptions<TEntity>> configure)
        where TEntity : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ShardingOptions<TEntity>();
        configure(options);

        ValidateOptions(options);

        var topology = options.BuildTopology();
        var router = options.BuildRouter(topology);

        // Register options
        services.Configure<ShardingOptions<TEntity>>(opt =>
        {
            foreach (var shard in options.Shards)
            {
                opt.AddShard(shard.Key, shard.Value.ConnectionString, shard.Value.Weight, shard.Value.IsActive);
            }

            opt.ScatterGather.MaxParallelism = options.ScatterGather.MaxParallelism;
            opt.ScatterGather.Timeout = options.ScatterGather.Timeout;
            opt.ScatterGather.AllowPartialResults = options.ScatterGather.AllowPartialResults;
        });

        // Register scatter-gather options
        services.Configure<ScatterGatherOptions>(opt =>
        {
            opt.MaxParallelism = options.ScatterGather.MaxParallelism;
            opt.Timeout = options.ScatterGather.Timeout;
            opt.AllowPartialResults = options.ScatterGather.AllowPartialResults;
        });

        // Register topology as singleton (keyed by entity type via wrapping)
        services.AddSingleton(topology);

        // Register topology provider (allows cached implementations to replace this)
        services.TryAddSingleton<IShardTopologyProvider>(new DefaultShardTopologyProvider(topology));

        // Register the base router
        services.TryAddSingleton<IShardRouter>(router);

        // Register the entity-aware router
        services.TryAddSingleton<IShardRouter<TEntity>>(new EntityShardRouter<TEntity>(router));

        return services;
    }

    private static void ValidateOptions<TEntity>(ShardingOptions<TEntity> options)
        where TEntity : notnull
    {
        if (options.Shards.Count == 0)
        {
            throw new InvalidOperationException(
                $"No shards configured for entity type '{typeof(TEntity).Name}'. " +
                "Call AddShard() at least once.");
        }

        if (options.RouterFactory is null)
        {
            throw new InvalidOperationException(
                $"No routing strategy configured for entity type '{typeof(TEntity).Name}'. " +
                "Call UseHashRouting(), UseRangeRouting(), UseDirectoryRouting(), UseGeoRouting(), or UseCustomRouting().");
        }

        foreach (var shard in options.Shards.Values)
        {
            if (string.IsNullOrWhiteSpace(shard.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"Shard '{shard.ShardId}' for entity type '{typeof(TEntity).Name}' has no connection string.");
            }
        }
    }
}
