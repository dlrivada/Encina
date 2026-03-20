using Microsoft.Extensions.Logging;

namespace Encina.Compliance.NIS2.Diagnostics;

/// <summary>
/// High-performance structured log messages for the NIS2 Compliance module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 9200-9299 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8299, DSR uses 8300-8399,
/// Anonymization uses 8400-8499, Retention uses 8500-8599, DataResidency uses 8600-8699,
/// BreachNotification uses 8700-8799, DORA uses 8800-8899, ISO27001 uses 8900-8999,
/// SOC2 uses 9000-9099, PCI-DSS uses 9100-9199).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>9200-9209</term><description>Pipeline behavior</description></item>
/// <item><term>9210-9219</term><description>Compliance validation</description></item>
/// <item><term>9220-9229</term><description>MFA enforcement</description></item>
/// <item><term>9230-9239</term><description>Encryption validation</description></item>
/// <item><term>9240-9249</term><description>Supply chain</description></item>
/// <item><term>9250-9259</term><description>Incident handling</description></item>
/// <item><term>9260-9269</term><description>Health check</description></item>
/// <item><term>9270-9279</term><description>Management accountability</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class NIS2LogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (9200-9209)
    // ========================================================================

    /// <summary>NIS2 pipeline skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 9200,
        Level = LogLevel.Trace,
        Message = "NIS2 pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void NIS2PipelineDisabled(this ILogger logger, string requestType);

    /// <summary>NIS2 pipeline skipped because no NIS2 attributes found on request type.</summary>
    [LoggerMessage(
        EventId = 9201,
        Level = LogLevel.Trace,
        Message = "NIS2 pipeline skipped (no NIS2 attributes). RequestType={RequestType}")]
    internal static partial void NIS2PipelineNoAttributes(this ILogger logger, string requestType);

    /// <summary>NIS2 pipeline started evaluating a request.</summary>
    [LoggerMessage(
        EventId = 9202,
        Level = LogLevel.Debug,
        Message = "NIS2 pipeline started. RequestType={RequestType}, EnforcementMode={EnforcementMode}")]
    internal static partial void NIS2PipelineStarted(this ILogger logger, string requestType, string enforcementMode);

    /// <summary>NIS2 pipeline blocked a request due to compliance check failure.</summary>
    [LoggerMessage(
        EventId = 9203,
        Level = LogLevel.Warning,
        Message = "NIS2 pipeline blocked request. RequestType={RequestType}, CheckType={CheckType}, Reason={Reason}")]
    internal static partial void NIS2PipelineBlocked(this ILogger logger, string requestType, string checkType, string reason);

    /// <summary>NIS2 pipeline issued a warning for a compliance check failure (Warn mode).</summary>
    [LoggerMessage(
        EventId = 9204,
        Level = LogLevel.Warning,
        Message = "NIS2 pipeline warning. RequestType={RequestType}, CheckType={CheckType}, Reason={Reason}")]
    internal static partial void NIS2PipelineWarning(this ILogger logger, string requestType, string checkType, string reason);

    /// <summary>NIS2 pipeline completed with all checks passed.</summary>
    [LoggerMessage(
        EventId = 9205,
        Level = LogLevel.Debug,
        Message = "NIS2 pipeline completed. RequestType={RequestType}, ChecksPerformed={ChecksPerformed}")]
    internal static partial void NIS2PipelineCompleted(this ILogger logger, string requestType, int checksPerformed);

    /// <summary>Exception occurred in the NIS2 pipeline.</summary>
    [LoggerMessage(
        EventId = 9206,
        Level = LogLevel.Error,
        Message = "NIS2 pipeline error. RequestType={RequestType}")]
    internal static partial void NIS2PipelineError(this ILogger logger, string requestType, Exception exception);

    /// <summary>NIS2 pipeline critical infrastructure operation executing with enhanced monitoring.</summary>
    [LoggerMessage(
        EventId = 9207,
        Level = LogLevel.Information,
        Message = "NIS2 critical infrastructure operation. RequestType={RequestType}, Description={Description}")]
    internal static partial void NIS2PipelineCriticalOperation(this ILogger logger, string requestType, string description);

    /// <summary>NIS2 pipeline audit recording failed (fire-and-forget, non-blocking).</summary>
    [LoggerMessage(
        EventId = 9208,
        Level = LogLevel.Warning,
        Message = "NIS2 pipeline audit recording failed. RequestType={RequestType}, ErrorMessage={ErrorMessage}")]
    internal static partial void NIS2PipelineAuditFailed(this ILogger logger, string requestType, string errorMessage);

    /// <summary>NIS2 pipeline audit recording exception (fire-and-forget, non-blocking).</summary>
    [LoggerMessage(
        EventId = 9209,
        Level = LogLevel.Warning,
        Message = "NIS2 pipeline audit recording exception. RequestType={RequestType}")]
    internal static partial void NIS2PipelineAuditException(this ILogger logger, string requestType, Exception exception);

    // ========================================================================
    // Compliance validation log messages (9210-9219)
    // ========================================================================

    /// <summary>Aggregate NIS2 compliance validation started.</summary>
    [LoggerMessage(
        EventId = 9210,
        Level = LogLevel.Debug,
        Message = "NIS2 compliance validation started. EntityType={EntityType}, Sector={Sector}, EvaluatorCount={EvaluatorCount}")]
    internal static partial void ComplianceValidationStarted(this ILogger logger, string entityType, string sector, int evaluatorCount);

    /// <summary>Aggregate NIS2 compliance validation completed.</summary>
    [LoggerMessage(
        EventId = 9211,
        Level = LogLevel.Information,
        Message = "NIS2 compliance validation completed. IsCompliant={IsCompliant}, Satisfied={Satisfied}, Missing={Missing}, Percentage={Percentage}")]
    internal static partial void ComplianceValidationCompleted(this ILogger logger, bool isCompliant, int satisfied, int missing, int percentage);

    /// <summary>Individual NIS2 measure evaluation completed.</summary>
    [LoggerMessage(
        EventId = 9212,
        Level = LogLevel.Debug,
        Message = "NIS2 measure evaluated. Measure={Measure}, IsSatisfied={IsSatisfied}, Details={Details}")]
    internal static partial void MeasureEvaluated(this ILogger logger, string measure, bool isSatisfied, string details);

    /// <summary>NIS2 measure evaluation failed with an error.</summary>
    [LoggerMessage(
        EventId = 9213,
        Level = LogLevel.Warning,
        Message = "NIS2 measure evaluation failed. Measure={Measure}")]
    internal static partial void MeasureEvaluationFailed(this ILogger logger, string measure, Exception exception);

    /// <summary>NIS2 compliance validation failed with an error.</summary>
    [LoggerMessage(
        EventId = 9214,
        Level = LogLevel.Error,
        Message = "NIS2 compliance validation error")]
    internal static partial void ComplianceValidationError(this ILogger logger, Exception exception);

    /// <summary>NIS2 missing requirements query completed.</summary>
    [LoggerMessage(
        EventId = 9215,
        Level = LogLevel.Debug,
        Message = "NIS2 missing requirements queried. MissingCount={MissingCount}")]
    internal static partial void MissingRequirementsQueried(this ILogger logger, int missingCount);

    // ========================================================================
    // MFA enforcement log messages (9220-9229)
    // ========================================================================

    /// <summary>MFA check started for a user.</summary>
    [LoggerMessage(
        EventId = 9220,
        Level = LogLevel.Debug,
        Message = "NIS2 MFA check started. UserId={UserId}")]
    internal static partial void MFACheckStarted(this ILogger logger, string userId);

    /// <summary>MFA check passed — user has MFA enabled.</summary>
    [LoggerMessage(
        EventId = 9221,
        Level = LogLevel.Debug,
        Message = "NIS2 MFA check passed. UserId={UserId}")]
    internal static partial void MFACheckPassed(this ILogger logger, string userId);

    /// <summary>MFA check failed — user does not have MFA enabled.</summary>
    [LoggerMessage(
        EventId = 9222,
        Level = LogLevel.Warning,
        Message = "NIS2 MFA check failed. UserId={UserId}, RequestType={RequestType}")]
    internal static partial void MFACheckFailed(this ILogger logger, string userId, string requestType);

    /// <summary>MFA enforcement returned pass-through (default implementation).</summary>
    [LoggerMessage(
        EventId = 9223,
        Level = LogLevel.Trace,
        Message = "NIS2 MFA enforcement pass-through (default implementation). RequestType={RequestType}")]
    internal static partial void MFAPassThrough(this ILogger logger, string requestType);

    // ========================================================================
    // Encryption validation log messages (9230-9239)
    // ========================================================================

    /// <summary>Encryption at-rest validation performed.</summary>
    [LoggerMessage(
        EventId = 9230,
        Level = LogLevel.Debug,
        Message = "NIS2 encryption at-rest check. DataCategory={DataCategory}, IsEncrypted={IsEncrypted}")]
    internal static partial void EncryptionAtRestChecked(this ILogger logger, string dataCategory, bool isEncrypted);

    /// <summary>Encryption in-transit validation performed.</summary>
    [LoggerMessage(
        EventId = 9231,
        Level = LogLevel.Debug,
        Message = "NIS2 encryption in-transit check. Endpoint={Endpoint}, IsEncrypted={IsEncrypted}")]
    internal static partial void EncryptionInTransitChecked(this ILogger logger, string endpoint, bool isEncrypted);

    /// <summary>Encryption policy validation performed.</summary>
    [LoggerMessage(
        EventId = 9232,
        Level = LogLevel.Debug,
        Message = "NIS2 encryption policy check. PolicyCompliant={PolicyCompliant}")]
    internal static partial void EncryptionPolicyChecked(this ILogger logger, bool policyCompliant);

    /// <summary>Encryption validation failed with an error.</summary>
    [LoggerMessage(
        EventId = 9233,
        Level = LogLevel.Warning,
        Message = "NIS2 encryption validation error. CheckType={CheckType}")]
    internal static partial void EncryptionValidationError(this ILogger logger, string checkType, Exception exception);

    // ========================================================================
    // Supply chain log messages (9240-9249)
    // ========================================================================

    /// <summary>Supply chain supplier assessment started.</summary>
    [LoggerMessage(
        EventId = 9240,
        Level = LogLevel.Debug,
        Message = "NIS2 supplier assessment started. SupplierId={SupplierId}")]
    internal static partial void SupplierAssessmentStarted(this ILogger logger, string supplierId);

    /// <summary>Supply chain supplier assessment completed.</summary>
    [LoggerMessage(
        EventId = 9241,
        Level = LogLevel.Information,
        Message = "NIS2 supplier assessment completed. SupplierId={SupplierId}, OverallRisk={OverallRisk}, RiskCount={RiskCount}")]
    internal static partial void SupplierAssessmentCompleted(this ILogger logger, string supplierId, string overallRisk, int riskCount);

    /// <summary>Supply chain supplier validation for an operation.</summary>
    [LoggerMessage(
        EventId = 9242,
        Level = LogLevel.Debug,
        Message = "NIS2 supplier validation. SupplierId={SupplierId}, IsAcceptable={IsAcceptable}")]
    internal static partial void SupplierValidated(this ILogger logger, string supplierId, bool isAcceptable);

    /// <summary>Supply chain supplier not found in configuration.</summary>
    [LoggerMessage(
        EventId = 9243,
        Level = LogLevel.Warning,
        Message = "NIS2 supplier not found. SupplierId={SupplierId}")]
    internal static partial void SupplierNotFound(this ILogger logger, string supplierId);

    /// <summary>Supply chain risk assessment returned supplier risks.</summary>
    [LoggerMessage(
        EventId = 9244,
        Level = LogLevel.Debug,
        Message = "NIS2 supplier risks retrieved. TotalSuppliers={TotalSuppliers}, HighRiskCount={HighRiskCount}")]
    internal static partial void SupplierRisksRetrieved(this ILogger logger, int totalSuppliers, int highRiskCount);

    /// <summary>Supply chain assessment error.</summary>
    [LoggerMessage(
        EventId = 9245,
        Level = LogLevel.Error,
        Message = "NIS2 supply chain assessment error. SupplierId={SupplierId}")]
    internal static partial void SupplyChainAssessmentError(this ILogger logger, string supplierId, Exception exception);

    // ========================================================================
    // Incident handling log messages (9250-9259)
    // ========================================================================

    /// <summary>NIS2 incident report submitted.</summary>
    [LoggerMessage(
        EventId = 9250,
        Level = LogLevel.Information,
        Message = "NIS2 incident reported. IncidentId={IncidentId}, Severity={Severity}, IsSignificant={IsSignificant}")]
    internal static partial void IncidentReported(this ILogger logger, string incidentId, string severity, bool isSignificant);

    /// <summary>NIS2 incident notification deadline checked.</summary>
    [LoggerMessage(
        EventId = 9251,
        Level = LogLevel.Debug,
        Message = "NIS2 incident deadline check. IncidentId={IncidentId}, Phase={Phase}, IsWithinDeadline={IsWithinDeadline}")]
    internal static partial void IncidentDeadlineChecked(this ILogger logger, string incidentId, string phase, bool isWithinDeadline);

    /// <summary>NIS2 incident notification deadline exceeded.</summary>
    [LoggerMessage(
        EventId = 9252,
        Level = LogLevel.Warning,
        Message = "NIS2 incident deadline exceeded. IncidentId={IncidentId}, Phase={Phase}, HoursOverdue={HoursOverdue}")]
    internal static partial void IncidentDeadlineExceeded(this ILogger logger, string incidentId, string phase, double hoursOverdue);

    /// <summary>NIS2 incident next deadline calculated.</summary>
    [LoggerMessage(
        EventId = 9253,
        Level = LogLevel.Debug,
        Message = "NIS2 incident next deadline. IncidentId={IncidentId}, NextPhase={NextPhase}, Deadline={Deadline}")]
    internal static partial void IncidentNextDeadline(this ILogger logger, string incidentId, string nextPhase, string deadline);

    /// <summary>NIS2 incident report delegated to breach notification service.</summary>
    [LoggerMessage(
        EventId = 9254,
        Level = LogLevel.Information,
        Message = "NIS2 incident delegated to breach notification. IncidentId={IncidentId}")]
    internal static partial void IncidentDelegatedToBreachNotification(this ILogger logger, string incidentId);

    /// <summary>NIS2 incident handling error.</summary>
    [LoggerMessage(
        EventId = 9255,
        Level = LogLevel.Error,
        Message = "NIS2 incident handling error. IncidentId={IncidentId}")]
    internal static partial void IncidentHandlingError(this ILogger logger, string incidentId, Exception exception);

    /// <summary>NIS2 all notification phases complete for incident.</summary>
    [LoggerMessage(
        EventId = 9256,
        Level = LogLevel.Information,
        Message = "NIS2 all notification phases complete. IncidentId={IncidentId}")]
    internal static partial void IncidentAllPhasesComplete(this ILogger logger, string incidentId);

    // ========================================================================
    // Health check log messages (9260-9269)
    // ========================================================================

    /// <summary>NIS2 compliance health check completed.</summary>
    [LoggerMessage(
        EventId = 9260,
        Level = LogLevel.Debug,
        Message = "NIS2 health check completed. Status={Status}, CompliancePercentage={CompliancePercentage}")]
    internal static partial void HealthCheckCompleted(this ILogger logger, string status, int compliancePercentage);

    /// <summary>NIS2 compliance health check degraded — partial compliance.</summary>
    [LoggerMessage(
        EventId = 9261,
        Level = LogLevel.Warning,
        Message = "NIS2 health check degraded. CompliancePercentage={CompliancePercentage}, MissingCount={MissingCount}")]
    internal static partial void HealthCheckDegraded(this ILogger logger, int compliancePercentage, int missingCount);

    /// <summary>NIS2 compliance health check failed with error.</summary>
    [LoggerMessage(
        EventId = 9262,
        Level = LogLevel.Error,
        Message = "NIS2 health check error")]
    internal static partial void HealthCheckError(this ILogger logger, Exception exception);

    // ========================================================================
    // Management accountability log messages (9270-9279)
    // ========================================================================

    /// <summary>NIS2 management accountability check — accountability record present.</summary>
    [LoggerMessage(
        EventId = 9270,
        Level = LogLevel.Debug,
        Message = "NIS2 management accountability verified. ResponsiblePerson={ResponsiblePerson}, Role={Role}")]
    internal static partial void ManagementAccountabilityVerified(this ILogger logger, string responsiblePerson, string role);

    /// <summary>NIS2 management accountability missing — Article 20 requires management body oversight.</summary>
    [LoggerMessage(
        EventId = 9271,
        Level = LogLevel.Warning,
        Message = "NIS2 management accountability missing. Article 20 requires management body oversight.")]
    internal static partial void ManagementAccountabilityMissing(this ILogger logger);

    /// <summary>NIS2 management training status check.</summary>
    [LoggerMessage(
        EventId = 9272,
        Level = LogLevel.Debug,
        Message = "NIS2 management training status. ResponsiblePerson={ResponsiblePerson}, TrainingCompleted={TrainingCompleted}")]
    internal static partial void ManagementTrainingStatus(this ILogger logger, string responsiblePerson, bool trainingCompleted);

    // ========================================================================
    // Cross-cutting integration log messages (9280-9299)
    // ========================================================================

    /// <summary>NIS2 incident forwarded to BreachNotificationService for persistent tracking.</summary>
    [LoggerMessage(
        EventId = 9280,
        Level = LogLevel.Information,
        Message = "NIS2 incident forwarded to BreachNotificationService. IncidentId={IncidentId}, BreachId={BreachId}")]
    internal static partial void IncidentForwardedToBreachNotification(this ILogger logger, string incidentId, string breachId);

    /// <summary>NIS2 incident forwarding to BreachNotificationService failed (non-blocking).</summary>
    [LoggerMessage(
        EventId = 9281,
        Level = LogLevel.Warning,
        Message = "NIS2 incident breach forwarding failed. IncidentId={IncidentId}, Reason={Reason}")]
    internal static partial void IncidentBreachForwardingFailed(this ILogger logger, string incidentId, string reason);

    /// <summary>NIS2 encryption infrastructure validation performed via IKeyProvider.</summary>
    [LoggerMessage(
        EventId = 9282,
        Level = LogLevel.Debug,
        Message = "NIS2 encryption infrastructure check. HasActiveKey={HasActiveKey}")]
    internal static partial void EncryptionInfrastructureChecked(this ILogger logger, bool hasActiveKey);

    /// <summary>NIS2 ABAC policy evaluation performed for access control measure.</summary>
    [LoggerMessage(
        EventId = 9283,
        Level = LogLevel.Debug,
        Message = "NIS2 ABAC check. HasPDP={HasPDP}, Effect={Effect}")]
    internal static partial void ABACPolicyChecked(this ILogger logger, bool hasPDP, string effect);

    /// <summary>NIS2 compliance result served from cache.</summary>
    [LoggerMessage(
        EventId = 9284,
        Level = LogLevel.Debug,
        Message = "NIS2 compliance result cache hit. CacheKey={CacheKey}")]
    internal static partial void ComplianceCacheHit(this ILogger logger, string cacheKey);

    /// <summary>NIS2 compliance result stored in cache.</summary>
    [LoggerMessage(
        EventId = 9285,
        Level = LogLevel.Debug,
        Message = "NIS2 compliance result cached. CacheKey={CacheKey}, TTL={TTLMinutes}min")]
    internal static partial void ComplianceResultCached(this ILogger logger, string cacheKey, int ttlMinutes);

    /// <summary>NIS2 GDPR alignment check performed.</summary>
    [LoggerMessage(
        EventId = 9286,
        Level = LogLevel.Debug,
        Message = "NIS2-GDPR alignment check. HasGDPRValidator={HasGDPRValidator}, IsGDPRCompliant={IsGDPRCompliant}")]
    internal static partial void GDPRAlignmentChecked(this ILogger logger, bool hasGDPRValidator, bool isGDPRCompliant);

    /// <summary>NIS2 GDPR alignment check failed (non-blocking).</summary>
    [LoggerMessage(
        EventId = 9287,
        Level = LogLevel.Warning,
        Message = "NIS2-GDPR alignment check error")]
    internal static partial void GDPRAlignmentError(this ILogger logger, Exception exception);
}
