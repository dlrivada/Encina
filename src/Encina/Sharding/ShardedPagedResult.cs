namespace Encina.Sharding;

/// <summary>
/// Contains paginated results from a specification-based scatter-gather query across
/// multiple shards, including per-shard count breakdown and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of the result items.</typeparam>
/// <param name="Items">The merged and paginated result items.</param>
/// <param name="TotalCount">The total number of matching items across all successful shards.</param>
/// <param name="Page">The 1-based page number that was requested.</param>
/// <param name="PageSize">The page size that was requested.</param>
/// <param name="CountPerShard">The total matching count for each successful shard.</param>
/// <param name="FailedShards">The shard IDs that failed, with their error information.</param>
/// <remarks>
/// <para>
/// The <see cref="Items"/> list contains at most <see cref="PageSize"/> items representing
/// the requested page from the merged, ordered cross-shard result set.
/// </para>
/// <para>
/// <see cref="TotalCount"/> reflects the total matching items across all successful shards.
/// When <see cref="IsPartial"/> is true, the actual total may be higher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pagination = new ShardedPaginationOptions { Page = 2, PageSize = 20 };
/// var result = await shardedRepo.QueryAllShardsPagedAsync(activeOrdersSpec, pagination, ct);
///
/// result.Match(
///     Right: r =>
///     {
///         Console.WriteLine($"Page {r.Page} of {r.TotalPages} ({r.TotalCount} total items)");
///         foreach (var item in r.Items)
///             Console.WriteLine($"  {item}");
///     },
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
/// </code>
/// </example>
public sealed record ShardedPagedResult<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int Page,
    int PageSize,
    IReadOnlyDictionary<string, long> CountPerShard,
    IReadOnlyList<ShardFailure> FailedShards)
{
    /// <summary>
    /// Gets the total number of pages based on <see cref="TotalCount"/> and <see cref="PageSize"/>.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets whether there are more pages after the current page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets whether there is a page before the current page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

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
