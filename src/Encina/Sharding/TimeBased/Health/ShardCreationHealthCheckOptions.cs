namespace Encina.Sharding.TimeBased.Health;

/// <summary>
/// Configuration options for the shard creation health check.
/// </summary>
/// <remarks>
/// <para>
/// The health check evaluates whether shards exist for the current and next time periods:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: Shards exist for both the current and next period.</description></item>
///   <item><description><b>Degraded</b>: The next period's shard is missing (within the warning window).</description></item>
///   <item><description><b>Unhealthy</b>: The current period's shard is missing.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ShardCreationHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the time period granularity for shard checks.
    /// Defaults to <see cref="ShardPeriod.Monthly"/>.
    /// </summary>
    public ShardPeriod Period { get; set; } = ShardPeriod.Monthly;

    /// <summary>
    /// Gets or sets the first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/>.
    /// </summary>
    public DayOfWeek WeekStart { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Gets or sets the prefix used for shard ID lookups.
    /// Must match the <c>ShardIdPrefix</c> used in the time-based sharding configuration.
    /// </summary>
    public string ShardIdPrefix { get; set; } = "shard";

    /// <summary>
    /// Gets or sets how many days before the end of the current period to start warning
    /// about a missing next-period shard.
    /// </summary>
    /// <value>Default: 3 days.</value>
    public int WarningWindowDays { get; set; } = 3;
}
