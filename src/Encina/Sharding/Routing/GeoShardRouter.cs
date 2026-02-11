using System.Collections.Frozen;
using LanguageExt;

namespace Encina.Sharding.Routing;

/// <summary>
/// Routes shard keys to shards based on geographic region with fallback chain support.
/// </summary>
/// <remarks>
/// <para>
/// Uses a user-provided region resolver function to extract the region from a shard key,
/// then looks up the corresponding shard. Fallback chain:
/// requested region → fallback region → default shard.
/// </para>
/// </remarks>
public sealed class GeoShardRouter : IShardRouter
{
    private readonly ShardTopology _topology;
    private readonly FrozenDictionary<string, GeoRegion> _regions;
    private readonly Func<string, string> _regionResolver;
    private readonly GeoShardRouterOptions _options;

    /// <summary>
    /// Initializes a new <see cref="GeoShardRouter"/>.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="regions">The geo region definitions.</param>
    /// <param name="regionResolver">A function that extracts a region code from a shard key.</param>
    /// <param name="options">Optional configuration.</param>
    public GeoShardRouter(
        ShardTopology topology,
        IEnumerable<GeoRegion> regions,
        Func<string, string> regionResolver,
        GeoShardRouterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(regionResolver);

        _topology = topology;
        _regionResolver = regionResolver;
        _options = options ?? new GeoShardRouterOptions();

        _regions = regions.ToFrozenDictionary(
            r => r.RegionCode,
            r => r,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        var regionCode = _regionResolver(shardKey);

        if (string.IsNullOrEmpty(regionCode))
        {
            return TryDefaultRegion();
        }

        // Try the requested region
        if (_regions.TryGetValue(regionCode, out var region))
        {
            return Either<EncinaError, string>.Right(region.ShardId);
        }

        if (_options.RequireExactMatch)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.RegionNotFound,
                    $"Region '{regionCode}' was not found and exact match is required."));
        }

        // Try fallback chain (prevent infinite loops with visited set)
        var visited = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { regionCode };
        var current = regionCode;

        while (true)
        {
            // Check if there's a region with this code that has a fallback
            if (!_regions.TryGetValue(current, out var currentRegion) || currentRegion.FallbackRegionCode is null)
            {
                break;
            }

            current = currentRegion.FallbackRegionCode;

            if (!visited.Add(current))
            {
                break; // Cycle detected
            }

            if (_regions.TryGetValue(current, out var fallbackRegion))
            {
                return Either<EncinaError, string>.Right(fallbackRegion.ShardId);
            }
        }

        // Last resort: default region
        return TryDefaultRegion(regionCode);
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
    /// Gets the shard ID for a compound key by resolving the region from the key.
    /// </summary>
    /// <remarks>
    /// If <see cref="GeoShardRouterOptions.CompoundRegionResolver"/> is configured,
    /// it receives the full compound key. Otherwise, the primary component is passed
    /// to the standard region resolver.
    /// </remarks>
    public Either<EncinaError, string> GetShardId(CompoundShardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_options.CompoundRegionResolver is not null)
        {
            var regionCode = _options.CompoundRegionResolver(key);
            return ResolveRegion(regionCode);
        }

        return GetShardId(key.PrimaryComponent);
    }

    /// <summary>
    /// Gets all shard IDs in the region resolved from the partial key.
    /// </summary>
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey)
    {
        ArgumentNullException.ThrowIfNull(partialKey);

        string regionCode;

        if (_options.CompoundRegionResolver is not null)
        {
            regionCode = _options.CompoundRegionResolver(partialKey);
        }
        else
        {
            regionCode = _regionResolver(partialKey.PrimaryComponent);
        }

        if (string.IsNullOrEmpty(regionCode))
        {
            return Either<EncinaError, IReadOnlyList<string>>.Right(_topology.AllShardIds);
        }

        // Find all shards in the resolved region and its fallback chain
        var shardIds = new List<string>();
        var visited = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? current = regionCode;

        while (current is not null && visited.Add(current))
        {
            if (_regions.TryGetValue(current, out var region))
            {
                shardIds.Add(region.ShardId);
                current = region.FallbackRegionCode;
            }
            else
            {
                break;
            }
        }

        if (shardIds.Count == 0)
        {
            if (_options.DefaultRegion is not null && _regions.TryGetValue(_options.DefaultRegion, out var defaultRegion))
            {
                return Either<EncinaError, IReadOnlyList<string>>.Right(new List<string> { defaultRegion.ShardId });
            }

            return Either<EncinaError, IReadOnlyList<string>>.Right(_topology.AllShardIds);
        }

        return Either<EncinaError, IReadOnlyList<string>>.Right(shardIds.Distinct(StringComparer.Ordinal).ToList());
    }

    private Either<EncinaError, string> ResolveRegion(string regionCode)
    {
        if (string.IsNullOrEmpty(regionCode))
        {
            return TryDefaultRegion();
        }

        if (_regions.TryGetValue(regionCode, out var region))
        {
            return Either<EncinaError, string>.Right(region.ShardId);
        }

        // Delegate to the standard string-based path for fallback chain resolution
        return GetShardId(regionCode);
    }

    private Either<EncinaError, string> TryDefaultRegion(string? requestedRegion = null)
    {
        if (_options.DefaultRegion is not null && _regions.TryGetValue(_options.DefaultRegion, out var defaultRegion))
        {
            return Either<EncinaError, string>.Right(defaultRegion.ShardId);
        }

        var message = requestedRegion is not null
            ? $"Region '{requestedRegion}' was not found and no default region is configured."
            : "Could not resolve region from shard key and no default region is configured.";

        return Either<EncinaError, string>.Left(
            EncinaErrors.Create(ShardingErrorCodes.RegionNotFound, message));
    }
}
