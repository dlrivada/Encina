using Encina.Sharding.Colocation;
using LanguageExt;

namespace Encina.Sharding.Routing;

/// <summary>
/// Routes shard keys using an explicit key-to-shard directory lookup.
/// </summary>
/// <remarks>
/// <para>
/// Uses an <see cref="IShardDirectoryStore"/> for thread-safe key-to-shard mappings.
/// When a key is not found in the directory, falls back to <see cref="DefaultShardId"/>
/// if configured.
/// </para>
/// <para>
/// This strategy is ideal when shard placement is explicitly managed (e.g., tenant-to-shard
/// assignments) rather than algorithmically determined.
/// </para>
/// </remarks>
public sealed class DirectoryShardRouter : IShardRouter
{
    private readonly ShardTopology _topology;
    private readonly IShardDirectoryStore _store;
    private readonly ColocationGroupRegistry? _colocationRegistry;

    /// <summary>
    /// Gets the default shard ID for keys not found in the directory.
    /// Null means no default (missing keys return an error).
    /// </summary>
    public string? DefaultShardId { get; }

    /// <summary>
    /// Initializes a new <see cref="DirectoryShardRouter"/>.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="store">The directory store for key-to-shard mappings.</param>
    /// <param name="defaultShardId">Optional default shard ID for unmapped keys.</param>
    /// <param name="colocationRegistry">
    /// Optional co-location group registry for co-location metadata lookups.
    /// When provided, <see cref="GetColocationGroup"/> returns group information.
    /// </param>
    public DirectoryShardRouter(
        ShardTopology topology,
        IShardDirectoryStore store,
        string? defaultShardId = null,
        ColocationGroupRegistry? colocationRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(store);

        _topology = topology;
        _store = store;
        _colocationRegistry = colocationRegistry;
        DefaultShardId = defaultShardId;
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        var mapped = _store.GetMapping(shardKey);

        if (mapped is not null)
        {
            return Either<EncinaError, string>.Right(mapped);
        }

        if (DefaultShardId is not null)
        {
            return Either<EncinaError, string>.Right(DefaultShardId);
        }

        return Either<EncinaError, string>.Left(
            EncinaErrors.Create(
                ShardingErrorCodes.ShardNotFound,
                $"Shard key '{shardKey}' has no mapping in the directory and no default shard is configured."));
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
    /// Gets the shard ID for a compound key by serializing components for directory lookup.
    /// </summary>
    public Either<EncinaError, string> GetShardId(CompoundShardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return GetShardId(key.ToString());
    }

    /// <summary>
    /// Gets shard IDs matching the partial key prefix by scanning the directory store.
    /// </summary>
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey)
    {
        ArgumentNullException.ThrowIfNull(partialKey);

        var prefix = partialKey.ToString();
        var allMappings = _store.GetAllMappings();
        var matchingShards = allMappings
            .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(kvp => kvp.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (matchingShards.Count == 0)
        {
            if (DefaultShardId is not null)
            {
                return Either<EncinaError, IReadOnlyList<string>>.Right(new List<string> { DefaultShardId });
            }

            return Either<EncinaError, IReadOnlyList<string>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.PartialKeyRoutingFailed,
                    $"No directory mappings match the partial key prefix '{prefix}' and no default shard is configured."));
        }

        return Either<EncinaError, IReadOnlyList<string>>.Right(matchingShards);
    }

    /// <summary>
    /// Adds a key-to-shard mapping to the directory.
    /// </summary>
    /// <param name="key">The shard key.</param>
    /// <param name="shardId">The shard ID.</param>
    public void AddMapping(string key, string shardId)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(shardId);
        _store.AddMapping(key, shardId);
    }

    /// <summary>
    /// Removes a key-to-shard mapping from the directory.
    /// </summary>
    /// <param name="key">The shard key to remove.</param>
    /// <returns>True if the mapping was removed; false if it did not exist.</returns>
    public bool RemoveMapping(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _store.RemoveMapping(key);
    }

    /// <summary>
    /// Gets the shard ID for a specific key from the directory.
    /// </summary>
    /// <param name="key">The shard key.</param>
    /// <returns>The shard ID if found; null otherwise.</returns>
    public string? GetMapping(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _store.GetMapping(key);
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
