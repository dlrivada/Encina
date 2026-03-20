using Microsoft.Extensions.Logging;

namespace Encina.Compliance.AIAct.Diagnostics;

/// <summary>
/// High-performance structured log messages for the AI Act compliance pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>LoggerMessage.Define</c> to avoid boxing and string formatting overhead
/// in hot paths. All methods are extension methods on <see cref="ILogger"/> for ergonomic use.
/// </para>
/// <para>
/// Event IDs are allocated in the 9500–9530 range reserved for AI Act compliance
/// (see <c>EventIdRanges.ComplianceAIAct</c>).
/// </para>
/// </remarks>
internal static class AIActLogMessages
{
    // -- 9500: Compliance check started --

    private static readonly Action<ILogger, string, string, Exception?> ComplianceCheckStartedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(9500, nameof(ComplianceCheckStarted)),
            "AI Act compliance check started. RequestType={RequestType}, SystemId={SystemId}");

    internal static void ComplianceCheckStarted(this ILogger logger, string requestType, string systemId)
        => ComplianceCheckStartedDef(logger, requestType, systemId, null);

    // -- 9501: Compliance check passed --

    private static readonly Action<ILogger, string, string, Exception?> ComplianceCheckPassedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(9501, nameof(ComplianceCheckPassed)),
            "AI Act compliance check passed. RequestType={RequestType}, RiskLevel={RiskLevel}");

    internal static void ComplianceCheckPassed(this ILogger logger, string requestType, string riskLevel)
        => ComplianceCheckPassedDef(logger, requestType, riskLevel, null);

    // -- 9502: Compliance check failed --

    private static readonly Action<ILogger, string, string, string, Exception?> ComplianceCheckFailedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(9502, nameof(ComplianceCheckFailed)),
            "AI Act compliance check failed. RequestType={RequestType}, SystemId={SystemId}, Reason={Reason}");

    internal static void ComplianceCheckFailed(this ILogger logger, string requestType, string systemId, string reason)
        => ComplianceCheckFailedDef(logger, requestType, systemId, reason, null);

    // -- 9503: Compliance check skipped (no attributes) --

    private static readonly Action<ILogger, string, Exception?> ComplianceCheckSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            new EventId(9503, nameof(ComplianceCheckSkipped)),
            "AI Act compliance check skipped (no AI Act attributes). RequestType={RequestType}");

    internal static void ComplianceCheckSkipped(this ILogger logger, string requestType)
        => ComplianceCheckSkippedDef(logger, requestType, null);

    // -- 9504: Pipeline disabled --

    private static readonly Action<ILogger, string, Exception?> PipelineDisabledDef =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            new EventId(9504, nameof(PipelineDisabled)),
            "AI Act compliance pipeline disabled. RequestType={RequestType}");

    internal static void PipelineDisabled(this ILogger logger, string requestType)
        => PipelineDisabledDef(logger, requestType, null);

    // -- 9505: Prohibited use blocked --

    private static readonly Action<ILogger, string, string, string, Exception?> ProhibitedUseBlockedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(9505, nameof(ProhibitedUseBlocked)),
            "AI Act: PROHIBITED practice detected. RequestType={RequestType}, SystemId={SystemId}, Violations={Violations}. Request blocked unconditionally per Art. 5.");

    internal static void ProhibitedUseBlocked(this ILogger logger, string requestType, string systemId, string violations)
        => ProhibitedUseBlockedDef(logger, requestType, systemId, violations, null);

    // -- 9506: Violations blocked (Block mode) --

    private static readonly Action<ILogger, string, string, string, Exception?> ViolationsBlockedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(9506, nameof(ViolationsBlocked)),
            "AI Act compliance violations detected (Block mode). RequestType={RequestType}, SystemId={SystemId}, Violations={Violations}");

    internal static void ViolationsBlocked(this ILogger logger, string requestType, string systemId, string violations)
        => ViolationsBlockedDef(logger, requestType, systemId, violations, null);

    // -- 9507: Violations warned (Warn mode) --

    private static readonly Action<ILogger, string, string, string, Exception?> ViolationsWarnedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(9507, nameof(ViolationsWarned)),
            "AI Act compliance violations detected (Warn mode, proceeding). RequestType={RequestType}, SystemId={SystemId}, Violations={Violations}");

    internal static void ViolationsWarned(this ILogger logger, string requestType, string systemId, string violations)
        => ViolationsWarnedDef(logger, requestType, systemId, violations, null);

    // -- 9508: Human oversight required --

    private static readonly Action<ILogger, string, string, Exception?> HumanOversightRequiredDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(9508, nameof(HumanOversightRequired)),
            "AI Act: Human oversight required (Art. 14). RequestType={RequestType}, SystemId={SystemId}");

    internal static void HumanOversightRequired(this ILogger logger, string requestType, string systemId)
        => HumanOversightRequiredDef(logger, requestType, systemId, null);

    // -- 9509: Transparency obligation --

    private static readonly Action<ILogger, string, string, Exception?> TransparencyObligationDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(9509, nameof(TransparencyObligation)),
            "AI Act: Transparency obligations apply (Art. 13/50). RequestType={RequestType}, SystemId={SystemId}");

    internal static void TransparencyObligation(this ILogger logger, string requestType, string systemId)
        => TransparencyObligationDef(logger, requestType, systemId, null);

    // -- 9510: Validator error --

    private static readonly Action<ILogger, string, string, Exception?> ValidatorErrorDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(9510, nameof(ValidatorError)),
            "AI Act compliance validator failed. RequestType={RequestType}, Error={Error}");

    internal static void ValidatorError(this ILogger logger, string requestType, string error)
        => ValidatorErrorDef(logger, requestType, error, null);

    // -- 9511: Auto-registration completed --

    private static readonly Action<ILogger, int, int, Exception?> AutoRegistrationCompletedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(9511, nameof(AutoRegistrationCompleted)),
            "AI Act auto-registration completed. SystemsRegistered={SystemsRegistered}, AssembliesScanned={AssembliesScanned}");

    internal static void AutoRegistrationCompleted(this ILogger logger, int systemsRegistered, int assembliesScanned)
        => AutoRegistrationCompletedDef(logger, systemsRegistered, assembliesScanned, null);

    // -- 9512: Health check completed --

    private static readonly Action<ILogger, string, int, Exception?> HealthCheckCompletedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(9512, nameof(HealthCheckCompleted)),
            "AI Act health check completed. Status={Status}, SystemCount={SystemCount}");

    internal static void HealthCheckCompleted(this ILogger logger, string status, int systemCount)
        => HealthCheckCompletedDef(logger, status, systemCount, null);
}
