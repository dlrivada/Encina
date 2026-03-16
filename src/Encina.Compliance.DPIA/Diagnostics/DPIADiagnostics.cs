using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace Encina.Compliance.DPIA.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina DPIA compliance observability.
/// </summary>
/// <remarks>
/// <para>
/// Follows the established Encina compliance observability pattern with:
/// <list type="bullet">
/// <item><description><see cref="ActivitySource"/> for OpenTelemetry distributed tracing.</description></item>
/// <item><description><see cref="Meter"/> for OpenTelemetry metrics (counters, histograms).</description></item>
/// </list>
/// </para>
/// <para>
/// Metric names follow the <c>dpia.*</c> prefix convention.
/// Tag names follow the <c>dpia.*</c> prefix convention.
/// </para>
/// </remarks>
internal static class DPIADiagnostics
{
    internal const string SourceName = "Encina.Compliance.DPIA";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ========================================================================
    // Counters
    // ========================================================================

    /// <summary>Total number of DPIA pipeline checks executed.</summary>
    internal static readonly Counter<long> PipelineCheckTotal =
        Meter.CreateCounter<long>("dpia.pipeline.checks.total",
            description: "Total number of DPIA pipeline compliance checks.");

    /// <summary>Number of DPIA pipeline checks that passed (assessment is current).</summary>
    internal static readonly Counter<long> PipelineCheckPassed =
        Meter.CreateCounter<long>("dpia.pipeline.checks.passed",
            description: "Number of DPIA pipeline checks that passed.");

    /// <summary>Number of DPIA pipeline checks that failed (no/expired/rejected assessment).</summary>
    internal static readonly Counter<long> PipelineCheckFailed =
        Meter.CreateCounter<long>("dpia.pipeline.checks.failed",
            description: "Number of DPIA pipeline checks that failed.");

    /// <summary>Number of requests skipped (no [RequiresDPIA] attribute).</summary>
    internal static readonly Counter<long> PipelineCheckSkipped =
        Meter.CreateCounter<long>("dpia.pipeline.checks.skipped",
            description: "Number of requests skipped (no [RequiresDPIA] attribute).");

    /// <summary>Number of draft assessments auto-registered at startup.</summary>
    internal static readonly Counter<long> AutoRegistrationCount =
        Meter.CreateCounter<long>("dpia.auto_registration.total",
            description: "Number of draft DPIA assessments auto-registered at startup.");

    /// <summary>Total number of review reminder cycles executed.</summary>
    internal static readonly Counter<long> ReviewReminderCyclesTotal =
        Meter.CreateCounter<long>("dpia.review_reminder.cycles.total",
            description: "Total number of DPIA review reminder cycles.");

    /// <summary>Number of expired assessments detected by the review reminder.</summary>
    internal static readonly Counter<long> ExpiredAssessmentsDetected =
        Meter.CreateCounter<long>("dpia.review_reminder.expired.total",
            description: "Number of expired DPIA assessments detected by the review reminder.");

    /// <summary>Total number of DPIA risk assessments executed by the engine.</summary>
    internal static readonly Counter<long> AssessmentTotal =
        Meter.CreateCounter<long>("dpia.assessment.total",
            description: "Total number of DPIA risk assessments executed by the engine.");

    /// <summary>Total number of DPO consultations requested.</summary>
    internal static readonly Counter<long> DPOConsultationTotal =
        Meter.CreateCounter<long>("dpia.dpo_consultation.total",
            description: "Total number of DPO consultations requested.");

    /// <summary>Total number of DPIA assessments created via the service.</summary>
    internal static readonly Counter<long> ServiceAssessmentCreated =
        Meter.CreateCounter<long>("dpia.service.assessments.created",
            description: "Total number of DPIA assessments created via the event-sourced service.");

    /// <summary>Total number of DPIA assessments evaluated via the service.</summary>
    internal static readonly Counter<long> ServiceAssessmentEvaluated =
        Meter.CreateCounter<long>("dpia.service.assessments.evaluated",
            description: "Total number of DPIA assessments evaluated via the event-sourced service.");

    /// <summary>Total number of DPIA assessments approved via the service.</summary>
    internal static readonly Counter<long> ServiceAssessmentApproved =
        Meter.CreateCounter<long>("dpia.service.assessments.approved",
            description: "Total number of DPIA assessments approved via the event-sourced service.");

    /// <summary>Total number of DPIA assessments rejected via the service.</summary>
    internal static readonly Counter<long> ServiceAssessmentRejected =
        Meter.CreateCounter<long>("dpia.service.assessments.rejected",
            description: "Total number of DPIA assessments rejected via the event-sourced service.");

    /// <summary>Total number of DPIA assessment revision requests via the service.</summary>
    internal static readonly Counter<long> ServiceRevisionRequested =
        Meter.CreateCounter<long>("dpia.service.assessments.revision_requested",
            description: "Total number of DPIA assessment revision requests via the event-sourced service.");

    /// <summary>Total number of DPIA assessments expired via the service.</summary>
    internal static readonly Counter<long> ServiceAssessmentExpired =
        Meter.CreateCounter<long>("dpia.service.assessments.expired",
            description: "Total number of DPIA assessments expired via the event-sourced service.");

