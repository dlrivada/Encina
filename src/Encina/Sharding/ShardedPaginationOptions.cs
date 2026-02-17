namespace Encina.Sharding;

/// <summary>
/// Configuration options for cross-shard paginated queries.
/// </summary>
/// <remarks>
/// <para>
/// Cross-shard pagination requires a merge strategy because no single shard
/// has a global view of the data. The <see cref="Strategy"/> property controls
/// how results are fetched and merged across shards.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ShardedPaginationOptions
/// {
///     Page = 2,
///     PageSize = 20,
///     Strategy = ShardedPaginationStrategy.OverfetchAndMerge
/// };
///
/// var result = await shardedRepo.QueryAllShardsPagedAsync(spec, options, ct);
/// </code>
/// </example>
public sealed class ShardedPaginationOptions
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Gets or sets the 1-based page number to retrieve.
    /// </summary>
    /// <value>The default is 1.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than 1.
    /// </exception>
    public int Page
    {
        get => _page;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _page = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    /// <value>The default is 20.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than 1.
    /// </exception>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _pageSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the merge strategy for combining results from multiple shards.
    /// </summary>
    /// <value>The default is <see cref="ShardedPaginationStrategy.OverfetchAndMerge"/>.</value>
    public ShardedPaginationStrategy Strategy { get; set; } = ShardedPaginationStrategy.OverfetchAndMerge;
}
