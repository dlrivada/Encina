using Encina.Database;

namespace Encina.Sharding.Health;

/// <summary>
/// Represents the health status of an individual shard, including its connection pool statistics.
/// </summary>
/// <param name="ShardId">The shard identifier.</param>
/// <param name="Status">The health status of this shard.</param>
/// <param name="PoolStats">Connection pool statistics for this shard.</param>
/// <param name="Description">An optional human-readable description.</param>
/// <param name="Exception">An optional exception if the shard is unhealthy.</param>
/// <remarks>
/// <para>
/// Health status semantics follow a three-state model:
/// <list type="bullet">
///   <item><description><see cref="DatabaseHealthStatus.Healthy"/>: The shard is fully operational
///   and its connection pool has available capacity.</description></item>
///   <item><description><see cref="DatabaseHealthStatus.Degraded"/>: The shard is reachable but
///   experiencing issues (high pool usage, slow responses). Routing still works but performance
///   may be affected.</description></item>
///   <item><description><see cref="DatabaseHealthStatus.Unhealthy"/>: The shard cannot be reached
///   or has critically failed. Routing to this shard will produce errors.</description></item>
/// </list>
/// </para>
/// <para>
/// Use the static factory methods <see cref="Healthy"/>, <see cref="Degraded"/>, and
/// <see cref="Unhealthy(string, string?, Exception?)"/> to construct results with sensible defaults.
/// These factories automatically set <see cref="PoolStats"/> to <see cref="ConnectionPoolStats.CreateEmpty"/>
/// when not explicitly provided.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create health results using factory methods
/// var healthy = ShardHealthResult.Healthy("shard-1", poolStats);
/// var degraded = ShardHealthResult.Degraded("shard-2", "High connection pool usage");
/// var unhealthy = ShardHealthResult.Unhealthy("shard-3", "Connection refused", ex);
///
/// // Use in health checks
/// if (result.IsHealthy)
///     logger.LogInformation("Shard {ShardId} is operational", result.ShardId);
/// else if (result.IsDegraded)
///     logger.LogWarning("Shard {ShardId}: {Description}", result.ShardId, result.Description);
/// else
///     logger.LogError(result.Exception, "Shard {ShardId} is down", result.ShardId);
/// </code>
/// </example>
public sealed record ShardHealthResult(
    string ShardId,
    DatabaseHealthStatus Status,
    ConnectionPoolStats PoolStats,
    string? Description = null,
    Exception? Exception = null)
{
    /// <summary>
    /// Gets the shard identifier.
    /// </summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));

    /// <summary>
    /// Gets whether the shard is healthy.
    /// </summary>
    public bool IsHealthy => Status == DatabaseHealthStatus.Healthy;

    /// <summary>
    /// Gets whether the shard is degraded.
    /// </summary>
    public bool IsDegraded => Status == DatabaseHealthStatus.Degraded;

    /// <summary>
    /// Gets whether the shard is unhealthy.
    /// </summary>
    public bool IsUnhealthy => Status == DatabaseHealthStatus.Unhealthy;

    /// <summary>
    /// Creates a healthy shard result.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="poolStats">The pool statistics.</param>
    /// <param name="description">An optional description.</param>
    /// <returns>A healthy <see cref="ShardHealthResult"/>.</returns>
    public static ShardHealthResult Healthy(
        string shardId,
        ConnectionPoolStats? poolStats = null,
        string? description = null)
        => new(shardId, DatabaseHealthStatus.Healthy, poolStats ?? ConnectionPoolStats.CreateEmpty(), description);

    /// <summary>
    /// Creates a degraded shard result.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="poolStats">The pool statistics.</param>
    /// <param name="exception">An optional exception.</param>
    /// <returns>A degraded <see cref="ShardHealthResult"/>.</returns>
    public static ShardHealthResult Degraded(
        string shardId,
        string? description = null,
        ConnectionPoolStats? poolStats = null,
        Exception? exception = null)
        => new(shardId, DatabaseHealthStatus.Degraded, poolStats ?? ConnectionPoolStats.CreateEmpty(), description, exception);

    /// <summary>
    /// Creates an unhealthy shard result.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="exception">An optional exception.</param>
    /// <returns>An unhealthy <see cref="ShardHealthResult"/>.</returns>
    public static ShardHealthResult Unhealthy(
        string shardId,
        string? description = null,
        Exception? exception = null)
        => new(shardId, DatabaseHealthStatus.Unhealthy, ConnectionPoolStats.CreateEmpty(), description, exception);
}
