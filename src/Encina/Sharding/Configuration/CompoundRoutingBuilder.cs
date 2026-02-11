using Encina.Sharding.Routing;

namespace Encina.Sharding.Configuration;

/// <summary>
/// Fluent builder for configuring compound shard routing with per-component routing strategies.
/// </summary>
/// <remarks>
/// <para>
/// Each compound key component is assigned an independent routing strategy.
/// Component indices must be contiguous starting from 0 (matching the order
/// of components in <see cref="CompoundShardKey"/>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSharding&lt;Order&gt;(options =>
/// {
///     options.UseCompoundRouting(compound =>
///     {
///         compound
///             .Component(0, new RangeShardRouter(topology, regionRanges))
///             .Component(1, new HashShardRouter(topology))
///             .CombineWith(parts => string.Join("-", parts));
///     })
///     .AddShard("shard-us-0", "Server=us0;Database=Orders;...")
///     .AddShard("shard-us-1", "Server=us1;Database=Orders;...")
///     .AddShard("shard-eu-0", "Server=eu0;Database=Orders;...");
/// });
/// </code>
/// </example>
public sealed class CompoundRoutingBuilder
{
    private readonly Dictionary<int, Func<ShardTopology, IShardRouter>> _componentFactories = [];
    private Func<IEnumerable<string>, string>? _combiner;

    /// <summary>
    /// Configures a component router using a pre-built <see cref="IShardRouter"/> instance.
    /// </summary>
    /// <param name="index">
    /// The zero-based component index corresponding to the position in <see cref="CompoundShardKey.Components"/>.
    /// </param>
    /// <param name="router">The router to use for this component.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="router"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public CompoundRoutingBuilder Component(int index, IShardRouter router)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _componentFactories[index] = _ => router;
        return this;
    }

    /// <summary>
    /// Configures a component router using a factory that receives the <see cref="ShardTopology"/>.
    /// </summary>
    /// <param name="index">
    /// The zero-based component index corresponding to the position in <see cref="CompoundShardKey.Components"/>.
    /// </param>
    /// <param name="routerFactory">A factory that creates the router from the topology.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="routerFactory"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public CompoundRoutingBuilder Component(int index, Func<ShardTopology, IShardRouter> routerFactory)
    {
        ArgumentNullException.ThrowIfNull(routerFactory);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _componentFactories[index] = routerFactory;
        return this;
    }

    /// <summary>
    /// Configures a hash-based router for a specific component.
    /// </summary>
    /// <param name="index">The zero-based component index.</param>
    /// <param name="configureOptions">Optional hash router configuration.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public CompoundRoutingBuilder HashComponent(int index, Action<HashShardRouterOptions>? configureOptions = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var options = new HashShardRouterOptions();
        configureOptions?.Invoke(options);

        _componentFactories[index] = topology => new HashShardRouter(topology, options);
        return this;
    }

    /// <summary>
    /// Configures a range-based router for a specific component.
    /// </summary>
    /// <param name="index">The zero-based component index.</param>
    /// <param name="ranges">The key ranges that define shard boundaries for this component.</param>
    /// <param name="comparer">Optional string comparer for key comparisons.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public CompoundRoutingBuilder RangeComponent(
        int index,
        IEnumerable<ShardRange> ranges,
        StringComparer? comparer = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentNullException.ThrowIfNull(ranges);

        var rangeList = ranges.ToList();
        _componentFactories[index] = topology => new RangeShardRouter(topology, rangeList, comparer);
        return this;
    }

    /// <summary>
    /// Configures a directory-based router for a specific component.
    /// </summary>
    /// <param name="index">The zero-based component index.</param>
    /// <param name="store">The directory store for key-to-shard mappings.</param>
    /// <param name="defaultShardId">Optional default shard ID for unmapped keys.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public CompoundRoutingBuilder DirectoryComponent(
        int index,
        IShardDirectoryStore store,
        string? defaultShardId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentNullException.ThrowIfNull(store);

        _componentFactories[index] = topology => new DirectoryShardRouter(topology, store, defaultShardId);
        return this;
    }

    /// <summary>
    /// Configures a geo-based router for a specific component.
    /// </summary>
    /// <param name="index">The zero-based component index.</param>
    /// <param name="regions">The geo region definitions.</param>
    /// <param name="regionResolver">A function that extracts a region code from a shard key.</param>
    /// <param name="configureOptions">Optional geo router configuration.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public CompoundRoutingBuilder GeoComponent(
        int index,
        IEnumerable<GeoRegion> regions,
        Func<string, string> regionResolver,
        Action<GeoShardRouterOptions>? configureOptions = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(regionResolver);

        var regionList = regions.ToList();
        var options = new GeoShardRouterOptions();
        configureOptions?.Invoke(options);

        _componentFactories[index] = topology => new GeoShardRouter(topology, regionList, regionResolver, options);
        return this;
    }

    /// <summary>
    /// Sets a custom combiner function for merging per-component shard IDs into a final shard ID.
    /// </summary>
    /// <param name="combiner">
    /// A function that receives the ordered list of shard IDs produced by each component router
    /// and returns the combined final shard ID.
    /// </param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <remarks>
    /// If not set, the default combiner joins results with a hyphen (e.g., <c>"shard-us-shard-42"</c>).
    /// </remarks>
    public CompoundRoutingBuilder CombineWith(Func<IEnumerable<string>, string> combiner)
    {
        ArgumentNullException.ThrowIfNull(combiner);
        _combiner = combiner;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="CompoundShardRouter"/> from the configured component routers.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <returns>The configured compound shard router.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no component routers are configured or indices are not contiguous.
    /// </exception>
    internal CompoundShardRouter Build(ShardTopology topology)
    {
        if (_componentFactories.Count == 0)
        {
            throw new InvalidOperationException(
                "No component routers configured. Call Component(), HashComponent(), " +
                "RangeComponent(), DirectoryComponent(), or GeoComponent() at least once.");
        }

        var options = new CompoundShardRouterOptions();

        foreach (var (index, factory) in _componentFactories)
        {
            options.ComponentRouters[index] = factory(topology);
        }

        if (_combiner is not null)
        {
            options.ShardIdCombiner = _combiner;
        }

        return new CompoundShardRouter(topology, options);
    }
}
