namespace Encina.Sharding;

/// <summary>
/// Defines the strategy for merging paginated results from multiple shards.
/// </summary>
/// <remarks>
/// <para>
/// Cross-shard pagination is fundamentally different from single-database pagination.
/// Since each shard only knows about its own data, the coordinator must choose how
/// to distribute page requests and merge results.
/// </para>
/// </remarks>
public enum ShardedPaginationStrategy
{
    /// <summary>
    /// Fetches <c>PageSize</c> items from each shard, merges all results, applies
    /// ordering, and trims to the requested page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the simplest and most correct strategy. It guarantees correct ordering
    /// across shards but fetches more data than necessary (up to <c>PageSize × ShardCount</c>
    /// items per request).
    /// </para>
    /// <para>
    /// Best for: small to medium page sizes, queries with strict ordering requirements,
    /// or when the number of shards is small.
    /// </para>
    /// </remarks>
    OverfetchAndMerge = 0,

    /// <summary>
    /// Estimates the total count per shard and distributes page requests proportionally,
    /// reducing the total data transferred.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This strategy first queries each shard for its count, then distributes the
    /// page request proportionally based on each shard's share of the total data.
    /// It reduces overfetching but requires an additional round-trip for counts.
    /// </para>
    /// <para>
    /// Best for: large page sizes, many shards, or when minimizing data transfer
    /// is more important than minimizing round-trips.
    /// </para>
    /// <para>
    /// Note: This strategy may produce slightly imprecise results when data distribution
    /// changes between the count and fetch phases.
    /// </para>
    /// </remarks>
    EstimateAndDistribute = 1
}
