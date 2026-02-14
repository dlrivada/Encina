namespace Encina.Cdc.Sharding;

/// <summary>
/// Defines how sharded CDC events are streamed and processed.
/// </summary>
public enum ShardedProcessingMode
{
    /// <summary>
    /// Streams events from all shards in parallel and merges them into a single
    /// ordered stream via <see cref="Abstractions.IShardedCdcConnector.StreamAllShardsAsync"/>.
    /// Best for scenarios requiring cross-shard ordering guarantees.
    /// </summary>
    Aggregated = 0,

    /// <summary>
    /// Processes each shard independently in parallel, with per-shard streaming
    /// via <see cref="Abstractions.IShardedCdcConnector.StreamShardAsync"/>.
    /// Best for high-throughput scenarios where cross-shard ordering is not required.
    /// </summary>
    PerShardParallel = 1
}
