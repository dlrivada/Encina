using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Consent.Diagnostics;

/// <summary>
/// High-performance structured log messages for the consent compliance pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>LoggerMessage.Define</c> to avoid boxing and string formatting overhead
/// in hot paths. All methods are extension methods on <see cref="ILogger"/> for ergonomic use.
/// </para>
/// <para>
/// Event IDs are allocated in the 8200-8299 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Security uses 8000-8099).
/// </para>
/// </remarks>
internal static class ConsentLogMessages
{
    // -- 8200: Consent check started --

    private static readonly Action<ILogger, string, string, Exception?> ConsentCheckStartedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8200, nameof(ConsentCheckStarted)),
            "Consent check started. RequestType={RequestType}, SubjectId={SubjectId}");

    internal static void ConsentCheckStarted(this ILogger logger, string requestType, string subjectId)
        => ConsentCheckStartedDef(logger, requestType, subjectId, null);

    // -- 8201: Consent check passed --

    private static readonly Action<ILogger, string, string, Exception?> ConsentCheckPassedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(8201, nameof(ConsentCheckPassed)),
            "Consent check passed. RequestType={RequestType}, SubjectId={SubjectId}");

    internal static void ConsentCheckPassed(this ILogger logger, string requestType, string subjectId)
        => ConsentCheckPassedDef(logger, requestType, subjectId, null);

    // -- 8202: Consent check failed --

    private static readonly Action<ILogger, string, string, string, Exception?> ConsentCheckFailedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(8202, nameof(ConsentCheckFailed)),
            "Consent check failed. RequestType={RequestType}, SubjectId={SubjectId}, Reason={Reason}");

    internal static void ConsentCheckFailed(this ILogger logger, string requestType, string subjectId, string reason)
        => ConsentCheckFailedDef(logger, requestType, subjectId, reason, null);

    // -- 8203: Consent missing --

    private static readonly Action<ILogger, string, string, string, Exception?> ConsentMissingDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(8203, nameof(ConsentMissing)),
            "Consent missing for required purpose. SubjectId={SubjectId}, Purpose={Purpose}, RequestType={RequestType}");

    internal static void ConsentMissing(this ILogger logger, string subjectId, string purpose, string requestType)
        => ConsentMissingDef(logger, subjectId, purpose, requestType, null);

    // -- 8204: Consent expired --

    private static readonly Action<ILogger, string, string, string, Exception?> ConsentExpiredDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(8204, nameof(ConsentExpired)),
            "Consent expired for purpose. SubjectId={SubjectId}, Purpose={Purpose}, RequestType={RequestType}");

    internal static void ConsentExpired(this ILogger logger, string subjectId, string purpose, string requestType)
        => ConsentExpiredDef(logger, subjectId, purpose, requestType, null);

    // -- 8205: Consent check skipped (no attribute) --

    private static readonly Action<ILogger, string, Exception?> ConsentCheckSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            new EventId(8205, nameof(ConsentCheckSkipped)),
            "Consent check skipped (no [RequireConsent] attribute). RequestType={RequestType}");

    internal static void ConsentCheckSkipped(this ILogger logger, string requestType)
        => ConsentCheckSkippedDef(logger, requestType, null);

    // -- 8206: Consent warning (warn-only mode) --

    private static readonly Action<ILogger, string, string, string, Exception?> ConsentWarningDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(8206, nameof(ConsentWarning)),
            "Consent issue (warn-only mode). RequestType={RequestType}, SubjectId={SubjectId}, Warning={Warning}");

    internal static void ConsentWarning(this ILogger logger, string requestType, string subjectId, string warning)
        => ConsentWarningDef(logger, requestType, subjectId, warning, null);

    // -- 8207: Consent enforcement disabled --

    private static readonly Action<ILogger, string, Exception?> ConsentEnforcementDisabledDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8207, nameof(ConsentEnforcementDisabled)),
            "Consent enforcement disabled, skipping validation. RequestType={RequestType}");

    internal static void ConsentEnforcementDisabled(this ILogger logger, string requestType)
        => ConsentEnforcementDisabledDef(logger, requestType, null);

    // ========================================================================
    // Store-level log messages (8210-8219)
    // ========================================================================

    // -- 8210: Consent recorded --

    private static readonly Action<ILogger, string, string, Exception?> ConsentRecordedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8210, nameof(ConsentRecorded)),
            "Consent recorded. SubjectId={SubjectId}, Purpose={Purpose}");

    internal static void ConsentRecorded(this ILogger logger, string subjectId, string purpose)
        => ConsentRecordedDef(logger, subjectId, purpose, null);

    // -- 8211: Consent withdrawn (store) --

    private static readonly Action<ILogger, string, string, Exception?> ConsentWithdrawnDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8211, nameof(ConsentWithdrawn)),
            "Consent withdrawn. SubjectId={SubjectId}, Purpose={Purpose}");

    internal static void ConsentWithdrawn(this ILogger logger, string subjectId, string purpose)
        => ConsentWithdrawnDef(logger, subjectId, purpose, null);

    // -- 8212: Consent not found --

    private static readonly Action<ILogger, string, string, Exception?> ConsentNotFoundDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8212, nameof(ConsentNotFound)),
            "No consent record found. SubjectId={SubjectId}, Purpose={Purpose}");

    internal static void ConsentNotFound(this ILogger logger, string subjectId, string purpose)
        => ConsentNotFoundDef(logger, subjectId, purpose, null);

    // -- 8213: Consent fetched --

    private static readonly Action<ILogger, string, string, string, Exception?> ConsentFetchedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(8213, nameof(ConsentFetched)),
            "Consent fetched. SubjectId={SubjectId}, Purpose={Purpose}, Status={Status}");

    internal static void ConsentFetched(this ILogger logger, string subjectId, string purpose, string status)
        => ConsentFetchedDef(logger, subjectId, purpose, status, null);

    // -- 8214: Consent expired detected by store --

    private static readonly Action<ILogger, string, string, Exception?> ConsentExpiredDetectedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8214, nameof(ConsentExpiredDetected)),
            "Consent expiration detected. SubjectId={SubjectId}, Purpose={Purpose}");

    internal static void ConsentExpiredDetected(this ILogger logger, string subjectId, string purpose)
        => ConsentExpiredDetectedDef(logger, subjectId, purpose, null);

    // -- 8215: Bulk consent recorded --

    private static readonly Action<ILogger, int, int, Exception?> BulkConsentRecordedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(8215, nameof(BulkConsentRecorded)),
            "Bulk consent record completed. SuccessCount={SuccessCount}, FailureCount={FailureCount}");

    internal static void BulkConsentRecorded(this ILogger logger, int successCount, int failureCount)
        => BulkConsentRecordedDef(logger, successCount, failureCount, null);

    // -- 8216: Bulk consent withdrawn --

    private static readonly Action<ILogger, string, int, int, Exception?> BulkConsentWithdrawnDef =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Debug,
            new EventId(8216, nameof(BulkConsentWithdrawn)),
            "Bulk consent withdrawal completed. SubjectId={SubjectId}, SuccessCount={SuccessCount}, FailureCount={FailureCount}");

    internal static void BulkConsentWithdrawn(this ILogger logger, string subjectId, int successCount, int failureCount)
        => BulkConsentWithdrawnDef(logger, subjectId, successCount, failureCount, null);

    // ========================================================================
    // Audit store log messages (8220-8225)
    // ========================================================================

    // -- 8220: Audit entry recorded --

    private static readonly Action<ILogger, string, string, string, Exception?> AuditEntryRecordedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(8220, nameof(AuditEntryRecorded)),
            "Consent audit entry recorded. SubjectId={SubjectId}, Action={Action}, Purpose={Purpose}");

    internal static void AuditEntryRecorded(this ILogger logger, string subjectId, string action, string purpose)
        => AuditEntryRecordedDef(logger, subjectId, action, purpose, null);

    // -- 8221: Audit trail fetched --

    private static readonly Action<ILogger, string, int, Exception?> AuditTrailFetchedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8221, nameof(AuditTrailFetched)),
            "Consent audit trail fetched. SubjectId={SubjectId}, EntryCount={EntryCount}");

    internal static void AuditTrailFetched(this ILogger logger, string subjectId, int entryCount)
        => AuditTrailFetchedDef(logger, subjectId, entryCount, null);

    // ========================================================================
    // Domain event log messages (8230-8239)
    // ========================================================================

    // -- 8230: Consent event published --

    private static readonly Action<ILogger, string, string, Exception?> ConsentEventPublishedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8230, nameof(ConsentEventPublished)),
            "Consent domain event published. EventType={EventType}, SubjectId={SubjectId}");

    internal static void ConsentEventPublished(this ILogger logger, string eventType, string subjectId)
        => ConsentEventPublishedDef(logger, eventType, subjectId, null);

    // -- 8231: Consent event publish failed --

    private static readonly Action<ILogger, string, string, Exception?> ConsentEventPublishFailedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8231, nameof(ConsentEventPublishFailed)),
            "Failed to publish consent domain event. EventType={EventType}, ErrorMessage={ErrorMessage}");

    internal static void ConsentEventPublishFailed(this ILogger logger, string eventType, string errorMessage)
        => ConsentEventPublishFailedDef(logger, eventType, errorMessage, null);

    // -- 8232: Consent version event published --

    private static readonly Action<ILogger, string, string, Exception?> ConsentVersionEventPublishedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8232, nameof(ConsentVersionEventPublished)),
            "Consent version domain event published. EventType={EventType}, Purpose={Purpose}");

    internal static void ConsentVersionEventPublished(this ILogger logger, string eventType, string purpose)
        => ConsentVersionEventPublishedDef(logger, eventType, purpose, null);

    // ========================================================================
    // Auto-registration log messages (8240-8249)
    // ========================================================================

    // -- 8240: Consent auto-registration completed --

    private static readonly Action<ILogger, int, int, Exception?> ConsentAutoRegistrationCompletedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(8240, nameof(ConsentAutoRegistrationCompleted)),
            "Consent auto-registration completed. PurposesDiscovered={PurposesDiscovered}, AssembliesScanned={AssembliesScanned}");

    internal static void ConsentAutoRegistrationCompleted(this ILogger logger, int purposesDiscovered, int assembliesScanned)
        => ConsentAutoRegistrationCompletedDef(logger, purposesDiscovered, assembliesScanned, null);

    // -- 8241: Unknown consent purpose detected --

    private static readonly Action<ILogger, string, string, Exception?> UnknownConsentPurposeDetectedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8241, nameof(UnknownConsentPurposeDetected)),
            "Unknown consent purpose detected in [RequireConsent] attribute. Purpose={Purpose}, RequestType={RequestType}");

    internal static void UnknownConsentPurposeDetected(this ILogger logger, string purpose, string requestType)
        => UnknownConsentPurposeDetectedDef(logger, purpose, requestType, null);

    // -- 8242: Consent auto-registration failed (strict mode) --

    private static readonly Action<ILogger, int, Exception?> ConsentAutoRegistrationFailedDef =
        LoggerMessage.Define<int>(
            LogLevel.Error,
            new EventId(8242, nameof(ConsentAutoRegistrationFailed)),
            "Consent auto-registration failed due to unknown purposes (FailOnUnknownPurpose=true). UnknownPurposeCount={UnknownPurposeCount}");

    internal static void ConsentAutoRegistrationFailed(this ILogger logger, int unknownPurposeCount)
        => ConsentAutoRegistrationFailedDef(logger, unknownPurposeCount, null);

    // -- 8243: Consent auto-registration skipped --

    private static readonly Action<ILogger, Exception?> ConsentAutoRegistrationSkippedDef =
        LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(8243, nameof(ConsentAutoRegistrationSkipped)),
            "Consent auto-registration skipped (no assemblies configured or auto-registration disabled)");

    internal static void ConsentAutoRegistrationSkipped(this ILogger logger)
        => ConsentAutoRegistrationSkippedDef(logger, null);

    // ========================================================================
    // Health check log messages (8250-8259)
    // ========================================================================

    // -- 8250: Consent health check completed --

    private static readonly Action<ILogger, string, int, Exception?> ConsentHealthCheckCompletedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8250, nameof(ConsentHealthCheckCompleted)),
            "Consent health check completed. Status={Status}, PurposeCount={PurposeCount}");

    internal static void ConsentHealthCheckCompleted(this ILogger logger, string status, int purposeCount)
        => ConsentHealthCheckCompletedDef(logger, status, purposeCount, null);
}
