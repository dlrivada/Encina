using Microsoft.Extensions.Logging;

namespace Encina.MassTransit;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // Publisher (1-4)
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Publishing notification {NotificationType} via MassTransit")]
    public static partial void PublishingNotification(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Successfully published notification {NotificationType} via MassTransit")]
    public static partial void PublishedNotification(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Sending request {RequestType} to {DestinationAddress} via MassTransit")]
    public static partial void SendingRequest(ILogger logger, string requestType, Uri destinationAddress);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Successfully sent request {RequestType} to {DestinationAddress} via MassTransit")]
    public static partial void SentRequest(ILogger logger, string requestType, Uri destinationAddress);

    // Request Consumer (10-12)
    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Consuming MassTransit message {RequestType} with MessageId {MessageId}")]
    public static partial void ConsumingRequest(ILogger logger, string requestType, Guid? messageId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Successfully processed request {RequestType} with MessageId {MessageId}")]
    public static partial void ProcessedRequest(ILogger logger, string requestType, Guid? messageId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Failed to process request {RequestType} with MessageId {MessageId}: {ErrorMessage}")]
    public static partial void FailedToProcessRequest(ILogger logger, string requestType, Guid? messageId, string errorMessage);

    // Notification Consumer (20-22)
    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Consuming MassTransit notification {NotificationType} with MessageId {MessageId}")]
    public static partial void ConsumingNotification(ILogger logger, string notificationType, Guid? messageId);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Successfully published notification {NotificationType} with MessageId {MessageId}")]
    public static partial void PublishedNotificationWithMessageId(ILogger logger, string notificationType, Guid? messageId);

    [LoggerMessage(EventId = 22, Level = LogLevel.Error, Message = "Failed to publish notification {NotificationType} with MessageId {MessageId}: {ErrorMessage}")]
    public static partial void FailedToPublishNotification(ILogger logger, string notificationType, Guid? messageId, string errorMessage);
}
