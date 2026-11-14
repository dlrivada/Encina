using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.SignalR;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class Log
{
    // SignalRNotificationBroadcaster: EventIds 4600-4605
    [LoggerMessage(EventId = 4600, Level = LogLevel.Debug, Message = "Notification {NotificationType} skipped due to conditional property")]
    public static partial void NotificationSkippedConditional(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 4601, Level = LogLevel.Warning, Message = "No hub context available for broadcasting notification {NotificationType}")]
    public static partial void NoHubContextAvailable(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 4602, Level = LogLevel.Debug, Message = "Broadcast notification {NotificationType} via method {MethodName}")]
    public static partial void BroadcastNotification(ILogger logger, string notificationType, string methodName);

    [LoggerMessage(EventId = 4603, Level = LogLevel.Error, Message = "Failed to broadcast notification {NotificationType}")]
    public static partial void FailedToBroadcastNotification(ILogger logger, Exception exception, string notificationType);

    [LoggerMessage(EventId = 4604, Level = LogLevel.Warning, Message = "Conditional property '{PropertyName}' not found or not boolean on {NotificationType}")]
    public static partial void ConditionalPropertyNotFound(ILogger logger, string propertyName, string notificationType);

    [LoggerMessage(EventId = 4605, Level = LogLevel.Warning, Message = "Property '{PropertyName}' not found on {NotificationType}")]
    public static partial void PropertyNotFound(ILogger logger, string propertyName, string notificationType);

    // EncinaHub: EventIds 4606-4610. Type names are the registered FullName; 4608 uses ClientTypeName.ForLog.
    [LoggerMessage(EventId = 4606, Level = LogLevel.Error, Message = "Error executing command {CommandType}")]
    public static partial void ErrorExecutingCommand(ILogger logger, Exception exception, string commandType);

    [LoggerMessage(EventId = 4607, Level = LogLevel.Error, Message = "Error executing query {QueryType}")]
    public static partial void ErrorExecutingQuery(ILogger logger, Exception exception, string queryType);

    // Debug, not Warning: an unauthenticated client can trigger it on every call (log flooding).
    [LoggerMessage(EventId = 4608, Level = LogLevel.Debug, Message = "EncinaHub denied a {MessageKind} call for type name '{TypeName}' (unknown, not exposed, wrong kind or unauthenticated)")]
    public static partial void HubCallDenied(ILogger logger, HubMessageKind messageKind, string typeName);

    [LoggerMessage(EventId = 4609, Level = LogLevel.Warning, Message = "Failed to deserialize notification of type '{NotificationType}'")]
    public static partial void FailedToDeserializeNotification(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 4610, Level = LogLevel.Error, Message = "Error publishing notification {NotificationType}")]
    public static partial void ErrorPublishingNotification(ILogger logger, Exception exception, string notificationType);

    // SignalRBroadcastHandler: EventIds 4611
    [LoggerMessage(EventId = 4611, Level = LogLevel.Error, Message = "Failed to broadcast notification {NotificationType} to SignalR")]
    public static partial void FailedToBroadcastNotificationToSignalR(ILogger logger, Exception exception, string notificationType);

    // SignalRAllowListStartupCheck: EventIds 4612-4613
    [LoggerMessage(EventId = 4612, Level = LogLevel.Information, Message = "EncinaHub allow-list exposes {MessageKind} type {TypeName} to anonymous callers (allowAnonymous: true)")]
    public static partial void AnonymousHubTypeAllowed(ILogger logger, HubMessageKind messageKind, string typeName);

    [LoggerMessage(EventId = 4613, Level = LogLevel.Warning, Message = "An EncinaHub is mapped but the allow-list is empty, so every SendCommand, SendQuery and PublishNotification call is denied; register types with SignalROptions.AllowRequest or AllowNotification")]
    public static partial void EmptyAllowListWithMappedHub(ILogger logger);

    // SignalRNotificationBroadcaster explicit audience: EventId 4614. Only the type FullName, never the payload.
    [LoggerMessage(EventId = 4614, Level = LogLevel.Error, Message = "Refused to broadcast notification {NotificationType}: it declares no targets and is not registered with SignalROptions.AllowBroadcastToAll, or its declared targets resolved to no user or group; nothing was sent")]
    public static partial void BroadcastRefusedNoTargets(ILogger logger, string notificationType);
}
