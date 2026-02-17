namespace Encina.OpenTelemetry.Sharding;

/// <summary>
/// Provides callback delegates for time-based sharding observable gauge metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class bridges the <c>Encina</c> sharding package with <c>Encina.OpenTelemetry</c>
/// without creating a direct project reference. The time-based sharding infrastructure registers
/// an instance of this class with the appropriate callbacks, and the
/// <see cref="TimeBasedShardingMetricsInitializer"/> uses it to create the
/// <see cref="TimeBasedShardingMetrics"/> on startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered by Encina when time-based sharding is enabled:
/// services.AddSingleton(new TimeBasedShardingMetricsCallbacks(
///     shardsPerTierCallback: () => tierCounts,
///     oldestHotShardAgeDaysCallback: () => oldestAgeDays));
/// </code>
/// </example>
public sealed class TimeBasedShardingMetricsCallbacks
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeBasedShardingMetricsCallbacks"/> class.
    /// </summary>
    /// <param name="shardsPerTierCallback">
    /// Callback that returns per-tier shard counts as tuples of (tier, count).
    /// </param>
    /// <param name="oldestHotShardAgeDaysCallback">
    /// Callback that returns the age in days of the oldest Hot-tier shard,
    /// or <see langword="null"/> if no Hot-tier shards exist.
    /// </param>
    public TimeBasedShardingMetricsCallbacks(
        Func<IEnumerable<(string Tier, int Count)>> shardsPerTierCallback,
        Func<double?> oldestHotShardAgeDaysCallback)
    {
        ArgumentNullException.ThrowIfNull(shardsPerTierCallback);
        ArgumentNullException.ThrowIfNull(oldestHotShardAgeDaysCallback);

        ShardsPerTierCallback = shardsPerTierCallback;
        OldestHotShardAgeDaysCallback = oldestHotShardAgeDaysCallback;
    }

    /// <summary>
    /// Gets the callback that returns per-tier shard counts.
    /// </summary>
    internal Func<IEnumerable<(string Tier, int Count)>> ShardsPerTierCallback { get; }

    /// <summary>
    /// Gets the callback that returns the oldest Hot-tier shard age in days.
    /// </summary>
    internal Func<double?> OldestHotShardAgeDaysCallback { get; }
}
