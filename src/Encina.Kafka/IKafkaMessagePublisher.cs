using LanguageExt;

namespace Encina.Kafka;

/// <summary>
/// Interface for publishing messages through Kafka.
/// </summary>
public interface IKafkaMessagePublisher
{
    /// <summary>
    /// Produces a message to a topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to produce.</param>
    /// <param name="topic">The topic name. If null, uses the default topic.</param>
    /// <param name="key">The partition key. If null, round-robin partitioning is used.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the delivery result.</returns>
    ValueTask<Either<MediatorError, KafkaDeliveryResult>> ProduceAsync<TMessage>(
        TMessage message,
        string? topic = null,
        string? key = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Produces a batch of messages to a topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the messages.</typeparam>
    /// <param name="messages">The messages to produce with their optional keys.</param>
    /// <param name="topic">The topic name. If null, uses the default topic.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the list of delivery results.</returns>
    ValueTask<Either<MediatorError, IReadOnlyList<KafkaDeliveryResult>>> ProduceBatchAsync<TMessage>(
        IEnumerable<(TMessage Message, string? Key)> messages,
        string? topic = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Produces a message with headers.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to produce.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="topic">The topic name. If null, uses the default topic.</param>
    /// <param name="key">The partition key. If null, round-robin partitioning is used.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the delivery result.</returns>
    ValueTask<Either<MediatorError, KafkaDeliveryResult>> ProduceWithHeadersAsync<TMessage>(
        TMessage message,
        IDictionary<string, byte[]> headers,
        string? topic = null,
        string? key = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}

/// <summary>
/// Result of a Kafka delivery operation.
/// </summary>
/// <param name="Topic">The topic the message was delivered to.</param>
/// <param name="Partition">The partition the message was delivered to.</param>
/// <param name="Offset">The offset of the message within the partition.</param>
/// <param name="Timestamp">The timestamp of the message.</param>
public sealed record KafkaDeliveryResult(
    string Topic,
    int Partition,
    long Offset,
    DateTimeOffset Timestamp);
