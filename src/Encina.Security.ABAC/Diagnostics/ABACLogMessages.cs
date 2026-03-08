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
        Message = "ABAC enforcement in Warn mode - would deny {RequestType}: {ErrorMessage}. Allowing request to proceed")]
    internal static partial void EnforcementWarnMode(
        ILogger logger, string requestType, string errorMessage);

    [LoggerMessage(
        EventId = 9005,
        Level = LogLevel.Warning,
        Message = "Permit obligations failed for {RequestType}. Overriding to Deny per XACML 7.18: {ErrorMessage}")]
    internal static partial void PermitObligationsFailed(
        ILogger logger, string requestType, string errorMessage);

    [LoggerMessage(
        EventId = 9006,
        Level = LogLevel.Debug,
        Message = "ABAC: NotApplicable for {RequestType} - allowing per DefaultNotApplicableEffect=Permit")]
    internal static partial void NotApplicablePermit(
        ILogger logger, string requestType);

    [LoggerMessage(
        EventId = 9007,
        Level = LogLevel.Debug,
        Message = "ABAC: NotApplicable for {RequestType} - denying per DefaultNotApplicableEffect=Deny")]
    internal static partial void NotApplicableDeny(
        ILogger logger, string requestType);

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
        Message = "Obligation handler for {ObligationId} failed: {ErrorMessage}. Access denied per XACML 7.18")]
    internal static partial void ObligationHandlerFailed(
        ILogger logger, string obligationId, string errorMessage);

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
        Message = "OnDeny obligation failed for {RequestType}: {ErrorMessage}")]
    internal static partial void OnDenyObligationFailed(
        ILogger logger, string requestType, string errorMessage);

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
        Message = "Advice handler for {AdviceId} failed: {ErrorMessage}. Continuing (advice is best-effort)")]
    internal static partial void AdviceHandlerFailed(
        ILogger logger, string adviceId, string errorMessage);

    [LoggerMessage(
        EventId = 9022,
        Level = LogLevel.Debug,
        Message = "Advice {AdviceId} executed successfully")]
    internal static partial void AdviceExecuted(
        ILogger logger, string adviceId);
}
