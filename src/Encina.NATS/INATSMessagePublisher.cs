using LanguageExt;

namespace Encina.NATS;

/// <summary>
/// Interface for publishing messages through NATS.
/// </summary>
public interface INATSMessagePublisher
{
    /// <summary>
    /// Publishes a message to a subject.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="subject">The subject. If null, derived from message type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? subject = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Performs a request/reply pattern.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="subject">The subject. If null, derived from request type.</param>
    /// <param name="timeout">The timeout for the reply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or the response.</returns>
    ValueTask<Either<EncinaError, TResponse>> RequestAsync<TRequest, TResponse>(
        TRequest request,
        string? subject = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Publishes a message to JetStream for persistent delivery.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="subject">The subject. If null, derived from message type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or the publish acknowledgment.</returns>
    ValueTask<Either<EncinaError, NATSPublishAck>> JetStreamPublishAsync<TMessage>(
        TMessage message,
        string? subject = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}

/// <summary>
/// Acknowledgment from a JetStream publish operation.
/// </summary>
/// <param name="Stream">The stream the message was published to.</param>
/// <param name="Sequence">The sequence number of the message.</param>
/// <param name="Duplicate">Whether this was a duplicate message.</param>
public sealed record NATSPublishAck(
    string Stream,
    ulong Sequence,
    bool Duplicate);
