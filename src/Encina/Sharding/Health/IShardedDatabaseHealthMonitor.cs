using Encina.Database;

namespace Encina.Sharding.Health;

/// <summary>
/// Monitors the health of all shards in a sharded database topology.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends the single-database <see cref="IDatabaseHealthMonitor"/>
/// concept to support per-shard health checks. It provides both individual shard
/// monitoring and aggregate health summaries.
/// </para>
/// <para>
/// The health status follows the same semantics as <see cref="DatabaseHealthStatus"/>:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: All shards are reachable and functioning.</description></item>
///   <item><description><b>Degraded</b>: Some shards are down but majority are functional.</description></item>
///   <item><description><b>Unhealthy</b>: Majority of shards are down.</description></item>
/// </list>
/// </para>
/// <para>
/// Provider-specific implementations should check actual database connectivity
/// for each shard using the same approach as single-database health monitors
/// (e.g., <c>SELECT 1</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var summary = await healthMonitor.CheckAllShardsHealthAsync(ct);
/// if (!summary.AllHealthy)
/// {
///     foreach (var shard in summary.ShardResults.Where(s => s.IsUnhealthy))
///     {
///         logger.LogError("Shard {ShardId} is unhealthy: {Desc}", shard.ShardId, shard.Description);
///     }
/// }
/// </code>
/// </example>
public interface IShardedDatabaseHealthMonitor
{
    /// <summary>
    /// Checks the health of a specific shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ShardHealthResult"/> for the specified shard.</returns>
    Task<ShardHealthResult> CheckShardHealthAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of all shards in the topology.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="ShardedHealthSummary"/> with the overall status and individual shard results.
    /// </returns>
    /// <remarks>
    /// Health checks are executed in parallel for all active shards.
    /// Inactive shards are not included in the health check.
    /// </remarks>
    Task<ShardedHealthSummary> CheckAllShardsHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection pool statistics for a specific shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// The <see cref="ConnectionPoolStats"/> for the specified shard,
    /// or <see cref="ConnectionPoolStats.CreateEmpty()"/> if the shard is not found.
    /// </returns>
    ConnectionPoolStats GetShardPoolStatistics(string shardId);

    /// <summary>
    /// Gets the connection pool statistics for all shards.
    /// </summary>
    /// <returns>
    /// A dictionary mapping shard IDs to their <see cref="ConnectionPoolStats"/>.
    /// </returns>
    IReadOnlyDictionary<string, ConnectionPoolStats> GetAllShardPoolStatistics();
}
