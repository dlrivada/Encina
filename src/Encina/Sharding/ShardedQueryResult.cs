namespace Encina.Sharding;

/// <summary>
/// Contains results from a scatter-gather query across multiple shards,
/// including shard metadata and partial failure information.
/// </summary>
/// <typeparam name="T">The type of the result items.</typeparam>
/// <param name="Results">The aggregated results from all successful shards.</param>
/// <param name="SuccessfulShards">The shard IDs that returned results successfully.</param>
/// <param name="FailedShards">The shard IDs that failed, with their error information.</param>
public sealed record ShardedQueryResult<T>(
    IReadOnlyList<T> Results,
    IReadOnlyList<string> SuccessfulShards,
    IReadOnlyList<ShardFailure> FailedShards)
{
    /// <summary>
    /// Gets whether all shards responded successfully.
    /// </summary>
    public bool IsComplete => FailedShards.Count == 0;

    /// <summary>
    /// Gets whether some shards failed but results are still available from others.
    /// </summary>
    public bool IsPartial => FailedShards.Count > 0 && SuccessfulShards.Count > 0;

    /// <summary>
    /// Gets the total number of shards queried (successful + failed).
    /// </summary>
    public int TotalShardsQueried => SuccessfulShards.Count + FailedShards.Count;
}

/// <summary>
/// Describes a shard failure during a scatter-gather operation.
/// </summary>
/// <param name="ShardId">The shard that failed.</param>
/// <param name="Error">The error that occurred.</param>
public sealed record ShardFailure(string ShardId, EncinaError Error);
