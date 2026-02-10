namespace Encina.Sharding.Configuration;

/// <summary>
/// Configuration options for scatter-gather query execution across shards.
/// </summary>
public sealed class ScatterGatherOptions
{
    /// <summary>
    /// Gets or sets the maximum number of shards to query in parallel.
    /// </summary>
    /// <value>The default is -1, meaning unlimited parallelism.</value>
    public int MaxParallelism { get; set; } = -1;

    /// <summary>
    /// Gets or sets the timeout for scatter-gather operations.
    /// </summary>
    /// <value>The default is 30 seconds.</value>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether partial results are returned
    /// when some shards fail during scatter-gather.
    /// </summary>
    /// <value>The default is true.</value>
    /// <remarks>
    /// When false, a single shard failure causes the entire operation to fail.
    /// When true, results from successful shards are returned along with failure information.
    /// </remarks>
    public bool AllowPartialResults { get; set; } = true;
}
