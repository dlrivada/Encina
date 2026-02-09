using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for CDC processing infrastructure.
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class CdcLog
{
    // =========================================================================
    // CDC Processor Lifecycle (EventIds 100-106)
    // =========================================================================

    /// <summary>Logs when the CDC processor is disabled.</summary>
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "CDC processor is disabled")]
    public static partial void ProcessorDisabled(ILogger logger);

    /// <summary>Logs when the CDC processor starts.</summary>
    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "CDC processor started. PollingInterval: {PollingInterval}, BatchSize: {BatchSize}")]
    public static partial void ProcessorStarted(
        ILogger logger,
        TimeSpan pollingInterval,
        int batchSize);

    /// <summary>Logs when the CDC processor stops gracefully.</summary>
    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Information,
        Message = "CDC processor stopped")]
    public static partial void ProcessorStopped(ILogger logger);

    /// <summary>Logs when processing a batch of change events.</summary>
    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Debug,
        Message = "Processing {Count} change events from connector '{ConnectorId}'")]
    public static partial void ProcessingChangeEvents(
        ILogger logger,
        int count,
        string connectorId);

    /// <summary>Logs when a batch of change events has been processed.</summary>
    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Information,
        Message = "Processed {SuccessCount}/{TotalCount} change events (Failed: {FailureCount}) from connector '{ConnectorId}'")]
    public static partial void ProcessedChangeEvents(
        ILogger logger,
        int successCount,
        int totalCount,
        int failureCount,
        string connectorId);

    /// <summary>Logs when an error occurs during CDC processing.</summary>
    [LoggerMessage(
        EventId = 105,
        Level = LogLevel.Error,
        Message = "Error processing CDC change events from connector '{ConnectorId}'")]
    public static partial void ErrorProcessingChangeEvents(
        ILogger logger,
        Exception exception,
        string connectorId);

    /// <summary>Logs when retrying after a transient failure.</summary>
    [LoggerMessage(
        EventId = 106,
        Level = LogLevel.Warning,
        Message = "CDC processing error for connector '{ConnectorId}'. Retry {RetryCount}/{MaxRetries} after {Delay}")]
    public static partial void RetryingAfterError(
        ILogger logger,
        Exception exception,
        string connectorId,
        int retryCount,
        int maxRetries,
        TimeSpan delay);

    // =========================================================================
    // CDC Dispatcher (EventIds 110-114)
    // =========================================================================

    /// <summary>Logs when dispatching a change event to a handler.</summary>
    [LoggerMessage(
        EventId = 110,
        Level = LogLevel.Debug,
        Message = "Dispatching {Operation} event for table '{TableName}' to handler '{HandlerType}'")]
    public static partial void DispatchingChangeEvent(
        ILogger logger,
        ChangeOperation operation,
        string tableName,
        string handlerType);

    /// <summary>Logs when no handler is found for a table.</summary>
    [LoggerMessage(
        EventId = 111,
        Level = LogLevel.Warning,
        Message = "No handler registered for table '{TableName}'. Skipping {Operation} event")]
    public static partial void NoHandlerForTable(
        ILogger logger,
        string tableName,
        ChangeOperation operation);

    /// <summary>Logs when a handler fails to process a change event.</summary>
    [LoggerMessage(
        EventId = 112,
        Level = LogLevel.Error,
        Message = "Handler '{HandlerType}' failed to process {Operation} event for table '{TableName}'")]
    public static partial void HandlerFailed(
        ILogger logger,
        Exception exception,
        string handlerType,
        ChangeOperation operation,
        string tableName);

    /// <summary>Logs when deserialization of a change event fails.</summary>
    [LoggerMessage(
        EventId = 113,
        Level = LogLevel.Error,
        Message = "Failed to deserialize {Operation} event for table '{TableName}' to type '{TargetType}'")]
    public static partial void DeserializationFailed(
        ILogger logger,
        Exception exception,
        ChangeOperation operation,
        string tableName,
        string targetType);

    /// <summary>Logs when a change event is successfully dispatched.</summary>
    [LoggerMessage(
        EventId = 114,
        Level = LogLevel.Debug,
        Message = "Successfully dispatched {Operation} event for table '{TableName}'")]
    public static partial void DispatchedChangeEvent(
        ILogger logger,
        ChangeOperation operation,
        string tableName);

    // =========================================================================
    // CDC Position Tracking (EventIds 120-122)
    // =========================================================================

    /// <summary>Logs when a position is saved.</summary>
    [LoggerMessage(
        EventId = 120,
        Level = LogLevel.Debug,
        Message = "Saved CDC position for connector '{ConnectorId}': {Position}")]
    public static partial void PositionSaved(
        ILogger logger,
        string connectorId,
        string position);

    /// <summary>Logs when a position is restored on startup.</summary>
    [LoggerMessage(
        EventId = 121,
        Level = LogLevel.Information,
        Message = "Restored CDC position for connector '{ConnectorId}': {Position}")]
    public static partial void PositionRestored(
        ILogger logger,
        string connectorId,
        string position);

    /// <summary>Logs when no saved position is found.</summary>
    [LoggerMessage(
        EventId = 122,
        Level = LogLevel.Information,
        Message = "No saved position found for connector '{ConnectorId}'. Starting from beginning")]
    public static partial void NoSavedPosition(
        ILogger logger,
        string connectorId);
}
