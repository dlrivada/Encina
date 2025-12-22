using Microsoft.Extensions.Logging;

namespace Encina.ADO.SqlServer;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // Inbox Pipeline Behavior (1-6)
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Idempotent request {RequestType} received without MessageId/IdempotencyKey (CorrelationId: {CorrelationId})")]
    public static partial void MissingIdempotencyKey(ILogger logger, string requestType, string correlationId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Processing idempotent request {RequestType} with MessageId {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessingIdempotentRequest(ILogger logger, string requestType, string messageId, string correlationId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Returning cached response for duplicate message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ReturningCachedResponse(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Message {MessageId} exceeded max retries ({MaxRetries}) (CorrelationId: {CorrelationId})")]
    public static partial void MaxRetriesExceeded(ILogger logger, string messageId, int maxRetries, string correlationId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Successfully processed and cached message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessedAndCachedMessage(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error processing message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ErrorProcessingMessage(ILogger logger, Exception exception, string messageId, string correlationId);

    // Outbox Post Processor (10-12)
    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Storing {Count} notifications in outbox for request {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void StoringNotificationsInOutbox(ILogger logger, int count, string requestType, string correlationId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Stored {Count} notifications in outbox (CorrelationId: {CorrelationId})")]
    public static partial void StoredNotificationsInOutbox(ILogger logger, int count, string correlationId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Skipping outbox storage for {Count} notifications due to error: {ErrorMessage} (CorrelationId: {CorrelationId})")]
    public static partial void SkippingOutboxStorageDueToError(ILogger logger, int count, string errorMessage, string correlationId);

    // Outbox Processor (20-27)
    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Outbox processor is disabled")]
    public static partial void OutboxProcessorDisabled(ILogger logger);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Outbox processor started. Interval: {Interval}, BatchSize: {BatchSize}")]
    public static partial void OutboxProcessorStarted(ILogger logger, TimeSpan interval, int batchSize);

    [LoggerMessage(EventId = 22, Level = LogLevel.Error, Message = "Error processing outbox messages")]
    public static partial void ErrorProcessingOutboxMessages(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 23, Level = LogLevel.Debug, Message = "Processing {Count} pending outbox messages")]
    public static partial void ProcessingPendingOutboxMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 24, Level = LogLevel.Debug, Message = "Processed outbox message {MessageId} of type {NotificationType}")]
    public static partial void ProcessedOutboxMessage(ILogger logger, Guid messageId, string notificationType);

    [LoggerMessage(EventId = 25, Level = LogLevel.Warning, Message = "Failed to process outbox message {MessageId}. Retry {RetryCount}/{MaxRetries}. Next retry at {NextRetry}")]
    public static partial void FailedToProcessOutboxMessage(ILogger logger, Exception exception, Guid messageId, int retryCount, int maxRetries, DateTime? nextRetry);

    [LoggerMessage(EventId = 26, Level = LogLevel.Information, Message = "Processed {TotalCount} outbox messages (Success: {SuccessCount}, Failed: {FailureCount})")]
    public static partial void ProcessedOutboxMessages(ILogger logger, int totalCount, int successCount, int failureCount);
}
