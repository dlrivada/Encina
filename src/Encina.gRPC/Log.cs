using Microsoft.Extensions.Logging;

namespace Encina.gRPC;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Processing gRPC request of type {RequestType}")]
    public static partial void ProcessingRequest(ILogger logger, string requestType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to process gRPC request of type {RequestType}")]
    public static partial void FailedToProcessRequest(ILogger logger, Exception exception, string requestType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Processing gRPC notification of type {NotificationType}")]
    public static partial void ProcessingNotification(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to process gRPC notification of type {NotificationType}")]
    public static partial void FailedToProcessNotification(ILogger logger, Exception exception, string notificationType);
}
