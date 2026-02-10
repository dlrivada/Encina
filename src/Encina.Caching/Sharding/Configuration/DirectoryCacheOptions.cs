namespace Encina.Caching.Sharding.Configuration;

/// <summary>
/// Configuration options for the shard directory router cache.
/// </summary>
/// <remarks>
/// These options control how <c>CachedShardDirectoryStore</c> caches
/// key-to-shard mappings from the underlying <c>IShardDirectoryStore</c>.
/// </remarks>
public sealed class DirectoryCacheOptions
{
    /// <summary>
    /// Gets or sets the duration for which directory cache entries are retained.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the cache invalidation strategy.
    /// Defaults to <see cref="CacheInvalidationStrategy.Immediate"/>.
    /// </summary>
    public CacheInvalidationStrategy InvalidationStrategy { get; set; } = CacheInvalidationStrategy.Immediate;

    /// <summary>
    /// Gets or sets the key prefix used for directory cache entries.
    /// Defaults to <c>"shard:dir"</c>.
    /// </summary>
    public string KeyPrefix { get; set; } = "shard:dir";

    /// <summary>
    /// Gets or sets whether distributed cache invalidation via pub/sub is enabled.
    /// When enabled, writes publish invalidation messages through <c>IPubSubProvider</c>
    /// to synchronize L1 caches across application instances.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableDistributedInvalidation { get; set; }

    /// <summary>
    /// Gets or sets the pub/sub channel name for distributed invalidation messages.
    /// Defaults to <c>"shard:dir:invalidate"</c>.
    /// </summary>
    public string InvalidationChannel { get; set; } = "shard:dir:invalidate";
}
