namespace Encina.Sharding;

/// <summary>
/// Contains the result of a count operation across multiple shards,
/// with per-shard count breakdown.
/// </summary>
/// <param name="TotalCount">The total count across all successful shards.</param>
/// <param name="CountPerShard">The count for each successful shard.</param>
/// <param name="FailedShards">The shard IDs that failed, with their error information.</param>
/// <remarks>
/// <para>
/// This is a lightweight result type for cross-shard count queries that avoids
/// fetching actual entity data. It complements the specification-based scatter-gather
/// by providing efficient count-only operations.
/// </para>
/// <para>
/// When <see cref="IsPartial"/> is true, <see cref="TotalCount"/> reflects only
/// the successful shards and the actual total may be higher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await shardedRepo.CountAllShardsAsync(activeOrdersSpec, ct);
///
/// result.Match(
///     Right: r =>
///     {
///         Console.WriteLine($"Total: {r.TotalCount} items across {r.ShardsQueried} shards");
///         foreach (var (shardId, count) in r.CountPerShard)
///             Console.WriteLine($"  Shard {shardId}: {count}");
///     },
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
/// </code>
/// </example>
public sealed record ShardedCountResult(
    long TotalCount,
    IReadOnlyDictionary<string, long> CountPerShard,
    IReadOnlyList<ShardFailure> FailedShards)
{
    /// <summary>
    /// Gets whether all shards responded successfully.
    /// </summary>
    public bool IsComplete => FailedShards.Count == 0;

    /// <summary>
    /// Gets whether some shards failed but results are still available from others.
    /// </summary>
    public bool IsPartial => FailedShards.Count > 0 && CountPerShard.Count > 0;

    /// <summary>
    /// Gets the total number of shards queried (successful + failed).
    /// </summary>
    public int ShardsQueried => CountPerShard.Count + FailedShards.Count;
}
