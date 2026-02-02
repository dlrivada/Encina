using Microsoft.Extensions.Logging;

namespace Encina.Security.Audit;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // AuditRetentionService: EventIds 1-19
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Audit retention service started. Purge interval: {IntervalHours} hours, Retention: {RetentionDays} days")]
    public static partial void AuditRetentionServiceStarted(ILogger logger, int intervalHours, int retentionDays);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Audit retention service stopped")]
    public static partial void AuditRetentionServiceStopped(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Audit retention service is disabled (EnableAutoPurge = false)")]
    public static partial void AuditRetentionServiceDisabled(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Starting audit purge for entries older than {CutoffDate:u}")]
    public static partial void AuditRetentionPurgeStarted(ILogger logger, DateTime cutoffDate);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Purged {Count} audit entries older than {CutoffDate:u}")]
    public static partial void AuditRetentionPurgeCompleted(ILogger logger, int count, DateTime cutoffDate);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "No audit entries to purge")]
    public static partial void AuditRetentionNothingToPurge(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Audit purge failed: {ErrorMessage}")]
    public static partial void AuditRetentionPurgeFailed(ILogger logger, string errorMessage);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Audit purge was cancelled")]
    public static partial void AuditRetentionPurgeCancelled(ILogger logger);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Unexpected error during audit purge")]
    public static partial void AuditRetentionPurgeError(ILogger logger, Exception exception);
}
