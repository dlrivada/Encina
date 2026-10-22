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
    // Inbox Pipeline Behavior (EventIds 2818-2823)
    // =========================================================================

    /// <summary>Logs when an idempotent request is received without a MessageId.</summary>
    [LoggerMessage(
        EventId = 2818,
        Level = LogLevel.Warning,
        Message = "Idempotent request {RequestType} received without MessageId/IdempotencyKey (CorrelationId: {CorrelationId})")]
    public static partial void MissingIdempotencyKey(
        ILogger logger,
        string requestType,
        string? correlationId);

    /// <summary>Logs when processing an idempotent request.</summary>
    [LoggerMessage(
        EventId = 2819,
        Level = LogLevel.Debug,
        Message = "Processing idempotent request {RequestType} with MessageId {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessingIdempotentRequest(
        ILogger logger,
        string requestType,
        string messageId,
        string? correlationId);

    /// <summary>Logs when returning a cached response for a duplicate message.</summary>
    [LoggerMessage(
        EventId = 2820,
        Level = LogLevel.Information,
        Message = "Returning cached response for duplicate message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ReturningCachedResponse(
        ILogger logger,
        string messageId,
        string? correlationId);

    /// <summary>Logs when a message has exceeded max retries.</summary>
    [LoggerMessage(
        EventId = 2821,
        Level = LogLevel.Warning,
        Message = "Message {MessageId} exceeded max retries ({MaxRetries}) (CorrelationId: {CorrelationId})")]
    public static partial void MaxRetriesExceeded(
        ILogger logger,
        string messageId,
        int maxRetries,
        string? correlationId);

    /// <summary>Logs when a message is successfully processed and cached.</summary>
    [LoggerMessage(
        EventId = 2822,
        Level = LogLevel.Information,
        Message = "Successfully processed and cached message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ProcessedAndCachedMessage(
        ILogger logger,
        string messageId,
        string? correlationId);

    /// <summary>Logs when an error occurs processing a message.</summary>
    [LoggerMessage(
        EventId = 2823,
        Level = LogLevel.Error,
        Message = "Error processing message {MessageId} (CorrelationId: {CorrelationId})")]
    public static partial void ErrorProcessingMessage(
        ILogger logger,
        Exception exception,
        string messageId,
        string? correlationId);

    // =========================================================================
    // Outbox Post Processor (EventIds 2824-2826)
    // =========================================================================

    /// <summary>Logs when storing notifications in the outbox.</summary>
    [LoggerMessage(
        EventId = 2824,
        Level = LogLevel.Debug,
        Message = "Storing {Count} notifications in outbox for request {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void StoringNotificationsInOutbox(
        ILogger logger,
        int count,
        string requestType,
        string? correlationId);

    /// <summary>Logs when notifications are stored in the outbox.</summary>
    [LoggerMessage(
        EventId = 2825,
        Level = LogLevel.Information,
        Message = "Stored {Count} notifications in outbox (CorrelationId: {CorrelationId})")]
    public static partial void StoredNotificationsInOutbox(
        ILogger logger,
        int count,
        string? correlationId);

    // =========================================================================
    // Outbox Processor (EventIds 2827-2833)
    // =========================================================================

    /// <summary>Logs when the outbox processor is disabled.</summary>
    [LoggerMessage(
        EventId = 2827,
        Level = LogLevel.Information,
        Message = "Outbox processor is disabled")]
    public static partial void OutboxProcessorDisabled(ILogger logger);

    /// <summary>Logs when the outbox processor starts.</summary>
    [LoggerMessage(
        EventId = 2828,
        Level = LogLevel.Information,
        Message = "Outbox processor started. Interval: {Interval}, BatchSize: {BatchSize}")]
    public static partial void OutboxProcessorStarted(
        ILogger logger,
        TimeSpan interval,
        int batchSize);

    /// <summary>Logs when an error occurs processing outbox messages.</summary>
    [LoggerMessage(
        EventId = 2829,
        Level = LogLevel.Error,
        Message = "Error processing outbox messages")]
    public static partial void ErrorProcessingOutboxMessages(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when processing pending outbox messages.</summary>
    [LoggerMessage(
        EventId = 2830,
        Level = LogLevel.Debug,
        Message = "Processing {Count} pending outbox messages")]
    public static partial void ProcessingPendingOutboxMessages(
        ILogger logger,
        int count);

    /// <summary>Logs when an outbox message is processed.</summary>
    [LoggerMessage(
        EventId = 2831,
        Level = LogLevel.Debug,
        Message = "Processed outbox message {MessageId} of type {NotificationType}")]
    public static partial void ProcessedOutboxMessage(
        ILogger logger,
        Guid messageId,
        string notificationType);

    /// <summary>Logs when an outbox message fails to process and a retry is scheduled.</summary>
    /// <remarks>
    /// <paramref name="failureReason"/> is the <see cref="EncinaError"/> code, the exception type, or a
    /// fixed description (unknown type, undeserializable payload); never <c>EncinaError.Message</c>,
    /// which can carry personal data.
    /// </remarks>
    [LoggerMessage(
        EventId = 2832,
        Level = LogLevel.Warning,
        Message = "Failed to process outbox message {MessageId}: {FailureReason}. Retry {RetryCount}/{MaxRetries}. Next retry at {NextRetry}")]
    public static partial void FailedToProcessOutboxMessage(
        ILogger logger,
        Exception? exception,
        Guid messageId,
        string failureReason,
        int retryCount,
        int maxRetries,
        DateTime? nextRetry);

    /// <summary>Logs a summary of processed outbox messages.</summary>
    [LoggerMessage(
        EventId = 2833,
        Level = LogLevel.Information,
        Message = "Processed {TotalCount} outbox messages (Success: {SuccessCount}, Failed: {FailureCount})")]
    public static partial void ProcessedOutboxMessages(
        ILogger logger,
        int totalCount,
        int successCount,
        int failureCount);

    // =========================================================================
    // Transaction Pipeline Behavior (EventIds 2834-2836)
    // =========================================================================

    /// <summary>Logs when a transaction is started.</summary>
    [LoggerMessage(
        EventId = 2834,
        Level = LogLevel.Debug,
        Message = "Transaction started for {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void TransactionStarted(
        ILogger logger,
        string requestType,
        string? correlationId);

    /// <summary>Logs when a transaction is committed.</summary>
    [LoggerMessage(
        EventId = 2835,
        Level = LogLevel.Debug,
        Message = "Transaction committed for {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void TransactionCommitted(
        ILogger logger,
        string requestType,
        string? correlationId);

    /// <summary>Logs when a transaction is rolled back.</summary>
    [LoggerMessage(
        EventId = 2836,
        Level = LogLevel.Debug,
        Message = "Transaction rolled back for {RequestType} (CorrelationId: {CorrelationId})")]
    public static partial void TransactionRolledBack(
        ILogger logger,
        string requestType,
        string? correlationId);

    // =========================================================================
    // Outbox Processor, exhausted retries (EventId 2958)
    // =========================================================================

    /// <summary>
    /// Logs when a failure uses up the retries of an outbox message, so it will not be fetched again.
    /// </summary>
    /// <remarks>
    /// The <paramref name="errorCode"/> is <c>outbox.max_retries_exceeded</c>; <paramref name="lastFailureReason"/>
    /// is the last failure's error code, exception type or fixed description, never <c>EncinaError.Message</c>.
    /// </remarks>
    [LoggerMessage(
        EventId = 2958,
        Level = LogLevel.Error,
        Message = "Outbox message {MessageId} of type {NotificationType} failed {RetryCount} times and will not be retried ({ErrorCode}). Last failure: {LastFailureReason}")]
    public static partial void OutboxMessageRetriesExhausted(
        ILogger logger,
        Exception? exception,
        Guid messageId,
        string notificationType,
        int retryCount,
        string errorCode,
        string lastFailureReason);

    // =========================================================================
    // Outbox Processor, Retry-After above the ceiling (EventId 5456)
    // =========================================================================

    /// <summary>
    /// Logs when a failure carries a <c>Retry-After</c> above <see cref="Outbox.OutboxOptions.MaxRetryAfter"/>, so
    /// the outbox message is exhausted and dead-lettered at once.
    /// </summary>
    /// <remarks>
    /// Event IDs: 5450-5499 (see EventIdRanges.MessagingRecoverability). <paramref name="errorCode"/> is the code
    /// of the original failure, never <c>EncinaError.Message</c>.
    /// </remarks>
    [LoggerMessage(
        EventId = 5456,
        Level = LogLevel.Warning,
        Message = "Outbox message {MessageId} of type {NotificationType} not retried: Retry-After {RetryAfter} exceeds the ceiling {MaxRetryAfter}, dead-lettered ({ErrorCode})")]
    public static partial void OutboxRetryAfterExceedsCeiling(
        ILogger logger,
        Guid messageId,
        string notificationType,
        string errorCode,
        TimeSpan retryAfter,
        TimeSpan maxRetryAfter);

    // =========================================================================
    // Outbox requeue of exhausted messages (EventId 2959)
    // =========================================================================

    /// <summary>
    /// Logs when exhausted outbox messages are returned to the pending state.
    /// </summary>
    /// <remarks>
    /// <paramref name="requeueScope"/> is <c>all</c> when every exhausted message was requested, or
    /// <c>ids:N</c> when N distinct message identifiers were requested.
    /// </remarks>
    [LoggerMessage(
        EventId = 2959,
        Level = LogLevel.Information,
        Message = "Requeued {RequeuedCount} exhausted outbox messages (scope: {RequeueScope})")]
    public static partial void OutboxExhaustedMessagesRequeued(
        ILogger logger,
        int requeuedCount,
        string requeueScope);

    // =========================================================================
    // Outbox Processor, store results and cancellation (EventIds 2960-2962)
    // =========================================================================

    /// <summary>
    /// Logs when the outbox store fails to save the outcomes of a processed batch, so none of them
    /// was persisted and the messages will be fetched again.
    /// </summary>
    [LoggerMessage(
        EventId = 2960,
        Level = LogLevel.Error,
        Message = "Failed to save the outcomes of {MessageCount} outbox messages; they will be delivered again (error code {ErrorCode})")]
    public static partial void OutboxBatchSaveFailed(
        ILogger logger,
        int messageCount,
        string errorCode);

    /// <summary>
    /// Logs when the outbox store fails to record the outcome of one message.
    /// </summary>
    /// <remarks><paramref name="operation"/> is <c>MarkAsProcessedAsync</c> or <c>MarkAsFailedAsync</c>.</remarks>
    [LoggerMessage(
        EventId = 2961,
        Level = LogLevel.Error,
        Message = "Outbox store failed to record {Operation} for message {MessageId}: error code {ErrorCode}")]
    public static partial void OutboxMessageOutcomeNotRecorded(
        ILogger logger,
        string operation,
        Guid messageId,
        string errorCode);

    /// <summary>
    /// Logs when cancellation stops an outbox batch; the interrupted message keeps its retry budget.
    /// </summary>
    [LoggerMessage(
        EventId = 2962,
        Level = LogLevel.Information,
        Message = "Outbox batch stopped by cancellation at message {MessageId}; it was not marked failed")]
    public static partial void OutboxBatchCancelled(
        ILogger logger,
        Guid messageId);

    // =========================================================================
    // Shared transaction and transactional outbox (EventIds 2963-2970)
    // =========================================================================

    /// <summary>Logs when a participant joins the scope's active transaction instead of beginning one.</summary>
    [LoggerMessage(
        EventId = 2963,
        Level = LogLevel.Debug,
        Message = "Participant joined the active transaction (join depth {JoinDepth})")]
    public static partial void TransactionJoined(
        ILogger logger,
        int joinDepth);

    /// <summary>Logs when a joined participant rolled back, so the owner's commit will be refused.</summary>
    [LoggerMessage(
        EventId = 2964,
        Level = LogLevel.Warning,
        Message = "A participant rolled back the shared transaction; it is now rollback-only")]
    public static partial void TransactionMarkedRollbackOnly(ILogger logger);

    /// <summary>Logs when the owner's commit was refused because the transaction is rollback-only.</summary>
    [LoggerMessage(
        EventId = 2965,
        Level = LogLevel.Warning,
        Message = "The shared transaction was rolled back instead of committed because a participant rolled back ({ErrorCode})")]
    public static partial void TransactionRollbackOnlyCommitRefused(
        ILogger logger,
        string errorCode);

    /// <summary>Logs when the commit of the shared transaction failed; the transaction was rolled back.</summary>
    /// <remarks>The exception is passed through <c>ForLogging()</c>: its type and stack trace, never its message.</remarks>
    [LoggerMessage(
        EventId = 2966,
        Level = LogLevel.Error,
        Message = "Committing the shared transaction failed ({ErrorCode})")]
    public static partial void TransactionCommitFailed(
        ILogger logger,
        Exception exception,
        string errorCode);

    /// <summary>Logs the notifications of a request written to the outbox inside its transaction.</summary>
    [LoggerMessage(
        EventId = 2967,
        Level = LogLevel.Debug,
        Message = "Wrote {NotificationCount} notifications of {RequestType} to the outbox inside its transaction")]
    public static partial void OutboxNotificationsWritten(
        ILogger logger,
        int notificationCount,
        string requestType);

    /// <summary>Logs when writing a request's notifications to the outbox failed; the request fails and its transaction rolls back.</summary>
    /// <remarks><paramref name="errorCode"/> is the <see cref="EncinaError"/> code; never its message.</remarks>
    [LoggerMessage(
        EventId = 2968,
        Level = LogLevel.Error,
        Message = "Writing the notifications of {RequestType} to the outbox failed ({ErrorCode}); the request fails and its transaction rolls back")]
    public static partial void OutboxWriteFailed(
        ILogger logger,
        string requestType,
        string errorCode);

    /// <summary>Logs when the transactional outbox behavior skipped a request that carries no notifications.</summary>
    [LoggerMessage(
        EventId = 2969,
        Level = LogLevel.Trace,
        Message = "Transactional outbox skipped {RequestType}: it does not implement IHasNotifications")]
    public static partial void OutboxBehaviorSkipped(
        ILogger logger,
        string requestType);

    /// <summary>Logs when rolling back the shared transaction failed; the failure is swallowed so cleanup never masks the original one.</summary>
    /// <remarks>The exception is passed through <c>ForLogging()</c>: its type and stack trace, never its message.</remarks>
    [LoggerMessage(
        EventId = 2970,
        Level = LogLevel.Warning,
        Message = "Rolling back the shared transaction failed; the database discards it on its own")]
    public static partial void TransactionRollbackFailed(
        ILogger logger,
        Exception exception);
}
