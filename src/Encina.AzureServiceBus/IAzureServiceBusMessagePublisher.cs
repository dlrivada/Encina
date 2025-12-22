using LanguageExt;

namespace Encina.AzureServiceBus;

/// <summary>
/// Interface for publishing messages through Azure Service Bus.
/// </summary>
public interface IAzureServiceBusMessagePublisher
{
    /// <summary>
    /// Sends a message to a queue.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="queueName">The queue name. If null, uses the default queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> SendToQueueAsync<TMessage>(
        TMessage message,
        string? queueName = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="topicName">The topic name. If null, uses the default topic.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> PublishToTopicAsync<TMessage>(
        TMessage message,
        string? topicName = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Schedules a message to be delivered at a specific time.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to schedule.</param>
    /// <param name="scheduledEnqueueTime">The time to deliver the message.</param>
    /// <param name="queueName">The queue name. If null, uses the default queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or the sequence number.</returns>
    ValueTask<Either<EncinaError, long>> ScheduleAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledEnqueueTime,
        string? queueName = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number of the scheduled message.</param>
    /// <param name="queueName">The queue name. If null, uses the default queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> CancelScheduledAsync(
        long sequenceNumber,
        string? queueName = null,
        CancellationToken cancellationToken = default);
}
