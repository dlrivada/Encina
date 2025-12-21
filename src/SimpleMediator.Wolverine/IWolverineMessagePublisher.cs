using LanguageExt;

namespace SimpleMediator.Wolverine;

/// <summary>
/// Interface for publishing messages through Wolverine.
/// </summary>
public interface IWolverineMessagePublisher
{
    /// <summary>
    /// Publishes a message to Wolverine for asynchronous processing.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Sends a message to a specific endpoint for processing.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="endpointName">The name of the endpoint to send to.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> SendToEndpointAsync<TMessage>(
        string endpointName,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Schedules a message to be processed at a specific time.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to schedule.</param>
    /// <param name="scheduledTime">The time to process the message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> ScheduleAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
