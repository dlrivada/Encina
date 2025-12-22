using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // Transaction Pipeline Behavior (1-5)
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Request {RequestType} is already in a transaction, reusing existing transaction (CorrelationId: {CorrelationId})")]
    public static partial void ReusingExistingTransaction(ILogger logger, string requestType, string correlationId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Beginning transaction for request {RequestType} with isolation level {IsolationLevel} (CorrelationId: {CorrelationId})")]
    public static partial void BeginningTransaction(ILogger logger, string requestType, System.Data.IsolationLevel? isolationLevel, string correlationId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Committing transaction for request {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void CommittingTransaction(ILogger logger, string requestType, string correlationId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Rolling back transaction for request {RequestType} due to error: {ErrorMessage} (CorrelationId: {CorrelationId})")]
    public static partial void RollingBackTransactionDueToError(ILogger logger, string requestType, string errorMessage, string correlationId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Rolling back transaction for request {RequestType} due to exception (CorrelationId: {CorrelationId})")]
    public static partial void RollingBackTransactionDueToException(ILogger logger, Exception exception, string requestType, string correlationId);

    // Inbox Pipeline Behavior (10-17)
    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Idempotent request {RequestType} received without MessageId/IdempotencyKey (CorrelationId: {CorrelationId})")]
    public static partial void MissingIdempotencyKey(ILogger logger, string requestType, string correlationId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Processing idempotent request {RequestType} with MessageId {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessingIdempotentRequest(ILogger logger, string requestType, string messageId, string correlationId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Returning cached response for duplicate message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ReturningCachedResponse(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Message {MessageId} exceeded max retries ({MaxRetries}) (CorrelationId: {CorrelationId})")]
    public static partial void MaxRetriesExceeded(ILogger logger, string messageId, int maxRetries, string correlationId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Successfully processed and cached message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessedAndCachedMessage(ILogger logger, string messageId, string correlationId);

    [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "Error processing message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ErrorProcessingMessage(ILogger logger, Exception exception, string messageId, string correlationId);

    // Outbox Post Processor (20-23)
    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Storing {Count} notifications in outbox for request {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void StoringNotificationsInOutbox(ILogger logger, int count, string requestType, string correlationId);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Stored {Count} notifications in outbox (CorrelationId: {CorrelationId})")]
    public static partial void StoredNotificationsInOutbox(ILogger logger, int count, string correlationId);

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "Skipping outbox storage for {Count} notifications due to error: {ErrorMessage} (CorrelationId: {CorrelationId})")]
    public static partial void SkippingOutboxStorageDueToError(ILogger logger, int count, string errorMessage, string correlationId);

    // Outbox Processor (30-39)
    [LoggerMessage(EventId = 30, Level = LogLevel.Information, Message = "Outbox Processor starting (Interval: {Interval}, BatchSize: {BatchSize}, MaxRetries: {MaxRetries})")]
    public static partial void OutboxProcessorStarting(ILogger logger, TimeSpan interval, int batchSize, int maxRetries);

    [LoggerMessage(EventId = 31, Level = LogLevel.Error, Message = "Error processing outbox messages")]
    public static partial void ErrorProcessingOutboxMessages(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 32, Level = LogLevel.Information, Message = "Outbox Processor stopping")]
    public static partial void OutboxProcessorStopping(ILogger logger);

    [LoggerMessage(EventId = 33, Level = LogLevel.Debug, Message = "Processing {Count} pending outbox messages")]
    public static partial void ProcessingPendingOutboxMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 34, Level = LogLevel.Error, Message = "Cannot find type {NotificationType} for outbox message {MessageId}")]
    public static partial void TypeNotFound(ILogger logger, string notificationType, Guid messageId);

    [LoggerMessage(EventId = 35, Level = LogLevel.Error, Message = "Failed to deserialize notification for outbox message {MessageId}")]
    public static partial void DeserializationFailed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 36, Level = LogLevel.Debug, Message = "Published notification {NotificationType} from outbox message {MessageId}")]
    public static partial void PublishedNotification(ILogger logger, string notificationType, Guid messageId);

    [LoggerMessage(EventId = 37, Level = LogLevel.Error, Message = "Error processing outbox message {MessageId}")]
    public static partial void ErrorProcessingOutboxMessage(ILogger logger, Exception exception, Guid messageId);

    [LoggerMessage(EventId = 38, Level = LogLevel.Information, Message = "Processed {TotalCount} outbox messages (Success: {SuccessCount}, Failed: {FailureCount})")]
    public static partial void ProcessedOutboxMessages(ILogger logger, int totalCount, int successCount, int failureCount);
}
