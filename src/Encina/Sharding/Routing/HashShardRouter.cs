using System.IO.Hashing;
using System.Text;
using LanguageExt;

namespace Encina.Sharding.Routing;

/// <summary>
/// Routes shard keys using consistent hashing with a virtual node ring.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="XxHash64"/> for computing 64-bit hash values and a
/// <see cref="SortedDictionary{TKey, TValue}"/> to map hash positions to shard IDs.
/// Virtual nodes ensure uniform key distribution across shards.
/// </para>
/// <para>
/// When the topology changes, only a fraction of keys (approximately 1/N for N shards)
/// need to be remapped, making this strategy suitable for dynamic scaling.
/// </para>
/// </remarks>
public sealed class HashShardRouter : IShardRouter, IShardRebalancer
{
    private readonly ShardTopology _topology;
    private readonly SortedDictionary<ulong, string> _ring;
    private readonly ulong[] _ringKeys;
    private readonly int _virtualNodesPerShard;

    /// <summary>
    /// Initializes a new <see cref="HashShardRouter"/> with the given topology and options.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="options">Optional hash router configuration.</param>
    public HashShardRouter(ShardTopology topology, HashShardRouterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _topology = topology;
        _virtualNodesPerShard = options?.VirtualNodesPerShard ?? 150;
        _ring = BuildRing(topology, _virtualNodesPerShard);
        _ringKeys = [.. _ring.Keys];
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        if (_ring.Count == 0)
        {
            return Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.NoActiveShards,
                    "No shards are configured in the hash ring."));
        }

        var hash = ComputeHash(shardKey);
        var shardId = FindShardOnRing(hash);
        return Either<EncinaError, string>.Right(shardId);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() => _topology.AllShardIds;

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        return _topology.GetConnectionString(shardId);
    }

    /// <inheritdoc />
    public IReadOnlyList<AffectedKeyRange> CalculateAffectedKeyRanges(
        ShardTopology oldTopology,
        ShardTopology newTopology)
    {
        ArgumentNullException.ThrowIfNull(oldTopology);
        ArgumentNullException.ThrowIfNull(newTopology);

        var oldRing = BuildRing(oldTopology, _virtualNodesPerShard);
        var newRing = BuildRing(newTopology, _virtualNodesPerShard);

        var oldKeys = oldRing.Keys.ToArray();
        var newKeys = newRing.Keys.ToArray();

        var allPositions = oldKeys
            .Union(newKeys)
            .OrderBy(k => k)
            .ToArray();

        var affected = new List<AffectedKeyRange>();

        for (var i = 0; i < allPositions.Length; i++)
        {
            var position = allPositions[i];
            var nextPosition = i + 1 < allPositions.Length
                ? allPositions[i + 1]
                : allPositions[0];

            var oldShard = FindShardOnRing(position, oldKeys, oldRing);
            var newShard = FindShardOnRing(position, newKeys, newRing);

            if (!string.Equals(oldShard, newShard, StringComparison.Ordinal))
            {
                affected.Add(new AffectedKeyRange(position, nextPosition, oldShard, newShard));
            }
        }

        return affected;
    }

    internal static ulong ComputeHash(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        return XxHash64.HashToUInt64(bytes);
    }

    private string FindShardOnRing(ulong hash)
    {
        return FindShardOnRing(hash, _ringKeys, _ring);
    }

    private static string FindShardOnRing(
        ulong hash,
        ulong[] ringKeys,
        SortedDictionary<ulong, string> ring)
    {
        var index = Array.BinarySearch(ringKeys, hash);

        if (index < 0)
        {
            // BinarySearch returns ~index for the next larger element
            index = ~index;
        }

        // Wrap around to first key if beyond max
        if (index >= ringKeys.Length)
        {
            index = 0;
        }

        return ring[ringKeys[index]];
    }

    private static SortedDictionary<ulong, string> BuildRing(ShardTopology topology, int virtualNodesPerShard)
    {
        var ring = new SortedDictionary<ulong, string>();

        foreach (var shard in topology.GetActiveShards())
        {
            for (var i = 0; i < virtualNodesPerShard; i++)
            {
                var virtualKey = $"{shard.ShardId}#vn{i}";
                var hash = ComputeHash(virtualKey);
                ring.TryAdd(hash, shard.ShardId);
            }
        }

        return ring;
    }
}
