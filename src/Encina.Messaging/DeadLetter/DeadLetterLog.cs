using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// High-performance logging for Dead Letter Queue operations.
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class DeadLetterLog
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Message {MessageId} added to DLQ. Type: {RequestType}, Source: {SourcePattern}, Error: {ErrorMessage}, Attempts: {TotalAttempts}, CorrelationId: {CorrelationId}")]
    public static partial void MessageAddedToDLQ(
        ILogger logger,
        Guid messageId,
        string requestType,
        string sourcePattern,
        string errorMessage,
        int totalAttempts,
        string? correlationId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "OnDeadLetter callback failed for message {MessageId}")]
    public static partial void OnDeadLetterCallbackFailed(
        ILogger logger,
        Exception exception,
        Guid messageId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Replaying dead letter message {MessageId}. Type: {RequestType}")]
    public static partial void ReplayingMessage(
        ILogger logger,
        Guid messageId,
        string requestType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Message {MessageId} replayed successfully")]
    public static partial void MessageReplayedSuccessfully(
        ILogger logger,
        Guid messageId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Message {MessageId} replay failed. Error: {ErrorMessage}")]
    public static partial void MessageReplayFailed(
        ILogger logger,
        Guid messageId,
        string errorMessage);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Exception during message {MessageId} replay")]
    public static partial void MessageReplayException(
        ILogger logger,
        Exception exception,
        Guid messageId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Batch replay started. Messages to process: {Count}")]
    public static partial void BatchReplayStarted(
        ILogger logger,
        int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Batch replay completed. Processed: {TotalProcessed}, Success: {SuccessCount}, Failed: {FailureCount}")]
    public static partial void BatchReplayCompleted(
        ILogger logger,
        int totalProcessed,
        int successCount,
        int failureCount);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Count} expired dead letter messages cleaned up")]
    public static partial void ExpiredMessagesCleanedUp(
        ILogger logger,
        int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Count} dead letter messages deleted")]
    public static partial void MessagesDeleted(
        ILogger logger,
        int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "DLQ cleanup processor is disabled")]
    public static partial void CleanupProcessorDisabled(
        ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "DLQ cleanup processor running. Interval: {Interval}")]
    public static partial void CleanupProcessorRunning(
        ILogger logger,
        TimeSpan interval);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error during DLQ cleanup")]
    public static partial void CleanupError(
        ILogger logger,
        Exception exception);
}
