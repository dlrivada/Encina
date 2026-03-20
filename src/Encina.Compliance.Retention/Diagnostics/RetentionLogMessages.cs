using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Retention.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Retention module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8500-8599 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8259, DSR uses 8300-8349,
/// LawfulBasis uses 8350-8399, Anonymization uses 8400-8499,
/// CrossBorderTransfer uses 9000-9059, ProcessorAgreements uses 9100-9199).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>8500-8509</term><description>Pipeline behavior</description></item>
/// <item><term>8510-8519</term><description>Enforcement service</description></item>
/// <item><term>8520-8529</term><description>Auto-registration</description></item>
/// <item><term>8530-8539</term><description>Health check</description></item>
/// <item><term>8540-8549</term><description>Legal hold management</description></item>
/// <item><term>8550-8559</term><description>Retention policy</description></item>
/// <item><term>8560-8569</term><description>Audit trail</description></item>
/// <item><term>8570-8585</term><description>Event-sourced services</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class RetentionLogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (8500-8509)
    // ========================================================================

    /// <summary>Retention pipeline check skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 8500,
        Level = LogLevel.Trace,
        Message = "Retention pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void RetentionPipelineDisabled(this ILogger logger, string requestType);

    /// <summary>Retention pipeline skipped because no [RetentionPeriod] attributes found on response type.</summary>
    [LoggerMessage(
        EventId = 8501,
        Level = LogLevel.Trace,
        Message = "Retention pipeline skipped (no [RetentionPeriod] attributes on response). RequestType={RequestType}, ResponseType={ResponseType}")]
    internal static partial void RetentionPipelineNoAttributes(this ILogger logger, string requestType, string responseType);

    /// <summary>Retention pipeline started creating retention records.</summary>
    [LoggerMessage(
        EventId = 8502,
        Level = LogLevel.Debug,
        Message = "Retention pipeline started. RequestType={RequestType}, ResponseType={ResponseType}, FieldCount={FieldCount}")]
    internal static partial void RetentionPipelineStarted(this ILogger logger, string requestType, string responseType, int fieldCount);

    /// <summary>Retention pipeline completed all retention record creation.</summary>
    [LoggerMessage(
        EventId = 8503,
        Level = LogLevel.Debug,
        Message = "Retention pipeline completed. RequestType={RequestType}, ResponseType={ResponseType}, RecordsCreated={RecordsCreated}")]
    internal static partial void RetentionPipelineCompleted(this ILogger logger, string requestType, string responseType, int recordsCreated);

    /// <summary>Retention pipeline received a handler error and is passing it through.</summary>
    [LoggerMessage(
        EventId = 8504,
        Level = LogLevel.Debug,
        Message = "Retention pipeline passing through handler error. RequestType={RequestType}")]
    internal static partial void RetentionPipelineHandlerError(this ILogger logger, string requestType);

    /// <summary>Retention record creation failed — blocked in Block mode.</summary>
    [LoggerMessage(
        EventId = 8505,
        Level = LogLevel.Warning,
        Message = "Retention record creation blocked. DataCategory={DataCategory}, ResponseType={ResponseType}, ErrorMessage={ErrorMessage}")]
    internal static partial void RetentionRecordCreationBlocked(this ILogger logger, string dataCategory, string responseType, string errorMessage);

    /// <summary>Retention record creation failed — warning in Warn mode.</summary>
    [LoggerMessage(
        EventId = 8506,
        Level = LogLevel.Warning,
        Message = "Retention record creation failed — proceeding in Warn mode. DataCategory={DataCategory}, ResponseType={ResponseType}, ErrorMessage={ErrorMessage}")]
    internal static partial void RetentionRecordCreationWarned(this ILogger logger, string dataCategory, string responseType, string errorMessage);

    /// <summary>Entity ID not found on response type.</summary>
    [LoggerMessage(
        EventId = 8507,
        Level = LogLevel.Warning,
        Message = "Entity ID not found on response type. ResponseType={ResponseType}")]
    internal static partial void RetentionEntityIdNotFound(this ILogger logger, string responseType);

    /// <summary>Exception occurred in the retention pipeline.</summary>
    [LoggerMessage(
        EventId = 8508,
        Level = LogLevel.Error,
        Message = "Retention pipeline error. RequestType={RequestType}, ResponseType={ResponseType}")]
    internal static partial void RetentionPipelineError(this ILogger logger, string requestType, string responseType, Exception exception);

    /// <summary>Retention record created successfully via the pipeline.</summary>
    [LoggerMessage(
        EventId = 8509,
        Level = LogLevel.Debug,
        Message = "Retention record created. EntityId={EntityId}, DataCategory={DataCategory}, ExpiresAtUtc={ExpiresAtUtc}, RetentionPeriod={RetentionPeriod}")]
    internal static partial void RetentionRecordCreated(this ILogger logger, string entityId, string dataCategory, DateTimeOffset expiresAtUtc, TimeSpan retentionPeriod);

    // ========================================================================
    // Enforcement service log messages (8510-8519)
    // ========================================================================

    /// <summary>Retention enforcement service started.</summary>
    [LoggerMessage(
        EventId = 8510,
        Level = LogLevel.Information,
        Message = "Retention enforcement service started. Interval={Interval}")]
    internal static partial void RetentionEnforcementServiceStarted(this ILogger logger, TimeSpan interval);

    /// <summary>Retention enforcement service disabled.</summary>
    [LoggerMessage(
        EventId = 8511,
        Level = LogLevel.Information,
        Message = "Retention enforcement service disabled via configuration")]
    internal static partial void RetentionEnforcementServiceDisabled(this ILogger logger);

    /// <summary>Retention enforcement cycle completed.</summary>
    [LoggerMessage(
        EventId = 8512,
        Level = LogLevel.Information,
        Message = "Retention enforcement cycle completed. RecordsDeleted={RecordsDeleted}, RecordsFailed={RecordsFailed}, RecordsUnderHold={RecordsUnderHold}")]
    internal static partial void RetentionEnforcementCycleCompleted(this ILogger logger, int recordsDeleted, int recordsFailed, int recordsUnderHold);

    /// <summary>Retention enforcement cycle failed.</summary>
    [LoggerMessage(
        EventId = 8513,
        Level = LogLevel.Error,
        Message = "Retention enforcement cycle failed")]
    internal static partial void RetentionEnforcementCycleFailed(this ILogger logger, Exception exception);

    /// <summary>Expiring data check completed.</summary>
    [LoggerMessage(
        EventId = 8514,
        Level = LogLevel.Debug,
        Message = "Expiring data check completed. ExpiringCount={ExpiringCount}, AlertWindow={AlertWindow}")]
    internal static partial void RetentionExpiringDataChecked(this ILogger logger, int expiringCount, TimeSpan alertWindow);

    /// <summary>Failed to publish expiring data notifications.</summary>
    [LoggerMessage(
        EventId = 8515,
        Level = LogLevel.Warning,
        Message = "Failed to publish expiring data notifications")]
    internal static partial void RetentionExpiringNotificationsFailed(this ILogger logger, Exception exception);

    /// <summary>Scheduled enforcement cycle starting.</summary>
    [LoggerMessage(
        EventId = 8516,
        Level = LogLevel.Debug,
        Message = "Starting scheduled retention enforcement cycle")]
    internal static partial void RetentionEnforcementCycleStarting(this ILogger logger);

    /// <summary>Expired records found during enforcement.</summary>
    [LoggerMessage(
        EventId = 8517,
        Level = LogLevel.Information,
        Message = "Expired retention records found. Count={Count}")]
    internal static partial void RetentionExpiredRecordsFound(this ILogger logger, int count);

    /// <summary>No expired records found during enforcement — empty cycle.</summary>
    [LoggerMessage(
        EventId = 8518,
        Level = LogLevel.Information,
        Message = "No expired retention records found. Enforcement cycle complete")]
    internal static partial void RetentionNoExpiredRecords(this ILogger logger);

    /// <summary>IDataErasureExecutor not registered — degraded mode.</summary>
    [LoggerMessage(
        EventId = 8519,
        Level = LogLevel.Warning,
        Message = "IDataErasureExecutor is not registered. Retention enforcement operates in degraded mode (records marked deleted but no physical erasure)")]
    internal static partial void RetentionErasureExecutorMissing(this ILogger logger);

    // ========================================================================
    // Auto-registration log messages (8520-8529)
    // ========================================================================

    /// <summary>Retention auto-registration completed.</summary>
    [LoggerMessage(
        EventId = 8520,
        Level = LogLevel.Information,
        Message = "Retention auto-registration completed. PoliciesCreated={PoliciesCreated}, AssembliesScanned={AssembliesScanned}")]
    internal static partial void RetentionAutoRegistrationCompleted(this ILogger logger, int policiesCreated, int assembliesScanned);

    /// <summary>Retention auto-registration skipped.</summary>
    [LoggerMessage(
        EventId = 8521,
        Level = LogLevel.Debug,
        Message = "Retention auto-registration skipped (no assemblies configured or auto-registration disabled)")]
    internal static partial void RetentionAutoRegistrationSkipped(this ILogger logger);

    /// <summary>Retention policy discovered via [RetentionPeriod] attribute.</summary>
    [LoggerMessage(
        EventId = 8522,
        Level = LogLevel.Debug,
        Message = "Retention policy discovered. EntityType={EntityType}, DataCategory={DataCategory}, RetentionPeriod={RetentionPeriod}")]
    internal static partial void RetentionPolicyDiscovered(this ILogger logger, string entityType, string dataCategory, TimeSpan retentionPeriod);

    /// <summary>Retention policy auto-creation skipped because a policy already exists.</summary>
    [LoggerMessage(
        EventId = 8523,
        Level = LogLevel.Debug,
        Message = "Retention policy already exists for category. DataCategory={DataCategory}, skipping auto-creation")]
    internal static partial void RetentionPolicyAlreadyExists(this ILogger logger, string dataCategory);

    /// <summary>Retention auto-registration failed to create policy.</summary>
    [LoggerMessage(
        EventId = 8524,
        Level = LogLevel.Warning,
        Message = "Failed to auto-register retention policy. DataCategory={DataCategory}")]
    internal static partial void RetentionAutoRegistrationPolicyFailed(this ILogger logger, string dataCategory, Exception exception);

    // ========================================================================
    // Health check log messages (8530-8539)
    // ========================================================================

    /// <summary>Retention health check completed.</summary>
    [LoggerMessage(
        EventId = 8530,
        Level = LogLevel.Debug,
        Message = "Retention health check completed. Status={Status}, StoresVerified={StoresVerified}")]
    internal static partial void RetentionHealthCheckCompleted(this ILogger logger, string status, int storesVerified);

    // ========================================================================
    // Legal hold management log messages (8540-8549)
    // ========================================================================

    /// <summary>Legal hold applied to entity.</summary>
    [LoggerMessage(
        EventId = 8540,
        Level = LogLevel.Information,
        Message = "Legal hold applied. HoldId={HoldId}, EntityId={EntityId}")]
    internal static partial void LegalHoldApplied(this ILogger logger, string holdId, string entityId);

    /// <summary>Legal hold released.</summary>
    [LoggerMessage(
        EventId = 8541,
        Level = LogLevel.Information,
        Message = "Legal hold released. HoldId={HoldId}, ReleasedBy={ReleasedBy}")]
    internal static partial void LegalHoldReleased(this ILogger logger, string holdId, string? releasedBy);

    /// <summary>Entity already has an active legal hold.</summary>
    [LoggerMessage(
        EventId = 8542,
        Level = LogLevel.Warning,
        Message = "Legal hold already active. EntityId={EntityId}")]
    internal static partial void LegalHoldAlreadyActive(this ILogger logger, string entityId);

    /// <summary>Legal hold not found in the store.</summary>
    [LoggerMessage(
        EventId = 8543,
        Level = LogLevel.Warning,
        Message = "Legal hold not found. HoldId={HoldId}")]
    internal static partial void LegalHoldNotFound(this ILogger logger, string holdId);

    /// <summary>Legal hold has already been released.</summary>
    [LoggerMessage(
        EventId = 8544,
        Level = LogLevel.Warning,
        Message = "Legal hold already released. HoldId={HoldId}")]
    internal static partial void LegalHoldAlreadyReleased(this ILogger logger, string holdId);

    /// <summary>Entity still has other active legal holds after release.</summary>
    [LoggerMessage(
        EventId = 8545,
        Level = LogLevel.Debug,
        Message = "Entity still under other legal holds after release. EntityId={EntityId}")]
    internal static partial void LegalHoldOtherHoldsRemain(this ILogger logger, string entityId);

    /// <summary>Cascaded hold status to retention records for an entity.</summary>
    [LoggerMessage(
        EventId = 8546,
        Level = LogLevel.Debug,
        Message = "Cascaded hold status to retention records. EntityId={EntityId}, Status={Status}, RecordCount={RecordCount}")]
    internal static partial void LegalHoldStatusCascaded(this ILogger logger, string entityId, string status, int recordCount);

    /// <summary>Failed to cascade hold status for an entity.</summary>
    [LoggerMessage(
        EventId = 8547,
        Level = LogLevel.Warning,
        Message = "Failed to cascade hold status. EntityId={EntityId}, ErrorMessage={ErrorMessage}")]
    internal static partial void LegalHoldCascadeFailed(this ILogger logger, string entityId, string errorMessage);

    /// <summary>Entity is under legal hold — deletion skipped during enforcement.</summary>
    [LoggerMessage(
        EventId = 8548,
        Level = LogLevel.Debug,
        Message = "Entity under legal hold, skipping deletion. EntityId={EntityId}")]
    internal static partial void RetentionDeletionSkippedLegalHold(this ILogger logger, string entityId);

    /// <summary>Legal hold apply started.</summary>
    [LoggerMessage(
        EventId = 8549,
        Level = LogLevel.Information,
        Message = "Applying legal hold. HoldId={HoldId}, EntityId={EntityId}, Reason={Reason}")]
    internal static partial void LegalHoldApplying(this ILogger logger, string holdId, string entityId, string reason);

    // ========================================================================
    // Retention policy log messages (8550-8559)
    // ========================================================================

    /// <summary>Retention period resolved from an explicit policy.</summary>
    [LoggerMessage(
        EventId = 8550,
        Level = LogLevel.Debug,
        Message = "Retention period resolved from policy. DataCategory={DataCategory}, RetentionDays={RetentionDays}, PolicyId={PolicyId}")]
    internal static partial void RetentionPeriodResolved(this ILogger logger, string dataCategory, double retentionDays, string policyId);

    /// <summary>Retention period resolved from the default configuration.</summary>
    [LoggerMessage(
        EventId = 8551,
        Level = LogLevel.Debug,
        Message = "Retention period resolved from default. DataCategory={DataCategory}, DefaultDays={DefaultDays}")]
    internal static partial void RetentionPeriodResolvedFromDefault(this ILogger logger, string dataCategory, double defaultDays);

    /// <summary>No retention policy defined and no default configured.</summary>
    [LoggerMessage(
        EventId = 8552,
        Level = LogLevel.Warning,
        Message = "No retention policy for category and no default configured. DataCategory={DataCategory}")]
    internal static partial void RetentionNoPolicyForCategory(this ILogger logger, string dataCategory);

    /// <summary>Entity expiration status checked.</summary>
    [LoggerMessage(
        EventId = 8553,
        Level = LogLevel.Debug,
        Message = "Entity expiration check. EntityId={EntityId}, DataCategory={DataCategory}, IsExpired={IsExpired}, ExpiresAtUtc={ExpiresAtUtc}")]
    internal static partial void RetentionExpirationChecked(this ILogger logger, string entityId, string dataCategory, bool isExpired, DateTimeOffset expiresAtUtc);

    /// <summary>No retention record found for entity in the specified category.</summary>
    [LoggerMessage(
        EventId = 8554,
        Level = LogLevel.Warning,
        Message = "No retention record found. EntityId={EntityId}, DataCategory={DataCategory}")]
    internal static partial void RetentionRecordNotFound(this ILogger logger, string entityId, string dataCategory);

    /// <summary>Retention record status recalculated after hold release.</summary>
    [LoggerMessage(
        EventId = 8555,
        Level = LogLevel.Debug,
        Message = "Retention record status recalculated after hold release. RecordId={RecordId}, NewStatus={NewStatus}")]
    internal static partial void RetentionRecordStatusRecalculated(this ILogger logger, string recordId, string newStatus);

    /// <summary>Failed to recalculate record statuses for entity.</summary>
    [LoggerMessage(
        EventId = 8556,
        Level = LogLevel.Warning,
        Message = "Failed to recalculate record statuses. EntityId={EntityId}, ErrorMessage={ErrorMessage}")]
    internal static partial void RetentionRecordRecalculationFailed(this ILogger logger, string entityId, string errorMessage);

    /// <summary>Data erasure failed for entity during enforcement.</summary>
    [LoggerMessage(
        EventId = 8557,
        Level = LogLevel.Warning,
        Message = "Data erasure failed. EntityId={EntityId}, ErrorMessage={ErrorMessage}")]
    internal static partial void RetentionErasureFailed(this ILogger logger, string entityId, string errorMessage);

    /// <summary>Exception during data erasure for entity.</summary>
    [LoggerMessage(
        EventId = 8558,
        Level = LogLevel.Error,
        Message = "Exception during data erasure. EntityId={EntityId}")]
    internal static partial void RetentionErasureException(this ILogger logger, string entityId, Exception exception);

    /// <summary>No erasure executor registered — marking as deleted without physical erasure.</summary>
    [LoggerMessage(
        EventId = 8559,
        Level = LogLevel.Debug,
        Message = "No erasure executor registered, marking as deleted without physical erasure. EntityId={EntityId}")]
    internal static partial void RetentionNoErasureExecutor(this ILogger logger, string entityId);

    // ========================================================================
    // Audit trail log messages (8560-8569)
    // ========================================================================

    /// <summary>Audit entry recorded successfully.</summary>
    [LoggerMessage(
        EventId = 8560,
        Level = LogLevel.Debug,
        Message = "Retention audit entry recorded. Action={Action}, EntityId={EntityId}")]
    internal static partial void RetentionAuditEntryRecorded(this ILogger logger, string action, string? entityId);

    /// <summary>Failed to record an audit entry.</summary>
    [LoggerMessage(
        EventId = 8561,
        Level = LogLevel.Warning,
        Message = "Failed to record retention audit entry. Action={Action}")]
    internal static partial void RetentionAuditEntryFailed(this ILogger logger, string action, Exception exception);

    /// <summary>Failed to publish notification.</summary>
    [LoggerMessage(
        EventId = 8562,
        Level = LogLevel.Warning,
        Message = "Failed to publish retention notification. NotificationType={NotificationType}")]
    internal static partial void RetentionNotificationFailed(this ILogger logger, string notificationType, Exception exception);

    /// <summary>Enforcement cycle cancelled due to service shutdown.</summary>
    [LoggerMessage(
        EventId = 8563,
        Level = LogLevel.Information,
        Message = "Retention enforcement cycle cancelled due to service shutdown")]
    internal static partial void RetentionEnforcementCycleCancelled(this ILogger logger);

    /// <summary>Expiring data found during check.</summary>
    [LoggerMessage(
        EventId = 8564,
        Level = LogLevel.Information,
        Message = "Expiring data found. Count={Count}, AlertDays={AlertDays}")]
    internal static partial void RetentionExpiringDataFound(this ILogger logger, int count, int alertDays);

    /// <summary>Exception while checking for expiring data.</summary>
    [LoggerMessage(
        EventId = 8565,
        Level = LogLevel.Warning,
        Message = "Exception while checking for expiring data. Continuing with enforcement")]
    internal static partial void RetentionExpiringDataCheckFailed(this ILogger logger, Exception exception);

    /// <summary>Failed to retrieve expired records during enforcement.</summary>
    [LoggerMessage(
        EventId = 8566,
        Level = LogLevel.Error,
        Message = "Failed to retrieve expired records. ErrorMessage={ErrorMessage}")]
    internal static partial void RetentionExpiredRecordsRetrievalFailed(this ILogger logger, string errorMessage);

    /// <summary>Failed to check expiring data.</summary>
    [LoggerMessage(
        EventId = 8567,
        Level = LogLevel.Warning,
        Message = "Failed to check for expiring data. ErrorMessage={ErrorMessage}")]
    internal static partial void RetentionExpiringDataCheckError(this ILogger logger, string errorMessage);

    /// <summary>Legal hold release started.</summary>
    [LoggerMessage(
        EventId = 8568,
        Level = LogLevel.Information,
        Message = "Releasing legal hold. HoldId={HoldId}")]
    internal static partial void LegalHoldReleasing(this ILogger logger, string holdId);

    /// <summary>No more active enforcement cycles — empty cycle with no records evaluated.</summary>
    [LoggerMessage(
        EventId = 8569,
        Level = LogLevel.Debug,
        Message = "Scheduled enforcement cycle complete: no expired records found")]
    internal static partial void RetentionEnforcementCycleEmpty(this ILogger logger);

    // ========================================================================
    // Event-sourced service log messages (8570-8585)
    // ========================================================================

    /// <summary>Retention policy created via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8570,
        Level = LogLevel.Information,
        Message = "Retention policy created. PolicyId={PolicyId}, DataCategory={DataCategory}, RetentionPeriod={RetentionPeriod}")]
    internal static partial void RetentionPolicyCreatedES(this ILogger logger, Guid policyId, string dataCategory, TimeSpan retentionPeriod);

    /// <summary>Retention policy updated via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8571,
        Level = LogLevel.Information,
        Message = "Retention policy updated. PolicyId={PolicyId}, NewRetentionPeriod={NewRetentionPeriod}")]
    internal static partial void RetentionPolicyUpdatedES(this ILogger logger, Guid policyId, TimeSpan newRetentionPeriod);

    /// <summary>Retention policy deactivated via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8572,
        Level = LogLevel.Information,
        Message = "Retention policy deactivated. PolicyId={PolicyId}, Reason={Reason}")]
    internal static partial void RetentionPolicyDeactivatedES(this ILogger logger, Guid policyId, string reason);

    /// <summary>Retention record tracked via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8573,
        Level = LogLevel.Information,
        Message = "Retention record tracked. RecordId={RecordId}, EntityId={EntityId}, DataCategory={DataCategory}, ExpiresAtUtc={ExpiresAtUtc}")]
    internal static partial void RetentionRecordTrackedES(this ILogger logger, Guid recordId, string entityId, string dataCategory, DateTimeOffset expiresAtUtc);

    /// <summary>Retention record expired via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8574,
        Level = LogLevel.Information,
        Message = "Retention record expired. RecordId={RecordId}")]
    internal static partial void RetentionRecordExpiredES(this ILogger logger, Guid recordId);

    /// <summary>Retention record placed under legal hold via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8575,
        Level = LogLevel.Information,
        Message = "Retention record held. RecordId={RecordId}, LegalHoldId={LegalHoldId}")]
    internal static partial void RetentionRecordHeldES(this ILogger logger, Guid recordId, Guid legalHoldId);

    /// <summary>Retention record released from legal hold via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8576,
        Level = LogLevel.Information,
        Message = "Retention record released. RecordId={RecordId}")]
    internal static partial void RetentionRecordReleasedES(this ILogger logger, Guid recordId);

    /// <summary>Data deleted for retention record via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8577,
        Level = LogLevel.Information,
        Message = "Data deleted for retention record. RecordId={RecordId}")]
    internal static partial void RetentionDataDeletedES(this ILogger logger, Guid recordId);

    /// <summary>Data anonymized for retention record via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8578,
        Level = LogLevel.Information,
        Message = "Data anonymized for retention record. RecordId={RecordId}")]
    internal static partial void RetentionDataAnonymizedES(this ILogger logger, Guid recordId);

    /// <summary>Invalid state transition attempted on aggregate.</summary>
    [LoggerMessage(
        EventId = 8579,
        Level = LogLevel.Warning,
        Message = "Invalid state transition. AggregateId={AggregateId}, Operation={Operation}")]
    internal static partial void RetentionInvalidStateTransition(this ILogger logger, Guid aggregateId, string operation);

    /// <summary>Service operation failed unexpectedly.</summary>
    [LoggerMessage(
        EventId = 8580,
        Level = LogLevel.Error,
        Message = "Retention service error. Operation={Operation}")]
    internal static partial void RetentionServiceError(this ILogger logger, string operation, Exception exception);

    /// <summary>Cache hit for retention entity.</summary>
    [LoggerMessage(
        EventId = 8581,
        Level = LogLevel.Debug,
        Message = "Retention cache hit. CacheKey={CacheKey}")]
    internal static partial void RetentionCacheHit(this ILogger logger, string cacheKey);

    /// <summary>Legal hold placed via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8582,
        Level = LogLevel.Information,
        Message = "Legal hold placed. HoldId={HoldId}, EntityId={EntityId}, Reason={Reason}")]
    internal static partial void LegalHoldPlacedES(this ILogger logger, Guid holdId, string entityId, string reason);

    /// <summary>Legal hold lifted via event-sourced service.</summary>
    [LoggerMessage(
        EventId = 8583,
        Level = LogLevel.Information,
        Message = "Legal hold lifted. HoldId={HoldId}, ReleasedByUserId={ReleasedByUserId}")]
    internal static partial void LegalHoldLiftedES(this ILogger logger, Guid holdId, string? releasedByUserId);

    /// <summary>Cross-aggregate cascade completed (hold placed/lifted on affected retention records).</summary>
    [LoggerMessage(
        EventId = 8584,
        Level = LogLevel.Information,
        Message = "Cross-aggregate cascade completed. EntityId={EntityId}, Operation={Operation}, AffectedRecords={AffectedRecords}")]
    internal static partial void RetentionCrossAggregateCascade(this ILogger logger, string entityId, string operation, int affectedRecords);

    /// <summary>Cache invalidated for retention entity.</summary>
    [LoggerMessage(
        EventId = 8585,
        Level = LogLevel.Debug,
        Message = "Retention cache invalidated. CacheKey={CacheKey}")]
    internal static partial void RetentionCacheInvalidated(this ILogger logger, string cacheKey);
}
