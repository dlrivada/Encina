using Encina.Database;
using Encina.Sharding.ReplicaSelection;

namespace Encina.Sharding.Health;

/// <summary>
/// Health check that reports per-shard replica health status.
/// </summary>
/// <remarks>
/// <para>
/// This health check evaluates the read replica topology across all shards and produces
/// an aggregate health status:
/// <list type="bullet">
///   <item><description><see cref="DatabaseHealthStatus.Healthy"/>: All shards have at least
///   <see cref="MinimumHealthyReplicasPerShard"/> healthy replicas.</description></item>
///   <item><description><see cref="DatabaseHealthStatus.Degraded"/>: Some shards have fewer than
///   the minimum healthy replicas, but at least one replica or the primary is still available.</description></item>
///   <item><description><see cref="DatabaseHealthStatus.Unhealthy"/>: One or more shards have zero
///   healthy replicas and fallback to primary is disabled.</description></item>
/// </list>
/// </para>
/// <para>
/// The check considers both health state and replication lag when determining replica availability.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var healthCheck = new ShardReplicaHealthCheck(topology, healthTracker, options);
/// var summary = await healthCheck.CheckReplicaHealthAsync(ct);
///
/// if (summary.OverallStatus != DatabaseHealthStatus.Healthy)
/// {
///     foreach (var result in summary.ShardResults.Where(r => !r.IsHealthy))
///     {
///         logger.LogWarning("Shard {ShardId}: {Status}", result.ShardId, result.Status);
///     }
/// }
/// </code>
/// </example>
public sealed class ShardReplicaHealthCheck
{
    private readonly ShardTopology _topology;
    private readonly IReplicaHealthTracker _healthTracker;
    private readonly ShardedReadWriteOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardReplicaHealthCheck"/> class.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="healthTracker">The replica health tracker.</param>
    /// <param name="options">The sharded read/write options.</param>
    public ShardReplicaHealthCheck(
        ShardTopology topology,
        IReplicaHealthTracker healthTracker,
        ShardedReadWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(healthTracker);
        ArgumentNullException.ThrowIfNull(options);

        _topology = topology;
        _healthTracker = healthTracker;
        _options = options;
    }

    /// <summary>
    /// Gets or sets the minimum number of healthy replicas per shard before
    /// the health check reports a degraded status.
    /// </summary>
    /// <value>Default: 1.</value>
    public int MinimumHealthyReplicasPerShard { get; set; } = 1;

    /// <summary>
    /// Checks the replica health across all shards.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ShardReplicaHealthSummary"/> with per-shard replica results.</returns>
    public Task<ShardReplicaHealthSummary> CheckReplicaHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<ShardReplicaHealthResult>();
        var overallStatus = DatabaseHealthStatus.Healthy;

        foreach (var shard in _topology.GetAllShards())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!shard.HasReplicas)
            {
                results.Add(new ShardReplicaHealthResult(
                    shard.ShardId,
                    DatabaseHealthStatus.Healthy,
                    HealthyReplicaCount: 0,
                    TotalReplicaCount: 0,
                    Description: "No replicas configured; primary only."));
                continue;
            }

            var healthyReplicas = _healthTracker.GetAvailableReplicas(
                shard.ShardId,
                shard.ReplicaConnectionStrings,
                _options.MaxAcceptableReplicationLag);

            var totalReplicas = shard.ReplicaConnectionStrings.Count;
            var healthyCount = healthyReplicas.Count;

            DatabaseHealthStatus shardStatus;
            string? description;

            if (healthyCount >= MinimumHealthyReplicasPerShard)
            {
                shardStatus = DatabaseHealthStatus.Healthy;
                description = null;
            }
            else if (healthyCount > 0)
            {
                shardStatus = DatabaseHealthStatus.Degraded;
                description = $"Only {healthyCount}/{totalReplicas} replicas healthy " +
                              $"(minimum: {MinimumHealthyReplicasPerShard}).";
                if (overallStatus == DatabaseHealthStatus.Healthy)
                {
                    overallStatus = DatabaseHealthStatus.Degraded;
                }
            }
            else
            {
                shardStatus = _options.FallbackToPrimaryWhenNoReplicas
                    ? DatabaseHealthStatus.Degraded
                    : DatabaseHealthStatus.Unhealthy;

                description = _options.FallbackToPrimaryWhenNoReplicas
                    ? $"All {totalReplicas} replicas unhealthy; falling back to primary."
                    : $"All {totalReplicas} replicas unhealthy; no fallback configured.";

                if (shardStatus == DatabaseHealthStatus.Unhealthy)
                {
                    overallStatus = DatabaseHealthStatus.Unhealthy;
                }
                else if (overallStatus == DatabaseHealthStatus.Healthy)
                {
                    overallStatus = DatabaseHealthStatus.Degraded;
                }
            }

            results.Add(new ShardReplicaHealthResult(
                shard.ShardId,
                shardStatus,
                healthyCount,
                totalReplicas,
                description));
        }

        return Task.FromResult(new ShardReplicaHealthSummary(overallStatus, results));
    }
}

/// <summary>
/// Represents the replica health status of a single shard.
/// </summary>
/// <param name="ShardId">The shard identifier.</param>
/// <param name="Status">The health status of this shard's replicas.</param>
/// <param name="HealthyReplicaCount">The number of healthy replicas.</param>
/// <param name="TotalReplicaCount">The total number of configured replicas.</param>
/// <param name="Description">An optional human-readable description.</param>
public sealed record ShardReplicaHealthResult(
    string ShardId,
    DatabaseHealthStatus Status,
    int HealthyReplicaCount,
    int TotalReplicaCount,
    string? Description = null)
{
    /// <summary>Gets whether all replicas for this shard are healthy.</summary>
    public bool IsHealthy => Status == DatabaseHealthStatus.Healthy;

    /// <summary>Gets whether this shard's replicas are in a degraded state.</summary>
    public bool IsDegraded => Status == DatabaseHealthStatus.Degraded;

    /// <summary>Gets whether this shard has no healthy replicas.</summary>
    public bool IsUnhealthy => Status == DatabaseHealthStatus.Unhealthy;
}

/// <summary>
/// Summary of the replica health across all shards.
/// </summary>
/// <param name="OverallStatus">The aggregate health status.</param>
/// <param name="ShardResults">Per-shard replica health results.</param>
public sealed record ShardReplicaHealthSummary(
    DatabaseHealthStatus OverallStatus,
    IReadOnlyList<ShardReplicaHealthResult> ShardResults)
{
    /// <summary>Gets whether all shards have sufficient healthy replicas.</summary>
    public bool AllHealthy => OverallStatus == DatabaseHealthStatus.Healthy;

    /// <summary>Gets the total number of shards evaluated.</summary>
    public int ShardCount => ShardResults.Count;

    /// <summary>Gets the number of shards with degraded replica health.</summary>
    public int DegradedCount => ShardResults.Count(r => r.IsDegraded);

    /// <summary>Gets the number of shards with no healthy replicas.</summary>
    public int UnhealthyCount => ShardResults.Count(r => r.IsUnhealthy);
}
