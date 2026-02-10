namespace Encina.Caching.Sharding;

/// <summary>
/// Utility for generating deterministic cache keys for sharding cache operations.
/// </summary>
/// <remarks>
/// All keys follow the pattern <c>{prefix}:{discriminator}</c> to enable
/// pattern-based invalidation via <c>ICacheProvider.RemoveByPatternAsync</c>.
/// </remarks>
public static class ShardCacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for a single directory mapping.
    /// </summary>
    /// <param name="prefix">The key prefix (e.g., <c>"shard:dir"</c>).</param>
    /// <param name="key">The shard key.</param>
    /// <returns>A cache key in the format <c>{prefix}:{key}</c>.</returns>
    public static string ForDirectory(string prefix, string key)
        => $"{prefix}:{key}";

    /// <summary>
    /// Generates a cache key for the full directory mapping collection.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <returns>A cache key in the format <c>{prefix}:all</c>.</returns>
    public static string ForDirectoryAll(string prefix)
        => $"{prefix}:all";

    /// <summary>
    /// Generates a cache key for the shard topology.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <returns>A cache key in the format <c>{prefix}:topology</c>.</returns>
    public static string ForTopology(string prefix)
        => $"{prefix}:topology";

    /// <summary>
    /// Generates a cache key for a scatter-gather query result.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <param name="cacheKey">The caller-provided cache key identifying the query.</param>
    /// <returns>A cache key in the format <c>{prefix}:{cacheKey}</c>.</returns>
    public static string ForScatterGather(string prefix, string cacheKey)
        => $"{prefix}:{cacheKey}";

    /// <summary>
    /// Generates a cache key for a scatter-gather query result scoped to a specific shard.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cacheKey">The caller-provided cache key identifying the query.</param>
    /// <returns>A cache key in the format <c>{prefix}:{shardId}:{cacheKey}</c>.</returns>
    public static string ForScatterGatherShard(string prefix, string shardId, string cacheKey)
        => $"{prefix}:{shardId}:{cacheKey}";

    /// <summary>
    /// Generates a pattern for invalidating all cache entries related to a specific shard.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>A pattern in the format <c>{prefix}:{shardId}:*</c>.</returns>
    public static string InvalidationPattern(string prefix, string shardId)
        => $"{prefix}:{shardId}:*";
}
