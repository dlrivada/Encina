using Microsoft.Extensions.Logging;

namespace Encina.Security.ABAC.Diagnostics;

/// <summary>
/// High-performance structured log messages for ABAC operations using
/// <see cref="LoggerMessageAttribute"/>-generated methods.
/// </summary>
/// <remarks>
/// Uses compile-time source generation for zero-allocation logging when the
/// log level is not enabled. Each method corresponds to a specific ABAC event.
/// EventIds are in the 9000-9099 range reserved for ABAC diagnostics.
/// </remarks>
internal static partial class ABACLogMessages
{
    // ── Pipeline Messages (9000-9009) ───────────────────────────────

    [LoggerMessage(
        EventId = 9000,
        Level = LogLevel.Debug,
        Message = "ABAC evaluation starting for {RequestType} ({PolicyCount} policy, {ConditionCount} condition attributes)")]
    internal static partial void EvaluationStarting(
        ILogger logger, string requestType, int policyCount, int conditionCount);

    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Debug,
        Message = "PDP decision for {RequestType}: {Effect} (policy: {PolicyId}, duration: {DurationMs:F2}ms)")]
    internal static partial void PdpDecisionReceived(
        ILogger logger, string requestType, string effect, string? policyId, double durationMs);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Debug,
        Message = "ABAC: Permit for {RequestType}")]
    internal static partial void EvaluationPermitted(
        ILogger logger, string requestType);

    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Debug,
        Message = "ABAC enforcement: denied {RequestType}")]
    internal static partial void EnforcementDenied(
        ILogger logger, string requestType);

    [LoggerMessage(
        EventId = 9004,
        Level = LogLevel.Warning,
        Message = "ABAC enforcement in Warn mode - would deny {RequestType}: {ErrorCode}. Allowing request to proceed")]
    internal static partial void EnforcementWarnMode(
        ILogger logger, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 9005,
        Level = LogLevel.Warning,
        Message = "Permit obligations failed for {RequestType}. Overriding to Deny per XACML 7.18: {ErrorCode}")]
    internal static partial void PermitObligationsFailed(
        ILogger logger, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 9008,
        Level = LogLevel.Warning,
        Message = "ABAC: Indeterminate for {RequestType}: {Reason}")]
    internal static partial void EvaluationIndeterminate(
        ILogger logger, string requestType, string reason);

    [LoggerMessage(
        EventId = 9009,
        Level = LogLevel.Error,
        Message = "ABAC evaluation failed for {RequestType} after {DurationMs:F2}ms")]
    internal static partial void EvaluationFailed(
        ILogger logger, Exception exception, string requestType, double durationMs);

    // ── Obligation Messages (9010-9019) ─────────────────────────────

    [LoggerMessage(
        EventId = 9010,
        Level = LogLevel.Error,
        Message = "No handler registered for mandatory obligation {ObligationId}. Access denied per XACML 7.18")]
    internal static partial void ObligationNoHandler(
        ILogger logger, string obligationId);

    [LoggerMessage(
        EventId = 9011,
        Level = LogLevel.Error,
        Message = "Obligation handler for {ObligationId} failed: {ErrorCode}. Access denied per XACML 7.18")]
    internal static partial void ObligationHandlerFailed(
        ILogger logger, string obligationId, string errorCode);

    [LoggerMessage(
        EventId = 9012,
        Level = LogLevel.Debug,
        Message = "Obligation {ObligationId} executed successfully")]
    internal static partial void ObligationExecuted(
        ILogger logger, string obligationId);

    [LoggerMessage(
        EventId = 9013,
        Level = LogLevel.Debug,
        Message = "{Count} obligation(s) executed successfully")]
    internal static partial void AllObligationsExecuted(
        ILogger logger, int count);

    [LoggerMessage(
        EventId = 9014,
        Level = LogLevel.Warning,
        Message = "OnDeny obligation failed for {RequestType}: {ErrorCode}")]
    internal static partial void OnDenyObligationFailed(
        ILogger logger, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 9015,
        Level = LogLevel.Debug,
        Message = "OnDeny obligations executed for {RequestType}")]
    internal static partial void OnDenyObligationsExecuted(
        ILogger logger, string requestType);

    // ── Advice Messages (9020-9029) ─────────────────────────────────

    [LoggerMessage(
        EventId = 9020,
        Level = LogLevel.Debug,
        Message = "No handler registered for advice {AdviceId}. Skipping (advice is best-effort)")]
    internal static partial void AdviceNoHandler(
        ILogger logger, string adviceId);

    [LoggerMessage(
        EventId = 9021,
        Level = LogLevel.Warning,
        Message = "Advice handler for {AdviceId} failed: {ErrorCode}. Continuing (advice is best-effort)")]
    internal static partial void AdviceHandlerFailed(
        ILogger logger, string adviceId, string errorCode);

    [LoggerMessage(
        EventId = 9022,
        Level = LogLevel.Debug,
        Message = "Advice {AdviceId} executed successfully")]
    internal static partial void AdviceExecuted(
        ILogger logger, string adviceId);

    // ── PAP Store Messages (9030-9040) ────────────────────────────────

    [LoggerMessage(
        EventId = 9030,
        Level = LogLevel.Debug,
        Message = "PAP loading {EntityType} from persistent store")]
    internal static partial void PapLoadStarting(
        ILogger logger, string entityType);

    [LoggerMessage(
        EventId = 9031,
        Level = LogLevel.Debug,
        Message = "PAP loaded {Count} {EntityType} from persistent store ({DurationMs:F2}ms)")]
    internal static partial void PapLoadCompleted(
        ILogger logger, int count, string entityType, double durationMs);

    [LoggerMessage(
        EventId = 9032,
        Level = LogLevel.Debug,
        Message = "PAP saving {EntityType} '{EntityId}' to persistent store")]
    internal static partial void PapSaveStarting(
        ILogger logger, string entityType, string entityId);

    [LoggerMessage(
        EventId = 9033,
        Level = LogLevel.Debug,
        Message = "PAP saved {EntityType} '{EntityId}' to persistent store ({DurationMs:F2}ms)")]
    internal static partial void PapSaveCompleted(
        ILogger logger, string entityType, string entityId, double durationMs);

    [LoggerMessage(
        EventId = 9034,
        Level = LogLevel.Debug,
        Message = "PAP deleting {EntityType} '{EntityId}' from persistent store")]
    internal static partial void PapDeleteStarting(
        ILogger logger, string entityType, string entityId);

    [LoggerMessage(
        EventId = 9035,
        Level = LogLevel.Debug,
        Message = "PAP deleted {EntityType} '{EntityId}' from persistent store ({DurationMs:F2}ms)")]
    internal static partial void PapDeleteCompleted(
        ILogger logger, string entityType, string entityId, double durationMs);

    [LoggerMessage(
        EventId = 9038,
        Level = LogLevel.Debug,
        Message = "PAP serialized {EntityType} to JSON ({JsonSize} bytes, {DurationMs:F2}ms)")]
    internal static partial void PapSerializeCompleted(
        ILogger logger, string entityType, int jsonSize, double durationMs);

    [LoggerMessage(
        EventId = 9039,
        Level = LogLevel.Information,
        Message = "Persistent PAP store connectivity verified ({PolicySetCount} policy sets, {PolicyCount} standalone policies)")]
    internal static partial void PapStoreConnectivityVerified(
        ILogger logger, int policySetCount, int policyCount);

    // ── XACML XML Serialization Messages (9050-9059) ─────────────────

    [LoggerMessage(
        EventId = 9050,
        Level = LogLevel.Debug,
        Message = "XACML XML serialization completed for {EntityType} '{EntityId}' ({XmlSizeBytes} bytes, {DurationMs:F2}ms)")]
    internal static partial void XacmlXmlSerializationCompleted(
        ILogger logger, string entityType, string entityId, long xmlSizeBytes, double durationMs);

    [LoggerMessage(
        EventId = 9051,
        Level = LogLevel.Debug,
        Message = "XACML XML deserialization completed for {EntityType} '{EntityId}' ({DurationMs:F2}ms)")]
    internal static partial void XacmlXmlDeserializationCompleted(
        ILogger logger, string entityType, string entityId, double durationMs);

    [LoggerMessage(
        EventId = 9052,
        Level = LogLevel.Warning,
        Message = "XACML XML deserialization failed for {EntityType}: {Reason}")]
    internal static partial void XacmlXmlDeserializationFailed(
        ILogger logger, string entityType, string reason);

    [LoggerMessage(
        EventId = 9053,
        Level = LogLevel.Information,
        Message = "XACML document has no Encina extensions for {EntityType} '{EntityId}'. Applying defaults (IsEnabled=true, Priority=0)")]
    internal static partial void XacmlXmlExtensionsMissing(
        ILogger logger, string entityType, string entityId);

    [LoggerMessage(
        EventId = 9054,
        Level = LogLevel.Warning,
        Message = "Unknown XACML function URN '{FunctionUrn}' encountered during deserialization. Passing through as-is")]
    internal static partial void XacmlXmlUnknownFunction(
        ILogger logger, string functionUrn);

    [LoggerMessage(
        EventId = 9055,
        Level = LogLevel.Debug,
        Message = "Unknown XACML expression element '{ElementName}' encountered. Treating as string AttributeValue")]
    internal static partial void XacmlXmlUnknownElement(
        ILogger logger, string elementName);

    // ── Required Policy, Condition and Handler Messages (9072-9078) ──
    // Event IDs: 9072-9078 (see EventIdRanges.SecurityABAC). Codes and exception types only,
    // never an error or exception message.

    [LoggerMessage(
        EventId = 9072,
        Level = LogLevel.Warning,
        Message = "Lookup of required policy {PolicyId} failed: {ErrorCode}. The policy is Indeterminate")]
    internal static partial void RequiredPolicyLookupFailed(
        ILogger logger, string policyId, string errorCode);

    [LoggerMessage(
        EventId = 9073,
        Level = LogLevel.Error,
        Message = "Unexpected error while evaluating required policy {PolicyId}. The policy is Indeterminate")]
    internal static partial void RequiredPolicyEvaluationFailed(
        ILogger logger, Exception exception, string policyId);

    [LoggerMessage(
        EventId = 9074,
        Level = LogLevel.Warning,
        Message = "Required policy {PolicyId} for {RequestType} is not a top-level policy set or standalone policy in the policy store. The request is denied in Block mode and proceeds in Warn mode")]
    internal static partial void RequiredPolicyNotFound(
        ILogger logger, string policyId, string requestType);

    [LoggerMessage(
        EventId = 9075,
        Level = LogLevel.Debug,
        Message = "Condition {ConditionIndex} for {RequestType} evaluated to false. Access denied")]
    internal static partial void ConditionNotMet(
        ILogger logger, int conditionIndex, string requestType);

    [LoggerMessage(
        EventId = 9076,
        Level = LogLevel.Warning,
        Message = "Condition {ConditionIndex} for {RequestType} could not be compiled: {ErrorCode}. The condition is Indeterminate")]
    internal static partial void ConditionCompilationFailed(
        ILogger logger, int conditionIndex, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 9077,
        Level = LogLevel.Warning,
        Message = "Condition {ConditionIndex} for {RequestType} failed during evaluation. The condition is Indeterminate")]
    internal static partial void ConditionEvaluationFailed(
        ILogger logger, Exception exception, int conditionIndex, string requestType);

    [LoggerMessage(
        EventId = 9078,
        Level = LogLevel.Error,
        Message = "Handler for obligation or advice {ObligationId} threw an exception")]
    internal static partial void ObligationHandlerThrew(
        ILogger logger, Exception exception, string obligationId);

    // ── Enforcement Disabled (9085) ──────────────────────────────────
    // Event ID 9085 (see EventIdRanges.SecurityABAC) is the "Enforcement Disabled" id of the
    // decision audit trail (#751), allocated by #1705: it is logged once at startup when the
    // final options disable enforcement; #751 adds its once-per-request-type call to this method.

    [LoggerMessage(
        EventId = 9085,
        Level = LogLevel.Warning,
        Message = "ABAC enforcement is disabled (EnforcementMode = Disabled): requests with [RequirePolicy] or [RequireCondition] run without any ABAC evaluation")]
    internal static partial void EnforcementDisabled(ILogger logger);

    // ── Fail-Closed Messages (9091-9093) ─────────────────────────────
    // Event IDs: 9091-9093 (see EventIdRanges.SecurityABAC; 9079-9090 are reserved for the
    // decision audit trail of #751). Codes and exception types only, never a user identifier,
    // an error message or an exception message (#1676).

    [LoggerMessage(
        EventId = 9091,
        Level = LogLevel.Warning,
        Message = "ABAC denied {RequestType}: the request has no authenticated caller ({ErrorCode}). The request is denied in every enforcement mode")]
    internal static partial void UnauthenticatedCaller(
        ILogger logger, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 9092,
        Level = LogLevel.Warning,
        Message = "Retrieval of the {Source} from the policy administration point failed: {ErrorCode}. The decision is Indeterminate")]
    internal static partial void PolicyRetrievalFailed(
        ILogger logger, string source, string errorCode);

    [LoggerMessage(
        EventId = 9093,
        Level = LogLevel.Error,
        Message = "Unexpected error while evaluating the policy store. The decision is Indeterminate")]
    internal static partial void StoreEvaluationFailed(
        ILogger logger, Exception exception);

    // ── Decision Audit (9079-9090) ───────────────────────────────────
    // Event IDs: 9079-9090 (see EventIdRanges.SecurityABAC), the block reserved for the decision
    // audit trail of #751; 9084, 9086 and 9087 are the startup check. Codes and exception types
    // only: never a subject, a tenant, an attribute value, an error message or an exception message.

    [LoggerMessage(
        EventId = 9079,
        Level = LogLevel.Debug,
        Message = "ABAC decision for {RequestType} recorded: outcome {EnforcedOutcome}, reason {ReasonCode}")]
    internal static partial void DecisionRecorded(
        ILogger logger, string requestType, string enforcedOutcome, string reasonCode);

    [LoggerMessage(
        EventId = 9080,
        Level = LogLevel.Error,
        Message = "The ABAC decision for {RequestType} could not be recorded ({FailureCode}). The request is denied (FailClosed)")]
    internal static partial void DecisionAuditFailedAccessDenied(
        ILogger logger, string requestType, string failureCode);

    [LoggerMessage(
        EventId = 9081,
        Level = LogLevel.Warning,
        Message = "The ABAC decision for {RequestType} could not be recorded ({FailureCode}). The request proceeds (BestEffort)")]
    internal static partial void DecisionAuditFailedProceeding(
        ILogger logger, string requestType, string failureCode);

    [LoggerMessage(
        EventId = 9082,
        Level = LogLevel.Error,
        Message = "The ABAC decision for {RequestType}, which was denied, could not be recorded ({FailureCode}). The denial stands")]
    internal static partial void DecisionAuditFailedForDeniedRequest(
        ILogger logger, string requestType, string failureCode);

    [LoggerMessage(
        EventId = 9083,
        Level = LogLevel.Error,
        Message = "The decision recorder threw while recording the ABAC decision for {RequestType}")]
    internal static partial void DecisionRecorderThrew(
        ILogger logger, Exception exception, string requestType);

    [LoggerMessage(
        EventId = 9088,
        Level = LogLevel.Debug,
        Message = "The evaluation trace of the ABAC decision for {RequestType} reached the limit of {MaxTraceEntries} entries and is truncated")]
    internal static partial void EvaluationTraceTruncated(
        ILogger logger, string requestType, int maxTraceEntries);

    /// <summary>
    /// The write of a decision record failed or timed out, but the entry with its decision id was
    /// found committed, so the record counts as written (Event IDs 9079-9090, see EventIdRanges.SecurityABAC).
    /// </summary>
    [LoggerMessage(
        EventId = 9089,
        Level = LogLevel.Debug,
        Message = "The write of ABAC decision {DecisionId} reported a failure ({FailureCode}) but the entry is stored; the record counts as written")]
    internal static partial void DecisionAlreadyStored(
        ILogger logger, Guid decisionId, string failureCode);

    /// <summary>
    /// A decision audit query ran without a tenant in a multi-tenant application because
    /// <c>AllowCrossTenantQueries</c> is set: the logged opt-out of the reader's tenant gate
    /// (Event IDs 9079-9090, see EventIdRanges.SecurityABAC).
    /// </summary>
    [LoggerMessage(
        EventId = 9090,
        Level = LogLevel.Warning,
        Message = "A decision audit query ran without a tenant in a multi-tenant application (AllowCrossTenantQueries is set)")]
    internal static partial void DecisionAuditCrossTenantQuery(ILogger logger);

    /// <summary>
    /// The decision audit runs in <c>BestEffort</c>: a request proceeds when its record cannot be
    /// written (startup check, Event ID 9084, see EventIdRanges.SecurityABAC).
    /// </summary>
    [LoggerMessage(
        EventId = 9084,
        Level = LogLevel.Warning,
        Message = "The ABAC decision audit runs in BestEffort mode: a request proceeds even when its decision record cannot be written, so the trail may be incomplete")]
    internal static partial void DecisionAuditBestEffort(ILogger logger);

    /// <summary>
    /// The decision audit writes to the in-memory operation audit store, which loses the trail on
    /// restart (startup check, Event ID 9086, see EventIdRanges.SecurityABAC).
    /// </summary>
    [LoggerMessage(
        EventId = 9086,
        Level = LogLevel.Warning,
        Message = "The ABAC decision audit writes to InMemoryOperationAuditStore: the trail is lost when the process stops. Register a persistent IOperationAuditStore for production")]
    internal static partial void DecisionAuditInMemoryStore(ILogger logger);

    /// <summary>
    /// The decision audit is enabled but no <c>IOperationAuditStore</c> is registered: the application
    /// fails to start (startup check, Event ID 9087, see EventIdRanges.SecurityABAC).
    /// </summary>
    [LoggerMessage(
        EventId = 9087,
        Level = LogLevel.Critical,
        Message = "The ABAC decision audit is enabled but no IOperationAuditStore is registered. Register one (for example through a provider package) or disable DecisionAudit")]
    internal static partial void DecisionAuditStoreMissing(ILogger logger);

    /// <summary>
    /// A stored decision audit entry could not be read back; the query fails with
    /// <c>abac.decision_audit_record_unreadable</c>. The exception arrives redacted (type and stack
    /// trace only) (Event ID 9098, see EventIdRanges.SecurityABAC).
    /// </summary>
    [LoggerMessage(
        EventId = 9098,
        Level = LogLevel.Error,
        Message = "A stored ABAC decision audit entry could not be read; the query fails")]
    internal static partial void DecisionAuditRecordUnreadable(ILogger logger, Exception exception);

    /// <summary>
    /// The write of a decision record failed and the look-up that would confirm a committed entry
    /// failed too (a store error code or an exception type, never a message), so the write stays
    /// unconfirmed (Event ID 9099, see EventIdRanges.SecurityABAC).
    /// </summary>
    [LoggerMessage(
        EventId = 9099,
        Level = LogLevel.Debug,
        Message = "The write of ABAC decision {DecisionId} failed ({FailureCode}) and the look-up that would confirm it failed too ({LookupFailure}); the write stays unconfirmed")]
    internal static partial void DecisionStoredCheckFailed(
        ILogger logger, Guid decisionId, string failureCode, string lookupFailure);
}