    /// <summary>Total number of DPIA service operation errors.</summary>
    internal static readonly Counter<long> ServiceOperationErrors =
        Meter.CreateCounter<long>("dpia.service.errors.total",
            description: "Total number of DPIA service operation errors.");

    /// <summary>Total number of ASP.NET Core DPIA endpoint requests.</summary>
    internal static readonly Counter<long> EndpointRequestTotal =
        Meter.CreateCounter<long>("dpia.endpoint.requests.total",
            description: "Total number of ASP.NET Core DPIA management endpoint requests.");

    // ========================================================================
    // Histograms
    // ========================================================================

    /// <summary>Duration of DPIA pipeline check in milliseconds.</summary>
    internal static readonly Histogram<double> PipelineCheckDuration =
        Meter.CreateHistogram<double>("dpia.pipeline.check.duration",
            unit: "ms",
            description: "Duration of DPIA pipeline compliance check in milliseconds.");

    /// <summary>Duration of DPIA risk assessment in milliseconds.</summary>
    internal static readonly Histogram<double> AssessmentDuration =
        Meter.CreateHistogram<double>("dpia.assessment.duration",
            unit: "ms",
            description: "Duration of DPIA risk assessment by the engine in milliseconds.");

    /// <summary>Duration of ASP.NET Core DPIA endpoint responses in milliseconds.</summary>
    internal static readonly Histogram<double> EndpointDuration =
        Meter.CreateHistogram<double>("dpia.endpoint.duration",
            unit: "ms",
            description: "Duration of ASP.NET Core DPIA management endpoint responses in milliseconds.");

    // ========================================================================
    // Tag names
    // ========================================================================

    internal const string TagOutcome = "dpia.outcome";
    internal const string TagRequestType = "dpia.request_type";
    internal const string TagFailureReason = "dpia.failure_reason";
    internal const string TagEnforcementMode = "dpia.enforcement_mode";
    internal const string TagAssessmentStatus = "dpia.assessment_status";
    internal const string TagRiskLevel = "dpia.risk_level";
    internal const string TagEndpoint = "dpia.endpoint";
    internal const string TagStatusCode = "dpia.status_code";
    internal const string TagAssessmentId = "dpia.assessment_id";
    internal const string TagCriterionName = "dpia.criterion_name";

    // ========================================================================
    // Activity helpers
    // ========================================================================

    /// <summary>
    /// Starts an activity for a DPIA pipeline check.
    /// </summary>
    /// <param name="requestTypeName">The request type being checked.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartPipelineCheck(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DPIA.PipelineCheck", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    /// <summary>Records a passed outcome on the activity.</summary>
    internal static void RecordPassed(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "passed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>Records a failed outcome on the activity.</summary>
    internal static void RecordFailed(Activity? activity, string failureReason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, failureReason);
        activity?.SetStatus(ActivityStatusCode.Error, failureReason);
    }

    /// <summary>Records a skipped outcome on the activity.</summary>
    internal static void RecordSkipped(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "skipped");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>Records a warned outcome on the activity (Warn enforcement mode).</summary>
    internal static void RecordWarned(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "warned");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Starts an activity for a DPIA review reminder cycle.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartReviewReminderCycle()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("DPIA.ReviewReminderCycle", ActivityKind.Internal);
    }

    /// <summary>
    /// Starts an activity for a DPIA risk assessment execution.
    /// </summary>
    /// <param name="requestTypeName">The request type being assessed.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartAssessment(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DPIA.Assessment", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    /// <summary>
    /// Starts an activity for a DPO consultation request.
    /// </summary>
    /// <param name="assessmentId">The assessment ID that requires DPO consultation.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartDPOConsultation(Guid assessmentId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DPIA.DPOConsultation", ActivityKind.Internal);
        activity?.SetTag(TagAssessmentId, assessmentId.ToString("D"));
        return activity;
    }

    /// <summary>
    /// Starts an activity for a DPIA management endpoint request.
    /// </summary>
    /// <param name="endpointName">The endpoint being invoked (e.g., "list", "assess", "approve").</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartEndpointExecution(string endpointName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DPIA.Endpoint", ActivityKind.Server);
        activity?.SetTag(TagEndpoint, endpointName);
        return activity;
    }

    /// <summary>Records a completed assessment outcome on the activity.</summary>
    internal static void RecordAssessmentCompleted(Activity? activity, string riskLevel)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetTag(TagRiskLevel, riskLevel);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>Records a failed assessment outcome on the activity.</summary>
    internal static void RecordAssessmentFailed(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    /// <summary>Records a completed endpoint execution on the activity.</summary>
    internal static void RecordEndpointCompleted(Activity? activity, int statusCode)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetTag(TagStatusCode, statusCode.ToString(CultureInfo.InvariantCulture));
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>Records a failed endpoint execution on the activity.</summary>
    internal static void RecordEndpointFailed(Activity? activity, int statusCode, string reason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagStatusCode, statusCode.ToString(CultureInfo.InvariantCulture));
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }
}
