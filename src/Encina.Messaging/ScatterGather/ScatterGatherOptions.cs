namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Configuration options for scatter-gather operations.
/// </summary>
public sealed class ScatterGatherOptions
{
    /// <summary>
    /// Gets or sets the default timeout for scatter-gather operations.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to execute scatter handlers in parallel.
    /// </summary>
    /// <remarks>
    /// When true, all scatter handlers run concurrently. When false, they run sequentially.
    /// </remarks>
    /// <value>Default: true</value>
    public bool ExecuteScattersInParallel { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for scatter execution.
    /// </summary>
    /// <value>Default: Environment.ProcessorCount</value>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets the default gather strategy.
    /// </summary>
    /// <value>Default: WaitForAll</value>
    public GatherStrategy DefaultGatherStrategy { get; set; } = GatherStrategy.WaitForAll;

    /// <summary>
    /// Gets or sets the default quorum count when using <see cref="GatherStrategy.WaitForQuorum"/>.
    /// </summary>
    /// <remarks>
    /// If not specified, quorum defaults to (scatterCount / 2) + 1.
    /// </remarks>
    /// <value>Default: null (auto-calculate)</value>
    public int? DefaultQuorumCount { get; set; }

    /// <summary>
    /// Gets or sets whether to include failed scatter results in the gather operation.
    /// </summary>
    /// <remarks>
    /// When true, the gather handler receives both successful and failed results.
    /// When false, only successful results are passed to the gather handler.
    /// </remarks>
    /// <value>Default: false</value>
    public bool IncludeFailedResultsInGather { get; set; }

    /// <summary>
    /// Gets or sets whether to cancel remaining scatter handlers when the gather strategy is satisfied.
    /// </summary>
    /// <remarks>
    /// For WaitForFirst and WaitForQuorum, remaining handlers can be cancelled once the condition is met.
    /// </remarks>
    /// <value>Default: true</value>
    public bool CancelRemainingOnStrategyComplete { get; set; } = true;
}
