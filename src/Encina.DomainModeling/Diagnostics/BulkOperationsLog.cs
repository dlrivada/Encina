using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// High-performance logging methods for bulk operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1300-1399 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class BulkOperationsLog
{
    /// <summary>Logs when a bulk operation begins execution.</summary>
    [LoggerMessage(
        EventId = 1300,
        Level = LogLevel.Debug,
        Message = "Starting bulk {Operation} for {EntityType} using {Provider}")]
    public static partial void StartingBulkOperation(
        ILogger logger,
        string operation,
        string entityType,
        string provider);

    /// <summary>Logs when a bulk operation completes successfully.</summary>
    [LoggerMessage(
        EventId = 1301,
        Level = LogLevel.Information,
        Message = "Bulk {Operation} for {EntityType} completed: {RowsAffected} rows affected in {DurationMs}ms")]
    public static partial void BulkOperationCompleted(
        ILogger logger,
        string operation,
        string entityType,
        int rowsAffected,
        long durationMs);

    /// <summary>Logs when a bulk operation fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 1302,
        Level = LogLevel.Warning,
        Message = "Bulk {Operation} for {EntityType} failed with error {ErrorCode}: {ErrorMessage}")]
    public static partial void BulkOperationFailed(
        ILogger logger,
        string operation,
        string entityType,
        string errorCode,
        string errorMessage);

    /// <summary>Logs when a bulk operation throws an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 1303,
        Level = LogLevel.Error,
        Message = "Bulk {Operation} for {EntityType} threw an unexpected exception")]
    public static partial void BulkOperationException(
        ILogger logger,
        Exception exception,
        string operation,
        string entityType);
}
