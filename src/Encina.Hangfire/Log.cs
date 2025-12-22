using Microsoft.Extensions.Logging;

namespace Encina.Hangfire;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // Request Job (1-4)
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Executing Hangfire job for request {RequestType}")]
    public static partial void ExecutingRequestJob(ILogger logger, string requestType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Hangfire job completed successfully for request {RequestType}")]
    public static partial void RequestJobCompleted(ILogger logger, string requestType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Hangfire job failed for request {RequestType}: {ErrorMessage}")]
    public static partial void RequestJobFailed(ILogger logger, string requestType, string errorMessage);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Unhandled exception in Hangfire job for request {RequestType}")]
    public static partial void RequestJobException(ILogger logger, Exception exception, string requestType);

    // Notification Job (10-13)
    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Publishing Hangfire notification job for {NotificationType}")]
    public static partial void PublishingNotificationJob(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Hangfire notification job completed successfully for {NotificationType}")]
    public static partial void NotificationJobCompleted(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Unhandled exception in Hangfire notification job for {NotificationType}")]
    public static partial void NotificationJobException(ILogger logger, Exception exception, string notificationType);
}
