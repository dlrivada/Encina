using LanguageExt;

namespace Encina.Cdc.Abstractions;

/// <summary>
/// Aggregates multiple per-shard <see cref="ICdcConnector"/> instances into a unified
/// change stream, enabling CDC across a sharded database topology.
/// </summary>
/// <remarks>
/// <para>
/// Each shard in the topology has its own independent <see cref="ICdcConnector"/> with
/// its own position in the change stream. This interface provides two streaming modes:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="StreamAllShardsAsync"/>: Merges events from all shards into a single
///       ordered stream of <see cref="ShardedChangeEvent"/> instances.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="StreamShardAsync"/>: Isolates the stream to a specific shard for
///       targeted consumption.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Implementations must be thread-safe and support concurrent streaming from multiple consumers.
/// </para>
/// </remarks>
public interface IShardedCdcConnector : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier of this sharded connector instance.
    /// </summary>
    /// <returns>The connector identifier used for position tracking and diagnostics.</returns>
    string GetConnectorId();

    /// <summary>
    /// Gets the identifiers of all currently active shards managed by this connector.
    /// </summary>
    IReadOnlyList<string> ActiveShardIds { get; }

    /// <summary>
    /// Streams change events from all shards as a unified, ordered stream.
    /// Events are ordered by <see cref="ChangeMetadata.CapturedAtUtc"/>, with
    /// <see cref="ShardedChangeEvent.ShardId"/> as a tiebreaker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop all shard streams.</param>
    /// <returns>
    /// An asynchronous stream of <see cref="ShardedChangeEvent"/> instances from all active shards,
    /// where each element is either an <see cref="EncinaError"/> or a change event with shard context.
    /// </returns>
    IAsyncEnumerable<Either<EncinaError, ShardedChangeEvent>> StreamAllShardsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams change events from a specific shard only.
    /// </summary>
    /// <param name="shardId">The unique identifier of the shard to stream from.</param>
    /// <param name="cancellationToken">Cancellation token to stop the stream.</param>
    /// <returns>
    /// An asynchronous stream of change events from the specified shard,
    /// where each element is either an <see cref="EncinaError"/> or a <see cref="ChangeEvent"/>.
    /// </returns>
    IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamShardAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current CDC positions across all active shards.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A read-only dictionary mapping shard identifiers to their current CDC positions.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>> GetAllPositionsAsync(
        CancellationToken cancellationToken = default);
}
