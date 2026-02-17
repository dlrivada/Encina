namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Represents the health state of a single replica within a shard.
/// </summary>
/// <param name="IsHealthy">Whether the replica is currently considered healthy.</param>
/// <param name="LastFailure">The UTC time of the most recent failure, or <see langword="null"/> if never failed.</param>
/// <param name="FailureCount">The cumulative number of failures recorded for this replica.</param>
/// <param name="LastSuccess">The UTC time of the most recent successful connection, or <see langword="null"/> if never succeeded.</param>
/// <param name="ObservedReplicationLag">
/// The most recently observed replication lag, or <see langword="null"/> if lag has never been reported.
/// </param>
/// <param name="LagObservedAtUtc">
/// The UTC time when the replication lag was last observed, or <see langword="null"/> if never reported.
/// </param>
public sealed record ReplicaHealthState(
    bool IsHealthy,
    DateTime? LastFailure,
    int FailureCount,
    DateTime? LastSuccess,
    TimeSpan? ObservedReplicationLag = null,
    DateTime? LagObservedAtUtc = null)
{
    /// <summary>
    /// Gets a healthy state with no recorded failures, successes, or lag observations.
    /// </summary>
    public static ReplicaHealthState Healthy { get; } = new(true, null, 0, null);
}
