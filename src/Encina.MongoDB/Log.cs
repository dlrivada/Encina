using Microsoft.Extensions.Logging;

namespace Encina.MongoDB;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // MongoDbIndexCreator: EventIds 1-5
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "MongoDB indexes created successfully")]
    public static partial void IndexesCreatedSuccessfully(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to create MongoDB indexes")]
    public static partial void FailedToCreateIndexes(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Created outbox indexes")]
    public static partial void CreatedOutboxIndexes(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Created inbox indexes")]
    public static partial void CreatedInboxIndexes(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Created saga indexes")]
    public static partial void CreatedSagaIndexes(ILogger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Created scheduling indexes")]
    public static partial void CreatedSchedulingIndexes(ILogger logger);

    // OutboxStoreMongoDB: EventIds 10-19
    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Added outbox message {MessageId}")]
    public static partial void AddedOutboxMessage(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Retrieved {Count} pending outbox messages")]
    public static partial void RetrievedPendingOutboxMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Marked outbox message {MessageId} as processed")]
    public static partial void MarkedOutboxMessageAsProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Outbox message {MessageId} not found for marking as processed")]
    public static partial void OutboxMessageNotFoundForProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Debug, Message = "Marked outbox message {MessageId} as failed: {ErrorMessage}")]
    public static partial void MarkedOutboxMessageAsFailed(ILogger logger, Guid messageId, string errorMessage);

    [LoggerMessage(EventId = 15, Level = LogLevel.Warning, Message = "Outbox message {MessageId} not found for marking as failed")]
    public static partial void OutboxMessageNotFoundForFailed(ILogger logger, Guid messageId);

    // InboxStoreMongoDB: EventIds 20-29
    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Added inbox message {MessageId}")]
    public static partial void AddedInboxMessage(ILogger logger, string messageId);

    [LoggerMessage(EventId = 21, Level = LogLevel.Debug, Message = "Found inbox message {MessageId}")]
    public static partial void FoundInboxMessage(ILogger logger, string messageId);

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "Marked inbox message {MessageId} as processed")]
    public static partial void MarkedInboxMessageAsProcessed(ILogger logger, string messageId);

    [LoggerMessage(EventId = 23, Level = LogLevel.Warning, Message = "Inbox message {MessageId} not found for marking as processed")]
    public static partial void InboxMessageNotFoundForProcessed(ILogger logger, string messageId);

    [LoggerMessage(EventId = 24, Level = LogLevel.Debug, Message = "Cleaned up {Count} expired inbox messages")]
    public static partial void CleanedUpExpiredInboxMessages(ILogger logger, long count);

    [LoggerMessage(EventId = 25, Level = LogLevel.Warning, Message = "Inbox message {MessageId} not found for marking as failed")]
    public static partial void InboxMessageNotFoundForFailed(ILogger logger, string messageId);

    [LoggerMessage(EventId = 26, Level = LogLevel.Debug, Message = "Marked inbox message {MessageId} as failed: {ErrorMessage}")]
    public static partial void MarkedInboxMessageAsFailed(ILogger logger, string messageId, string errorMessage);

    [LoggerMessage(EventId = 27, Level = LogLevel.Debug, Message = "Retrieved {Count} expired inbox messages")]
    public static partial void RetrievedExpiredInboxMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 28, Level = LogLevel.Debug, Message = "Removed {Count} expired inbox messages")]
    public static partial void RemovedExpiredInboxMessages(ILogger logger, long count);

    // SagaStoreMongoDB: EventIds 30-49
    [LoggerMessage(EventId = 30, Level = LogLevel.Debug, Message = "Created saga {SagaId} of type {SagaType}")]
    public static partial void CreatedSaga(ILogger logger, Guid sagaId, string sagaType);

    [LoggerMessage(EventId = 31, Level = LogLevel.Debug, Message = "Retrieved saga {SagaId}")]
    public static partial void RetrievedSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 32, Level = LogLevel.Debug, Message = "Saga {SagaId} not found")]
    public static partial void SagaNotFound(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 33, Level = LogLevel.Debug, Message = "Updated saga {SagaId} state to step {CurrentStep}")]
    public static partial void UpdatedSagaState(ILogger logger, Guid sagaId, int currentStep);

    [LoggerMessage(EventId = 34, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for state update")]
    public static partial void SagaNotFoundForStateUpdate(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 35, Level = LogLevel.Debug, Message = "Completed saga {SagaId}")]
    public static partial void CompletedSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 36, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for completion")]
    public static partial void SagaNotFoundForCompletion(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 37, Level = LogLevel.Debug, Message = "Failed saga {SagaId}: {ErrorMessage}")]
    public static partial void FailedSaga(ILogger logger, Guid sagaId, string errorMessage);

    [LoggerMessage(EventId = 38, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for failure")]
    public static partial void SagaNotFoundForFailure(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 39, Level = LogLevel.Debug, Message = "Compensating saga {SagaId}")]
    public static partial void CompensatingSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 40, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for compensation")]
    public static partial void SagaNotFoundForCompensation(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 41, Level = LogLevel.Debug, Message = "Compensated saga {SagaId}")]
    public static partial void CompensatedSaga(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 42, Level = LogLevel.Warning, Message = "Saga {SagaId} not found for compensated status")]
    public static partial void SagaNotFoundForCompensated(ILogger logger, Guid sagaId);

    [LoggerMessage(EventId = 43, Level = LogLevel.Debug, Message = "Retrieved {Count} stuck sagas")]
    public static partial void RetrievedStuckSagas(ILogger logger, int count);

    [LoggerMessage(EventId = 44, Level = LogLevel.Debug, Message = "Retrieved {Count} expired sagas")]
    public static partial void RetrievedExpiredSagas(ILogger logger, int count);

    // ScheduledMessageStoreMongoDB: EventIds 50-59
    [LoggerMessage(EventId = 50, Level = LogLevel.Debug, Message = "Added scheduled message {MessageId} for {ScheduledAt}")]
    public static partial void AddedScheduledMessage(ILogger logger, Guid messageId, DateTime scheduledAt);

    [LoggerMessage(EventId = 51, Level = LogLevel.Debug, Message = "Retrieved {Count} due scheduled messages")]
    public static partial void RetrievedDueScheduledMessages(ILogger logger, int count);

    [LoggerMessage(EventId = 52, Level = LogLevel.Debug, Message = "Marked scheduled message {MessageId} as processed")]
    public static partial void MarkedScheduledMessageAsProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 53, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for marking as processed")]
    public static partial void ScheduledMessageNotFoundForProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 54, Level = LogLevel.Debug, Message = "Marked scheduled message {MessageId} as failed: {ErrorMessage}")]
    public static partial void MarkedScheduledMessageAsFailed(ILogger logger, Guid messageId, string errorMessage);

    [LoggerMessage(EventId = 55, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for marking as failed")]
    public static partial void ScheduledMessageNotFoundForFailed(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 56, Level = LogLevel.Debug, Message = "Rescheduled message {MessageId} for {NextScheduledAtUtc}")]
    public static partial void RescheduledMessage(ILogger logger, Guid messageId, DateTime nextScheduledAtUtc);

    [LoggerMessage(EventId = 57, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for rescheduling")]
    public static partial void ScheduledMessageNotFoundForRescheduling(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 58, Level = LogLevel.Debug, Message = "Cancelled scheduled message {MessageId}")]
    public static partial void CancelledScheduledMessage(ILogger logger, Guid messageId);

    [LoggerMessage(EventId = 59, Level = LogLevel.Warning, Message = "Scheduled message {MessageId} not found for cancellation")]
    public static partial void ScheduledMessageNotFoundForCancellation(ILogger logger, Guid messageId);

    // Module Isolation: EventIds 60-69
    [LoggerMessage(EventId = 60, Level = LogLevel.Warning, Message = "No module context available when accessing collection '{CollectionName}', falling back to base database")]
    public static partial void NoModuleContextFallingBackToBaseDatabase(ILogger logger, string collectionName);

    [LoggerMessage(EventId = 61, Level = LogLevel.Debug, Message = "Using module database '{DatabaseName}' for module '{ModuleName}'")]
    public static partial void UsingModuleDatabase(ILogger logger, string databaseName, string moduleName);

    [LoggerMessage(EventId = 62, Level = LogLevel.Information, Message = "Module isolation enabled with database-per-module strategy")]
    public static partial void ModuleIsolationEnabled(ILogger logger);

    [LoggerMessage(EventId = 63, Level = LogLevel.Debug, Message = "Module database mapping: '{ModuleName}' -> '{DatabaseName}'")]
    public static partial void ModuleDatabaseMapping(ILogger logger, string moduleName, string databaseName);

    // AuditLogStoreMongoDB: EventIds 70-79
    [LoggerMessage(EventId = 70, Level = LogLevel.Debug, Message = "Added audit log entry {EntryId} for {EntityType}:{EntityId}")]
    public static partial void AddedAuditLogEntry(ILogger logger, string entryId, string entityType, string entityId);

    [LoggerMessage(EventId = 71, Level = LogLevel.Debug, Message = "Retrieved {Count} audit log entries for {EntityType}:{EntityId}")]
    public static partial void RetrievedAuditLogHistory(ILogger logger, int count, string entityType, string entityId);

    [LoggerMessage(EventId = 72, Level = LogLevel.Debug, Message = "Created audit log indexes")]
    public static partial void CreatedAuditLogIndexes(ILogger logger);

    // AuditStoreMongoDB (Security Audit): EventIds 80-89
    [LoggerMessage(EventId = 80, Level = LogLevel.Debug, Message = "Added security audit entry {EntryId} for {EntityType}:{EntityId}")]
    public static partial void AddedSecurityAuditEntry(ILogger logger, Guid entryId, string entityType, string? entityId);

    [LoggerMessage(EventId = 81, Level = LogLevel.Debug, Message = "Created security audit indexes")]
    public static partial void CreatedSecurityAuditIndexes(ILogger logger);

    [LoggerMessage(EventId = 82, Level = LogLevel.Error, Message = "Failed to record audit entry {EntryId}")]
    public static partial void FailedToRecordAuditEntry(ILogger logger, Exception exception, Guid entryId);

    [LoggerMessage(EventId = 83, Level = LogLevel.Error, Message = "Failed to query audit entries by entity type {EntityType}")]
    public static partial void FailedToQueryAuditEntriesByEntity(ILogger logger, Exception exception, string entityType);

    [LoggerMessage(EventId = 84, Level = LogLevel.Error, Message = "Failed to query audit entries by user {UserId}")]
    public static partial void FailedToQueryAuditEntriesByUser(ILogger logger, Exception exception, string userId);

    [LoggerMessage(EventId = 85, Level = LogLevel.Error, Message = "Failed to query audit entries by correlation ID {CorrelationId}")]
    public static partial void FailedToQueryAuditEntriesByCorrelationId(ILogger logger, Exception exception, string correlationId);

    [LoggerMessage(EventId = 86, Level = LogLevel.Error, Message = "Failed to execute audit query")]
    public static partial void FailedToExecuteAuditQuery(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 87, Level = LogLevel.Information, Message = "Purged {Count} audit entries older than {OlderThanUtc}")]
    public static partial void PurgedAuditEntries(ILogger logger, int count, DateTime olderThanUtc);

    [LoggerMessage(EventId = 88, Level = LogLevel.Error, Message = "Failed to purge audit entries older than {OlderThanUtc}")]
    public static partial void FailedToPurgeAuditEntries(ILogger logger, Exception exception, DateTime olderThanUtc);

    // ConsentStoreMongoDB: EventIds 90-99
    [LoggerMessage(EventId = 90, Level = LogLevel.Debug, Message = "Recorded consent for subject {SubjectId} purpose {Purpose}")]
    public static partial void RecordedConsent(ILogger logger, string subjectId, string purpose);

    [LoggerMessage(EventId = 91, Level = LogLevel.Debug, Message = "Retrieved consent for subject {SubjectId} purpose {Purpose}")]
    public static partial void RetrievedConsent(ILogger logger, string subjectId, string purpose);

    [LoggerMessage(EventId = 92, Level = LogLevel.Debug, Message = "Withdrew consent for subject {SubjectId} purpose {Purpose}")]
    public static partial void WithdrewConsent(ILogger logger, string subjectId, string purpose);

    [LoggerMessage(EventId = 93, Level = LogLevel.Debug, Message = "Bulk recorded {SuccessCount} consents ({FailureCount} failures)")]
    public static partial void BulkRecordedConsents(ILogger logger, int successCount, int failureCount);

    [LoggerMessage(EventId = 94, Level = LogLevel.Debug, Message = "Bulk withdrew {SuccessCount} consents for subject {SubjectId} ({FailureCount} failures)")]
    public static partial void BulkWithdrewConsents(ILogger logger, string subjectId, int successCount, int failureCount);

    [LoggerMessage(EventId = 95, Level = LogLevel.Debug, Message = "Recorded consent audit entry {EntryId} for subject {SubjectId}")]
    public static partial void RecordedConsentAuditEntry(ILogger logger, Guid entryId, string subjectId);

    [LoggerMessage(EventId = 96, Level = LogLevel.Debug, Message = "Retrieved {Count} consent audit entries for subject {SubjectId}")]
    public static partial void RetrievedConsentAuditTrail(ILogger logger, int count, string subjectId);

    [LoggerMessage(EventId = 97, Level = LogLevel.Debug, Message = "Published consent version {VersionId} for purpose {Purpose}")]
    public static partial void PublishedConsentVersion(ILogger logger, string versionId, string purpose);

    [LoggerMessage(EventId = 98, Level = LogLevel.Debug, Message = "Created consent indexes")]
    public static partial void CreatedConsentIndexes(ILogger logger);

    [LoggerMessage(EventId = 99, Level = LogLevel.Error, Message = "Consent store operation failed: {Operation}")]
    public static partial void ConsentStoreOperationFailed(ILogger logger, Exception exception, string operation);
}
