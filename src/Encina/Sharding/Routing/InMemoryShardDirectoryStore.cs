using System.Collections.Concurrent;

namespace Encina.Sharding.Routing;

/// <summary>
/// Default in-memory implementation of <see cref="IShardDirectoryStore"/>
/// using a <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread safety.
/// </summary>
public sealed class InMemoryShardDirectoryStore : IShardDirectoryStore
{
    private readonly ConcurrentDictionary<string, string> _mappings = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new empty <see cref="InMemoryShardDirectoryStore"/>.
    /// </summary>
    public InMemoryShardDirectoryStore()
    {
    }

    /// <summary>
    /// Initializes a new <see cref="InMemoryShardDirectoryStore"/> with initial mappings.
    /// </summary>
    /// <param name="initialMappings">The initial key-to-shard mappings.</param>
    public InMemoryShardDirectoryStore(IEnumerable<KeyValuePair<string, string>> initialMappings)
    {
        ArgumentNullException.ThrowIfNull(initialMappings);

        foreach (var mapping in initialMappings)
        {
            _mappings.TryAdd(mapping.Key, mapping.Value);
        }
    }

    /// <inheritdoc />
    public string? GetMapping(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _mappings.TryGetValue(key, out var shardId) ? shardId : null;
    }

    /// <inheritdoc />
    public void AddMapping(string key, string shardId)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(shardId);
        _mappings.AddOrUpdate(key, shardId, (_, _) => shardId);
    }

    /// <inheritdoc />
    public bool RemoveMapping(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _mappings.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetAllMappings()
    {
        return _mappings.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}
