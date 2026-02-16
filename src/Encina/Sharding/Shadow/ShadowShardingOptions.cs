namespace Encina.Sharding.Shadow;

/// <summary>
/// Configuration options for shadow sharding that enable testing new shard topologies
/// under real production traffic without risk.
/// </summary>
/// <remarks>
/// <para>
/// Shadow sharding allows dual-writing to both production and shadow topologies, with
/// configurable shadow reads for result comparison. Shadow operations never affect the
/// production path: failures are logged but never propagated.
/// </para>
/// <para>
/// Use <see cref="ShadowTopology"/> to define the new shard topology to test,
/// <see cref="ShadowReadPercentage"/> to control the percentage of reads that also
/// execute against the shadow, and <see cref="DiscrepancyHandler"/> for custom handling
/// of routing or result mismatches.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ShadowShardingOptions
/// {
///     ShadowTopology = newTopology,
///     DualWriteEnabled = true,
///     ShadowReadPercentage = 10,
///     CompareResults = true,
///     ShadowWriteTimeout = TimeSpan.FromSeconds(3),
///     DiscrepancyHandler = async (result, context, ct) =>
///     {
///         logger.LogWarning(
///             "Shadow discrepancy for {ShardKey}: prod={ProdShard}, shadow={ShadowShard}",
///             result.ShardKey, result.ProductionShardId, result.ShadowShardId);
///     }
/// };
/// </code>
/// </example>
public sealed class ShadowShardingOptions
{
    private ShardTopology? _shadowTopology;
    private int _shadowReadPercentage;
    private TimeSpan _shadowWriteTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the shadow shard topology configuration.
    /// </summary>
    /// <value>The shadow topology that will be tested alongside the production topology.</value>
    /// <exception cref="ArgumentNullException">Thrown when setting a <c>null</c> value.</exception>
    /// <remarks>
    /// This property must be set before shadow sharding registration completes. Validation is
    /// performed during service registration to ensure a topology has been configured.
    /// </remarks>
    public ShardTopology? ShadowTopology
    {
        get => _shadowTopology;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _shadowTopology = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether writes are sent to both the production and
    /// shadow topologies (dual-write mode).
    /// </summary>
    /// <value>
    /// <c>true</c> to enable dual-write (default); <c>false</c> to disable shadow writes entirely.
    /// </value>
    /// <remarks>
    /// Shadow writes are fire-and-forget: failures are logged but never affect the production path.
    /// The shadow write is cancelled after <see cref="ShadowWriteTimeout"/> to prevent unbounded waits.
    /// </remarks>
    public bool DualWriteEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the percentage of reads that also execute against the shadow topology
    /// for comparison.
    /// </summary>
    /// <value>A value between 0 and 100 (inclusive). Default is 0 (no shadow reads).</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than 0 or greater than 100.
    /// </exception>
    /// <remarks>
    /// A value of 0 disables shadow reads entirely. A value of 100 means every read
    /// is also executed against the shadow topology. Use gradual rollout (e.g., 1 -> 10 -> 50 -> 100)
    /// to validate the shadow topology incrementally before cutover.
    /// </remarks>
    public int ShadowReadPercentage
    {
        get => _shadowReadPercentage;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
            _shadowReadPercentage = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether query results from the production and shadow
    /// topologies are compared during shadow reads.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable result comparison (default); <c>false</c> to only compare routing decisions.
    /// </value>
    /// <remarks>
    /// When enabled, shadow reads compare the results from both topologies and populate
    /// <see cref="ShadowComparisonResult.ResultsMatch"/>. When disabled, only routing
    /// decisions are compared and <see cref="ShadowComparisonResult.ResultsMatch"/> is <c>null</c>.
    /// </remarks>
    public bool CompareResults { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional delegate invoked when a routing or result discrepancy is detected.
    /// </summary>
    /// <value>
    /// A delegate that receives the <see cref="ShadowComparisonResult"/>, the current
    /// <see cref="IRequestContext"/>, and a <see cref="CancellationToken"/>. May be <c>null</c>
    /// if no custom discrepancy handling is needed.
    /// </value>
    /// <remarks>
    /// The handler is invoked asynchronously after a comparison detects a mismatch.
    /// Exceptions thrown by the handler are caught and logged but never propagate to
    /// the production path.
    /// </remarks>
    public Func<ShadowComparisonResult, IRequestContext, CancellationToken, Task>? DiscrepancyHandler { get; set; }

    /// <summary>
    /// Gets or sets a factory that creates the shadow <see cref="IShardRouter"/> from the
    /// <see cref="ShadowTopology"/>.
    /// </summary>
    /// <value>
    /// A factory function, or <c>null</c> to use hash-based routing as the default strategy.
    /// </value>
    /// <remarks>
    /// Override this to use a different routing strategy for the shadow topology (e.g., range or
    /// directory routing). When <c>null</c>, a <see cref="Routing.HashShardRouter"/> with default
    /// options is used.
    /// </remarks>
    public Func<ShardTopology, IShardRouter>? ShadowRouterFactory { get; set; }

    /// <summary>
    /// Gets or sets the timeout for fire-and-forget shadow write operations.
    /// </summary>
    /// <value>The maximum time to wait for a shadow write. Default is 5 seconds.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than or equal to <see cref="TimeSpan.Zero"/>.
    /// </exception>
    /// <remarks>
    /// After this timeout, the shadow write is cancelled. This prevents shadow writes from
    /// consuming resources indefinitely. The production write is unaffected by this timeout.
    /// </remarks>
    public TimeSpan ShadowWriteTimeout
    {
        get => _shadowWriteTimeout;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            _shadowWriteTimeout = value;
        }
    }
}
