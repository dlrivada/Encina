using Microsoft.Extensions.Logging;

namespace Encina.AzureServiceBus;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Sending message of type {MessageType} to queue {Queue}")]
    public static partial void SendingToQueue(ILogger logger, string messageType, string queue);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully sent message of type {MessageType} to queue {Queue}")]
    public static partial void SuccessfullySentToQueue(ILogger logger, string messageType, string queue);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to send message of type {MessageType} to queue {Queue}")]
    public static partial void FailedToSendToQueue(ILogger logger, Exception exception, string messageType, string queue);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to topic {Topic}")]
    public static partial void PublishingToTopic(ILogger logger, string messageType, string topic);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Successfully published message of type {MessageType} to topic {Topic}")]
    public static partial void SuccessfullyPublishedToTopic(ILogger logger, string messageType, string topic);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType} to topic {Topic}")]
    public static partial void FailedToPublishToTopic(ILogger logger, Exception exception, string messageType, string topic);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Scheduling message of type {MessageType} for {ScheduledTime} to queue {Queue}")]
    public static partial void SchedulingMessage(ILogger logger, string messageType, DateTimeOffset scheduledTime, string queue);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Successfully scheduled message of type {MessageType} with sequence number {SequenceNumber}")]
    public static partial void SuccessfullyScheduledMessage(ILogger logger, string messageType, long sequenceNumber);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Failed to schedule message of type {MessageType}")]
    public static partial void FailedToScheduleMessage(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Cancelling scheduled message with sequence number {SequenceNumber} from queue {Queue}")]
    public static partial void CancellingScheduledMessage(ILogger logger, long sequenceNumber, string queue);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Successfully cancelled scheduled message with sequence number {SequenceNumber}")]
    public static partial void SuccessfullyCancelledMessage(ILogger logger, long sequenceNumber);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Failed to cancel scheduled message with sequence number {SequenceNumber}")]
    public static partial void FailedToCancelMessage(ILogger logger, Exception exception, long sequenceNumber);
}
