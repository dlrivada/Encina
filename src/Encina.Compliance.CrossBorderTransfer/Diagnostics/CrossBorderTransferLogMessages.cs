using Microsoft.Extensions.Logging;

namespace Encina.Compliance.CrossBorderTransfer.Diagnostics;

/// <summary>
/// High-performance structured log messages for the cross-border transfer compliance module.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>LoggerMessage.Define</c> to avoid boxing and string formatting overhead
/// in hot paths. All methods are extension methods on <see cref="ILogger"/> for ergonomic use.
/// </para>
/// <para>
/// Event IDs are allocated in the 9000-9059 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8259, BreachNotification uses 8400-8415).
/// </para>
/// </remarks>
internal static class CrossBorderTransferLogMessages
{
    // ========================================================================
    // Pipeline log messages (9000-9009)
    // ========================================================================

    // -- 8500: Transfer validation started --

    private static readonly Action<ILogger, string, string, string, Exception?> TransferValidationStartedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(9000, nameof(TransferValidationStarted)),
            "Transfer validation started. RequestType={RequestType}, Source={Source}, Destination={Destination}");

    internal static void TransferValidationStarted(this ILogger logger, string requestType, string source, string destination)
        => TransferValidationStartedDef(logger, requestType, source, destination, null);

    // -- 8501: Transfer validation completed (allowed) --

    private static readonly Action<ILogger, string, string, string, string, Exception?> TransferValidationAllowedDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Debug,
            new EventId(9001, nameof(TransferValidationAllowed)),
            "Transfer validation completed: ALLOWED. RequestType={RequestType}, Source={Source}, Destination={Destination}, Basis={Basis}");

    internal static void TransferValidationAllowed(this ILogger logger, string requestType, string source, string destination, string basis)
        => TransferValidationAllowedDef(logger, requestType, source, destination, basis, null);

    // -- 8502: Transfer blocked by policy --

    private static readonly Action<ILogger, string, string, string, string, Exception?> TransferBlockedByPolicyDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(9002, nameof(TransferBlockedByPolicy)),
            "Transfer BLOCKED by policy. RequestType={RequestType}, Source={Source}, Destination={Destination}, Reason={Reason}");

    internal static void TransferBlockedByPolicy(this ILogger logger, string requestType, string source, string destination, string reason)
        => TransferBlockedByPolicyDef(logger, requestType, source, destination, reason, null);

    // -- 8503: Transfer allowed by adequacy --

    private static readonly Action<ILogger, string, string, Exception?> TransferAllowedByAdequacyDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(9003, nameof(TransferAllowedByAdequacy)),
            "Transfer allowed: adequacy decision (Art. 45). Source={Source}, Destination={Destination}");

    internal static void TransferAllowedByAdequacy(this ILogger logger, string source, string destination)
        => TransferAllowedByAdequacyDef(logger, source, destination, null);

    // -- 8504: Transfer allowed by SCC --

    private static readonly Action<ILogger, string, string, Exception?> TransferAllowedBySCCDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(9004, nameof(TransferAllowedBySCC)),
            "Transfer resolved via SCC agreement. Source={Source}, Destination={Destination}");

    internal static void TransferAllowedBySCC(this ILogger logger, string source, string destination)
        => TransferAllowedBySCCDef(logger, source, destination, null);

    // -- 8505: Transfer requires TIA --

    private static readonly Action<ILogger, string, string, string, Exception?> TransferRequiresTIADef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(9005, nameof(TransferRequiresTIA)),
            "Transfer resolved via TIA. Source={Source}, Destination={Destination}, Category={Category}");

    internal static void TransferRequiresTIA(this ILogger logger, string source, string destination, string category)
        => TransferRequiresTIADef(logger, source, destination, category, null);

    // -- 8506: Transfer warning (warn-only mode) --

    private static readonly Action<ILogger, string, string, string, string, Exception?> TransferWarnedDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(9006, nameof(TransferWarned)),
            "Transfer WARNING (warn-only mode). RequestType={RequestType}, Source={Source}, Destination={Destination}, Reason={Reason}");

    internal static void TransferWarned(this ILogger logger, string requestType, string source, string destination, string reason)
        => TransferWarnedDef(logger, requestType, source, destination, reason, null);

    // -- 8507: Transfer enforcement disabled --

    private static readonly Action<ILogger, string, Exception?> TransferEnforcementDisabledDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(9007, nameof(TransferEnforcementDisabled)),
            "Cross-border transfer enforcement disabled, skipping validation. RequestType={RequestType}");

    internal static void TransferEnforcementDisabled(this ILogger logger, string requestType)
        => TransferEnforcementDisabledDef(logger, requestType, null);

    // -- 8508: Transfer destination unresolved --

    private static readonly Action<ILogger, string, Exception?> TransferDestinationUnresolvedDef =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(9008, nameof(TransferDestinationUnresolved)),
            "Transfer destination could not be resolved. Set Destination or DestinationProperty on [RequiresCrossBorderTransfer]. RequestType={RequestType}");

    internal static void TransferDestinationUnresolved(this ILogger logger, string requestType)
        => TransferDestinationUnresolvedDef(logger, requestType, null);

    // -- 8509: Transfer validation warning from outcome --

    private static readonly Action<ILogger, string, string, Exception?> TransferOutcomeWarningDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(9009, nameof(TransferOutcomeWarning)),
            "Cross-border transfer warning. RequestType={RequestType}, Warning={Warning}");

    internal static void TransferOutcomeWarning(this ILogger logger, string requestType, string warning)
        => TransferOutcomeWarningDef(logger, requestType, warning, null);

    // ========================================================================
    // TIA service log messages (9010-9019)
    // ========================================================================

    // -- 8510: TIA created --

    private static readonly Action<ILogger, string, string, string, string, Exception?> TIACreatedDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(9010, nameof(TIACreated)),
            "TIA created. TIAId={TIAId}, Source={Source}, Destination={Destination}, Category={Category}");

    internal static void TIACreated(this ILogger logger, string tiaId, string source, string destination, string category)
        => TIACreatedDef(logger, tiaId, source, destination, category, null);

    // -- 8511: TIA risk assessed --

    private static readonly Action<ILogger, string, double, Exception?> TIARiskAssessedDef =
        LoggerMessage.Define<string, double>(
            LogLevel.Information,
            new EventId(9011, nameof(TIARiskAssessed)),
            "TIA risk assessed. TIAId={TIAId}, RiskScore={RiskScore}");

    internal static void TIARiskAssessed(this ILogger logger, string tiaId, double riskScore)
        => TIARiskAssessedDef(logger, tiaId, riskScore, null);

    // -- 8512: TIA completed --

    private static readonly Action<ILogger, string, Exception?> TIACompletedDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(9012, nameof(TIACompleted)),
            "TIA completed (DPO approved). TIAId={TIAId}");

    internal static void TIACompleted(this ILogger logger, string tiaId)
        => TIACompletedDef(logger, tiaId, null);

    // -- 8513: TIA DPO review submitted --

    private static readonly Action<ILogger, string, string, Exception?> TIADPOReviewSubmittedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(9013, nameof(TIADPOReviewSubmitted)),
            "TIA submitted for DPO review. TIAId={TIAId}, SubmittedBy={SubmittedBy}");

    internal static void TIADPOReviewSubmitted(this ILogger logger, string tiaId, string submittedBy)
        => TIADPOReviewSubmittedDef(logger, tiaId, submittedBy, null);

    // -- 8514: TIA supplementary measure added --

    private static readonly Action<ILogger, string, string, string, Exception?> TIASupplementaryMeasureAddedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(9014, nameof(TIASupplementaryMeasureAdded)),
            "Supplementary measure added to TIA. TIAId={TIAId}, MeasureId={MeasureId}, MeasureType={MeasureType}");

    internal static void TIASupplementaryMeasureAdded(this ILogger logger, string tiaId, string measureId, string measureType)
        => TIASupplementaryMeasureAddedDef(logger, tiaId, measureId, measureType, null);

    // -- 8515: TIA store error --

    private static readonly Action<ILogger, string, Exception?> TIAStoreErrorDef =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(9015, nameof(TIAStoreError)),
            "TIA store operation failed. Operation={Operation}");

    internal static void TIAStoreError(this ILogger logger, string operation, Exception? exception = null)
        => TIAStoreErrorDef(logger, operation, exception);

    // -- 8516: TIA invalid state transition --

    private static readonly Action<ILogger, string, string, Exception?> TIAInvalidStateTransitionDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(9016, nameof(TIAInvalidStateTransition)),
            "Invalid TIA state transition. TIAId={TIAId}, Operation={Operation}");

    internal static void TIAInvalidStateTransition(this ILogger logger, string tiaId, string operation, Exception? exception = null)
        => TIAInvalidStateTransitionDef(logger, tiaId, operation, exception);

    // ========================================================================
    // SCC service log messages (9020-9029)
    // ========================================================================

    // -- 8520: SCC agreement registered --

    private static readonly Action<ILogger, string, string, string, Exception?> SCCAgreementRegisteredDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(9020, nameof(SCCAgreementRegistered)),
            "SCC agreement registered. AgreementId={AgreementId}, ProcessorId={ProcessorId}, Module={Module}");

    internal static void SCCAgreementRegistered(this ILogger logger, string agreementId, string processorId, string module)
        => SCCAgreementRegisteredDef(logger, agreementId, processorId, module, null);

    // -- 8521: SCC agreement revoked --

    private static readonly Action<ILogger, string, string, string, Exception?> SCCAgreementRevokedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(9021, nameof(SCCAgreementRevoked)),
            "SCC agreement revoked. AgreementId={AgreementId}, RevokedBy={RevokedBy}, Reason={Reason}");

    internal static void SCCAgreementRevoked(this ILogger logger, string agreementId, string revokedBy, string reason)
        => SCCAgreementRevokedDef(logger, agreementId, revokedBy, reason, null);

    // -- 8522: SCC supplementary measure added --

    private static readonly Action<ILogger, string, string, string, Exception?> SCCSupplementaryMeasureAddedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(9022, nameof(SCCSupplementaryMeasureAdded)),
            "Supplementary measure added to SCC agreement. AgreementId={AgreementId}, MeasureId={MeasureId}, MeasureType={MeasureType}");

    internal static void SCCSupplementaryMeasureAdded(this ILogger logger, string agreementId, string measureId, string measureType)
        => SCCSupplementaryMeasureAddedDef(logger, agreementId, measureId, measureType, null);

    // -- 8523: SCC store error --

    private static readonly Action<ILogger, string, Exception?> SCCStoreErrorDef =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(9023, nameof(SCCStoreError)),
            "SCC store operation failed. Operation={Operation}");

    internal static void SCCStoreError(this ILogger logger, string operation, Exception? exception = null)
        => SCCStoreErrorDef(logger, operation, exception);

    // ========================================================================
    // Approved transfer service log messages (9030-9039)
    // ========================================================================

    // -- 8530: Approved transfer created --

    private static readonly Action<ILogger, string, string, string, string, Exception?> ApprovedTransferCreatedDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(9030, nameof(ApprovedTransferCreated)),
            "Transfer approved. TransferId={TransferId}, Source={Source}, Destination={Destination}, Basis={Basis}");

    internal static void ApprovedTransferCreated(this ILogger logger, string transferId, string source, string destination, string basis)
        => ApprovedTransferCreatedDef(logger, transferId, source, destination, basis, null);

    // -- 8531: Approved transfer revoked --

    private static readonly Action<ILogger, string, string, string, Exception?> ApprovedTransferRevokedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(9031, nameof(ApprovedTransferRevoked)),
            "Transfer revoked. TransferId={TransferId}, RevokedBy={RevokedBy}, Reason={Reason}");

    internal static void ApprovedTransferRevoked(this ILogger logger, string transferId, string revokedBy, string reason)
        => ApprovedTransferRevokedDef(logger, transferId, revokedBy, reason, null);

    // -- 8532: Approved transfer renewed --

    private static readonly Action<ILogger, string, string, string, Exception?> ApprovedTransferRenewedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(9032, nameof(ApprovedTransferRenewed)),
            "Transfer renewed. TransferId={TransferId}, NewExpiry={NewExpiry}, RenewedBy={RenewedBy}");

    internal static void ApprovedTransferRenewed(this ILogger logger, string transferId, string newExpiry, string renewedBy)
        => ApprovedTransferRenewedDef(logger, transferId, newExpiry, renewedBy, null);

    // -- 8533: Transfer store error --

    private static readonly Action<ILogger, string, Exception?> TransferStoreErrorDef =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(9033, nameof(TransferStoreError)),
            "Transfer store operation failed. Operation={Operation}");

    internal static void TransferStoreError(this ILogger logger, string operation, Exception? exception = null)
        => TransferStoreErrorDef(logger, operation, exception);

    // ========================================================================
    // Cache log messages (9040-9045)
    // ========================================================================

    // -- 8540: Cache hit --

    private static readonly Action<ILogger, string, string, Exception?> CacheHitDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(9040, nameof(CacheHit)),
            "Cache hit. CacheKey={CacheKey}, EntityType={EntityType}");

    internal static void CacheHit(this ILogger logger, string cacheKey, string entityType)
        => CacheHitDef(logger, cacheKey, entityType, null);

    // -- 8541: Cache miss --

    private static readonly Action<ILogger, string, string, Exception?> CacheMissDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(9041, nameof(CacheMiss)),
            "Cache miss. CacheKey={CacheKey}, EntityType={EntityType}");

    internal static void CacheMiss(this ILogger logger, string cacheKey, string entityType)
        => CacheMissDef(logger, cacheKey, entityType, null);

    // -- 8542: Cache invalidated --

    private static readonly Action<ILogger, string, Exception?> CacheInvalidatedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(9042, nameof(CacheInvalidated)),
            "Cache invalidated. CacheKey={CacheKey}");

    internal static void CacheInvalidated(this ILogger logger, string cacheKey)
        => CacheInvalidatedDef(logger, cacheKey, null);

    // ========================================================================
    // Validator log messages (9045-9049)
    // ========================================================================

    // -- 8545: Validation chain started --

    private static readonly Action<ILogger, string, string, string, Exception?> ValidationChainStartedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(9045, nameof(ValidationChainStarted)),
            "Transfer validation chain started. Source={Source}, Destination={Destination}, Category={Category}");

    internal static void ValidationChainStarted(this ILogger logger, string source, string destination, string category)
        => ValidationChainStartedDef(logger, source, destination, category, null);

    // -- 8546: Validation chain completed (blocked) --

    private static readonly Action<ILogger, string, string, Exception?> ValidationChainBlockedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(9046, nameof(ValidationChainBlocked)),
            "Transfer validation chain completed: BLOCKED. Source={Source}, Destination={Destination}");

    internal static void ValidationChainBlocked(this ILogger logger, string source, string destination)
        => ValidationChainBlockedDef(logger, source, destination, null);

    // ========================================================================
    // Health check log messages (9047-9049)
    // ========================================================================

    // -- 8547: Health check completed --

    private static readonly Action<ILogger, string, string, Exception?> HealthCheckCompletedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(9047, nameof(HealthCheckCompleted)),
            "Cross-border transfer health check completed. Status={Status}, EnforcementMode={EnforcementMode}");

    internal static void HealthCheckCompleted(this ILogger logger, string status, string enforcementMode)
        => HealthCheckCompletedDef(logger, status, enforcementMode, null);

    // ========================================================================
    // Expiration monitor log messages (9050-9059)
    // ========================================================================

    // -- 8550: Expiration monitor started --

    private static readonly Action<ILogger, string, Exception?> ExpirationMonitorStartedDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(9050, nameof(ExpirationMonitorStarted)),
            "Transfer expiration monitor started with check interval {Interval}");

    internal static void ExpirationMonitorStarted(this ILogger logger, string interval)
        => ExpirationMonitorStartedDef(logger, interval, null);

    // -- 8551: Expiration monitor cycle completed --

    private static readonly Action<ILogger, int, int, Exception?> ExpirationMonitorCycleCompletedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(9051, nameof(ExpirationMonitorCycleCompleted)),
            "Expiration monitor cycle completed. ExpiringCount={ExpiringCount}, ExpiredCount={ExpiredCount}");

    internal static void ExpirationMonitorCycleCompleted(this ILogger logger, int expiringCount, int expiredCount)
        => ExpirationMonitorCycleCompletedDef(logger, expiringCount, expiredCount, null);

    // -- 8552: Expiration monitor cycle error --

    private static readonly Action<ILogger, Exception?> ExpirationMonitorCycleErrorDef =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(9052, nameof(ExpirationMonitorCycleError)),
            "Unhandled exception during transfer expiration monitoring cycle");

    internal static void ExpirationMonitorCycleError(this ILogger logger, Exception? exception = null)
        => ExpirationMonitorCycleErrorDef(logger, exception);

    // -- 8553: Expiration monitor cycle cancelled --

    private static readonly Action<ILogger, Exception?> ExpirationMonitorCycleCancelledDef =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(9053, nameof(ExpirationMonitorCycleCancelled)),
            "Transfer expiration monitoring cycle cancelled");

    internal static void ExpirationMonitorCycleCancelled(this ILogger logger)
        => ExpirationMonitorCycleCancelledDef(logger, null);

    // -- 8554: Transfer expiring soon --

    private static readonly Action<ILogger, string, string, string, int, Exception?> TransferExpiringSoonDef =
        LoggerMessage.Define<string, string, string, int>(
            LogLevel.Warning,
            new EventId(9054, nameof(TransferExpiringSoon)),
            "Approved transfer expiring soon. TransferId={TransferId}, Source={Source}, Destination={Destination}, DaysRemaining={DaysRemaining}");

    internal static void TransferExpiringSoon(this ILogger logger, string transferId, string source, string destination, int daysRemaining)
        => TransferExpiringSoonDef(logger, transferId, source, destination, daysRemaining, null);

    // -- 8555: Transfer expired --

    private static readonly Action<ILogger, string, string, string, Exception?> TransferExpiredDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(9055, nameof(TransferExpired)),
            "Approved transfer has expired. TransferId={TransferId}, Source={Source}, Destination={Destination}");

    internal static void TransferExpired(this ILogger logger, string transferId, string source, string destination)
        => TransferExpiredDef(logger, transferId, source, destination, null);
}
