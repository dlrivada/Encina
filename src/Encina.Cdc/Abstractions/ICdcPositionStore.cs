using LanguageExt;

namespace Encina.Cdc.Abstractions;

/// <summary>
/// Provides persistent storage for CDC positions, enabling connectors to resume
/// from their last known position after a restart.
/// </summary>
/// <remarks>
/// Each connector is identified by a unique <c>connectorId</c>, allowing multiple
/// connectors to track their positions independently.
/// </remarks>
public interface ICdcPositionStore
{
    /// <summary>
    /// Retrieves the last saved position for the specified connector.
    /// </summary>
    /// <param name="connectorId">The unique identifier of the connector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The last saved position, or <see cref="Option{A}.None"/> if no position has been saved.
    /// </returns>
    Task<Either<EncinaError, Option<CdcPosition>>> GetPositionAsync(
        string connectorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current position for the specified connector, overwriting any previous value.
    /// </summary>
    /// <param name="connectorId">The unique identifier of the connector.</param>
    /// <param name="position">The position to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    Task<Either<EncinaError, Unit>> SavePositionAsync(
        string connectorId,
        CdcPosition position,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the saved position for the specified connector.
    /// </summary>
    /// <param name="connectorId">The unique identifier of the connector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    Task<Either<EncinaError, Unit>> DeletePositionAsync(
        string connectorId,
        CancellationToken cancellationToken = default);
}
