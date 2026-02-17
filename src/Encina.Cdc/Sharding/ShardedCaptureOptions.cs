using Encina.Sharding;

namespace Encina.Cdc.Sharding;

/// <summary>
/// Configuration options for sharded CDC capture.
/// Controls shard discovery, processing mode, topology change callbacks,
/// and health check thresholds.
/// </summary>
/// <example>
/// <code>
/// config.WithShardedCapture(opts =>
/// {
///     opts.AutoDiscoverShards = true;
///     opts.ProcessingMode = ShardedProcessingMode.Aggregated;
///     opts.MaxLagThreshold = TimeSpan.FromMinutes(5);
///     opts.OnShardAdded = shard => logger.LogInformation("Shard added: {ShardId}", shard.ShardId);
///     opts.OnShardRemoved = shardId => logger.LogInformation("Shard removed: {ShardId}", shardId);
/// });
/// </code>
/// </example>
public sealed class ShardedCaptureOptions
{
    /// <summary>
    /// Gets or sets whether to automatically discover shards from the
    /// <see cref="IShardTopologyProvider"/> topology at startup.
    /// Default is <c>true</c>.
    /// </summary>
    public bool AutoDiscoverShards { get; set; } = true;

    /// <summary>
    /// Gets or sets the processing mode for sharded CDC events.
    /// Default is <see cref="ShardedProcessingMode.Aggregated"/>.
    /// </summary>
    public ShardedProcessingMode ProcessingMode { get; set; } = ShardedProcessingMode.Aggregated;

    /// <summary>
    /// Gets or sets a callback invoked when a new shard is added to the topology.
    /// Receives the <see cref="ShardInfo"/> of the added shard.
    /// Default is <c>null</c> (no callback).
    /// </summary>
    public Action<ShardInfo>? OnShardAdded { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when a shard is removed from the topology.
    /// Receives the shard identifier of the removed shard.
    /// Default is <c>null</c> (no callback).
    /// </summary>
    public Action<string>? OnShardRemoved { get; set; }

    /// <summary>
    /// Gets or sets a custom position store implementation type.
    /// When set, this type is registered as the <see cref="Abstractions.IShardedCdcPositionStore"/>
    /// implementation instead of the default in-memory store.
    /// Must implement <see cref="Abstractions.IShardedCdcPositionStore"/>.
    /// Default is <c>null</c> (uses in-memory store).
    /// </summary>
    public Type? PositionStoreType { get; set; }

    /// <summary>
    /// Gets or sets the maximum lag threshold before health checks report degradation.
    /// When the time since the last processed event exceeds this threshold, the
    /// health check transitions to a degraded state.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan MaxLagThreshold { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the unique connector identifier for the sharded CDC connector.
    /// Default is <c>"sharded-cdc"</c>.
    /// </summary>
    public string ConnectorId { get; set; } = "sharded-cdc";
}
