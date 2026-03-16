using Microsoft.Extensions.Logging;

namespace Encina.Compliance.DPIA.Diagnostics;

/// <summary>
/// High-performance structured log messages for the DPIA module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8800-8899 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8299, DSR uses 8300-8399,
/// Anonymization uses 8400-8499, Retention uses 8500-8599, DataResidency uses 8600-8699,
/// BreachNotification uses 8700-8799).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>8800-8809</term><description>Pipeline behavior</description></item>
/// <item><term>8810-8819</term><description>Auto-detection</description></item>
/// <item><term>8820-8829</term><description>Auto-registration</description></item>
/// <item><term>8830-8839</term><description>Assessment engine</description></item>
/// <item><term>8840-8849</term><description>Expiration monitoring</description></item>
/// <item><term>8850-8859</term><description>Health check</description></item>
/// <item><term>8860-8869</term><description>Event sourcing</description></item>
/// <item><term>8870-8879</term><description>Service operations</description></item>
/// <item><term>8880-8889</term><description>DPO consultation</description></item>
/// <item><term>8890-8899</term><description>ASP.NET Core endpoints</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class DPIALogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (8800-8809)
    // ========================================================================

    /// <summary>DPIA pipeline skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 8800,
        Level = LogLevel.Trace,
        Message = "DPIA pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void DPIAPipelineDisabled(this ILogger logger, string requestType);

    /// <summary>DPIA pipeline skipped because no [RequiresDPIA] attribute found on request type.</summary>
    [LoggerMessage(
        EventId = 8801,
        Level = LogLevel.Trace,
        Message = "DPIA pipeline skipped (no [RequiresDPIA] attribute). RequestType={RequestType}")]
    internal static partial void DPIAPipelineNoAttribute(this ILogger logger, string requestType);

    /// <summary>DPIA pipeline check started.</summary>
    [LoggerMessage(
        EventId = 8802,
        Level = LogLevel.Debug,
        Message = "DPIA pipeline check started. RequestType={RequestType}, EnforcementMode={EnforcementMode}")]
    internal static partial void DPIAPipelineStarted(this ILogger logger, string requestType, string enforcementMode);

    /// <summary>DPIA pipeline check passed — assessment is current and approved.</summary>
    [LoggerMessage(
        EventId = 8803,
        Level = LogLevel.Debug,
        Message = "DPIA pipeline check passed. RequestType={RequestType}, AssessmentId={AssessmentId}")]
    internal static partial void DPIAPipelinePassed(this ILogger logger, string requestType, Guid assessmentId);

    /// <summary>DPIA pipeline check failed — no assessment exists for the request type.</summary>
    [LoggerMessage(
        EventId = 8804,
        Level = LogLevel.Warning,
        Message = "DPIA pipeline check failed (no assessment). RequestType={RequestType}")]
    internal static partial void DPIAPipelineNoAssessment(this ILogger logger, string requestType);

    /// <summary>DPIA pipeline check failed — assessment exists but is expired.</summary>
    [LoggerMessage(
        EventId = 8805,
        Level = LogLevel.Warning,
        Message = "DPIA pipeline check failed (assessment expired). RequestType={RequestType}, AssessmentId={AssessmentId}, ExpiredAt={ExpiredAt}")]
    internal static partial void DPIAPipelineExpired(this ILogger logger, string requestType, Guid assessmentId, DateTimeOffset? expiredAt);

    /// <summary>DPIA pipeline check failed — assessment exists but is not approved.</summary>
    [LoggerMessage(
        EventId = 8806,
        Level = LogLevel.Warning,
        Message = "DPIA pipeline check failed (assessment not approved). RequestType={RequestType}, AssessmentId={AssessmentId}, Status={Status}")]
    internal static partial void DPIAPipelineNotApproved(this ILogger logger, string requestType, Guid assessmentId, string status);

    /// <summary>DPIA pipeline blocked the request in Block enforcement mode.</summary>
    [LoggerMessage(
        EventId = 8807,
        Level = LogLevel.Warning,
        Message = "DPIA pipeline blocked request. RequestType={RequestType}, Reason={Reason}")]
    internal static partial void DPIAPipelineBlocked(this ILogger logger, string requestType, string reason);

    /// <summary>DPIA pipeline issued a warning in Warn enforcement mode but allowed the request.</summary>
    [LoggerMessage(
        EventId = 8808,
        Level = LogLevel.Warning,
        Message = "DPIA pipeline warning (request allowed). RequestType={RequestType}, Reason={Reason}")]
    internal static partial void DPIAPipelineWarned(this ILogger logger, string requestType, string reason);

    /// <summary>Exception occurred in the DPIA pipeline.</summary>
    [LoggerMessage(
        EventId = 8809,
        Level = LogLevel.Error,
        Message = "DPIA pipeline error. RequestType={RequestType}")]
    internal static partial void DPIAPipelineError(this ILogger logger, string requestType, Exception exception);

    // ========================================================================
    // Auto-detection log messages (8810-8819)
    // ========================================================================

    /// <summary>Auto-detection identified a request type as high-risk.</summary>
    [LoggerMessage(
        EventId = 8810,
        Level = LogLevel.Information,
        Message = "DPIA auto-detection: high-risk processing identified. RequestType={RequestType}, Triggers={Triggers}")]
    internal static partial void AutoDetectionHighRisk(this ILogger logger, string requestType, string triggers);

    /// <summary>Auto-detection determined a request type is not high-risk.</summary>
    [LoggerMessage(
        EventId = 8811,
        Level = LogLevel.Trace,
        Message = "DPIA auto-detection: not high-risk. RequestType={RequestType}")]
    internal static partial void AutoDetectionNotHighRisk(this ILogger logger, string requestType);

    // ========================================================================
    // Auto-registration log messages (8820-8829)
    // ========================================================================

    /// <summary>Auto-registration started scanning assemblies.</summary>
    [LoggerMessage(
        EventId = 8820,
        Level = LogLevel.Information,
        Message = "DPIA auto-registration started. AssemblyCount={AssemblyCount}")]
    internal static partial void AutoRegistrationStarted(this ILogger logger, int assemblyCount);

    /// <summary>Auto-registration completed.</summary>
    [LoggerMessage(
        EventId = 8821,
        Level = LogLevel.Information,
        Message = "DPIA auto-registration completed. RegisteredCount={RegisteredCount}, SkippedCount={SkippedCount}")]
    internal static partial void AutoRegistrationCompleted(this ILogger logger, int registeredCount, int skippedCount);

    /// <summary>Auto-registration created a draft assessment for a request type.</summary>
    [LoggerMessage(
        EventId = 8822,
        Level = LogLevel.Debug,
        Message = "DPIA auto-registration: draft assessment created. RequestType={RequestType}, AssessmentId={AssessmentId}")]
    internal static partial void AutoRegistrationDraftCreated(this ILogger logger, string requestType, Guid assessmentId);

    /// <summary>Auto-registration skipped a request type because an assessment already exists.</summary>
    [LoggerMessage(
        EventId = 8823,
        Level = LogLevel.Debug,
        Message = "DPIA auto-registration: skipped (assessment exists). RequestType={RequestType}")]
    internal static partial void AutoRegistrationSkipped(this ILogger logger, string requestType);

    /// <summary>Auto-registration failed for a request type.</summary>
    [LoggerMessage(
        EventId = 8824,
        Level = LogLevel.Warning,
        Message = "DPIA auto-registration: failed to create draft. RequestType={RequestType}")]
    internal static partial void AutoRegistrationFailed(this ILogger logger, string requestType, Exception exception);

    // ========================================================================
    // Assessment engine log messages (8830-8839)
    // ========================================================================

    /// <summary>DPIA risk assessment started for a request type.</summary>
    [LoggerMessage(
        EventId = 8830,
        Level = LogLevel.Debug,
        Message = "DPIA assessment started. RequestType={RequestType}, CriteriaCount={CriteriaCount}")]
    internal static partial void AssessmentStarted(this ILogger logger, string requestType, int criteriaCount);

    /// <summary>DPIA risk assessment completed successfully.</summary>
    [LoggerMessage(
        EventId = 8831,
        Level = LogLevel.Information,
        Message = "DPIA assessment completed. RequestType={RequestType}, OverallRisk={OverallRisk}, Risks={RiskCount}, Mitigations={MitigationCount}, RequiresPriorConsultation={RequiresPriorConsultation}")]
    internal static partial void AssessmentCompleted(this ILogger logger, string requestType, string overallRisk, int riskCount, int mitigationCount, bool requiresPriorConsultation);

    /// <summary>Risk criterion triggered during assessment.</summary>
    [LoggerMessage(
        EventId = 8832,
        Level = LogLevel.Debug,
        Message = "DPIA risk criterion triggered. Criterion={CriterionName}, Category={Category}, Level={Level}")]
    internal static partial void CriterionTriggered(this ILogger logger, string criterionName, string category, string level);

    /// <summary>Risk criterion threw an exception during evaluation.</summary>
    [LoggerMessage(
        EventId = 8833,
        Level = LogLevel.Warning,
        Message = "DPIA risk criterion failed during evaluation. Criterion={CriterionName}")]
    internal static partial void CriterionFailed(this ILogger logger, string criterionName, Exception exception);

    /// <summary>DPIA template resolved for assessment context.</summary>
    [LoggerMessage(
        EventId = 8834,
        Level = LogLevel.Debug,
        Message = "DPIA template resolved. ProcessingType={ProcessingType}, TemplateName={TemplateName}")]
    internal static partial void TemplateResolved(this ILogger logger, string processingType, string templateName);

    /// <summary>Prior supervisory authority consultation is required (Article 36).</summary>
    [LoggerMessage(
        EventId = 8835,
        Level = LogLevel.Warning,
        Message = "DPIA assessment requires prior consultation (Art. 36). RequestType={RequestType}, OverallRisk={OverallRisk}")]
    internal static partial void PriorConsultationRequired(this ILogger logger, string requestType, string overallRisk);

    // ========================================================================
    // Expiration monitoring / review reminder log messages (8840-8849)
    // ========================================================================

    /// <summary>Review reminder service started.</summary>
    [LoggerMessage(
        EventId = 8840,
        Level = LogLevel.Information,
        Message = "DPIA review reminder service started. CheckInterval={CheckInterval}")]
    internal static partial void ReviewReminderStarted(this ILogger logger, TimeSpan checkInterval);

    /// <summary>Review reminder service is disabled.</summary>
    [LoggerMessage(
        EventId = 8841,
        Level = LogLevel.Information,
        Message = "DPIA review reminder service is disabled (EnableExpirationMonitoring=false).")]
    internal static partial void ReviewReminderDisabled(this ILogger logger);

    /// <summary>Review reminder cycle started.</summary>
    [LoggerMessage(
        EventId = 8842,
        Level = LogLevel.Debug,
        Message = "DPIA review reminder cycle starting.")]
    internal static partial void ReviewReminderCycleStarting(this ILogger logger);

    /// <summary>Review reminder cycle completed with expired assessments found.</summary>
    [LoggerMessage(
        EventId = 8843,
        Level = LogLevel.Warning,
        Message = "DPIA review reminder: {ExpiredCount} expired assessment(s) require review.")]
    internal static partial void ReviewReminderExpiredFound(this ILogger logger, int expiredCount);

    /// <summary>Review reminder cycle completed with no expired assessments.</summary>
    [LoggerMessage(
        EventId = 8844,
        Level = LogLevel.Debug,
        Message = "DPIA review reminder cycle completed. No expired assessments found.")]
    internal static partial void ReviewReminderCycleEmpty(this ILogger logger);

    /// <summary>Review reminder cycle was cancelled.</summary>
    [LoggerMessage(
        EventId = 8845,
        Level = LogLevel.Information,
        Message = "DPIA review reminder cycle was cancelled.")]
    internal static partial void ReviewReminderCycleCancelled(this ILogger logger);

    /// <summary>Review reminder cycle failed with an exception.</summary>
    [LoggerMessage(
        EventId = 8846,
        Level = LogLevel.Error,
        Message = "DPIA review reminder cycle failed.")]
    internal static partial void ReviewReminderCycleFailed(this ILogger logger, Exception exception);

    /// <summary>Individual expired assessment detected during review reminder cycle.</summary>
    [LoggerMessage(
        EventId = 8847,
        Level = LogLevel.Warning,
        Message = "DPIA assessment expired: RequestType={RequestType}, AssessmentId={AssessmentId}, NextReviewAtUtc={NextReviewAtUtc}")]
    internal static partial void ReviewReminderAssessmentExpired(this ILogger logger, string requestType, Guid assessmentId, DateTimeOffset? nextReviewAtUtc);

    // ========================================================================
    // Event sourcing log messages (8860-8869)
    // ========================================================================

    /// <summary>Aggregate loaded from event store.</summary>
    [LoggerMessage(
        EventId = 8860,
        Level = LogLevel.Debug,
        Message = "DPIA aggregate loaded from event store. AssessmentId={AssessmentId}, Version={Version}")]
    internal static partial void AggregateLoaded(this ILogger logger, Guid assessmentId, long version);

    /// <summary>Aggregate events saved to event store.</summary>
    [LoggerMessage(
        EventId = 8861,
        Level = LogLevel.Debug,
        Message = "DPIA aggregate events saved. AssessmentId={AssessmentId}, EventCount={EventCount}")]
    internal static partial void AggregateSaved(this ILogger logger, Guid assessmentId, int eventCount);

    /// <summary>Event history retrieved for an assessment.</summary>
    [LoggerMessage(
        EventId = 8862,
        Level = LogLevel.Debug,
        Message = "DPIA event history retrieved. AssessmentId={AssessmentId}, EventCount={EventCount}")]
    internal static partial void EventHistoryRetrieved(this ILogger logger, Guid assessmentId, int eventCount);

    /// <summary>Read model cache invalidated for a request type.</summary>
    [LoggerMessage(
        EventId = 8863,
        Level = LogLevel.Debug,
        Message = "DPIA read model cache invalidated. RequestType={RequestType}")]
    internal static partial void CacheInvalidated(this ILogger logger, string requestType);

    // ========================================================================
    // Service operation log messages (8870-8879)
    // ========================================================================

    /// <summary>Assessment created successfully via the DPIA service.</summary>
    [LoggerMessage(
        EventId = 8870,
        Level = LogLevel.Information,
        Message = "DPIA assessment created. AssessmentId={AssessmentId}, RequestType={RequestType}")]
    internal static partial void AssessmentCreated(this ILogger logger, Guid assessmentId, string requestType);

    /// <summary>Assessment evaluated successfully via the DPIA service.</summary>
    [LoggerMessage(
        EventId = 8871,
        Level = LogLevel.Information,
        Message = "DPIA assessment evaluated. AssessmentId={AssessmentId}, OverallRisk={OverallRisk}")]
    internal static partial void AssessmentEvaluated(this ILogger logger, Guid assessmentId, string overallRisk);

    /// <summary>Assessment approved via the DPIA service.</summary>
    [LoggerMessage(
        EventId = 8872,
        Level = LogLevel.Information,
        Message = "DPIA assessment approved. AssessmentId={AssessmentId}, ApprovedBy={ApprovedBy}")]
    internal static partial void AssessmentApproved(this ILogger logger, Guid assessmentId, string approvedBy);

    /// <summary>Assessment rejected via the DPIA service.</summary>
    [LoggerMessage(
        EventId = 8873,
        Level = LogLevel.Information,
        Message = "DPIA assessment rejected. AssessmentId={AssessmentId}, RejectedBy={RejectedBy}")]
    internal static partial void AssessmentRejected(this ILogger logger, Guid assessmentId, string rejectedBy);

    /// <summary>Assessment revision requested via the DPIA service.</summary>
    [LoggerMessage(
        EventId = 8874,
        Level = LogLevel.Information,
        Message = "DPIA assessment revision requested. AssessmentId={AssessmentId}, RequestedBy={RequestedBy}")]
    internal static partial void AssessmentRevisionRequested(this ILogger logger, Guid assessmentId, string requestedBy);

    /// <summary>Assessment expired via the DPIA service.</summary>
    [LoggerMessage(
        EventId = 8875,
        Level = LogLevel.Information,
        Message = "DPIA assessment expired. AssessmentId={AssessmentId}")]
    internal static partial void AssessmentExpired(this ILogger logger, Guid assessmentId);

    /// <summary>DPIA service store operation failed.</summary>
    [LoggerMessage(
        EventId = 8876,
        Level = LogLevel.Error,
        Message = "DPIA service operation failed. Operation={Operation}")]
    internal static partial void ServiceOperationError(this ILogger logger, string operation, Exception exception);

    /// <summary>DPO response recorded via the DPIA service.</summary>
    [LoggerMessage(
        EventId = 8877,
        Level = LogLevel.Information,
        Message = "DPO response recorded. AssessmentId={AssessmentId}, ConsultationId={ConsultationId}, Decision={Decision}")]
    internal static partial void DPOResponseRecorded(this ILogger logger, Guid assessmentId, Guid consultationId, string decision);

    // ========================================================================
    // DPO consultation log messages (8880-8889)
    // ========================================================================

    /// <summary>DPO consultation request started.</summary>
    [LoggerMessage(
        EventId = 8880,
        Level = LogLevel.Information,
        Message = "DPO consultation requested. AssessmentId={AssessmentId}")]
    internal static partial void DPOConsultationStarted(this ILogger logger, Guid assessmentId);

    /// <summary>DPO consultation completed successfully.</summary>
    [LoggerMessage(
        EventId = 8881,
        Level = LogLevel.Information,
        Message = "DPO consultation created. AssessmentId={AssessmentId}, ConsultationId={ConsultationId}, DPOEmail={DPOEmail}")]
    internal static partial void DPOConsultationCreated(this ILogger logger, Guid assessmentId, Guid consultationId, string dpoEmail);

    /// <summary>DPO consultation failed — no DPO configured.</summary>
    [LoggerMessage(
        EventId = 8882,
        Level = LogLevel.Warning,
        Message = "DPO consultation failed (no DPO configured). AssessmentId={AssessmentId}")]
    internal static partial void DPOConsultationNoDPO(this ILogger logger, Guid assessmentId);

    /// <summary>DPO contact resolved from configured options or GDPR module.</summary>
    [LoggerMessage(
        EventId = 8883,
        Level = LogLevel.Debug,
        Message = "DPO contact resolved. Source={Source}, DPOName={DPOName}, DPOEmail={DPOEmail}")]
    internal static partial void DPOContactResolved(this ILogger logger, string source, string? dpoName, string? dpoEmail);

    // ========================================================================
    // ASP.NET Core endpoint log messages (8890-8899)
    // ========================================================================

    /// <summary>DPIA endpoint request received.</summary>
    [LoggerMessage(
        EventId = 8890,
        Level = LogLevel.Debug,
        Message = "DPIA endpoint request. Endpoint={Endpoint}, Method={Method}, Path={Path}")]
    internal static partial void EndpointRequestReceived(this ILogger logger, string endpoint, string method, string path);

    /// <summary>DPIA endpoint request completed.</summary>
    [LoggerMessage(
        EventId = 8891,
        Level = LogLevel.Debug,
        Message = "DPIA endpoint completed. Endpoint={Endpoint}, StatusCode={StatusCode}, Duration={Duration}ms")]
    internal static partial void EndpointRequestCompleted(this ILogger logger, string endpoint, int statusCode, double duration);

    /// <summary>DPIA endpoint request failed with an error.</summary>
    [LoggerMessage(
        EventId = 8892,
        Level = LogLevel.Warning,
        Message = "DPIA endpoint failed. Endpoint={Endpoint}, StatusCode={StatusCode}, Duration={Duration}ms")]
    internal static partial void EndpointRequestFailed(this ILogger logger, string endpoint, int statusCode, double duration);

    /// <summary>DPIA assess endpoint triggered.</summary>
    [LoggerMessage(
        EventId = 8893,
        Level = LogLevel.Information,
        Message = "DPIA assessment triggered via endpoint. RequestType={RequestType}")]
    internal static partial void EndpointAssessTriggered(this ILogger logger, string requestType);

    /// <summary>DPIA approve endpoint triggered.</summary>
    [LoggerMessage(
        EventId = 8894,
        Level = LogLevel.Information,
        Message = "DPIA approval triggered via endpoint. AssessmentId={AssessmentId}")]
    internal static partial void EndpointApproveTriggered(this ILogger logger, Guid assessmentId);

    /// <summary>DPIA reject endpoint triggered.</summary>
    [LoggerMessage(
        EventId = 8895,
        Level = LogLevel.Information,
        Message = "DPIA rejection triggered via endpoint. AssessmentId={AssessmentId}")]
    internal static partial void EndpointRejectTriggered(this ILogger logger, Guid assessmentId);
}
