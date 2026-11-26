using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// High-performance logging for Dead Letter Queue operations.
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Event IDs: 11695-11707 and 11740-11748 (see EventIdRanges.Messaging).
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class DeadLetterLog
{
    [LoggerMessage(
        EventId = 11695,
        Level = LogLevel.Warning,
        Message = "Message {MessageId} added to DLQ. Type: {RequestType}, Source: {SourcePattern}, ErrorCode: {ErrorCode}, Attempts: {TotalAttempts}, CorrelationId: {CorrelationId}")]
    public static partial void MessageAddedToDLQ(
        ILogger logger,
        Guid messageId,
        string requestType,
        string sourcePattern,
        string errorCode,
        int totalAttempts,
        string? correlationId);

    [LoggerMessage(
        EventId = 11696,
        Level = LogLevel.Error,
        Message = "OnDeadLetter callback failed for message {MessageId}")]
    public static partial void OnDeadLetterCallbackFailed(
        ILogger logger,
        Exception exception,
        Guid messageId);

    [LoggerMessage(
        EventId = 11697,
        Level = LogLevel.Information,
        Message = "Replaying dead letter message {MessageId}. Type: {RequestType}")]
    public static partial void ReplayingMessage(
        ILogger logger,
        Guid messageId,
        string requestType);

    [LoggerMessage(
        EventId = 11698,
        Level = LogLevel.Information,
        Message = "Message {MessageId} replayed successfully")]
    public static partial void MessageReplayedSuccessfully(
        ILogger logger,
        Guid messageId);

    [LoggerMessage(
        EventId = 11699,
        Level = LogLevel.Warning,
        Message = "Message {MessageId} replay failed. ErrorCode: {ErrorCode}")]
    public static partial void MessageReplayFailed(
        ILogger logger,
        Guid messageId,
        string errorCode);

    [LoggerMessage(
        EventId = 11700,
        Level = LogLevel.Error,
        Message = "Exception during message {MessageId} replay")]
    public static partial void MessageReplayException(
        ILogger logger,
        Exception exception,
        Guid messageId);

    [LoggerMessage(
        EventId = 11701,
        Level = LogLevel.Information,
        Message = "Batch replay started. Messages to process: {Count}")]
    public static partial void BatchReplayStarted(
        ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 11702,
        Level = LogLevel.Information,
        Message = "Batch replay completed. Processed: {TotalProcessed}, Success: {SuccessCount}, Failed: {FailureCount}")]
    public static partial void BatchReplayCompleted(
        ILogger logger,
        int totalProcessed,
        int successCount,
        int failureCount);

    [LoggerMessage(
        EventId = 11703,
        Level = LogLevel.Information,
        Message = "{Count} expired dead letter messages cleaned up")]
    public static partial void ExpiredMessagesCleanedUp(
        ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 11704,
        Level = LogLevel.Information,
        Message = "{Count} dead letter messages deleted")]
    public static partial void MessagesDeleted(
        ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 11705,
        Level = LogLevel.Information,
        Message = "DLQ cleanup processor is disabled")]
    public static partial void CleanupProcessorDisabled(
        ILogger logger);

    [LoggerMessage(
        EventId = 11706,
        Level = LogLevel.Debug,
        Message = "DLQ cleanup processor running. Interval: {Interval}")]
    public static partial void CleanupProcessorRunning(
        ILogger logger,
        TimeSpan interval);

    [LoggerMessage(
        EventId = 11707,
        Level = LogLevel.Error,
        Message = "Error during DLQ cleanup")]
    public static partial void CleanupError(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 11740,
        Level = LogLevel.Warning,
        Message = "Dead letter store write failed. Source: {SourcePattern}, ErrorCode: {ErrorCode}")]
    public static partial void StoreWriteFailed(
        ILogger logger,
        string sourcePattern,
        string errorCode);

    [LoggerMessage(
        EventId = 11741,
        Level = LogLevel.Debug,
        Message = "Source message already dead-lettered as {MessageId} (Source: {SourcePattern}); duplicate ignored")]
    public static partial void DuplicateIgnored(
        ILogger logger,
        Guid messageId,
        string sourcePattern);

    [LoggerMessage(
        EventId = 11742,
        Level = LogLevel.Warning,
        Message = "Replay outcome of dead letter message {MessageId} was not recorded. ErrorCode: {ErrorCode}")]
    public static partial void ReplayOutcomeNotRecorded(
        ILogger logger,
        Guid messageId,
        string errorCode);

    [LoggerMessage(
        EventId = 11743,
        Level = LogLevel.Warning,
        Message = "Dead letter operation {Operation} ran across all tenants (explicit AllTenants opt-out)")]
    public static partial void AllTenantsOptOut(
        ILogger logger,
        string operation);

    [LoggerMessage(
        EventId = 11744,
        Level = LogLevel.Warning,
        Message = "Dead letter operation {Operation} denied: multi-tenancy is in use and no tenant is resolved. ErrorCode: {ErrorCode}")]
    public static partial void TenantRequiredDenied(
        ILogger logger,
        string operation,
        string errorCode);

    [LoggerMessage(
        EventId = 11745,
        Level = LogLevel.Error,
        Message = "Batch replay aborted by a store failure. Processed: {TotalProcessed}, Success: {SuccessCount}, Failed: {FailureCount}, ErrorCode: {ErrorCode}")]
    public static partial void BatchReplayAborted(
        ILogger logger,
        int totalProcessed,
        int successCount,
        int failureCount,
        string errorCode);

    [LoggerMessage(
        EventId = 11746,
        Level = LogLevel.Error,
        Message = "Dead letter capture from {SourcePattern} failed. ErrorCode: {ErrorCode}")]
    public static partial void SourceCaptureFailed(
        ILogger logger,
        string sourcePattern,
        string errorCode);

    [LoggerMessage(
        EventId = 11747,
        Level = LogLevel.Error,
        Message = "Dead letter capture from {SourcePattern} threw")]
    public static partial void SourceCaptureThrew(
        ILogger logger,
        Exception exception,
        string sourcePattern);

    [LoggerMessage(
        EventId = 11748,
        Level = LogLevel.Error,
        Message = "Dead letter capture from {SourcePattern} rejected: the dead letter queue cannot store the message. ErrorCode: {ErrorCode}")]
    public static partial void SourceCaptureRejected(
        ILogger logger,
        Exception? exception,
        string sourcePattern,
        string errorCode);
}
