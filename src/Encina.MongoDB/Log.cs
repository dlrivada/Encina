using Microsoft.Extensions.Logging;

namespace Encina.MongoDB;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // MongoDbIndexCreator: EventIds 3100-3105
    [LoggerMessage(EventId = 3100, Level = LogLevel.Information, Message = "MongoDB indexes created successfully")]
    public static partial void IndexesCreatedSuccessfully(ILogger logger);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Error, Message = "Failed to create MongoDB indexes")]
    public static partial void FailedToCreateIndexes(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Debug, Message = "Created outbox indexes")]
    public static partial void CreatedOutboxIndexes(ILogger logger);

    [LoggerMessage(EventId = 3103, Level = LogLevel.Debug, Message = "Created inbox indexes")]
    public static partial void CreatedInboxIndexes(ILogger logger);

    [LoggerMessage(EventId = 3104, Level = LogLevel.Debug, Message = "Created saga indexes")]
    public static partial void CreatedSagaIndexes(ILogger logger);

    [LoggerMessage(EventId = 3105, Level = LogLevel.Debug, Message = "Created scheduling indexes")]
    public static partial void CreatedSchedulingIndexes(ILogger logger);

    // OutboxStoreMongoDB: EventIds 3106-3111
    [LoggerMessage(EventId = 3106, Level = LogLevel.Debug, Message = "Added outbox message {MessageId}")]
    public static partial void AddedOutboxMessage(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3107, Level = LogLevel.Debug, Message = "Retrieved {Count} pending outbox messages")]
    public static partial void RetrievedPendingOutboxMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 3108, Level = LogLevel.Debug, Message = "Marked outbox message {MessageId} as processed")]
    public static partial void MarkedOutboxMessageAsProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3109, Level = LogLevel.Warning, Message = "Outbox message {MessageId} not found for marking as processed")]
    public static partial void OutboxMessageNotFoundForProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3110, Level = LogLevel.Debug, Message = "Marked outbox message {MessageId} as failed: {ErrorMessage}")]
    public static partial void MarkedOutboxMessageAsFailed(ILogger logger, Guid messageId, string errorMessage);

    [LoggerMessage(EventId = 3111, Level = LogLevel.Warning, Message = "Outbox message {MessageId} not found for marking as failed")]
    public static partial void OutboxMessageNotFoundForFailed(ILogger logger, Guid messageId);

    // InboxStoreMongoDB: EventIds 3112-3120
    [LoggerMessage(EventId = 3112, Level = LogLevel.Debug, Message = "Added inbox message {MessageId}")]
    public static partial void AddedInboxMessage(ILogger logger, string messageId);

    [LoggerMessage(EventId = 3113, Level = LogLevel.Debug, Message = "Found inbox message {MessageId}")]
    public static partial void FoundInboxMessage(ILogger logger, string messageId);

    [LoggerMessage(EventId = 3114, Level = LogLevel.Debug, Message = "Marked inbox message {MessageId} as processed")]
    public static partial void MarkedInboxMessageAsProcessed(ILogger logger, string messageId);

    [LoggerMessage(EventId = 3115, Level = LogLevel.Warning, Message = "Inbox message {MessageId} not found for marking as processed")]
    public static partial void InboxMessageNotFoundForProcessed(ILogger logger, string messageId);

    [LoggerMessage(EventId = 3116, Level = LogLevel.Debug, Message = "Cleaned up {Count} expired inbox messages")]
    public static partial void CleanedUpExpiredInboxMessages(ILogger logger, long count);

    [LoggerMessage(EventId = 3117, Level = LogLevel.Warning, Message = "Inbox message {MessageId} not found for marking as failed")]
    public static partial void InboxMessageNotFoundForFailed(ILogger logger, string messageId);

    [LoggerMessage(EventId = 3118, Level = LogLevel.Debug, Message = "Marked inbox message {MessageId} as failed: {ErrorMessage}")]
    public static partial void MarkedInboxMessageAsFailed(ILogger logger, string messageId, string errorMessage);

    [LoggerMessage(EventId = 3119, Level = LogLevel.Debug, Message = "Retrieved {Count} expired inbox messages")]
    public static partial void RetrievedExpiredInboxMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 3120, Level = LogLevel.Debug, Message = "Removed {Count} expired inbox messages")]
    public static partial void RemovedExpiredInboxMessages(ILogger logger, long count);

    // SagaStoreMongoDB: EventIds 3121-3135
    [LoggerMessage(EventId = 3121, Level = LogLevel.Debug, Message = "Created saga {SagaId} of type {SagaType}")]
    public static partial void CreatedSaga(ILogger logger, Guid sagaId, string sagaType);

    [LoggerMessage(EventId = 3122, Level = LogLevel.Debug, Message = "Retrieved saga {SagaId}")]
    public static partial void RetrievedSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3123, Level = LogLevel.Debug, Message = "Saga {SagaId} not found")]
    public static partial void SagaNotFound(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3124, Level = LogLevel.Debug, Message = "Updated saga {SagaId} state to step {CurrentStep}")]
    public static partial void UpdatedSagaState(ILogger logger, Guid sagaId, int currentStep);

    [LoggerMessage(EventId = 3125, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for state update")]
    public static partial void SagaNotFoundForStateUpdate(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3126, Level = LogLevel.Debug, Message = "Completed saga {SagaId}")]
    public static partial void CompletedSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3127, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for completion")]
    public static partial void SagaNotFoundForCompletion(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3128, Level = LogLevel.Debug, Message = "Failed saga {SagaId}: {ErrorMessage}")]
    public static partial void FailedSaga(ILogger logger, Guid sagaId, string errorMessage);

    [LoggerMessage(EventId = 3129, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for failure")]
    public static partial void SagaNotFoundForFailure(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3130, Level = LogLevel.Debug, Message = "Compensating saga {SagaId}")]
    public static partial void CompensatingSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3131, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for compensation")]
    public static partial void SagaNotFoundForCompensation(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3132, Level = LogLevel.Debug, Message = "Compensated saga {SagaId}")]
    public static partial void CompensatedSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3133, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for compensated status")]
    public static partial void SagaNotFoundForCompensated(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 3134, Level = LogLevel.Debug, Message = "Retrieved {Count} stuck sagas")]
    public static partial void RetrievedStuckSagas(ILogger logger, int count);

    [LoggerMessage(EventId = 3135, Level = LogLevel.Debug, Message = "Retrieved {Count} expired sagas")]
    public static partial void RetrievedExpiredSagas(ILogger logger, int count);

    // ScheduledMessageStoreMongoDB: EventIds 3136-3145
    [LoggerMessage(EventId = 3136, Level = LogLevel.Debug, Message = "Added scheduled message {MessageId} for {ScheduledAt}")]
    public static partial void AddedScheduledMessage(ILogger logger, Guid messageId, DateTime scheduledAt);

    [LoggerMessage(EventId = 3137, Level = LogLevel.Debug, Message = "Retrieved {Count} due scheduled messages")]
    public static partial void RetrievedDueScheduledMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 3138, Level = LogLevel.Debug, Message = "Marked scheduled message {MessageId} as processed")]
    public static partial void MarkedScheduledMessageAsProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3139, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for marking as processed")]
    public static partial void ScheduledMessageNotFoundForProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3140, Level = LogLevel.Debug, Message = "Marked scheduled message {MessageId} as failed: {ErrorMessage}")]
    public static partial void MarkedScheduledMessageAsFailed(ILogger logger, Guid messageId, string errorMessage);

    [LoggerMessage(EventId = 3141, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for marking as failed")]
    public static partial void ScheduledMessageNotFoundForFailed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3142, Level = LogLevel.Debug, Message = "Rescheduled message {MessageId} for {NextScheduledAtUtc}")]
    public static partial void RescheduledMessage(ILogger logger, Guid messageId, DateTime nextScheduledAtUtc);

    [LoggerMessage(EventId = 3143, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for rescheduling")]
    public static partial void ScheduledMessageNotFoundForRescheduling(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3144, Level = LogLevel.Debug, Message = "Cancelled scheduled message {MessageId}")]
    public static partial void CancelledScheduledMessage(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 3145, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for cancellation")]
    public static partial void ScheduledMessageNotFoundForCancellation(ILogger logger, Guid messageId);

    // Module Isolation: EventIds 3146-3149
    [LoggerMessage(EventId = 3146, Level = LogLevel.Warning, Message = "No module context available when accessing collection '{CollectionName}', falling back to base database")]
    public static partial void NoModuleContextFallingBackToBaseDatabase(ILogger logger, string collectionName);

    [LoggerMessage(EventId = 3147, Level = LogLevel.Debug, Message = "Using module database '{DatabaseName}' for module '{ModuleName}'")]
    public static partial void UsingModuleDatabase(ILogger logger, string databaseName, string moduleName);

    [LoggerMessage(EventId = 3148, Level = LogLevel.Information, Message = "Module isolation enabled with database-per-module strategy")]
    public static partial void ModuleIsolationEnabled(ILogger logger);

    [LoggerMessage(EventId = 3149, Level = LogLevel.Debug, Message = "Module database mapping: '{ModuleName}' -> '{DatabaseName}'")]
    public static partial void ModuleDatabaseMapping(ILogger logger, string moduleName, string databaseName);

    // AuditLogStoreMongoDB: EventIds 3150-3152
    [LoggerMessage(EventId = 3150, Level = LogLevel.Debug, Message = "Added audit log entry {EntryId} for {EntityType}:{EntityId}")]
    public static partial void AddedAuditLogEntry(ILogger logger, string entryId, string entityType, string entityId);

    [LoggerMessage(EventId = 3151, Level = LogLevel.Debug, Message = "Retrieved {Count} audit log entries for {EntityType}:{EntityId}")]
    public static partial void RetrievedAuditLogHistory(ILogger logger, int count, string entityType, string entityId);

    [LoggerMessage(EventId = 3152, Level = LogLevel.Debug, Message = "Created audit log indexes")]
    public static partial void CreatedAuditLogIndexes(ILogger logger);

    // AuditStoreMongoDB (Security Audit): EventIds 3153-3161
    [LoggerMessage(EventId = 3153, Level = LogLevel.Debug, Message = "Added security audit entry {EntryId} for {EntityType}:{EntityId}")]
    public static partial void AddedSecurityAuditEntry(ILogger logger, Guid entryId, string entityType, string? entityId);

    [LoggerMessage(EventId = 3154, Level = LogLevel.Debug, Message = "Created security audit indexes")]
    public static partial void CreatedSecurityAuditIndexes(ILogger logger);

    [LoggerMessage(EventId = 3155, Level = LogLevel.Error, Message = "Failed to record audit entry {EntryId}")]
    public static partial void FailedToRecordAuditEntry(ILogger logger, Exception exception, Guid entryId);

    [LoggerMessage(EventId = 3156, Level = LogLevel.Error, Message = "Failed to query audit entries by entity type {EntityType}")]
    public static partial void FailedToQueryAuditEntriesByEntity(ILogger logger, Exception exception, string entityType);

    [LoggerMessage(EventId = 3157, Level = LogLevel.Error, Message = "Failed to query audit entries by user {UserId}")]
    public static partial void FailedToQueryAuditEntriesByUser(ILogger logger, Exception exception, string userId);

    [LoggerMessage(EventId = 3158, Level = LogLevel.Error, Message = "Failed to query audit entries by correlation ID {CorrelationId}")]
    public static partial void FailedToQueryAuditEntriesByCorrelationId(ILogger logger, Exception exception, string correlationId);

    [LoggerMessage(EventId = 3159, Level = LogLevel.Error, Message = "Failed to execute audit query")]
    public static partial void FailedToExecuteAuditQuery(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3160, Level = LogLevel.Information, Message = "Purged {Count} audit entries older than {OlderThanUtc}")]
    public static partial void PurgedAuditEntries(ILogger logger, int count, DateTime olderThanUtc);

    [LoggerMessage(EventId = 3161, Level = LogLevel.Error, Message = "Failed to purge audit entries older than {OlderThanUtc}")]
    public static partial void FailedToPurgeAuditEntries(ILogger logger, Exception exception, DateTime olderThanUtc);

}
