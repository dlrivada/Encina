namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Tracks the health of read replicas across shards, enabling automatic failover
/// to healthy replicas and recovery of previously unhealthy ones.
/// </summary>
/// <remarks>
/// <para>
/// The health tracker maintains per-shard, per-replica health state. Replicas marked
/// as unhealthy are excluded from selection by <see cref="IShardReplicaSelector"/> until
/// they recover after the configured recovery window.
/// </para>
/// <para>
/// All operations are thread-safe and designed for high-concurrency scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark a replica as unhealthy after a connection failure
/// tracker.MarkUnhealthy("shard-0", "Server=replica1;...");
///
/// // Get only healthy replicas for selection
/// var available = tracker.GetAvailableReplicas("shard-0", allReplicas);
///
/// // Mark as healthy after a successful connection
/// tracker.MarkHealthy("shard-0", "Server=replica1;...");
/// </code>
/// </example>
public interface IReplicaHealthTracker
{
    /// <summary>
    /// Marks a replica as healthy for the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    void MarkHealthy(string shardId, string replicaConnectionString);

    /// <summary>
    /// Marks a replica as unhealthy for the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    void MarkUnhealthy(string shardId, string replicaConnectionString);

    /// <summary>
    /// Gets the list of available (healthy) replicas for the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="allReplicas">The full list of replica connection strings for the shard.</param>
    /// <returns>
    /// A filtered list containing only replicas currently considered healthy.
    /// Replicas that have passed the recovery window are automatically re-included.
    /// </returns>
    IReadOnlyList<string> GetAvailableReplicas(string shardId, IReadOnlyList<string> allReplicas);

    /// <summary>
    /// Gets the health state of a specific replica within a shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    /// <returns>
    /// The <see cref="ReplicaHealthState"/> for the replica, or <see cref="ReplicaHealthState.Healthy"/>
    /// if no state has been recorded.
    /// </returns>
    ReplicaHealthState GetHealthState(string shardId, string replicaConnectionString);

    /// <summary>
    /// Gets the health states of all tracked replicas for the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// A dictionary mapping replica connection strings to their health states.
    /// Returns an empty dictionary if no state has been recorded for the shard.
    /// </returns>
    IReadOnlyDictionary<string, ReplicaHealthState> GetAllHealthStates(string shardId);

    /// <summary>
    /// Reports the observed replication lag for a specific replica.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    /// <param name="lag">The observed replication lag.</param>
    /// <remarks>
    /// <para>
    /// Replication lag can be measured by provider-specific mechanisms:
    /// <list type="bullet">
    ///   <item><description>SQL Server: <c>sys.dm_exec_sessions</c> or <c>sys.dm_hadr_database_replica_states</c></description></item>
    ///   <item><description>PostgreSQL: <c>pg_stat_replication</c> lag columns</description></item>
    ///   <item><description>MySQL: <c>Seconds_Behind_Master</c> from <c>SHOW SLAVE STATUS</c></description></item>
    ///   <item><description>MongoDB: <c>rs.status()</c> optime difference</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    void ReportReplicationLag(string shardId, string replicaConnectionString, TimeSpan lag);

    /// <summary>
    /// Gets replicas that are healthy and within the specified staleness threshold.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="allReplicas">The full list of replica connection strings for the shard.</param>
    /// <param name="maxAcceptableLag">
    /// The maximum acceptable replication lag. Replicas with observed lag exceeding this
    /// value are excluded. Pass <see langword="null"/> to skip lag filtering (only health
    /// filtering is applied).
    /// </param>
    /// <returns>
    /// A filtered list containing only replicas that are healthy and within the lag threshold.
    /// </returns>
    IReadOnlyList<string> GetAvailableReplicas(string shardId, IReadOnlyList<string> allReplicas, TimeSpan? maxAcceptableLag);
}
