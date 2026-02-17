namespace Encina.Caching.Sharding.Configuration;

/// <summary>
/// Root configuration options for sharding cache integration.
/// </summary>
/// <remarks>
/// <para>
/// This class aggregates all caching options for the three sharding cache components:
/// <list type="bullet">
///   <item><description>Directory router caching (<see cref="DirectoryCache"/>)</description></item>
///   <item><description>Topology background refresh (controlled by <see cref="EnableBackgroundRefresh"/>)</description></item>
///   <item><description>Scatter-gather result caching (<see cref="ScatterGatherCache"/>)</description></item>
/// </list>
/// </para>
/// <para>
/// All components are opt-in and disabled by default.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaShardingCaching(options =>
/// {
///     options.EnableDirectoryCaching = true;
///     options.DirectoryCache.InvalidationStrategy = CacheInvalidationStrategy.WriteThrough;
///
///     options.EnableBackgroundRefresh = true;
///     options.TopologyRefreshInterval = TimeSpan.FromSeconds(30);
///
///     options.EnableScatterGatherCaching = true;
///     options.ScatterGatherCache.DefaultCacheDuration = TimeSpan.FromMinutes(2);
/// });
/// </code>
/// </example>
public sealed class ShardingCacheOptions
{
    // --- Topology caching ---

    /// <summary>
    /// Gets or sets the interval for background topology refresh.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan TopologyRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the duration for which the topology is cached in the distributed cache.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan TopologyCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether background topology refresh is enabled.
    /// When enabled, a hosted service periodically refreshes the topology
    /// and optionally the directory store L1 cache.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableBackgroundRefresh { get; set; }

    // --- Directory caching ---

    /// <summary>
    /// Gets or sets whether directory router caching is enabled.
    /// When enabled, <c>CachedShardDirectoryStore</c> wraps the registered
    /// <c>IShardDirectoryStore</c> with an L1 in-memory cache.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableDirectoryCaching { get; set; }

    /// <summary>
    /// Gets or sets the directory cache options.
    /// </summary>
    public DirectoryCacheOptions DirectoryCache { get; set; } = new();

    // --- Scatter-gather caching ---

    /// <summary>
    /// Gets or sets whether scatter-gather result caching is enabled.
    /// When enabled, <c>CachedShardedQueryExecutor</c> wraps the registered
    /// <c>IShardedQueryExecutor</c> with result caching.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableScatterGatherCaching { get; set; }

    /// <summary>
    /// Gets or sets the scatter-gather cache options.
    /// </summary>
    public ScatterGatherCacheOptions ScatterGatherCache { get; set; } = new();
}
