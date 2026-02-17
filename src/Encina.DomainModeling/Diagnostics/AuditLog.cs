using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// High-performance logging methods for audit trail operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1600-1699 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class AuditLog
{
    /// <summary>Logs when an audit entry recording begins.</summary>
    [LoggerMessage(
        EventId = 1600,
        Level = LogLevel.Debug,
        Message = "Recording audit entry for {EntityType} action {Action}")]
    public static partial void RecordingAuditEntry(
        ILogger logger,
        string entityType,
        string action);

    /// <summary>Logs when an audit entry is successfully recorded.</summary>
    [LoggerMessage(
        EventId = 1601,
        Level = LogLevel.Information,
        Message = "Audit entry recorded for {EntityType} ({Action})")]
    public static partial void AuditEntryRecorded(
        ILogger logger,
        string entityType,
        string action);

    /// <summary>Logs when recording an audit entry fails.</summary>
    [LoggerMessage(
        EventId = 1602,
        Level = LogLevel.Warning,
        Message = "Failed to record audit entry for {EntityType}: {ErrorMessage}")]
    public static partial void AuditEntryFailed(
        ILogger logger,
        string entityType,
        string errorMessage);

    /// <summary>Logs when an audit trail query begins.</summary>
    [LoggerMessage(
        EventId = 1603,
        Level = LogLevel.Debug,
        Message = "Querying audit trail ({QueryType}) for {EntityType}")]
    public static partial void QueryingAuditTrail(
        ILogger logger,
        string queryType,
        string entityType);

    /// <summary>Logs when an audit query completes.</summary>
    [LoggerMessage(
        EventId = 1604,
        Level = LogLevel.Debug,
        Message = "Audit query completed with {ResultCount} entries")]
    public static partial void AuditQueryCompleted(
        ILogger logger,
        int resultCount);

    /// <summary>Logs when an audit operation fails with an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 1605,
        Level = LogLevel.Error,
        Message = "Audit operation failed with unexpected exception")]
    public static partial void AuditOperationException(
        ILogger logger,
        Exception exception);
}
