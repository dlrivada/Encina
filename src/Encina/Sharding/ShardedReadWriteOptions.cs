using Encina.Sharding.ReplicaSelection;

namespace Encina.Sharding;

/// <summary>
/// Configuration options for combining sharding with read/write separation.
/// </summary>
/// <remarks>
/// <para>
/// These options control how read replicas are selected within each shard, how health
/// checks are performed, and what happens when no replicas are available.
/// </para>
/// <para>
/// Per-shard overrides can be configured via <see cref="ShardInfo.ReplicaStrategy"/>.
/// When a shard does not specify a strategy, the <see cref="DefaultReplicaStrategy"/>
/// from this options class is used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ShardedReadWriteOptions
/// {
///     DefaultReplicaStrategy = ReplicaSelectionStrategy.LeastLatency,
///     FallbackToPrimaryWhenNoReplicas = true,
///     UnhealthyReplicaRecoveryDelay = TimeSpan.FromSeconds(60),
/// };
///
/// options.AddShard("shard-0", "Server=primary0;...",
///     new[] { "Server=replica0a;...", "Server=replica0b;..." },
///     ReplicaSelectionStrategy.RoundRobin);
///
/// options.AddShard("shard-1", "Server=primary1;...",
///     new[] { "Server=replica1a;..." });
/// </code>
/// </example>
public sealed class ShardedReadWriteOptions
{
    private readonly List<ShardReplicaConfig> _shards = [];

    /// <summary>
    /// Gets or sets the default replica selection strategy used when a shard does not
    /// specify its own strategy via <see cref="ShardInfo.ReplicaStrategy"/>.
    /// </summary>
    /// <value>Default: <see cref="ReplicaSelectionStrategy.RoundRobin"/>.</value>
    public ReplicaSelectionStrategy DefaultReplicaStrategy { get; set; } = ReplicaSelectionStrategy.RoundRobin;

    /// <summary>
    /// Gets or sets the interval between periodic replica health checks.
    /// </summary>
    /// <value>Default: 30 seconds.</value>
    public TimeSpan ReplicaHealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the delay before an unhealthy replica is reconsidered for selection.
    /// </summary>
    /// <value>Default: 30 seconds.</value>
    public TimeSpan UnhealthyReplicaRecoveryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum acceptable replication lag before a replica is considered stale.
    /// </summary>
    /// <remarks>
    /// When set, replicas with replication lag exceeding this value are excluded from selection.
    /// Set to <see langword="null"/> to disable replication lag monitoring.
    /// </remarks>
    /// <value>Default: <see langword="null"/> (disabled).</value>
    public TimeSpan? MaxAcceptableReplicationLag { get; set; }

    /// <summary>
    /// Gets or sets whether to fall back to the primary (write) connection string when
    /// a shard has no replicas configured or all replicas are unhealthy.
    /// </summary>
    /// <value>Default: <see langword="true"/>.</value>
    public bool FallbackToPrimaryWhenNoReplicas { get; set; } = true;

    /// <summary>
    /// Gets the configured shard replica entries.
    /// </summary>
    internal IReadOnlyList<ShardReplicaConfig> Shards => _shards;

    /// <summary>
    /// Adds a shard with its primary connection string, optional replicas, and optional per-shard strategy.
    /// </summary>
    /// <param name="shardId">The unique shard identifier.</param>
    /// <param name="primaryConnectionString">The connection string for the shard's primary (write) endpoint.</param>
    /// <param name="replicaConnectionStrings">Optional list of read replica connection strings.</param>
    /// <param name="strategy">Optional per-shard replica selection strategy override.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardedReadWriteOptions AddShard(
        string shardId,
        string primaryConnectionString,
        IReadOnlyList<string>? replicaConnectionStrings = null,
        ReplicaSelectionStrategy? strategy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryConnectionString);

        _shards.Add(new ShardReplicaConfig(
            shardId,
            primaryConnectionString,
            replicaConnectionStrings ?? [],
            strategy));

        return this;
    }

    /// <summary>
    /// Internal record for a shard's replica configuration entry.
    /// </summary>
    internal sealed record ShardReplicaConfig(
        string ShardId,
        string PrimaryConnectionString,
        IReadOnlyList<string> ReplicaConnectionStrings,
        ReplicaSelectionStrategy? Strategy);
}
