using Microsoft.Extensions.Logging;
using Quartz;

namespace Encina.Quartz;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // Request Job (1-5)
    [LoggerMessage(EventId = 4050, Level = LogLevel.Error, Message = "Request not found in JobDataMap for job {JobKey}")]
    public static partial void RequestNotFoundInJobDataMap(ILogger logger, JobKey jobKey);

    [LoggerMessage(EventId = 4051, Level = LogLevel.Information, Message = "Executing Quartz job {JobKey} for request {RequestType}")]
    public static partial void ExecutingRequestJob(ILogger logger, JobKey jobKey, string requestType);

    [LoggerMessage(EventId = 4052, Level = LogLevel.Information, Message = "Quartz job {JobKey} completed successfully for request {RequestType}")]
    public static partial void RequestJobCompleted(ILogger logger, JobKey jobKey, string requestType);

    [LoggerMessage(EventId = 4053, Level = LogLevel.Error, Message = "Quartz job {JobKey} failed for request {RequestType} with error code {ErrorCode}")]
    public static partial void RequestJobFailed(ILogger logger, JobKey jobKey, string requestType, string errorCode);

    [LoggerMessage(EventId = 4054, Level = LogLevel.Error, Message = "Unhandled exception in Quartz job {JobKey} for request {RequestType}")]
    public static partial void RequestJobException(ILogger logger, Exception exception, JobKey jobKey, string requestType);

    // Notification Job (10-14)
    [LoggerMessage(EventId = 4055, Level = LogLevel.Error, Message = "Notification not found in JobDataMap for job {JobKey}")]
    public static partial void NotificationNotFoundInJobDataMap(ILogger logger, JobKey jobKey);

    [LoggerMessage(EventId = 4056, Level = LogLevel.Information, Message = "Publishing Quartz notification job {JobKey} for {NotificationType}")]
    public static partial void PublishingNotificationJob(ILogger logger, JobKey jobKey, string notificationType);

    [LoggerMessage(EventId = 4057, Level = LogLevel.Information, Message = "Quartz notification job {JobKey} completed successfully for {NotificationType}")]
    public static partial void NotificationJobCompleted(ILogger logger, JobKey jobKey, string notificationType);

    [LoggerMessage(EventId = 4058, Level = LogLevel.Error, Message = "Unhandled exception in Quartz notification job {JobKey} for {NotificationType}")]
    public static partial void NotificationJobException(ILogger logger, Exception exception, JobKey jobKey, string notificationType);

    [LoggerMessage(EventId = 4059, Level = LogLevel.Error, Message = "Quartz notification job {JobKey} failed for {NotificationType} with error code {ErrorCode}")]
    public static partial void NotificationJobFailed(ILogger logger, JobKey jobKey, string notificationType, string errorCode);

    // Cancellation
    [LoggerMessage(EventId = 4060, Level = LogLevel.Warning, Message = "Quartz job {JobKey} for request {RequestType} was cancelled")]
    public static partial void RequestJobCancelled(ILogger logger, JobKey jobKey, string requestType);

    [LoggerMessage(EventId = 4061, Level = LogLevel.Warning, Message = "Quartz notification job {JobKey} for {NotificationType} was cancelled")]
    public static partial void NotificationJobCancelled(ILogger logger, JobKey jobKey, string notificationType);
}
