using Microsoft.Extensions.Logging;

namespace Encina.AmazonSQS;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Sending message of type {MessageType} to queue {Queue}")]
    public static partial void SendingToQueue(ILogger logger, string messageType, string queue);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully sent message of type {MessageType} with ID {MessageId}")]
    public static partial void SuccessfullySentMessage(ILogger logger, string messageType, string messageId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to send message of type {MessageType} to queue {Queue}")]
    public static partial void FailedToSendToQueue(ILogger logger, Exception exception, string messageType, string queue);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to topic {Topic}")]
    public static partial void PublishingToTopic(ILogger logger, string messageType, string topic);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Successfully published message of type {MessageType} with ID {MessageId}")]
    public static partial void SuccessfullyPublishedMessage(ILogger logger, string messageType, string messageId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType} to topic {Topic}")]
    public static partial void FailedToPublishToTopic(ILogger logger, Exception exception, string messageType, string topic);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Sending batch of {Count} messages of type {MessageType}")]
    public static partial void SendingBatch(ILogger logger, int count, string messageType);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Batch send partially failed: {FailedCount} of {TotalCount} messages failed")]
    public static partial void BatchPartiallyFailed(ILogger logger, int failedCount, int totalCount);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "Successfully sent {Count} messages")]
    public static partial void SuccessfullySentBatch(ILogger logger, int count);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Failed to send batch of messages of type {MessageType}")]
    public static partial void FailedToSendBatch(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Sending FIFO message of type {MessageType} to queue {Queue} with group {Group}")]
    public static partial void SendingFifoMessage(ILogger logger, string messageType, string queue, string group);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Successfully sent FIFO message with ID {MessageId}")]
    public static partial void SuccessfullySentFifoMessage(ILogger logger, string messageId);

    [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Failed to send FIFO message of type {MessageType}")]
    public static partial void FailedToSendFifoMessage(ILogger logger, Exception exception, string messageType);
}
