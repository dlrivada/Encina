using LanguageExt;

namespace Encina.Cdc.Abstractions;

/// <summary>
/// Provides persistent storage for CDC positions in sharded environments, enabling
/// each shard's connector to resume from its last known position independently.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="ICdcPositionStore"/> which tracks a single position per connector,
/// this interface tracks positions per <c>(shardId, connectorId)</c> composite key.
/// This allows a single sharded CDC connector to manage multiple shards, each with
/// its own independent position in the change stream.
/// </para>
/// <para>
/// Implementations must be thread-safe, as multiple shard processors may save
/// positions concurrently.
/// </para>
/// </remarks>
public interface IShardedCdcPositionStore
{
    /// <summary>
    /// Retrieves the last saved position for a specific shard within a connector.
    /// </summary>
    /// <param name="shardId">The unique identifier of the shard.</param>
    /// <param name="connectorId">The unique identifier of the connector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The last saved position for the shard, or <see cref="Option{A}.None"/> if no position
    /// has been saved for this shard/connector combination.
    /// </returns>
    Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(
        string shardId,
        string connectorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current position for a specific shard within a connector, overwriting any previous value.
    /// </summary>
    /// <param name="shardId">The unique identifier of the shard.</param>
    /// <param name="connectorId">The unique identifier of the connector.</param>
    /// <param name="position">The position to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    Task<Either<EncinaError, Unit>> SavePositionAsync(
        string shardId,
        string connectorId,
        CdcPosition position,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all saved positions for the specified connector across all shards.
    /// </summary>
    /// <param name="connectorId">The unique identifier of the connector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A read-only dictionary mapping shard identifiers to their last saved positions.
    /// Returns an empty dictionary if no positions have been saved.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>> GetAllPositionsAsync(
        string connectorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the saved position for a specific shard within a connector.
    /// </summary>
    /// <param name="shardId">The unique identifier of the shard.</param>
    /// <param name="connectorId">The unique identifier of the connector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    Task<Either<EncinaError, Unit>> DeletePositionAsync(
        string shardId,
        string connectorId,
        CancellationToken cancellationToken = default);
}
