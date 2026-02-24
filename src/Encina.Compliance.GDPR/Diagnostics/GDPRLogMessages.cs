using Microsoft.Extensions.Logging;

namespace Encina.Compliance.GDPR.Diagnostics;

/// <summary>
/// High-performance structured log messages for the GDPR compliance pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>LoggerMessage.Define</c> to avoid boxing and string formatting overhead
/// in hot paths. All methods are extension methods on <see cref="ILogger"/> for ergonomic use.
/// </para>
/// <para>
/// Event IDs are allocated in the 8100–8199 range to avoid collisions with other
/// Encina subsystems (Security uses 8000–8099).
/// </para>
/// </remarks>
internal static class GDPRLogMessages
{
    // -- 8100: Compliance check started --

    private static readonly Action<ILogger, string, Exception?> ComplianceCheckStartedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8100, nameof(ComplianceCheckStarted)),
            "GDPR compliance check started. RequestType={RequestType}");

    internal static void ComplianceCheckStarted(this ILogger logger, string requestType)
        => ComplianceCheckStartedDef(logger, requestType, null);

    // -- 8101: Compliance check passed --

    private static readonly Action<ILogger, string, string, Exception?> ComplianceCheckPassedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(8101, nameof(ComplianceCheckPassed)),
            "GDPR compliance check passed. RequestType={RequestType}, LawfulBasis={LawfulBasis}");

    internal static void ComplianceCheckPassed(this ILogger logger, string requestType, string lawfulBasis)
        => ComplianceCheckPassedDef(logger, requestType, lawfulBasis, null);

    // -- 8102: Compliance check failed --

    private static readonly Action<ILogger, string, string, Exception?> ComplianceCheckFailedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8102, nameof(ComplianceCheckFailed)),
            "GDPR compliance check failed. RequestType={RequestType}, Reason={Reason}");

    internal static void ComplianceCheckFailed(this ILogger logger, string requestType, string reason)
        => ComplianceCheckFailedDef(logger, requestType, reason, null);

    // -- 8103: Unregistered processing activity --

    private static readonly Action<ILogger, string, Exception?> UnregisteredActivityDef =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(8103, nameof(UnregisteredActivity)),
            "No processing activity registered for request that processes personal data. RequestType={RequestType}");

    internal static void UnregisteredActivity(this ILogger logger, string requestType)
        => UnregisteredActivityDef(logger, requestType, null);

    // -- 8104: Skipped (no GDPR attributes) --

    private static readonly Action<ILogger, string, Exception?> ComplianceCheckSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            new EventId(8104, nameof(ComplianceCheckSkipped)),
            "GDPR compliance check skipped (no GDPR attributes). RequestType={RequestType}");

    internal static void ComplianceCheckSkipped(this ILogger logger, string requestType)
        => ComplianceCheckSkippedDef(logger, requestType, null);

    // -- 8105: Processing activity logged for accountability --

    private static readonly Action<ILogger, string, string, string, Exception?> ProcessingActivityLoggedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(8105, nameof(ProcessingActivityLogged)),
            "Processing activity recorded for accountability (Article 5(2)). RequestType={RequestType}, Purpose={Purpose}, LawfulBasis={LawfulBasis}");

    internal static void ProcessingActivityLogged(this ILogger logger, string requestType, string purpose, string lawfulBasis)
        => ProcessingActivityLoggedDef(logger, requestType, purpose, lawfulBasis, null);

    // -- 8106: Compliance warning (warn-only mode) --

    private static readonly Action<ILogger, string, string, Exception?> ComplianceWarningDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8106, nameof(ComplianceWarning)),
            "GDPR compliance issue (warn-only mode). RequestType={RequestType}, Warning={Warning}");

    internal static void ComplianceWarning(this ILogger logger, string requestType, string warning)
        => ComplianceWarningDef(logger, requestType, warning, null);

    // -- 8107: Auto-registration completed --

    private static readonly Action<ILogger, int, int, Exception?> AutoRegistrationCompletedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(8107, nameof(AutoRegistrationCompleted)),
            "GDPR auto-registration completed. ActivitiesRegistered={ActivitiesRegistered}, AssembliesScanned={AssembliesScanned}");

    internal static void AutoRegistrationCompleted(this ILogger logger, int activitiesRegistered, int assembliesScanned)
        => AutoRegistrationCompletedDef(logger, activitiesRegistered, assembliesScanned, null);

    // -- 8108: Auto-registration skipped (custom registry) --

    private static readonly Action<ILogger, string, Exception?> AutoRegistrationSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8108, nameof(AutoRegistrationSkipped)),
            "GDPR auto-registration skipped. Registry type '{RegistryType}' does not support attribute scanning.");

    internal static void AutoRegistrationSkipped(this ILogger logger, string registryType)
        => AutoRegistrationSkippedDef(logger, registryType, null);

    // -- 8109: RoPA export started --

    private static readonly Action<ILogger, string, int, Exception?> RoPAExportStartedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(8109, nameof(RoPAExportStarted)),
            "RoPA export started. Format={Format}, ActivityCount={ActivityCount}");

    internal static void RoPAExportStarted(this ILogger logger, string format, int activityCount)
        => RoPAExportStartedDef(logger, format, activityCount, null);

    // -- 8110: RoPA export completed --

    private static readonly Action<ILogger, string, int, int, Exception?> RoPAExportCompletedDef =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Information,
            new EventId(8110, nameof(RoPAExportCompleted)),
            "RoPA export completed. Format={Format}, ActivityCount={ActivityCount}, ByteSize={ByteSize}");

    internal static void RoPAExportCompleted(this ILogger logger, string format, int activityCount, int byteSize)
        => RoPAExportCompletedDef(logger, format, activityCount, byteSize, null);

    // -- 8111: RoPA export failed --

    private static readonly Action<ILogger, string, string, Exception?> RoPAExportFailedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(8111, nameof(RoPAExportFailed)),
            "RoPA export failed. Format={Format}, Reason={Reason}");

    internal static void RoPAExportFailed(this ILogger logger, string format, string reason)
        => RoPAExportFailedDef(logger, format, reason, null);

    // -- 8112: Health check completed --

    private static readonly Action<ILogger, string, int, Exception?> HealthCheckCompletedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8112, nameof(HealthCheckCompleted)),
            "GDPR health check completed. Status={Status}, ActivityCount={ActivityCount}");

    internal static void HealthCheckCompleted(this ILogger logger, string status, int activityCount)
        => HealthCheckCompletedDef(logger, status, activityCount, null);

    // =====================================================
    // Lawful Basis DI & Auto-Registration (8211–8213)
    // Note: Validation events 8200–8210 are now in
    // LawfulBasisLogMessages (source-generated).
    // =====================================================

    // -- 8211: Lawful basis auto-registration completed --

    private static readonly Action<ILogger, int, int, int, Exception?> LawfulBasisAutoRegistrationCompletedDef =
        LoggerMessage.Define<int, int, int>(
            LogLevel.Information,
            new EventId(8211, nameof(LawfulBasisAutoRegistrationCompleted)),
            "Lawful basis auto-registration completed. TotalRegistered={TotalRegistered}, AssembliesScanned={AssembliesScanned}, DefaultBasesApplied={DefaultBasesApplied}");

    internal static void LawfulBasisAutoRegistrationCompleted(this ILogger logger, int totalRegistered, int assembliesScanned, int defaultBasesApplied)
        => LawfulBasisAutoRegistrationCompletedDef(logger, totalRegistered, assembliesScanned, defaultBasesApplied, null);

    // -- 8212: Lawful basis auto-registration skipped (custom registry) --

    private static readonly Action<ILogger, string, Exception?> LawfulBasisAutoRegistrationSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8212, nameof(LawfulBasisAutoRegistrationSkipped)),
            "Lawful basis auto-registration skipped. Registry type '{RegistryType}' does not support attribute scanning.");

    internal static void LawfulBasisAutoRegistrationSkipped(this ILogger logger, string registryType)
        => LawfulBasisAutoRegistrationSkippedDef(logger, registryType, null);

    // -- 8213: Lawful basis health check completed --

    private static readonly Action<ILogger, string, int, Exception?> LawfulBasisHealthCheckCompletedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8213, nameof(LawfulBasisHealthCheckCompleted)),
            "Lawful basis health check completed. Status={Status}, RegistrationCount={RegistrationCount}");

    internal static void LawfulBasisHealthCheckCompleted(this ILogger logger, string status, int registrationCount)
        => LawfulBasisHealthCheckCompletedDef(logger, status, registrationCount, null);
}
