using LanguageExt;

namespace Encina.AmazonSQS;

/// <summary>
/// Interface for publishing messages through Amazon SQS and SNS.
/// </summary>
public interface IAmazonSQSMessagePublisher
{
    /// <summary>
    /// Sends a message to an SQS queue.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="queueUrl">The queue URL. If null, uses the default queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or the message ID.</returns>
    ValueTask<Either<EncinaError, string>> SendToQueueAsync<TMessage>(
        TMessage message,
        string? queueUrl = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Publishes a message to an SNS topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="topicArn">The topic ARN. If null, uses the default topic.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or the message ID.</returns>
    ValueTask<Either<EncinaError, string>> PublishToTopicAsync<TMessage>(
        TMessage message,
        string? topicArn = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Sends a batch of messages to an SQS queue.
    /// </summary>
    /// <typeparam name="TMessage">The type of the messages.</typeparam>
    /// <param name="messages">The messages to send.</param>
    /// <param name="queueUrl">The queue URL. If null, uses the default queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or the list of message IDs.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<string>>> SendBatchAsync<TMessage>(
        IEnumerable<TMessage> messages,
        string? queueUrl = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Sends a message to a FIFO queue with deduplication.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="messageGroupId">The message group ID for FIFO ordering.</param>
    /// <param name="deduplicationId">The deduplication ID. If null, content-based deduplication is used.</param>
    /// <param name="queueUrl">The queue URL. If null, uses the default queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or the message ID.</returns>
    ValueTask<Either<EncinaError, string>> SendToFifoQueueAsync<TMessage>(
        TMessage message,
        string messageGroupId,
        string? deduplicationId = null,
        string? queueUrl = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
