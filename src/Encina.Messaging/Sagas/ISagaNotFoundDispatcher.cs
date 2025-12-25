using LanguageExt;

namespace Encina.Messaging.Sagas;

/// <summary>
/// Dispatches saga not found events to registered handlers.
/// </summary>
/// <remarks>
/// <para>
/// This dispatcher is used internally by the saga infrastructure to invoke
/// registered <see cref="IHandleSagaNotFound{TMessage}"/> handlers when
/// a saga cannot be found for a given message.
/// </para>
/// <para>
/// If no handler is registered for a message type, the dispatcher returns
/// a successful result (pass-through behavior).
/// </para>
/// </remarks>
public interface ISagaNotFoundDispatcher
{
    /// <summary>
    /// Dispatches a saga not found event to the appropriate handler.
    /// </summary>
    /// <typeparam name="TMessage">The type of message that failed to correlate.</typeparam>
    /// <param name="message">The message that could not be correlated.</param>
    /// <param name="context">Context providing saga information and available actions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A successful result if the handler completed or no handler was registered;
    /// an error if the handler threw an exception.
    /// </returns>
    Task<Either<EncinaError, Unit>> DispatchAsync<TMessage>(
        TMessage message,
        SagaNotFoundContext context,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
