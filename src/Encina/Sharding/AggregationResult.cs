namespace Encina.Sharding;

/// <summary>
/// Contains the final result of a distributed aggregation operation across shards,
/// including metadata about the shards queried and any failures.
/// </summary>
/// <typeparam name="T">The type of the aggregated value.</typeparam>
/// <param name="Value">The final aggregated value.</param>
/// <param name="ShardsQueried">The total number of shards that were queried.</param>
/// <param name="FailedShards">The list of shards that failed during the aggregation.</param>
/// <param name="Duration">The total duration of the aggregation operation.</param>
/// <remarks>
/// <para>
/// This record follows the same immutable record pattern as <see cref="ShardedQueryResult{T}"/>
/// and provides partial result awareness through <see cref="IsPartial"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await repo.CountAcrossShardsAsync(e => e.IsActive, ct);
///
/// result.Match(
///     Right: agg =>
///     {
///         Console.WriteLine($"Count: {agg.Value}");
///         Console.WriteLine($"Queried {agg.ShardsQueried} shards in {agg.Duration.TotalMilliseconds}ms");
///         if (agg.IsPartial)
///             Console.WriteLine($"Warning: {agg.FailedShards.Count} shards failed");
///     },
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
/// </code>
/// </example>
public sealed record AggregationResult<T>(
    T Value,
    int ShardsQueried,
    IReadOnlyList<ShardFailure> FailedShards,
    TimeSpan Duration)
{
    /// <summary>
    /// Gets whether the result is partial due to one or more shard failures.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the <see cref="Value"/> is computed from only the successful shards
    /// and may not represent the complete aggregate across the entire dataset.
    /// </remarks>
    public bool IsPartial => FailedShards.Count > 0;
}
