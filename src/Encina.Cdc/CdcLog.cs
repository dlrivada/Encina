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

    // =========================================================================
    // Sharded CDC Connector (EventIds 130-136)
    // =========================================================================

    /// <summary>Logs when the sharded CDC connector initializes.</summary>
    [LoggerMessage(
        EventId = 130,
        Level = LogLevel.Information,
        Message = "Sharded CDC connector '{ConnectorId}' initialized with {ShardCount} shard(s): [{ShardIds}]")]
    public static partial void ShardedConnectorInitialized(
        ILogger logger,
        string connectorId,
        int shardCount,
        string shardIds);

    /// <summary>Logs when a per-shard CDC stream starts.</summary>
    [LoggerMessage(
        EventId = 131,
        Level = LogLevel.Information,
        Message = "Started CDC stream for shard '{ShardId}' in connector '{ConnectorId}'")]
    public static partial void ShardStreamStarted(
        ILogger logger,
        string shardId,
        string connectorId);

    /// <summary>Logs when a per-shard CDC stream stops.</summary>
    [LoggerMessage(
        EventId = 132,
        Level = LogLevel.Information,
        Message = "Stopped CDC stream for shard '{ShardId}' in connector '{ConnectorId}'")]
    public static partial void ShardStreamStopped(
        ILogger logger,
        string shardId,
        string connectorId);

    /// <summary>Logs when a per-shard CDC stream encounters an error.</summary>
    [LoggerMessage(
        EventId = 133,
        Level = LogLevel.Error,
        Message = "CDC stream error for shard '{ShardId}' in connector '{ConnectorId}'")]
    public static partial void ShardStreamError(
        ILogger logger,
        Exception exception,
        string shardId,
        string connectorId);

    /// <summary>Logs when a shard connector is added dynamically.</summary>
    [LoggerMessage(
        EventId = 134,
        Level = LogLevel.Information,
        Message = "Added CDC connector for shard '{ShardId}' in connector '{ConnectorId}'")]
    public static partial void ShardConnectorAdded(
        ILogger logger,
        string shardId,
        string connectorId);

    /// <summary>Logs when a shard connector is removed dynamically.</summary>
    [LoggerMessage(
        EventId = 135,
        Level = LogLevel.Information,
        Message = "Removed CDC connector for shard '{ShardId}' in connector '{ConnectorId}'")]
    public static partial void ShardConnectorRemoved(
        ILogger logger,
        string shardId,
        string connectorId);

    /// <summary>Logs when the sharded CDC connector is disposed.</summary>
    [LoggerMessage(
        EventId = 136,
        Level = LogLevel.Information,
        Message = "Sharded CDC connector '{ConnectorId}' disposed")]
    public static partial void ShardedConnectorDisposed(
        ILogger logger,
        string connectorId);

    // =========================================================================
    // Sharded CDC Processor (EventIds 140-145)
    // =========================================================================

    /// <summary>Logs when the sharded CDC processor starts.</summary>
    [LoggerMessage(
        EventId = 140,
        Level = LogLevel.Information,
        Message = "Sharded CDC processor started. PollingInterval: {PollingInterval}, BatchSize: {BatchSize}")]
    public static partial void ShardedProcessorStarted(
        ILogger logger,
        TimeSpan pollingInterval,
        int batchSize);

    /// <summary>Logs when the sharded CDC processor stops gracefully.</summary>
    [LoggerMessage(
        EventId = 141,
        Level = LogLevel.Information,
        Message = "Sharded CDC processor stopped")]
    public static partial void ShardedProcessorStopped(ILogger logger);

    /// <summary>Logs when a batch of sharded change events has been processed.</summary>
    [LoggerMessage(
        EventId = 142,
        Level = LogLevel.Information,
        Message = "Sharded CDC processed {SuccessCount}/{TotalCount} events (Failed: {FailureCount}) from connector '{ConnectorId}'")]
    public static partial void ShardedProcessedChangeEvents(
        ILogger logger,
        int successCount,
        int totalCount,
        int failureCount,
        string connectorId);

    /// <summary>Logs when a per-shard CDC position is saved.</summary>
    [LoggerMessage(
        EventId = 143,
        Level = LogLevel.Debug,
        Message = "Saved sharded CDC position for shard '{ShardId}' connector '{ConnectorId}': {Position}")]
    public static partial void ShardPositionSaved(
        ILogger logger,
        string shardId,
        string connectorId,
        string position);

    // =========================================================================
    // Sharded CDC Observability (EventIds 200-204)
    // =========================================================================

    /// <summary>Logs when a shard CDC connector starts capturing changes.</summary>
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Information,
        Message = "Shard CDC connector started for shard '{ShardId}' in connector '{ConnectorId}'")]
    public static partial void ShardCdcConnectorStarted(
        ILogger logger,
        string shardId,
        string connectorId);

    /// <summary>Logs when a shard CDC connector stops capturing changes.</summary>
    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Information,
        Message = "Shard CDC connector stopped for shard '{ShardId}' in connector '{ConnectorId}'")]
    public static partial void ShardCdcConnectorStopped(
        ILogger logger,
        string shardId,
        string connectorId);

    /// <summary>Logs when a shard's replication lag exceeds the configured threshold.</summary>
    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Warning,
        Message = "Shard CDC lag exceeded threshold for shard '{ShardId}' in connector '{ConnectorId}'. Current lag: {LagMs}ms, Threshold: {ThresholdMs}ms")]
    public static partial void ShardCdcLagExceeded(
        ILogger logger,
        string shardId,
        string connectorId,
        double lagMs,
        double thresholdMs);

    /// <summary>Logs when a shard CDC position is checkpointed to persistent storage.</summary>
    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Debug,
        Message = "Shard CDC position checkpointed for shard '{ShardId}' in connector '{ConnectorId}': {Position}")]
    public static partial void ShardCdcPositionCheckpointed(
        ILogger logger,
        string shardId,
        string connectorId,
        string position);

    /// <summary>Logs when the shard topology changes (shards added or removed).</summary>
    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Information,
        Message = "Shard topology changed for connector '{ConnectorId}'. Active shards: {ActiveShardCount}. Added: [{AddedShardIds}], Removed: [{RemovedShardIds}]")]
    public static partial void ShardTopologyChanged(
        ILogger logger,
        string connectorId,
        int activeShardCount,
        string addedShardIds,
        string removedShardIds);
}
