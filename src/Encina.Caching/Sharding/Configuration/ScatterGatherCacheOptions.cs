namespace Encina.Caching.Sharding.Configuration;

/// <summary>
/// Configuration options for scatter-gather query result caching.
/// </summary>
/// <remarks>
/// These options control how <c>CachedShardedQueryExecutor</c> caches
/// the aggregated results of scatter-gather operations across shards.
/// </remarks>
public sealed class ScatterGatherCacheOptions
{
    /// <summary>
    /// Gets or sets the default duration for which scatter-gather results are cached.
    /// Defaults to 2 minutes.
    /// </summary>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the maximum number of result items that can be cached per query.
    /// Results exceeding this limit are not cached to prevent excessive memory usage.
    /// Defaults to 10,000.
    /// </summary>
    public int MaxCachedResultSize { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the pub/sub channel name for scatter-gather cache invalidation.
    /// Defaults to <c>"shard:scatter:invalidate"</c>.
    /// </summary>
    public string InvalidationChannel { get; set; } = "shard:scatter:invalidate";

    /// <summary>
    /// Gets or sets whether scatter-gather result caching is enabled.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableResultCaching { get; set; }
}
