using LanguageExt;

namespace Encina.Cdc.Abstractions;

/// <summary>
/// Represents a CDC connector that streams database changes as events.
/// Each provider (SQL Server, PostgreSQL, MySQL, MongoDB, Debezium) implements
/// this interface with its specific change capture mechanism.
/// </summary>
/// <remarks>
/// <para>
/// The connector produces a potentially infinite stream of <see cref="ChangeEvent"/>
/// instances wrapped in <see cref="Either{EncinaError, ChangeEvent}"/> following
/// the Railway Oriented Programming pattern. This allows individual change events
/// to report errors without terminating the stream.
/// </para>
/// <para>
/// All implementations must support position tracking for resume after restart.
/// </para>
/// </remarks>
public interface ICdcConnector
{
    /// <summary>
    /// Gets the unique identifier of this connector instance,
    /// used for position tracking and diagnostics.
    /// </summary>
    string ConnectorId { get; }

    /// <summary>
    /// Streams database changes as an asynchronous enumerable of change events.
    /// The stream continues until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the stream.</param>
    /// <returns>
    /// An asynchronous stream of change events, where each element is either
    /// an <see cref="EncinaError"/> (for recoverable errors) or a <see cref="ChangeEvent"/>.
    /// </returns>
    IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current position of the change stream.
    /// This can be used to determine how far behind a connector is.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or the current position in the change stream.</returns>
    Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default);
}
