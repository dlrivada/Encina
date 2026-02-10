using Encina.Database;

namespace Encina.Sharding.Health;

/// <summary>
/// Provides an aggregate health summary across all shards in the topology.
/// </summary>
/// <param name="OverallStatus">The aggregate health status across all shards.</param>
/// <param name="ShardResults">Individual health results for each shard.</param>
/// <param name="Description">A human-readable description of the overall health.</param>
/// <remarks>
/// <para>
/// The overall status is determined by the following rules:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: All shards are healthy.</description></item>
///   <item><description><b>Degraded</b>: Some shards are down but the majority are functional.</description></item>
///   <item><description><b>Unhealthy</b>: The majority of shards are down.</description></item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="CalculateOverallStatus"/> to derive the aggregate status from individual
/// <see cref="ShardHealthResult"/> instances. The summary exposes convenience properties
/// (<see cref="HealthyCount"/>, <see cref="DegradedCount"/>, <see cref="UnhealthyCount"/>)
/// for dashboard and alerting integrations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Aggregate health from individual shard checks
/// var results = new[]
/// {
///     ShardHealthResult.Healthy("shard-1", poolStats1),
///     ShardHealthResult.Healthy("shard-2", poolStats2),
///     ShardHealthResult.Unhealthy("shard-3", "Connection refused", ex)
/// };
///
/// var overallStatus = ShardedHealthSummary.CalculateOverallStatus(results);
/// var summary = new ShardedHealthSummary(overallStatus, results,
///     $"{results.Length} shards checked: {overallStatus}");
///
/// // Use for monitoring
/// logger.LogInformation(
///     "Shards: {Healthy}/{Total} healthy, {Degraded} degraded, {Unhealthy} unhealthy",
///     summary.HealthyCount, summary.TotalShards,
///     summary.DegradedCount, summary.UnhealthyCount);
/// </code>
/// </example>
public sealed record ShardedHealthSummary(
    DatabaseHealthStatus OverallStatus,
    IReadOnlyList<ShardHealthResult> ShardResults,
    string? Description = null)
{
    /// <summary>
    /// Gets the number of healthy shards.
    /// </summary>
    public int HealthyCount => ShardResults.Count(r => r.IsHealthy);

    /// <summary>
    /// Gets the number of degraded shards.
    /// </summary>
    public int DegradedCount => ShardResults.Count(r => r.IsDegraded);

    /// <summary>
    /// Gets the number of unhealthy shards.
    /// </summary>
    public int UnhealthyCount => ShardResults.Count(r => r.IsUnhealthy);

    /// <summary>
    /// Gets the total number of shards.
    /// </summary>
    public int TotalShards => ShardResults.Count;

    /// <summary>
    /// Gets whether all shards are healthy.
    /// </summary>
    public bool AllHealthy => OverallStatus == DatabaseHealthStatus.Healthy;

    /// <summary>
    /// Calculates the aggregate health status from individual shard results.
    /// </summary>
    /// <param name="shardResults">The individual shard health results.</param>
    /// <returns>The aggregate <see cref="DatabaseHealthStatus"/>.</returns>
    /// <remarks>
    /// <para>Rules:</para>
    /// <list type="bullet">
    ///   <item><description>All healthy → Healthy</description></item>
    ///   <item><description>Majority (>50%) healthy or degraded → Degraded</description></item>
    ///   <item><description>Majority unhealthy → Unhealthy</description></item>
    ///   <item><description>No shards → Unhealthy</description></item>
    /// </list>
    /// </remarks>
    public static DatabaseHealthStatus CalculateOverallStatus(IReadOnlyList<ShardHealthResult> shardResults)
    {
        ArgumentNullException.ThrowIfNull(shardResults);

        if (shardResults.Count == 0)
        {
            return DatabaseHealthStatus.Unhealthy;
        }

        var unhealthyCount = shardResults.Count(r => r.IsUnhealthy);

        if (unhealthyCount == 0 && shardResults.All(r => r.IsHealthy))
        {
            return DatabaseHealthStatus.Healthy;
        }

        // If majority of shards are unhealthy, the cluster is unhealthy
        if (unhealthyCount > shardResults.Count / 2)
        {
            return DatabaseHealthStatus.Unhealthy;
        }

        return DatabaseHealthStatus.Degraded;
    }
}
