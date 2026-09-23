using Microsoft.Extensions.Logging;

namespace Encina.Hangfire;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
/// <remarks>Event IDs: 4000-4009 (see <c>EventIdRanges.Hangfire</c>, 4000-4049).</remarks>
internal static partial class Log
{
    // Request Job
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Executing Hangfire job for request {RequestType}")]
    public static partial void ExecutingRequestJob(ILogger logger, string requestType);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Hangfire job completed successfully for request {RequestType}")]
    public static partial void RequestJobCompleted(ILogger logger, string requestType);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Error, Message = "Hangfire job failed for request {RequestType} with error code {ErrorCode}")]
    public static partial void RequestJobFailed(ILogger logger, string requestType, string errorCode);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Error, Message = "Unhandled exception in Hangfire job for request {RequestType}")]
    public static partial void RequestJobException(ILogger logger, Exception exception, string requestType);

    // Notification Job
    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Publishing Hangfire notification job for {NotificationType}")]
    public static partial void PublishingNotificationJob(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Information, Message = "Hangfire notification job completed successfully for {NotificationType}")]
    public static partial void NotificationJobCompleted(ILogger logger, string notificationType);

    [LoggerMessage(EventId = 4006, Level = LogLevel.Error, Message = "Unhandled exception in Hangfire notification job for {NotificationType}")]
    public static partial void NotificationJobException(ILogger logger, Exception exception, string notificationType);

    [LoggerMessage(EventId = 4007, Level = LogLevel.Error, Message = "Hangfire notification job failed for {NotificationType} with error code {ErrorCode}")]
    public static partial void NotificationJobFailed(ILogger logger, string notificationType, string errorCode);

    // Cancellation
    [LoggerMessage(EventId = 4008, Level = LogLevel.Warning, Message = "Hangfire job for request {RequestType} was cancelled")]
    public static partial void RequestJobCancelled(ILogger logger, string requestType);

    [LoggerMessage(EventId = 4009, Level = LogLevel.Warning, Message = "Hangfire notification job for {NotificationType} was cancelled")]
    public static partial void NotificationJobCancelled(ILogger logger, string notificationType);
}
