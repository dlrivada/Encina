using Microsoft.Extensions.Logging;

namespace Encina.Wolverine;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} via Wolverine")]
    public static partial void PublishingMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully published message of type {MessageType}")]
    public static partial void SuccessfullyPublishedMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType}")]
    public static partial void FailedToPublishMessage(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Sending message of type {MessageType} to endpoint {Endpoint}")]
    public static partial void SendingToEndpoint(ILogger logger, string messageType, string endpoint);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Successfully sent message of type {MessageType} to endpoint {Endpoint}")]
    public static partial void SuccessfullySentToEndpoint(ILogger logger, string messageType, string endpoint);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to send message of type {MessageType} to endpoint {Endpoint}")]
    public static partial void FailedToSendToEndpoint(ILogger logger, Exception exception, string messageType, string endpoint);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Scheduling message of type {MessageType} for {ScheduledTime}")]
    public static partial void SchedulingMessage(ILogger logger, string messageType, DateTimeOffset scheduledTime);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Successfully scheduled message of type {MessageType} for {ScheduledTime}")]
    public static partial void SuccessfullyScheduledMessage(ILogger logger, string messageType, DateTimeOffset scheduledTime);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Failed to schedule message of type {MessageType} for {ScheduledTime}")]
    public static partial void FailedToScheduleMessage(ILogger logger, Exception exception, string messageType, DateTimeOffset scheduledTime);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Handling Wolverine message of type {MessageType} via Encina")]
    public static partial void HandlingMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Successfully handled message of type {MessageType}")]
    public static partial void SuccessfullyHandledMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Message handling failed for type {MessageType}: {ErrorMessage}")]
    public static partial void MessageHandlingFailed(ILogger logger, string messageType, string errorMessage);

    [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "Handling Wolverine notification of type {NotificationType} via Encina")]
    public static partial void HandlingNotification(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 14, Level = LogLevel.Debug, Message = "Successfully published notification of type {NotificationType}")]
    public static partial void SuccessfullyPublishedNotification(ILogger logger, string notificationType);
}
