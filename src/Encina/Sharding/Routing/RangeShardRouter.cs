using Encina.Sharding.Colocation;
using LanguageExt;

namespace Encina.Sharding.Routing;

/// <summary>
/// Routes shard keys based on configured key ranges using binary search.
/// </summary>
/// <remarks>
/// <para>
/// Supports both string-based (lexicographic) and numeric range comparisons.
/// Ranges must not overlap and are validated during construction.
/// </para>
/// <para>
/// Example configuration: keys "A"-"M" → shard-0, "M"-"Z" → shard-1.
/// </para>
/// </remarks>
public sealed class RangeShardRouter : IShardRouter
{
    private readonly ShardTopology _topology;
    private readonly ColocationGroupRegistry? _colocationRegistry;
    private readonly ShardRange[] _sortedRanges;
    private readonly StringComparer _comparer;

    /// <summary>
    /// Initializes a new <see cref="RangeShardRouter"/>.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="ranges">The key ranges. Must not overlap.</param>
    /// <param name="comparer">Optional string comparer for key comparisons. Defaults to <see cref="StringComparer.Ordinal"/>.</param>
    /// <param name="colocationRegistry">
    /// Optional co-location group registry for co-location metadata lookups.
    /// When provided, <see cref="GetColocationGroup"/> returns group information.
    /// </param>
    public RangeShardRouter(
        ShardTopology topology,
        IEnumerable<ShardRange> ranges,
        StringComparer? comparer = null,
        ColocationGroupRegistry? colocationRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(ranges);

        _topology = topology;
        _colocationRegistry = colocationRegistry;
        _comparer = comparer ?? StringComparer.Ordinal;

        var rangeList = ranges.ToList();
        ValidateNoOverlaps(rangeList, _comparer);

        _sortedRanges = [.. rangeList.OrderBy(r => r.StartKey, _comparer)];
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        if (_sortedRanges.Length == 0)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.NoActiveShards,
                    "No ranges are configured in the range router."));
        }

        // Binary search for the range that contains this key
        var lo = 0;
        var hi = _sortedRanges.Length - 1;

        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            var range = _sortedRanges[mid];

            var startCmp = _comparer.Compare(shardKey, range.StartKey);

            if (startCmp < 0)
            {
                hi = mid - 1;
            }
            else
            {
                // Key >= StartKey, check EndKey
                if (range.EndKey is null || _comparer.Compare(shardKey, range.EndKey) < 0)
                {
                    return Either<EncinaError, string>.Right(range.ShardId);
                }

                lo = mid + 1;
            }
        }

        return Either<EncinaError, string>.Left(
            EncinaErrors.Create(
                ShardingErrorCodes.KeyOutsideRange,
                $"Shard key '{shardKey}' does not fall within any configured range."));
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
    /// Gets the shard ID for a compound key by serializing components for lexicographic range comparison.
    /// </summary>
    public Either<EncinaError, string> GetShardId(CompoundShardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return GetShardId(key.ToString());
    }

    /// <summary>
    /// Gets all shard IDs whose ranges overlap with the partial key prefix.
    /// </summary>
    /// <remarks>
    /// For range routing, a partial key (e.g., just the primary component) matches
    /// all ranges whose start key begins with the partial key prefix.
    /// </remarks>
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey)
    {
        ArgumentNullException.ThrowIfNull(partialKey);

        var prefix = partialKey.PrimaryComponent;
        var matchingShards = new List<string>();

        foreach (var range in _sortedRanges)
        {
            // A range overlaps with the prefix if:
            // - The range's start key begins with the prefix, OR
            // - The prefix falls within the range [StartKey, EndKey)
            var startsWithPrefix = range.StartKey.StartsWith(prefix, StringComparison.Ordinal);
            var prefixInRange = _comparer.Compare(prefix, range.StartKey) >= 0
                && (range.EndKey is null || _comparer.Compare(prefix, range.EndKey) < 0);
            var endOverlaps = range.EndKey is not null
                && range.EndKey.StartsWith(prefix, StringComparison.Ordinal);

            if (startsWithPrefix || prefixInRange || endOverlaps)
            {
                matchingShards.Add(range.ShardId);
            }
        }

        if (matchingShards.Count == 0)
        {
            return Either<EncinaError, IReadOnlyList<string>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.PartialKeyRoutingFailed,
                    $"No ranges match the partial key prefix '{prefix}'."));
        }

        return Either<EncinaError, IReadOnlyList<string>>.Right(matchingShards);
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

    private static void ValidateNoOverlaps(List<ShardRange> ranges, StringComparer comparer)
    {
        var sorted = ranges.OrderBy(r => r.StartKey, comparer).ToList();

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            // If current has no end key (unbounded), it overlaps with anything after it
            if (current.EndKey is null)
            {
                throw new ArgumentException(
                    $"Range for shard '{current.ShardId}' (start='{current.StartKey}', end=unbounded) " +
                    $"overlaps with range for shard '{next.ShardId}' (start='{next.StartKey}').");
            }

            // If current.EndKey > next.StartKey, there's an overlap
            if (comparer.Compare(current.EndKey, next.StartKey) > 0)
            {
                throw new ArgumentException(
                    $"Range for shard '{current.ShardId}' (start='{current.StartKey}', end='{current.EndKey}') " +
                    $"overlaps with range for shard '{next.ShardId}' (start='{next.StartKey}').");
            }
        }
    }
}
