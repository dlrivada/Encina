using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// Shared across all messaging providers (Dapper, ADO.NET, EF Core).
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class MessagingLog
{
    // =========================================================================
    // Inbox Pipeline Behavior (EventIds 1-6)
    // =========================================================================

    /// <summary>Logs when an idempotent request is received without a MessageId.</summary>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Idempotent request {RequestType} received without MessageId/IdempotencyKey (CorrelationId: {CorrelationId})")]
    public static partial void MissingIdempotencyKey(
        ILogger logger,
        string requestType,
        string? correlationId);

    /// <summary>Logs when processing an idempotent request.</summary>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Processing idempotent request {RequestType} with MessageId {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessingIdempotentRequest(
        ILogger logger,
        string requestType,
        string messageId,
        string? correlationId);

    /// <summary>Logs when returning a cached response for a duplicate message.</summary>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Returning cached response for duplicate message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ReturningCachedResponse(
        ILogger logger,
        string messageId,
        string? correlationId);

    /// <summary>Logs when a message has exceeded max retries.</summary>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Message {MessageId} exceeded max retries ({MaxRetries}) (CorrelationId: {CorrelationId})")]
    public static partial void MaxRetriesExceeded(
        ILogger logger,
        string messageId,
        int maxRetries,
        string? correlationId);

    /// <summary>Logs when a message is successfully processed and cached.</summary>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Successfully processed and cached message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessedAndCachedMessage(
        ILogger logger,
        string messageId,
        string? correlationId);

    /// <summary>Logs when an error occurs processing a message.</summary>
    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Error processing message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ErrorProcessingMessage(
        ILogger logger,
        Exception exception,
        string messageId,
        string? correlationId);

    // =========================================================================
    // Outbox Post Processor (EventIds 10-12)
    // =========================================================================

    /// <summary>Logs when storing notifications in the outbox.</summary>
    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "Storing {Count} notifications in outbox for request {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void StoringNotificationsInOutbox(
        ILogger logger,
        int count,
        string requestType,
        string? correlationId);

    /// <summary>Logs when notifications are stored in the outbox.</summary>
    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Stored {Count} notifications in outbox (CorrelationId: {CorrelationId})")]
    public static partial void StoredNotificationsInOutbox(
        ILogger logger,
        int count,
        string? correlationId);

    /// <summary>Logs when skipping outbox storage due to an error.</summary>
    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Debug,
        Message = "Skipping outbox storage for {Count} notifications due to error: {ErrorMessage} (CorrelationId: {CorrelationId})")]
    public static partial void SkippingOutboxStorageDueToError(
        ILogger logger,
        int count,
        string errorMessage,
        string? correlationId);

    // =========================================================================
    // Outbox Processor (EventIds 20-26)
    // =========================================================================

    /// <summary>Logs when the outbox processor is disabled.</summary>
    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Information,
        Message = "Outbox processor is disabled")]
    public static partial void OutboxProcessorDisabled(ILogger logger);

    /// <summary>Logs when the outbox processor starts.</summary>
    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Information,
        Message = "Outbox processor started. Interval: {Interval}, BatchSize: {BatchSize}")]
    public static partial void OutboxProcessorStarted(
        ILogger logger,
        TimeSpan interval,
        int batchSize);

    /// <summary>Logs when an error occurs processing outbox messages.</summary>
    [LoggerMessage(
        EventId = 22,
        Level = LogLevel.Error,
        Message = "Error processing outbox messages")]
    public static partial void ErrorProcessingOutboxMessages(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when processing pending outbox messages.</summary>
    [LoggerMessage(
        EventId = 23,
        Level = LogLevel.Debug,
        Message = "Processing {Count} pending outbox messages")]
    public static partial void ProcessingPendingOutboxMessages(
        ILogger logger,
        int count);

    /// <summary>Logs when an outbox message is processed.</summary>
    [LoggerMessage(
        EventId = 24,
        Level = LogLevel.Debug,
        Message = "Processed outbox message {MessageId} of type {NotificationType}")]
    public static partial void ProcessedOutboxMessage(
        ILogger logger,
        Guid messageId,
        string notificationType);

    /// <summary>Logs when an outbox message fails to process.</summary>
    [LoggerMessage(
        EventId = 25,
        Level = LogLevel.Warning,
        Message = "Failed to process outbox message {MessageId}. Retry {RetryCount}/{MaxRetries}. Next retry at {NextRetry}")]
    public static partial void FailedToProcessOutboxMessage(
        ILogger logger,
        Exception exception,
        Guid messageId,
        int retryCount,
        int maxRetries,
        DateTime? nextRetry);

    /// <summary>Logs a summary of processed outbox messages.</summary>
    [LoggerMessage(
        EventId = 26,
        Level = LogLevel.Information,
        Message = "Processed {TotalCount} outbox messages (Success: {SuccessCount}, Failed: {FailureCount})")]
    public static partial void ProcessedOutboxMessages(
        ILogger logger,
        int totalCount,
        int successCount,
        int failureCount);

    // =========================================================================
    // Transaction Pipeline Behavior (EventIds 30-32)
    // =========================================================================

    /// <summary>Logs when a transaction is started.</summary>
    [LoggerMessage(
        EventId = 30,
        Level = LogLevel.Debug,
        Message = "Transaction started for {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void TransactionStarted(
        ILogger logger,
        string requestType,
        string? correlationId);

    /// <summary>Logs when a transaction is committed.</summary>
    [LoggerMessage(
        EventId = 31,
        Level = LogLevel.Debug,
        Message = "Transaction committed for {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void TransactionCommitted(
        ILogger logger,
        string requestType,
        string? correlationId);

    /// <summary>Logs when a transaction is rolled back.</summary>
    [LoggerMessage(
        EventId = 32,
        Level = LogLevel.Debug,
        Message = "Transaction rolled back for {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void TransactionRolledBack(
        ILogger logger,
        string requestType,
        string? correlationId);
}
