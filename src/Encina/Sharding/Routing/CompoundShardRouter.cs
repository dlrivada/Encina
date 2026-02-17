using Encina.Sharding.Colocation;
using LanguageExt;

namespace Encina.Sharding.Routing;

/// <summary>
/// Routes compound shard keys by delegating each component to a dedicated router
/// and combining the results into a final shard ID.
/// </summary>
/// <remarks>
/// <para>
/// Enables mixed routing strategies such as range-on-first-key + hash-on-second-key.
/// Each component of the <see cref="CompoundShardKey"/> is routed independently through
/// its configured <see cref="IShardRouter"/>, and the results are combined using
/// <see cref="CompoundShardRouterOptions.ShardIdCombiner"/>.
/// </para>
/// <para>
/// For partial key routing, only the available components are routed. If a compound key
/// has fewer components than configured routers, only the configured routers for the
/// available components are used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Region (range) + Customer (hash) routing
/// var options = new CompoundShardRouterOptions
/// {
///     ComponentRouters =
///     {
///         [0] = new RangeShardRouter(topology, regionRanges),
///         [1] = new HashShardRouter(topology)
///     },
///     ShardIdCombiner = parts => string.Join("-", parts)
/// };
///
/// var router = new CompoundShardRouter(topology, options);
/// var key = new CompoundShardKey("us-east", "customer-123");
/// var result = router.GetShardId(key); // e.g., "shard-us-shard-42"
/// </code>
/// </example>
public sealed class CompoundShardRouter : IShardRouter
{
    private readonly ShardTopology _topology;
    private readonly ColocationGroupRegistry? _colocationRegistry;
    private readonly IShardRouter[] _orderedRouters;
    private readonly Func<IEnumerable<string>, string> _combiner;

    /// <summary>
    /// Initializes a new <see cref="CompoundShardRouter"/>.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="options">The compound router configuration with per-component routers.</param>
    /// <param name="colocationRegistry">
    /// Optional co-location group registry for co-location metadata lookups.
    /// When provided, <see cref="GetColocationGroup"/> returns group information.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when component indices are not contiguous starting from 0 or no routers are configured.
    /// </exception>
    public CompoundShardRouter(
        ShardTopology topology,
        CompoundShardRouterOptions options,
        ColocationGroupRegistry? colocationRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ComponentRouters.Count == 0)
        {
            throw new ArgumentException(
                "At least one component router must be configured.",
                nameof(options));
        }

        // Validate contiguous indices starting from 0
        var maxIndex = options.ComponentRouters.Keys.Max();
        if (maxIndex != options.ComponentRouters.Count - 1
            || options.ComponentRouters.Keys.Min() != 0)
        {
            throw new ArgumentException(
                "Component router indices must be contiguous starting from 0.",
                nameof(options));
        }

        _topology = topology;
        _colocationRegistry = colocationRegistry;
        _combiner = options.ShardIdCombiner;
        _orderedRouters = new IShardRouter[options.ComponentRouters.Count];

        for (var i = 0; i < _orderedRouters.Length; i++)
        {
            _orderedRouters[i] = options.ComponentRouters[i];
        }
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        // When called with a simple string, delegate to the first router
        return _orderedRouters[0].GetShardId(shardKey);
    }

    /// <summary>
    /// Routes each component through its dedicated router and combines the results.
    /// </summary>
    public Either<EncinaError, string> GetShardId(CompoundShardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var routerCount = Math.Min(key.ComponentCount, _orderedRouters.Length);
        var results = new string[routerCount];

        for (var i = 0; i < routerCount; i++)
        {
            var result = _orderedRouters[i].GetShardId(key.Components[i]);

            if (result.IsLeft)
            {
                return result;
            }

            results[i] = result.Match(Right: shardId => shardId, Left: _ => string.Empty);
        }

        return Either<EncinaError, string>.Right(_combiner(results));
    }

    /// <summary>
    /// Routes available components for partial key routing and returns matching shard IDs.
    /// </summary>
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey)
    {
        ArgumentNullException.ThrowIfNull(partialKey);

        if (partialKey.ComponentCount == 0)
        {
            return Either<EncinaError, IReadOnlyList<string>>.Right(_topology.AllShardIds);
        }

        // Route using only the first available component for scatter-gather
        var firstResult = _orderedRouters[0].GetShardId(partialKey.PrimaryComponent);

        return firstResult.Match<Either<EncinaError, IReadOnlyList<string>>>(
            Right: _ => Either<EncinaError, IReadOnlyList<string>>.Right(_topology.AllShardIds),
            Left: error => Either<EncinaError, IReadOnlyList<string>>.Left(error));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() => _topology.AllShardIds;

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        return _topology.GetConnectionString(shardId);
    }

    /// <summary>
    /// Gets the co-location group for a given entity type, if it belongs to one.
    /// </summary>
    /// <param name="entityType">The entity type to look up.</param>
    /// <returns>The co-location group if found; otherwise, <c>null</c>.</returns>
    public IColocationGroup? GetColocationGroup(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        if (_colocationRegistry is not null && _colocationRegistry.TryGetGroup(entityType, out var group))
        {
            return group;
        }

        return null;
    }
}
