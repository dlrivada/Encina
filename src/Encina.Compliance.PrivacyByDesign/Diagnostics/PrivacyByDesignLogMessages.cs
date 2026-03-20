using Microsoft.Extensions.Logging;

namespace Encina.Compliance.PrivacyByDesign.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Privacy by Design module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8900-8949 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8299, DSR uses 8300-8349,
/// LawfulBasis uses 8350-8399, Anonymization uses 8400-8499, Retention uses 8500-8599,
/// DataResidency uses 8600-8699, BreachNotification uses 8700-8799, DPIA uses 8800-8899,
/// ProcessorAgreements uses 9100-9199, NIS2 uses 9200-9299).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>8900-8909</term><description>Pipeline behavior</description></item>
/// <item><term>8910-8919</term><description>Validator operations</description></item>
/// <item><term>8920-8929</term><description>Analyzer operations</description></item>
/// <item><term>8930-8939</term><description>Purpose registry / hosted service</description></item>
/// <item><term>8940-8942</term><description>Health check + notifications</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class PrivacyByDesignLogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (8900-8909)
    // ========================================================================

    /// <summary>PbD pipeline skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 8900,
        Level = LogLevel.Trace,
        Message = "PbD pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void PbDPipelineDisabled(this ILogger logger, string requestType);

    /// <summary>PbD pipeline skipped because no [EnforceDataMinimization] attribute found on request type.</summary>
    [LoggerMessage(
        EventId = 8901,
        Level = LogLevel.Trace,
        Message = "PbD pipeline skipped (no [EnforceDataMinimization] attribute). RequestType={RequestType}")]
    internal static partial void PbDPipelineNoAttribute(this ILogger logger, string requestType);

    /// <summary>PbD pipeline check started.</summary>
    [LoggerMessage(
        EventId = 8902,
        Level = LogLevel.Debug,
        Message = "PbD pipeline check started. RequestType={RequestType}, EnforcementMode={EnforcementMode}")]
    internal static partial void PbDPipelineStarted(this ILogger logger, string requestType, string enforcementMode);

    /// <summary>PbD pipeline check passed — request is compliant.</summary>
    [LoggerMessage(
        EventId = 8903,
        Level = LogLevel.Debug,
        Message = "PbD pipeline check passed. RequestType={RequestType}, MinimizationScore={MinimizationScore}")]
    internal static partial void PbDPipelinePassed(this ILogger logger, string requestType, double minimizationScore);

    /// <summary>PbD pipeline check found violations (GDPR Art. 25).</summary>
    [LoggerMessage(
        EventId = 8904,
        Level = LogLevel.Warning,
        Message = "PbD pipeline check found violations. RequestType={RequestType}, ViolationCount={ViolationCount}, MinimizationScore={MinimizationScore}")]
    internal static partial void PbDPipelineViolations(this ILogger logger, string requestType, int violationCount, double minimizationScore);

    /// <summary>PbD pipeline blocked the request in Block enforcement mode.</summary>
    [LoggerMessage(
        EventId = 8905,
        Level = LogLevel.Warning,
        Message = "PbD pipeline blocked request. RequestType={RequestType}, Reason={Reason}")]
    internal static partial void PbDPipelineBlocked(this ILogger logger, string requestType, string reason);

    /// <summary>PbD pipeline issued a warning in Warn enforcement mode but allowed the request.</summary>
    [LoggerMessage(
        EventId = 8906,
        Level = LogLevel.Warning,
        Message = "PbD pipeline warning (request allowed). RequestType={RequestType}, Reason={Reason}")]
    internal static partial void PbDPipelineWarned(this ILogger logger, string requestType, string reason);

    /// <summary>Exception occurred in the PbD pipeline.</summary>
    [LoggerMessage(
        EventId = 8907,
        Level = LogLevel.Error,
        Message = "PbD pipeline error. RequestType={RequestType}")]
    internal static partial void PbDPipelineError(this ILogger logger, string requestType, Exception exception);

    /// <summary>PbD pipeline minimization score below threshold (GDPR Art. 25(2)).</summary>
    [LoggerMessage(
        EventId = 8908,
        Level = LogLevel.Warning,
        Message = "PbD pipeline minimization score below threshold. RequestType={RequestType}, Score={Score}, Threshold={Threshold}")]
    internal static partial void PbDPipelineScoreBelowThreshold(this ILogger logger, string requestType, double score, double threshold);

    // ========================================================================
    // Validator log messages (8910-8919)
    // ========================================================================

    /// <summary>PbD validation completed for a request type.</summary>
    [LoggerMessage(
        EventId = 8910,
        Level = LogLevel.Debug,
        Message = "PbD validation completed. RequestType={RequestType}, IsCompliant={IsCompliant}, ViolationCount={ViolationCount}, ModuleId={ModuleId}")]
    internal static partial void PbDValidationCompleted(this ILogger logger, string requestType, bool isCompliant, int violationCount, string? moduleId);

    /// <summary>PbD data minimization analysis failed for a request type.</summary>
    [LoggerMessage(
        EventId = 8911,
        Level = LogLevel.Warning,
        Message = "PbD data minimization analysis failed. RequestType={RequestType}, ErrorMessage={ErrorMessage}")]
    internal static partial void PbDMinimizationAnalysisFailed(this ILogger logger, string requestType, string errorMessage);

    /// <summary>PbD purpose limitation validation failed for a request type (GDPR Art. 5(1)(b)).</summary>
    [LoggerMessage(
        EventId = 8912,
        Level = LogLevel.Warning,
        Message = "PbD purpose limitation validation failed. RequestType={RequestType}, ErrorMessage={ErrorMessage}")]
    internal static partial void PbDPurposeValidationFailed(this ILogger logger, string requestType, string errorMessage);

    /// <summary>PbD privacy defaults inspection failed for a request type (GDPR Art. 25(2)).</summary>
    [LoggerMessage(
        EventId = 8913,
        Level = LogLevel.Warning,
        Message = "PbD privacy defaults inspection failed. RequestType={RequestType}, ErrorMessage={ErrorMessage}")]
    internal static partial void PbDDefaultsInspectionFailed(this ILogger logger, string requestType, string errorMessage);

    /// <summary>PbD validation threw an exception.</summary>
    [LoggerMessage(
        EventId = 8914,
        Level = LogLevel.Error,
        Message = "PbD validation failed with exception. RequestType={RequestType}")]
    internal static partial void PbDValidationError(this ILogger logger, string requestType, Exception exception);

    /// <summary>PbD purpose limitation validation threw an exception.</summary>
    [LoggerMessage(
        EventId = 8915,
        Level = LogLevel.Error,
        Message = "PbD purpose limitation validation failed with exception. RequestType={RequestType}")]
    internal static partial void PbDPurposeLimitationError(this ILogger logger, string requestType, Exception exception);

    // ========================================================================
    // Analyzer log messages (8920-8929)
    // ========================================================================

    /// <summary>Data minimization analysis completed.</summary>
    [LoggerMessage(
        EventId = 8920,
        Level = LogLevel.Debug,
        Message = "PbD data minimization analysis completed. RequestType={RequestType}, Score={Score}, NecessaryFields={NecessaryCount}, UnnecessaryFields={UnnecessaryCount}")]
    internal static partial void PbDAnalysisCompleted(this ILogger logger, string requestType, double score, int necessaryCount, int unnecessaryCount);

    /// <summary>Data minimization analysis threw an exception.</summary>
    [LoggerMessage(
        EventId = 8921,
        Level = LogLevel.Error,
        Message = "PbD data minimization analysis failed with exception. RequestType={RequestType}")]
    internal static partial void PbDAnalysisError(this ILogger logger, string requestType, Exception exception);

    /// <summary>Privacy defaults inspection completed.</summary>
    [LoggerMessage(
        EventId = 8922,
        Level = LogLevel.Debug,
        Message = "PbD privacy defaults inspection completed. RequestType={RequestType}, FieldCount={FieldCount}, MatchingCount={MatchingCount}")]
    internal static partial void PbDDefaultsInspectionCompleted(this ILogger logger, string requestType, int fieldCount, int matchingCount);

    /// <summary>Privacy defaults inspection threw an exception.</summary>
    [LoggerMessage(
        EventId = 8923,
        Level = LogLevel.Error,
        Message = "PbD privacy defaults inspection failed with exception. RequestType={RequestType}")]
    internal static partial void PbDDefaultsInspectionError(this ILogger logger, string requestType, Exception exception);

    // ========================================================================
    // Purpose registry / hosted service log messages (8930-8939)
    // ========================================================================

    /// <summary>Purpose registration starting at startup.</summary>
    [LoggerMessage(
        EventId = 8930,
        Level = LogLevel.Information,
        Message = "PbD purpose registration starting. PurposeCount={PurposeCount}")]
    internal static partial void PbDPurposeRegistrationStarting(this ILogger logger, int purposeCount);

    /// <summary>Purpose registered successfully.</summary>
    [LoggerMessage(
        EventId = 8931,
        Level = LogLevel.Debug,
        Message = "PbD purpose registered: '{PurposeName}' (ModuleId={ModuleId}, PurposeId={PurposeId})")]
    internal static partial void PbDPurposeRegistered(this ILogger logger, string purposeName, string moduleId, string purposeId);

    /// <summary>Purpose registration failed.</summary>
    [LoggerMessage(
        EventId = 8932,
        Level = LogLevel.Warning,
        Message = "PbD purpose registration failed: '{PurposeName}' (ModuleId={ModuleId}): {ErrorMessage}")]
    internal static partial void PbDPurposeRegistrationFailed(this ILogger logger, string purposeName, string moduleId, string errorMessage);

    /// <summary>Purpose registration completed.</summary>
    [LoggerMessage(
        EventId = 8933,
        Level = LogLevel.Information,
        Message = "PbD purpose registration completed. Registered={Registered}, Failed={Failed}")]
    internal static partial void PbDPurposeRegistrationCompleted(this ILogger logger, int registered, int failed);

    // ========================================================================
    // Health check log messages (8940-8949)
    // ========================================================================

    /// <summary>PbD health check completed.</summary>
    [LoggerMessage(
        EventId = 8940,
        Level = LogLevel.Debug,
        Message = "PbD health check completed: {Status} ({WarningCount} warnings)")]
    internal static partial void PbDHealthCheckCompleted(this ILogger logger, string status, int warningCount);

    // ========================================================================
    // Notification log messages (8941-8942)
    // ========================================================================

    /// <summary>PbD violation notification published successfully.</summary>
    [LoggerMessage(
        EventId = 8941,
        Level = LogLevel.Debug,
        Message = "PbD violation notification published. RequestType={RequestType}, ViolationCount={ViolationCount}")]
    internal static partial void PbDNotificationPublished(this ILogger logger, string requestType, int violationCount);

    /// <summary>PbD notification publish failed (non-blocking).</summary>
    [LoggerMessage(
        EventId = 8942,
        Level = LogLevel.Warning,
        Message = "PbD violation notification publish failed. RequestType={RequestType}")]
    internal static partial void PbDNotificationFailed(this ILogger logger, string requestType, Exception exception);
}
