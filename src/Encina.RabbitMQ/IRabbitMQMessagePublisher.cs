using LanguageExt;

namespace Encina.RabbitMQ;

/// <summary>
/// Interface for publishing messages through RabbitMQ.
/// </summary>
public interface IRabbitMQMessagePublisher
{
    /// <summary>
    /// Publishes a message to an exchange.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="routingKey">The routing key for the message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? routingKey = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Sends a message directly to a queue.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> SendToQueueAsync<TMessage>(
        string queueName,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Performs a request/reply pattern.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="timeout">The timeout for the reply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the response.</returns>
    ValueTask<Either<MediatorError, TResponse>> RequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;
}
