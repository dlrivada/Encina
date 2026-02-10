using Encina.Sharding.Routing;

namespace Encina.Sharding.Configuration;

/// <summary>
/// Configuration options for database sharding of a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type being sharded.</typeparam>
/// <remarks>
/// <para>
/// Configure sharding when calling <c>AddEncinaSharding&lt;TEntity&gt;()</c>:
/// </para>
/// <code>
/// services.AddEncinaSharding&lt;Order&gt;(options =>
/// {
///     options.UseHashRouting()
///         .AddShard("shard-0", "Server=shard0;Database=Orders;...")
///         .AddShard("shard-1", "Server=shard1;Database=Orders;...");
/// });
/// </code>
/// </remarks>
public sealed class ShardingOptions<TEntity>
    where TEntity : notnull
{
    private readonly Dictionary<string, ShardInfo> _shards = new(StringComparer.OrdinalIgnoreCase);
    private Func<ShardTopology, IShardRouter>? _routerFactory;

    /// <summary>
    /// Gets the scatter-gather options for cross-shard queries.
    /// </summary>
    public ScatterGatherOptions ScatterGather { get; } = new();

    /// <summary>
    /// Gets the configured shards.
    /// </summary>
    internal IReadOnlyDictionary<string, ShardInfo> Shards => _shards;

    /// <summary>
    /// Gets the router factory, or null if no routing strategy has been configured.
    /// </summary>
    internal Func<ShardTopology, IShardRouter>? RouterFactory => _routerFactory;

    /// <summary>
    /// Adds a shard to the configuration.
    /// </summary>
    /// <param name="shardId">The unique shard identifier.</param>
    /// <param name="connectionString">The database connection string for this shard.</param>
    /// <param name="weight">Relative weight for load distribution (default 1).</param>
    /// <param name="isActive">Whether the shard is active (default true).</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> AddShard(
        string shardId,
        string connectionString,
        int weight = 1,
        bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(connectionString);

        _shards[shardId] = new ShardInfo(shardId, connectionString, weight, isActive);
        return this;
    }

    /// <summary>
    /// Configures hash-based routing using consistent hashing with xxHash64.
    /// </summary>
    /// <param name="configureOptions">Optional configuration for the hash router.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseHashRouting(Action<HashShardRouterOptions>? configureOptions = null)
    {
        var options = new HashShardRouterOptions();
        configureOptions?.Invoke(options);

        _routerFactory = topology => new HashShardRouter(topology, options);
        return this;
    }

    /// <summary>
    /// Configures range-based routing.
    /// </summary>
    /// <param name="ranges">The key ranges that define shard boundaries.</param>
    /// <param name="comparer">Optional string comparer for key comparisons.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseRangeRouting(
        IEnumerable<ShardRange> ranges,
        StringComparer? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        var rangeList = ranges.ToList();
        _routerFactory = topology => new RangeShardRouter(topology, rangeList, comparer);
        return this;
    }

    /// <summary>
    /// Configures directory-based routing.
    /// </summary>
    /// <param name="store">The directory store for key-to-shard mappings.</param>
    /// <param name="defaultShardId">Optional default shard ID for unmapped keys.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseDirectoryRouting(
        IShardDirectoryStore store,
        string? defaultShardId = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _routerFactory = topology => new DirectoryShardRouter(topology, store, defaultShardId);
        return this;
    }

    /// <summary>
    /// Configures geo-based routing.
    /// </summary>
    /// <param name="regions">The geo region definitions.</param>
    /// <param name="regionResolver">A function that extracts a region code from a shard key.</param>
    /// <param name="configureOptions">Optional configuration for the geo router.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseGeoRouting(
        IEnumerable<GeoRegion> regions,
        Func<string, string> regionResolver,
        Action<GeoShardRouterOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(regionResolver);

        var regionList = regions.ToList();
        var options = new GeoShardRouterOptions();
        configureOptions?.Invoke(options);

        _routerFactory = topology => new GeoShardRouter(topology, regionList, regionResolver, options);
        return this;
    }

    /// <summary>
    /// Configures a custom router.
    /// </summary>
    /// <param name="routerFactory">A factory that creates the router from the topology.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseCustomRouting(Func<ShardTopology, IShardRouter> routerFactory)
    {
        ArgumentNullException.ThrowIfNull(routerFactory);
        _routerFactory = routerFactory;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ShardTopology"/> from the configured shards.
    /// </summary>
    internal ShardTopology BuildTopology() => new(_shards.Values);

    /// <summary>
    /// Builds the <see cref="IShardRouter"/> from the configured router factory and topology.
    /// </summary>
    internal IShardRouter BuildRouter(ShardTopology topology)
    {
        if (_routerFactory is null)
        {
            throw new InvalidOperationException(
                "No routing strategy has been configured. " +
                "Call UseHashRouting(), UseRangeRouting(), UseDirectoryRouting(), UseGeoRouting(), or UseCustomRouting() before building.");
        }

        return _routerFactory(topology);
    }
}
