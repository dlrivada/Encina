namespace Encina.Caching.Sharding.Configuration;

/// <summary>
/// Defines how cache entries are invalidated when the underlying shard directory store is modified.
/// </summary>
public enum CacheInvalidationStrategy
{
    /// <summary>
    /// Invalidate the L1 cache entry on every write, forcing the next read to go to the inner store.
    /// This provides the strongest consistency guarantee at the cost of more cache misses.
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Update the L1 cache and the inner store simultaneously on writes.
    /// Provides good consistency with fewer cache misses, but writes are slightly more expensive.
    /// </summary>
    WriteThrough = 1,

    /// <summary>
    /// Rely solely on TTL-based expiration for cache invalidation. Writes update only the inner store.
    /// Provides the best write performance but may serve stale data until the TTL expires.
    /// </summary>
    Lazy = 2
}
