using Microsoft.Extensions.Logging;

namespace Encina.NServiceBus;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Sending command of type {CommandType} via NServiceBus")]
    public static partial void SendingCommand(ILogger logger, string commandType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully sent command of type {CommandType}")]
    public static partial void SuccessfullySentCommand(ILogger logger, string commandType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to send command of type {CommandType}")]
    public static partial void FailedToSendCommand(ILogger logger, Exception exception, string commandType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Publishing event of type {EventType} via NServiceBus")]
    public static partial void PublishingEvent(ILogger logger, string eventType);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Successfully published event of type {EventType}")]
    public static partial void SuccessfullyPublishedEvent(ILogger logger, string eventType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to publish event of type {EventType}")]
    public static partial void FailedToPublishEvent(ILogger logger, Exception exception, string eventType);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Scheduling message of type {MessageType} for {DeliveryTime} via NServiceBus")]
    public static partial void SchedulingMessage(ILogger logger, string messageType, DateTimeOffset deliveryTime);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Successfully scheduled message of type {MessageType} for {DeliveryTime}")]
    public static partial void SuccessfullyScheduledMessage(ILogger logger, string messageType, DateTimeOffset deliveryTime);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Failed to schedule message of type {MessageType}")]
    public static partial void FailedToScheduleMessage(ILogger logger, Exception exception, string messageType);
}
