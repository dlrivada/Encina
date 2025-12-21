using LanguageExt;

namespace SimpleMediator.NServiceBus;

/// <summary>
/// Interface for publishing messages through NServiceBus.
/// </summary>
public interface INServiceBusMessagePublisher
{
    /// <summary>
    /// Sends a command to the specified destination.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class;

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="eventMessage">The event to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> PublishAsync<TEvent>(
        TEvent eventMessage,
        CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Schedules a message to be sent at a specific time.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to schedule.</param>
    /// <param name="deliveryTime">The time to deliver the message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> ScheduleAsync<TMessage>(
        TMessage message,
        DateTimeOffset deliveryTime,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
