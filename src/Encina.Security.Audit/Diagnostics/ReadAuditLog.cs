using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Audit.Diagnostics;

/// <summary>
/// High-performance structured log messages for read audit operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1700-1799 range to avoid collisions with other Encina modules.
/// Write-audit trail uses 1600-1699 in <see cref="DomainModeling.Diagnostics.AuditLog"/>.
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>1700-1709</term><description>Audited repository decorator</description></item>
/// <item><term>1710-1719</term><description>Store log operations</description></item>
/// <item><term>1720-1729</term><description>Store query operations</description></item>
/// <item><term>1730-1739</term><description>Retention/purge service</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class ReadAuditLog
{
    // ========================================================================
    // Audited repository decorator (1700-1709)
    // ========================================================================

    /// <summary>Read access recorded via the audited repository decorator.</summary>
    [LoggerMessage(
        EventId = 1700,
        Level = LogLevel.Debug,
        Message = "Read audit recorded for {EntityType}.{MethodName}")]
    public static partial void ReadAccessRecorded(
        ILogger logger,
        string entityType,
        string methodName);

    /// <summary>Failed to record read access via the audited repository decorator.</summary>
    [LoggerMessage(
        EventId = 1701,
        Level = LogLevel.Error,
        Message = "Failed to record read audit for {EntityType}.{MethodName}")]
    public static partial void ReadAccessFailed(
        ILogger logger,
        string entityType,
        string methodName,
        Exception exception);

    /// <summary>Read access without declared purpose (GDPR Art. 15 compliance warning).</summary>
    [LoggerMessage(
        EventId = 1702,
        Level = LogLevel.Warning,
        Message = "Read access to {EntityType} by user '{UserId}' without declared purpose")]
    public static partial void PurposeNotDeclared(
        ILogger logger,
        string entityType,
        string? userId);

    // ========================================================================
    // Store log operations (1710-1719)
    // ========================================================================

    /// <summary>Read audit entry being logged to the store.</summary>
    [LoggerMessage(
        EventId = 1710,
        Level = LogLevel.Debug,
        Message = "Logging read audit entry. EntityType={EntityType}, AccessMethod={AccessMethod}")]
    public static partial void LoggingEntry(
        ILogger logger,
        string entityType,
        string accessMethod);

    /// <summary>Read audit entry successfully logged to the store.</summary>
    [LoggerMessage(
        EventId = 1711,
        Level = LogLevel.Information,
        Message = "Read audit entry logged. EntityType={EntityType}, EntityId={EntityId}, AccessMethod={AccessMethod}")]
    public static partial void EntryLogged(
        ILogger logger,
        string entityType,
        string? entityId,
        string accessMethod);

    /// <summary>Failed to log read audit entry to the store.</summary>
    [LoggerMessage(
        EventId = 1712,
        Level = LogLevel.Warning,
        Message = "Failed to log read audit entry. EntityType={EntityType}, ErrorMessage={ErrorMessage}")]
    public static partial void EntryLogFailed(
        ILogger logger,
        string entityType,
        string errorMessage);

    // ========================================================================
    // Store query operations (1720-1729)
    // ========================================================================

    /// <summary>Read audit query started.</summary>
    [LoggerMessage(
        EventId = 1720,
        Level = LogLevel.Debug,
        Message = "Read audit query started. QueryType={QueryType}, EntityType={EntityType}")]
    public static partial void QueryStarted(
        ILogger logger,
        string queryType,
        string? entityType);

    /// <summary>Read audit query completed successfully.</summary>
    [LoggerMessage(
        EventId = 1721,
        Level = LogLevel.Debug,
        Message = "Read audit query completed. QueryType={QueryType}, ResultCount={ResultCount}")]
    public static partial void QueryCompleted(
        ILogger logger,
        string queryType,
        int resultCount);

    /// <summary>Read audit query failed.</summary>
    [LoggerMessage(
        EventId = 1722,
        Level = LogLevel.Warning,
        Message = "Read audit query failed. QueryType={QueryType}, ErrorMessage={ErrorMessage}")]
    public static partial void QueryFailed(
        ILogger logger,
        string queryType,
        string errorMessage);

    // ========================================================================
    // Retention/purge service (1730-1739)
    // ========================================================================

    /// <summary>Read audit retention service started.</summary>
    [LoggerMessage(
        EventId = 1730,
        Level = LogLevel.Information,
        Message = "Read audit retention service started. IntervalHours={IntervalHours}, RetentionDays={RetentionDays}")]
    public static partial void RetentionServiceStarted(
        ILogger logger,
        int intervalHours,
        int retentionDays);

    /// <summary>Read audit retention service stopped.</summary>
    [LoggerMessage(
        EventId = 1731,
        Level = LogLevel.Information,
        Message = "Read audit retention service stopped")]
    public static partial void RetentionServiceStopped(
        ILogger logger);

    /// <summary>Read audit retention service is disabled.</summary>
    [LoggerMessage(
        EventId = 1732,
        Level = LogLevel.Debug,
        Message = "Read audit retention service disabled (EnableAutoPurge = false)")]
    public static partial void RetentionServiceDisabled(
        ILogger logger);

    /// <summary>Read audit purge operation started.</summary>
    [LoggerMessage(
        EventId = 1733,
        Level = LogLevel.Debug,
        Message = "Read audit purge started. CutoffDate={CutoffDate}")]
    public static partial void PurgeStarted(
        ILogger logger,
        DateTimeOffset cutoffDate);

    /// <summary>Read audit purge operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 1734,
        Level = LogLevel.Information,
        Message = "Purged {Count} read audit entries older than {CutoffDate}")]
    public static partial void PurgeCompleted(
        ILogger logger,
        int count,
        DateTimeOffset cutoffDate);

    /// <summary>No read audit entries to purge.</summary>
    [LoggerMessage(
        EventId = 1735,
        Level = LogLevel.Debug,
        Message = "No read audit entries to purge")]
    public static partial void NothingToPurge(
        ILogger logger);

    /// <summary>Read audit purge failed with a store error.</summary>
    [LoggerMessage(
        EventId = 1736,
        Level = LogLevel.Warning,
        Message = "Read audit purge failed: {ErrorMessage}")]
    public static partial void PurgeFailed(
        ILogger logger,
        string errorMessage);

    /// <summary>Read audit purge was cancelled.</summary>
    [LoggerMessage(
        EventId = 1737,
        Level = LogLevel.Warning,
        Message = "Read audit purge was cancelled")]
    public static partial void PurgeCancelled(
        ILogger logger);

    /// <summary>Unexpected error during read audit purge.</summary>
    [LoggerMessage(
        EventId = 1738,
        Level = LogLevel.Error,
        Message = "Unexpected error during read audit purge")]
    public static partial void PurgeError(
        ILogger logger,
        Exception exception);
}
