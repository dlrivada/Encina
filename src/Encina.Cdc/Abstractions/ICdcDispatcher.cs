using LanguageExt;

namespace Encina.Cdc.Abstractions;

/// <summary>
/// Routes change events to the appropriate typed handlers based on table name mappings.
/// The dispatcher deserializes the raw <see cref="ChangeEvent.Before"/> and
/// <see cref="ChangeEvent.After"/> objects to the target entity type and invokes
/// the registered <see cref="IChangeEventHandler{TEntity}"/>.
/// </summary>
public interface ICdcDispatcher
{
    /// <summary>
    /// Dispatches a change event to the appropriate typed handler.
    /// </summary>
    /// <param name="changeEvent">The change event to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    /// <remarks>
    /// If no handler is registered for the table in the change event,
    /// the dispatcher logs a warning and returns success (skips the event).
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DispatchAsync(
        ChangeEvent changeEvent,
        CancellationToken cancellationToken = default);
}
