using Microsoft.Extensions.Logging;

namespace Encina.SignalR;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // SignalRNotificationBroadcaster: EventIds 1-10
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Notification {NotificationType} skipped due to conditional property")]
    public static partial void NotificationSkippedConditional(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "No hub context available for broadcasting notification {NotificationType}")]
    public static partial void NoHubContextAvailable(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Broadcast notification {NotificationType} via method {MethodName}")]
    public static partial void BroadcastNotification(ILogger logger, string notificationType, string methodName);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to broadcast notification {NotificationType}")]
    public static partial void FailedToBroadcastNotification(ILogger logger, Exception exception, string notificationType);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Conditional property '{PropertyName}' not found or not boolean on {NotificationType}")]
    public static partial void ConditionalPropertyNotFound(ILogger logger, string propertyName, string notificationType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Property '{PropertyName}' not found on {NotificationType}")]
    public static partial void PropertyNotFound(ILogger logger, string propertyName, string notificationType);

    // EncinaHub: EventIds 11-25
    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Error executing command {CommandType}")]
    public static partial void ErrorExecutingCommand(ILogger logger, Exception exception, string commandType);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error executing query {QueryType}")]
    public static partial void ErrorExecutingQuery(ILogger logger, Exception exception, string queryType);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Notification type '{NotificationType}' not found")]
    public static partial void NotificationTypeNotFound(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "Failed to deserialize notification of type '{NotificationType}'")]
    public static partial void FailedToDeserializeNotification(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "Error publishing notification {NotificationType}")]
    public static partial void ErrorPublishingNotification(ILogger logger, Exception exception, string notificationType);

    // SignalRBroadcastHandler: EventIds 26-30
    [LoggerMessage(EventId = 26, Level = LogLevel.Error, Message = "Failed to broadcast notification {NotificationType} to SignalR")]
    public static partial void FailedToBroadcastNotificationToSignalR(ILogger logger, Exception exception, string notificationType);
}
