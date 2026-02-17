namespace Encina.Sharding.TimeBased.Health;

/// <summary>
/// Configuration options for the tier transition health check.
/// </summary>
/// <remarks>
/// <para>
/// The health check evaluates whether tier transitions are running on schedule by examining
/// shard ages against configurable per-tier thresholds:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: All shards are within their expected tier age thresholds.</description></item>
///   <item><description><b>Degraded</b>: Some shards have exceeded their expected tier age,
///   indicating missed tier transitions.</description></item>
///   <item><description><b>Unhealthy</b>: Shards are significantly overdue for tier transitions.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class TierTransitionHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the maximum expected age (in days from period end) for Hot-tier shards
    /// before the health check reports degraded status.
    /// </summary>
    /// <value>Default: 35 days (1 month + 5 day buffer).</value>
    public int MaxExpectedHotAgeDays { get; set; } = 35;

    /// <summary>
    /// Gets or sets the maximum expected age (in days from period end) for Warm-tier shards
    /// before the health check reports degraded status.
    /// </summary>
    /// <value>Default: 95 days (3 months + 5 day buffer).</value>
    public int MaxExpectedWarmAgeDays { get; set; } = 95;

    /// <summary>
    /// Gets or sets the maximum expected age (in days from period end) for Cold-tier shards
    /// before the health check reports degraded status.
    /// </summary>
    /// <value>Default: 370 days (1 year + 5 day buffer).</value>
    public int MaxExpectedColdAgeDays { get; set; } = 370;

    /// <summary>
    /// Gets or sets the multiplier applied to the age threshold to determine the unhealthy threshold.
    /// </summary>
    /// <value>Default: 2.0 (double the degraded threshold).</value>
    public double UnhealthyMultiplier { get; set; } = 2.0;
}
