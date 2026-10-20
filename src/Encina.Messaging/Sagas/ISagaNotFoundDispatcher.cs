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
/// <para>
/// With the dead letter queue registered and <c>DeadLetterOptions.IntegrateWithSagas</c> on, the dispatcher connects
/// <see cref="SagaNotFoundContext.MoveToDeadLetterAsync"/> to the dead letter capture: the handler's call stores the
/// message, keyed by <see cref="SagaNotFoundContext.SourceMessageId"/>. When the handler's last
/// <see cref="SagaNotFoundContext.MoveToDeadLetterAsync"/> call failed and it then neither moved nor ignored the
/// message, the dispatcher returns that error instead of success, so the caller does not acknowledge a message that
/// is in no dead letter queue.
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
    /// A successful result if the handler completed or no handler was registered; an error if the handler threw
    /// an exception, or if its move to the dead letter queue failed and it did not recover from it.
    /// </returns>
    Task<Either<EncinaError, Unit>> DispatchAsync<TMessage>(
        TMessage message,
        SagaNotFoundContext context,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
