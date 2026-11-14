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

    private static readonly Action<ILogger, string, Exception?> ConsentCheckStartedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8200, nameof(ConsentCheckStarted)),
            "Consent check started. RequestType={RequestType}");

    // Note: none of these templates carry the data subject's own identifier — it is personal data
    // and must never reach a log sink in plain text (#1314). Correlate via request type, purpose,
    // consent id or error code instead.
    internal static void ConsentCheckStarted(this ILogger logger, string requestType)
        => ConsentCheckStartedDef(logger, requestType, null);

    // -- 8201: Consent check passed --

    private static readonly Action<ILogger, string, Exception?> ConsentCheckPassedDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(8201, nameof(ConsentCheckPassed)),
            "Consent check passed. RequestType={RequestType}");

    internal static void ConsentCheckPassed(this ILogger logger, string requestType)
        => ConsentCheckPassedDef(logger, requestType, null);

    // -- 8202: Consent check failed --

    private static readonly Action<ILogger, string, string, Exception?> ConsentCheckFailedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8202, nameof(ConsentCheckFailed)),
            "Consent check failed. RequestType={RequestType}, Reason={Reason}");

    internal static void ConsentCheckFailed(this ILogger logger, string requestType, string reason)
        => ConsentCheckFailedDef(logger, requestType, reason, null);

    // -- 8203: Consent missing --

    private static readonly Action<ILogger, string, string, Exception?> ConsentMissingDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8203, nameof(ConsentMissing)),
            "Consent missing for required purpose. Purpose={Purpose}, RequestType={RequestType}");

    internal static void ConsentMissing(this ILogger logger, string purpose, string requestType)
        => ConsentMissingDef(logger, purpose, requestType, null);

    // -- 8204: Consent expired --

    private static readonly Action<ILogger, string, string, Exception?> ConsentExpiredDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8204, nameof(ConsentExpired)),
            "Consent expired for purpose. Purpose={Purpose}, RequestType={RequestType}");

    internal static void ConsentExpired(this ILogger logger, string purpose, string requestType)
        => ConsentExpiredDef(logger, purpose, requestType, null);

    // -- 8205: Consent check skipped (no attribute) --

    private static readonly Action<ILogger, string, Exception?> ConsentCheckSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            new EventId(8205, nameof(ConsentCheckSkipped)),
            "Consent check skipped (no [RequireConsent] attribute). RequestType={RequestType}");

    internal static void ConsentCheckSkipped(this ILogger logger, string requestType)
        => ConsentCheckSkippedDef(logger, requestType, null);

    // -- 8206: Consent warning (warn-only mode) --

    private static readonly Action<ILogger, string, string, Exception?> ConsentWarningDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8206, nameof(ConsentWarning)),
            "Consent issue (warn-only mode). RequestType={RequestType}, Warning={Warning}");

    internal static void ConsentWarning(this ILogger logger, string requestType, string warning)
        => ConsentWarningDef(logger, requestType, warning, null);

    // -- 8207: Consent enforcement disabled --

    private static readonly Action<ILogger, string, Exception?> ConsentEnforcementDisabledDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8207, nameof(ConsentEnforcementDisabled)),
            "Consent enforcement disabled, skipping validation. RequestType={RequestType}");

    internal static void ConsentEnforcementDisabled(this ILogger logger, string requestType)
        => ConsentEnforcementDisabledDef(logger, requestType, null);

    // ========================================================================
    // Domain event log messages (8230-8239)
    // ========================================================================

    // -- 8230: Consent event published --

    private static readonly Action<ILogger, string, Exception?> ConsentEventPublishedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8230, nameof(ConsentEventPublished)),
            "Consent domain event published. EventType={EventType}");

    internal static void ConsentEventPublished(this ILogger logger, string eventType)
        => ConsentEventPublishedDef(logger, eventType, null);

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
    // Service-level log messages (8260-8279)
    // Covers ConsentService aggregate operations (grant, withdraw, renew, etc.)
    // ========================================================================

    // -- 8260: Consent granted (service) --

    // The consent id is a hash of the tenant, the data subject and the purpose (#3769), so it is
    // not logged at Information (#3771).
    private static readonly Action<ILogger, string, Exception?> ConsentGrantedServiceDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(8260, nameof(ConsentGrantedService)),
            "Consent granted. Purpose={Purpose}");

    internal static void ConsentGrantedService(this ILogger logger, string purpose)
        => ConsentGrantedServiceDef(logger, purpose, null);

    // -- 8261: Consent withdrawn (service) --

    // 8261-8263 and 8265 do not log the consent id either: it is a hash of the tenant, the data
    // subject and the purpose (#3769), never logged at Information or above (#3771).
    private static readonly Action<ILogger, Exception?> ConsentWithdrawnServiceDef =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(8261, nameof(ConsentWithdrawnService)),
            "Consent withdrawn via service.");

    internal static void ConsentWithdrawnService(this ILogger logger)
        => ConsentWithdrawnServiceDef(logger, null);

    // -- 8262: Consent renewed (service) --

    private static readonly Action<ILogger, string, Exception?> ConsentRenewedServiceDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(8262, nameof(ConsentRenewedService)),
            "Consent renewed. NewVersionId={NewVersionId}");

    internal static void ConsentRenewedService(this ILogger logger, string newVersionId)
        => ConsentRenewedServiceDef(logger, newVersionId, null);

    // -- 8263: Reconsent provided (service) --

    private static readonly Action<ILogger, string, Exception?> ReconsentProvidedServiceDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(8263, nameof(ReconsentProvidedService)),
            "Reconsent provided. NewVersionId={NewVersionId}");

    internal static void ReconsentProvidedService(this ILogger logger, string newVersionId)
        => ReconsentProvidedServiceDef(logger, newVersionId, null);

    // -- 8264: Consent service error --

    private static readonly Action<ILogger, string, Exception?> ConsentServiceErrorDef =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(8264, nameof(ConsentServiceError)),
            "Consent service operation failed. Operation={Operation}");

    internal static void ConsentServiceError(this ILogger logger, string operation, Exception? exception = null)
        => ConsentServiceErrorDef(logger, operation, exception);

    // -- 8265: Consent invalid state transition (service) --

    private static readonly Action<ILogger, string, Exception?> ConsentInvalidStateTransitionDef =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(8265, nameof(ConsentInvalidStateTransition)),
            "Invalid consent state transition. Operation={Operation}");

    internal static void ConsentInvalidStateTransition(this ILogger logger, string operation, Exception? exception = null)
        => ConsentInvalidStateTransitionDef(logger, operation, exception);

    // -- 8266: Cache hit (consent service) --

    private static readonly Action<ILogger, string, string, Exception?> ConsentCacheHitDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8266, nameof(ConsentCacheHit)),
            "Consent cache hit. CacheKey={CacheKey}, EntityType={EntityType}");

    internal static void ConsentCacheHit(this ILogger logger, string cacheKey, string entityType)
        => ConsentCacheHitDef(logger, cacheKey, entityType, null);

    // -- 8267: Consent query rejected — no tenant in the ambient request context --

    private static readonly Action<ILogger, string, Exception?> ConsentTenantContextMissingDef =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(8267, nameof(ConsentTenantContextMissing)),
            "Consent query rejected: no tenant in the ambient request context. Operation={Operation}");

    /// <summary>
    /// Logs that a consent query was refused because tenant isolation is required (see
    /// <c>ConsentOptions.RequireTenantContext</c>) and no tenant is present in the ambient
    /// request context (#1315).
    /// </summary>
    internal static void ConsentTenantContextMissing(this ILogger logger, string operation)
        => ConsentTenantContextMissingDef(logger, operation, null);

    // -- 8268: Consent tenant enforcement explicitly opted out in a multi-tenant application --

    private static readonly Action<ILogger, Exception?> ConsentTenantEnforcementOptedOutDef =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(8268, nameof(ConsentTenantEnforcementOptedOut)),
            "Consent tenant context enforcement explicitly disabled (RequireTenantContext=false) "
                + "while Encina.Tenancy is registered. A missing ambient tenant will run an unscoped "
                + "consent query instead of failing closed.");

    /// <summary>
    /// Logs, once at startup, that a multi-tenant application explicitly opted out of consent
    /// tenant-context enforcement (<c>ConsentOptions.RequireTenantContext = false</c>) so the
    /// opt-out is never silent (#1315, SPEC-002 DEC-006).
    /// </summary>
    internal static void ConsentTenantEnforcementOptedOut(this ILogger logger)
        => ConsentTenantEnforcementOptedOutDef(logger, null);

    // 8269-8274 are reserved by the open consent plans (#811, #810, #1199, #1255).

    // -- 8275: Grant on an existing consent recorded --
    // The consent id is a hash of the tenant, the data subject and the purpose (#3769), so it is
    // never logged at Information or above (#3771); correlate via purpose and transition.

    private static readonly Action<ILogger, string, string, bool, Exception?> ConsentGrantOnExistingRecordedDef =
        LoggerMessage.Define<string, string, bool>(
            LogLevel.Information,
            new EventId(8275, nameof(ConsentGrantOnExistingRecorded)),
            "Grant on an existing consent recorded. Purpose={Purpose}, Transition={Transition}, AppliedToState={AppliedToState}");

    /// <summary>
    /// Logs that a grant on an existing consent was recorded as a renewal, a regrant or a
    /// reconsent, and whether it changed the current state or was a late act kept only in the
    /// history (#3771).
    /// </summary>
    internal static void ConsentGrantOnExistingRecorded(this ILogger logger, string purpose, string transition, bool appliedToState)
        => ConsentGrantOnExistingRecordedDef(logger, purpose, transition, appliedToState, null);

    // -- 8276: Grant rejected — the consent requires reconsent to a newer version --

    private static readonly Action<ILogger, string, Exception?> ConsentGrantReconsentRequiredDef =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(8276, nameof(ConsentGrantReconsentRequired)),
            "Grant rejected: the consent requires reconsent and the grant carries an older consent version. Purpose={Purpose}");

    /// <summary>
    /// Logs that a grant on a consent in RequiresReconsent was rejected because it did not carry the
    /// current consent version (#3771).
    /// </summary>
    internal static void ConsentGrantReconsentRequired(this ILogger logger, string purpose)
        => ConsentGrantReconsentRequiredDef(logger, purpose, null);

    // -- 8277: Grant on an existing consent hit a concurrency conflict; retrying --

    private static readonly Action<ILogger, string, Exception?> ConsentGrantConcurrencyRetryDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8277, nameof(ConsentGrantConcurrencyRetry)),
            "Grant on an existing consent hit a concurrency conflict; reloading and retrying. Purpose={Purpose}");

    /// <summary>
    /// Logs that a grant on an existing consent lost an optimistic concurrency race and is retried
    /// on the reloaded stream (#3771).
    /// </summary>
    internal static void ConsentGrantConcurrencyRetry(this ILogger logger, string purpose)
        => ConsentGrantConcurrencyRetryDef(logger, purpose, null);

    // -- 8278: Grant rejected — the act timestamp is in the future --

    private static readonly Action<ILogger, string, Exception?> ConsentGrantActInFutureDef =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(8278, nameof(ConsentGrantActInFuture)),
            "Grant rejected: the act timestamp is further in the future than the allowed clock drift. Purpose={Purpose}");

    /// <summary>
    /// Logs that a grant was rejected because its act timestamp is beyond the allowed clock drift
    /// in the future (#3771).
    /// </summary>
    internal static void ConsentGrantActInFuture(this ILogger logger, string purpose)
        => ConsentGrantActInFutureDef(logger, purpose, null);

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
