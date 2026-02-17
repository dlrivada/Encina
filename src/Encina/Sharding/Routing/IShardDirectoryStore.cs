namespace Encina.Sharding.Routing;

/// <summary>
/// Provides storage for shard key-to-shard mappings used by <see cref="DirectoryShardRouter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be thread-safe, as the <see cref="DirectoryShardRouter"/> may invoke
/// methods concurrently from multiple request threads. The built-in
/// <see cref="InMemoryShardDirectoryStore"/> uses <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
/// internally for this purpose.
/// </para>
/// <para>
/// For persistent storage, implement this interface against a database or distributed cache.
/// Wrap with <c>CachedShardDirectoryStore</c> from <c>Encina.Caching</c> for write-through caching
/// that reduces lookup latency while maintaining consistency.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom database-backed directory store
/// public class SqlDirectoryStore : IShardDirectoryStore
/// {
///     public string? GetMapping(string key) =&gt; /* query shard_directory table */;
///     public void AddMapping(string key, string shardId) =&gt; /* upsert into shard_directory */;
///     public bool RemoveMapping(string key) =&gt; /* delete from shard_directory */;
///     public IReadOnlyDictionary&lt;string, string&gt; GetAllMappings() =&gt; /* select all */;
/// }
/// </code>
/// </example>
public interface IShardDirectoryStore
{
    /// <summary>
    /// Gets the shard ID mapped to a given key.
    /// </summary>
    /// <param name="key">The shard key.</param>
    /// <returns>The shard ID if found; null otherwise.</returns>
    string? GetMapping(string key);

    /// <summary>
    /// Adds or updates a mapping from a key to a shard.
    /// </summary>
    /// <param name="key">The shard key.</param>
    /// <param name="shardId">The shard ID to map to.</param>
    void AddMapping(string key, string shardId);

    /// <summary>
    /// Removes a key-to-shard mapping.
    /// </summary>
    /// <param name="key">The shard key to remove.</param>
    /// <returns>True if the mapping was removed; false if it did not exist.</returns>
    bool RemoveMapping(string key);

    /// <summary>
    /// Gets all current mappings.
    /// </summary>
    /// <returns>A read-only dictionary of all key-to-shard mappings.</returns>
    IReadOnlyDictionary<string, string> GetAllMappings();
}
