using Encina.Cdc.Abstractions;

namespace Encina.Cdc.Errors;

/// <summary>
/// Factory methods for creating CDC-specific <see cref="EncinaError"/> instances.
/// Follows the same pattern as <see cref="EncinaErrorCodes"/> in the core package.
/// </summary>
public static class CdcErrors
{
    /// <summary>
    /// Creates an error indicating a connection failure to the CDC source.
    /// </summary>
    /// <param name="reason">Description of why the connection failed.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.ConnectionFailed"/>.</returns>
    public static EncinaError ConnectionFailed(string reason)
        => EncinaError.New($"[{CdcErrorCodes.ConnectionFailed}] CDC connection failed: {reason}");

    /// <summary>
    /// Creates an error indicating a connection failure with an associated exception.
    /// </summary>
    /// <param name="reason">Description of why the connection failed.</param>
    /// <param name="exception">The underlying exception.</param>
    /// <returns>An <see cref="EncinaError"/> with the exception attached.</returns>
    public static EncinaError ConnectionFailed(string reason, Exception exception)
        => EncinaError.New(exception, $"[{CdcErrorCodes.ConnectionFailed}] CDC connection failed: {reason}");

    /// <summary>
    /// Creates an error indicating an invalid or corrupted CDC position.
    /// </summary>
    /// <param name="position">The invalid position.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.PositionInvalid"/>.</returns>
    public static EncinaError PositionInvalid(CdcPosition position)
        => EncinaError.New($"[{CdcErrorCodes.PositionInvalid}] CDC position is invalid: {position}");

    /// <summary>
    /// Creates an error indicating the change stream was interrupted.
    /// </summary>
    /// <param name="exception">The exception that caused the interruption.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.StreamInterrupted"/>.</returns>
    public static EncinaError StreamInterrupted(Exception exception)
        => EncinaError.New(exception, $"[{CdcErrorCodes.StreamInterrupted}] CDC stream interrupted");

    /// <summary>
    /// Creates an error indicating a handler failed to process a change event.
    /// </summary>
    /// <param name="tableName">The table name associated with the failed event.</param>
    /// <param name="exception">The exception thrown by the handler.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.HandlerFailed"/>.</returns>
    public static EncinaError HandlerFailed(string tableName, Exception exception)
        => EncinaError.New(exception, $"[{CdcErrorCodes.HandlerFailed}] CDC handler failed for table '{tableName}'");

    /// <summary>
    /// Creates an error indicating deserialization of a change event payload failed.
    /// </summary>
    /// <param name="tableName">The table name associated with the event.</param>
    /// <param name="targetType">The target type for deserialization.</param>
    /// <param name="exception">The deserialization exception.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.DeserializationFailed"/>.</returns>
    public static EncinaError DeserializationFailed(string tableName, Type targetType, Exception exception)
        => EncinaError.New(exception, $"[{CdcErrorCodes.DeserializationFailed}] Failed to deserialize change event for table '{tableName}' to type '{targetType.Name}'");

    /// <summary>
    /// Creates an error indicating a failure in the position store.
    /// </summary>
    /// <param name="connectorId">The connector whose position operation failed.</param>
    /// <param name="exception">The underlying exception.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.PositionStoreFailed"/>.</returns>
    public static EncinaError PositionStoreFailed(string connectorId, Exception exception)
        => EncinaError.New(exception, $"[{CdcErrorCodes.PositionStoreFailed}] Position store operation failed for connector '{connectorId}'");

    /// <summary>
    /// Creates an error indicating a shard was not found in the sharded CDC connector.
    /// </summary>
    /// <param name="shardId">The shard identifier that was not found.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.ShardNotFound"/>.</returns>
    public static EncinaError ShardNotFound(string shardId)
        => EncinaError.New($"[{CdcErrorCodes.ShardNotFound}] Shard '{shardId}' not found in sharded CDC connector");

    /// <summary>
    /// Creates an error indicating a per-shard CDC stream failed.
    /// </summary>
    /// <param name="shardId">The shard identifier whose stream failed.</param>
    /// <param name="exception">The underlying exception.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.ShardStreamFailed"/>.</returns>
    public static EncinaError ShardStreamFailed(string shardId, Exception exception)
        => EncinaError.New(exception, $"[{CdcErrorCodes.ShardStreamFailed}] CDC stream failed for shard '{shardId}'");

    /// <summary>
    /// Creates an error indicating a failure to persist a CDC event to the dead letter store.
    /// </summary>
    /// <param name="exception">The underlying exception.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.DeadLetterStoreFailed"/>.</returns>
    public static EncinaError DeadLetterStoreFailed(Exception exception)
        => EncinaError.New(exception, $"[{CdcErrorCodes.DeadLetterStoreFailed}] Failed to persist event to CDC dead letter store");

    /// <summary>
    /// Creates an error indicating a dead letter entry was not found.
    /// </summary>
    /// <param name="id">The identifier of the dead letter entry that was not found.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.DeadLetterNotFound"/>.</returns>
    public static EncinaError DeadLetterNotFound(Guid id)
        => EncinaError.New($"[{CdcErrorCodes.DeadLetterNotFound}] Dead letter entry '{id}' not found");

    /// <summary>
    /// Creates an error indicating a dead letter entry has already been resolved.
    /// </summary>
    /// <param name="id">The identifier of the already-resolved dead letter entry.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.DeadLetterAlreadyResolved"/>.</returns>
    public static EncinaError DeadLetterAlreadyResolved(Guid id)
        => EncinaError.New($"[{CdcErrorCodes.DeadLetterAlreadyResolved}] Dead letter entry '{id}' has already been resolved");

    /// <summary>
    /// Creates an error indicating an invalid resolution was attempted on a dead letter entry.
    /// </summary>
    /// <param name="id">The identifier of the dead letter entry.</param>
    /// <param name="resolution">The invalid resolution that was attempted.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="CdcErrorCodes.DeadLetterInvalidResolution"/>.</returns>
    public static EncinaError DeadLetterInvalidResolution(Guid id, string resolution)
        => EncinaError.New($"[{CdcErrorCodes.DeadLetterInvalidResolution}] Invalid resolution '{resolution}' for dead letter entry '{id}'");
}
