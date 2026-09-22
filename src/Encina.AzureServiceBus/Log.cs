using Microsoft.Extensions.Logging;

namespace Encina.AzureServiceBus;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 4300, Level = LogLevel.Debug, Message = "Sending message of type {MessageType} to queue {Queue}")]
    public static partial void SendingToQueue(ILogger logger, string messageType, string queue);

    [LoggerMessage(EventId = 4301, Level = LogLevel.Debug, Message = "Successfully sent message of type {MessageType} to queue {Queue}")]
    public static partial void SuccessfullySentToQueue(ILogger logger, string messageType, string queue);

    [LoggerMessage(EventId = 4302, Level = LogLevel.Error, Message = "Failed to send message of type {MessageType} to queue {Queue}")]
    public static partial void FailedToSendToQueue(ILogger logger, Exception exception, string messageType, string queue);

    [LoggerMessage(EventId = 4303, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to topic {Topic}")]
    public static partial void PublishingToTopic(ILogger logger, string messageType, string topic);

    [LoggerMessage(EventId = 4304, Level = LogLevel.Debug, Message = "Successfully published message of type {MessageType} to topic {Topic}")]
    public static partial void SuccessfullyPublishedToTopic(ILogger logger, string messageType, string topic);

    [LoggerMessage(EventId = 4305, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType} to topic {Topic}")]
    public static partial void FailedToPublishToTopic(ILogger logger, Exception exception, string messageType, string topic);

    [LoggerMessage(EventId = 4306, Level = LogLevel.Debug, Message = "Scheduling message of type {MessageType} for {ScheduledTime} to queue {Queue}")]
    public static partial void SchedulingMessage(ILogger logger, string messageType, DateTimeOffset scheduledTime, string queue);

    [LoggerMessage(EventId = 4307, Level = LogLevel.Debug, Message = "Successfully scheduled message of type {MessageType} with sequence number {SequenceNumber}")]
    public static partial void SuccessfullyScheduledMessage(ILogger logger, string messageType, long sequenceNumber);

    [LoggerMessage(EventId = 4308, Level = LogLevel.Error, Message = "Failed to schedule message of type {MessageType}")]
    public static partial void FailedToScheduleMessage(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 4309, Level = LogLevel.Debug, Message = "Cancelling scheduled message with sequence number {SequenceNumber} from queue {Queue}")]
    public static partial void CancellingScheduledMessage(ILogger logger, long sequenceNumber, string queue);

    [LoggerMessage(EventId = 4310, Level = LogLevel.Debug, Message = "Successfully cancelled scheduled message with sequence number {SequenceNumber}")]
    public static partial void SuccessfullyCancelledMessage(ILogger logger, long sequenceNumber);

    [LoggerMessage(EventId = 4311, Level = LogLevel.Error, Message = "Failed to cancel scheduled message with sequence number {SequenceNumber}")]
    public static partial void FailedToCancelMessage(ILogger logger, Exception exception, long sequenceNumber);
}
