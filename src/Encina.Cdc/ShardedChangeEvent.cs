using Encina.Cdc.Abstractions;

namespace Encina.Cdc;

/// <summary>
/// Wraps a standard <see cref="ChangeEvent"/> with shard-specific context, enabling
/// per-shard CDC position tracking in sharded database environments.
/// </summary>
/// <remarks>
/// <para>
/// In a sharded architecture, each shard produces its own independent change stream with
/// its own position sequence. <see cref="ShardedChangeEvent"/> associates each captured event
/// with the shard it originated from and the shard-specific position at which it was captured.
/// </para>
/// <para>
/// This type is consumed by <c>IShardedCdcConnector</c> (defined in later phases) to produce
/// a unified, multi-shard event stream while preserving per-shard position information
/// for reliable resume after restart.
/// </para>
/// </remarks>
/// <param name="ShardId">The unique identifier of the shard that produced this change event.</param>
/// <param name="Event">The underlying change event captured from the shard's change stream.</param>
/// <param name="ShardPosition">The CDC position within the originating shard's change stream.</param>
public sealed record ShardedChangeEvent(
    string ShardId,
    ChangeEvent Event,
    CdcPosition ShardPosition);
