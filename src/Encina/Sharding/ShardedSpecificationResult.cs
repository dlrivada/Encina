namespace Encina.Sharding;

/// <summary>
/// Contains results from a specification-based scatter-gather query across multiple shards,
/// with per-shard metadata including item counts and query durations.
/// </summary>
/// <typeparam name="T">The type of the result items.</typeparam>
/// <param name="Items">The merged result items from all successful shards.</param>
/// <param name="ItemsPerShard">The number of items returned by each successful shard.</param>
/// <param name="TotalDuration">The wall-clock duration of the entire scatter-gather operation.</param>
/// <param name="DurationPerShard">The query duration for each individual shard.</param>
/// <param name="FailedShards">The shard IDs that failed, with their error information.</param>
/// <remarks>
/// <para>
/// This result type extends the base <see cref="ShardedQueryResult{T}"/> pattern with
/// per-shard metadata (item counts and durations) that are useful for observability
/// and performance analysis of cross-shard specification queries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await shardedRepo.QueryAllShardsAsync(activeOrdersSpec, ct);
///
/// result.Match(
///     Right: r =>
///     {
///         Console.WriteLine($"Found {r.Items.Count} items across {r.ShardsQueried} shards");
///         Console.WriteLine($"Total duration: {r.TotalDuration.TotalMilliseconds}ms");
///         foreach (var (shardId, count) in r.ItemsPerShard)
///             Console.WriteLine($"  Shard {shardId}: {count} items in {r.DurationPerShard[shardId].TotalMilliseconds}ms");
///     },
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
/// </code>
/// </example>
public sealed record ShardedSpecificationResult<T>(
    IReadOnlyList<T> Items,
    IReadOnlyDictionary<string, int> ItemsPerShard,
    TimeSpan TotalDuration,
    IReadOnlyDictionary<string, TimeSpan> DurationPerShard,
    IReadOnlyList<ShardFailure> FailedShards)
{
    /// <summary>
    /// Gets whether all shards responded successfully.
    /// </summary>
    public bool IsComplete => FailedShards.Count == 0;

    /// <summary>
    /// Gets whether some shards failed but results are still available from others.
    /// </summary>
    public bool IsPartial => FailedShards.Count > 0 && ItemsPerShard.Count > 0;

    /// <summary>
    /// Gets the total number of shards queried (successful + failed).
    /// </summary>
    public int ShardsQueried => ItemsPerShard.Count + FailedShards.Count;
}
