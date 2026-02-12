using System.Collections.Frozen;
using Encina.Sharding.Colocation;
using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// Manages the collection of shards and provides lookup and enumeration methods.
/// </summary>
/// <remarks>
/// <para>
/// ShardTopology is immutable after construction. To modify the topology,
/// create a new instance with the updated shard collection.
/// </para>
/// <para>
/// Optionally, a <see cref="ColocationGroupRegistry"/> can be associated with the topology
/// to enable co-location metadata lookups via <see cref="GetColocationGroup"/>.
/// </para>
/// </remarks>
public sealed class ShardTopology
{
    private readonly FrozenDictionary<string, ShardInfo> _shards;
    private readonly ColocationGroupRegistry? _colocationRegistry;

    /// <summary>
    /// Initializes a new instance of <see cref="ShardTopology"/> with the given shards.
    /// </summary>
    /// <param name="shards">The collection of shard definitions.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shards"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when duplicate shard IDs are detected.</exception>
    public ShardTopology(IEnumerable<ShardInfo> shards)
        : this(shards, colocationRegistry: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ShardTopology"/> with the given shards
    /// and an optional co-location group registry.
    /// </summary>
    /// <param name="shards">The collection of shard definitions.</param>
    /// <param name="colocationRegistry">
    /// Optional co-location group registry for co-location metadata lookups.
    /// When provided, <see cref="GetColocationGroup"/> returns group information.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shards"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when duplicate shard IDs are detected.</exception>
    public ShardTopology(IEnumerable<ShardInfo> shards, ColocationGroupRegistry? colocationRegistry)
    {
        ArgumentNullException.ThrowIfNull(shards);

        _colocationRegistry = colocationRegistry;

        var shardList = shards.ToList();

        var duplicates = shardList
            .GroupBy(s => s.ShardId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ArgumentException(
                $"Duplicate shard IDs detected: {string.Join(", ", duplicates)}.",
                nameof(shards));
        }

        _shards = shardList.ToFrozenDictionary(
            s => s.ShardId,
            s => s,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all shard IDs in the topology.
    /// </summary>
    public IReadOnlyList<string> AllShardIds => _shards.Keys.ToList();

    /// <summary>
    /// Gets all active shard IDs in the topology.
    /// </summary>
    public IReadOnlyList<string> ActiveShardIds =>
        _shards.Values.Where(s => s.IsActive).Select(s => s.ShardId).ToList();

    /// <summary>
    /// Gets the total number of shards in the topology.
    /// </summary>
    public int Count => _shards.Count;

    /// <summary>
    /// Gets a shard by its ID.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>Right with the shard info; Left with an error if not found.</returns>
    public Either<EncinaError, ShardInfo> GetShard(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        return _shards.TryGetValue(shardId, out var shard)
            ? Either<EncinaError, ShardInfo>.Right(shard)
            : Either<EncinaError, ShardInfo>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardNotFound,
                    $"Shard '{shardId}' was not found in the topology."));
    }

    /// <summary>
    /// Gets the connection string for a shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>Right with the connection string; Left with an error if not found.</returns>
    public Either<EncinaError, string> GetConnectionString(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        return _shards.TryGetValue(shardId, out var shard)
            ? Either<EncinaError, string>.Right(shard.ConnectionString)
            : Either<EncinaError, string>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardNotFound,
                    $"Shard '{shardId}' was not found in the topology."));
    }

    /// <summary>
    /// Gets all shard definitions.
    /// </summary>
    /// <returns>All shards in the topology.</returns>
    public IReadOnlyList<ShardInfo> GetAllShards() => _shards.Values.ToList();

    /// <summary>
    /// Gets all active shard definitions.
    /// </summary>
    /// <returns>Active shards in the topology.</returns>
    public IReadOnlyList<ShardInfo> GetActiveShards() =>
        _shards.Values.Where(s => s.IsActive).ToList();

    /// <summary>
    /// Checks if a shard ID exists in the topology.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>True if the shard exists; false otherwise.</returns>
    public bool ContainsShard(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        return _shards.ContainsKey(shardId);
    }

    /// <summary>
    /// Gets the co-location group for a given entity type, if it belongs to one.
    /// </summary>
    /// <param name="entityType">The entity type to look up (can be root or co-located).</param>
    /// <returns>
    /// The <see cref="IColocationGroup"/> if the entity type belongs to a co-location group;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> if no <see cref="ColocationGroupRegistry"/> was provided during
    /// construction or if the entity type does not belong to any co-location group.
    /// </para>
    /// </remarks>
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
