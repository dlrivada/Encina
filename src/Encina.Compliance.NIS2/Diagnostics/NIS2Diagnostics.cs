using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.NIS2.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina NIS2 Compliance observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.NIS2</c>)
/// for fine-grained trace filtering, and a dedicated <see cref="Meter"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>nis2.measure</c>, <c>nis2.outcome</c>,
/// <c>nis2.enforcement_mode</c>, <c>nis2.check_type</c>) to enable flexible dashboards
/// without creating separate counters per outcome.
/// </para>
/// <para>
/// Key metrics include:
/// <list type="bullet">
/// <item><description><c>nis2.pipeline.executions.total</c> — total pipeline behavior invocations</description></item>
/// <item><description><c>nis2.compliance.checks.total</c> — aggregate compliance validations</description></item>
/// <item><description><c>nis2.measure.evaluations.total</c> — individual measure evaluations</description></item>
/// <item><description><c>nis2.mfa.checks.total</c> — MFA enforcement checks</description></item>
/// <item><description><c>nis2.supply_chain.checks.total</c> — supply chain validations</description></item>
/// <item><description><c>nis2.incident.reports.total</c> — incident report submissions</description></item>
/// <item><description><c>nis2.pipeline.duration.ms</c> — pipeline execution duration histogram</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class NIS2Diagnostics
{
    internal const string SourceName = "Encina.Compliance.NIS2";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    /// <summary>The NIS2 measure being evaluated (e.g., RiskAnalysisAndSecurityPolicies).</summary>
    internal const string TagMeasure = "nis2.measure";

    /// <summary>Outcome of the operation: passed, failed, blocked, warned, skipped.</summary>
    internal const string TagOutcome = "nis2.outcome";

    /// <summary>The enforcement mode: Block, Warn, Disabled.</summary>
    internal const string TagEnforcementMode = "nis2.enforcement_mode";

    /// <summary>The type of compliance check: mfa, supply_chain, critical, compliance, encryption.</summary>
    internal const string TagCheckType = "nis2.check_type";

    /// <summary>The request type name triggering the pipeline.</summary>
    internal const string TagRequestType = "nis2.request_type";

    /// <summary>The entity type: Essential, Important.</summary>
    internal const string TagEntityType = "nis2.entity_type";

    /// <summary>The NIS2 sector.</summary>
    internal const string TagSector = "nis2.sector";

    /// <summary>The supplier identifier for supply chain checks.</summary>
    internal const string TagSupplierId = "nis2.supplier_id";

    /// <summary>The supplier risk level: Low, Medium, High, Critical.</summary>
    internal const string TagSupplierRiskLevel = "nis2.supplier_risk_level";

    /// <summary>The incident severity: Low, Medium, High, Critical.</summary>
    internal const string TagIncidentSeverity = "nis2.incident_severity";

    /// <summary>The notification phase: EarlyWarning, IncidentNotification, IntermediateReport, FinalReport.</summary>
    internal const string TagNotificationPhase = "nis2.notification_phase";

    /// <summary>The reason a check or operation failed.</summary>
    internal const string TagFailureReason = "nis2.failure_reason";

    /// <summary>The incident identifier.</summary>
    internal const string TagIncidentId = "nis2.incident_id";

    // ---- Counters ----

    /// <summary>
    /// Total pipeline behavior executions, tagged with <c>nis2.outcome</c> and <c>nis2.enforcement_mode</c>.
    /// </summary>
    internal static readonly Counter<long> PipelineExecutionsTotal =
        Meter.CreateCounter<long>("nis2.pipeline.executions.total",
            description: "Total number of NIS2 compliance pipeline executions.");

    /// <summary>
    /// Total aggregate compliance validations, tagged with <c>nis2.outcome</c>, <c>nis2.entity_type</c>, <c>nis2.sector</c>.
    /// </summary>
    internal static readonly Counter<long> ComplianceChecksTotal =
        Meter.CreateCounter<long>("nis2.compliance.checks.total",
            description: "Total number of NIS2 aggregate compliance validations.");

    /// <summary>
    /// Total individual measure evaluations, tagged with <c>nis2.measure</c> and <c>nis2.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> MeasureEvaluationsTotal =
        Meter.CreateCounter<long>("nis2.measure.evaluations.total",
            description: "Total number of individual NIS2 measure evaluations.");

    /// <summary>
    /// Total MFA enforcement checks, tagged with <c>nis2.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> MFAChecksTotal =
        Meter.CreateCounter<long>("nis2.mfa.checks.total",
            description: "Total number of MFA enforcement checks.");

    /// <summary>
    /// Total supply chain validations, tagged with <c>nis2.supplier_id</c> and <c>nis2.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> SupplyChainChecksTotal =
        Meter.CreateCounter<long>("nis2.supply_chain.checks.total",
            description: "Total number of supply chain security validations.");

    /// <summary>
    /// Total encryption validations, tagged with <c>nis2.check_type</c> and <c>nis2.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> EncryptionChecksTotal =
        Meter.CreateCounter<long>("nis2.encryption.checks.total",
            description: "Total number of encryption validations.");

    /// <summary>
    /// Total incident report submissions, tagged with <c>nis2.incident_severity</c> and <c>nis2.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> IncidentReportsTotal =
        Meter.CreateCounter<long>("nis2.incident.reports.total",
            description: "Total number of NIS2 incident reports submitted.");

    /// <summary>
    /// Total notification deadline checks, tagged with <c>nis2.notification_phase</c> and <c>nis2.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> DeadlineChecksTotal =
        Meter.CreateCounter<long>("nis2.incident.deadline_checks.total",
            description: "Total number of notification deadline checks.");

    /// <summary>
    /// Total supplier assessments, tagged with <c>nis2.supplier_id</c> and <c>nis2.supplier_risk_level</c>.
    /// </summary>
    internal static readonly Counter<long> SupplierAssessmentsTotal =
        Meter.CreateCounter<long>("nis2.supply_chain.assessments.total",
            description: "Total number of supplier risk assessments.");

    // ---- Histograms ----

    /// <summary>
    /// Duration of NIS2 pipeline behavior execution in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PipelineDuration =
        Meter.CreateHistogram<double>("nis2.pipeline.duration.ms",
            unit: "ms",
            description: "Duration of NIS2 compliance pipeline behavior execution in milliseconds.");

    /// <summary>
    /// Duration of aggregate compliance validation in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> ComplianceCheckDuration =
        Meter.CreateHistogram<double>("nis2.compliance.check.duration.ms",
            unit: "ms",
            description: "Duration of NIS2 aggregate compliance validation in milliseconds.");

    /// <summary>
    /// Duration of individual measure evaluation in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> MeasureEvaluationDuration =
        Meter.CreateHistogram<double>("nis2.measure.evaluation.duration.ms",
            unit: "ms",
            description: "Duration of individual NIS2 measure evaluation in milliseconds.");

    /// <summary>
    /// Duration of supply chain assessment in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> SupplyChainAssessmentDuration =
        Meter.CreateHistogram<double>("nis2.supply_chain.assessment.duration.ms",
            unit: "ms",
            description: "Duration of supply chain security assessment in milliseconds.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>NIS2.Pipeline</c> activity for a pipeline behavior execution.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type triggering the pipeline.</param>
    /// <param name="enforcementMode">The enforcement mode.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPipelineExecution(string requestTypeName, string enforcementMode)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("NIS2.Pipeline", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagEnforcementMode, enforcementMode);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>NIS2.ComplianceCheck</c> activity for an aggregate compliance validation.
    /// </summary>
    /// <param name="entityType">The NIS2 entity type.</param>
    /// <param name="sector">The NIS2 sector.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartComplianceCheck(string entityType, string sector)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("NIS2.ComplianceCheck", ActivityKind.Internal);
        activity?.SetTag(TagEntityType, entityType);
        activity?.SetTag(TagSector, sector);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>NIS2.MeasureEvaluation</c> activity for an individual measure evaluation.
    /// </summary>
    /// <param name="measure">The NIS2 measure being evaluated.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartMeasureEvaluation(string measure)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("NIS2.MeasureEvaluation", ActivityKind.Internal);
        activity?.SetTag(TagMeasure, measure);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>NIS2.IncidentReport</c> activity for an incident report submission.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="severity">The incident severity.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartIncidentReport(string incidentId, string severity)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("NIS2.IncidentReport", ActivityKind.Internal);
        activity?.SetTag(TagIncidentId, incidentId);
        activity?.SetTag(TagIncidentSeverity, severity);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>NIS2.SupplyChainAssessment</c> activity for a supplier assessment.
    /// </summary>
    /// <param name="supplierId">The supplier identifier.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartSupplyChainAssessment(string supplierId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("NIS2.SupplyChainAssessment", ActivityKind.Internal);
        activity?.SetTag(TagSupplierId, supplierId);
        return activity;
    }

    // ---- Outcome recorders ----

    /// <summary>
    /// Records a successful completion on an activity.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    internal static void RecordCompleted(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a failure on an activity.
    /// </summary>
    /// <param name="activity">The activity to mark as failed (may be <c>null</c>).</param>
    /// <param name="reason">The failure reason.</param>
    internal static void RecordFailed(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    /// <summary>
    /// Records a skipped outcome on an activity (enforcement disabled or no attributes found).
    /// </summary>
    /// <param name="activity">The activity to mark as skipped (may be <c>null</c>).</param>
    internal static void RecordSkipped(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "skipped");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a blocked outcome on an activity (enforcement mode = Block, check failed).
    /// </summary>
    /// <param name="activity">The activity to mark as blocked (may be <c>null</c>).</param>
    /// <param name="reason">The reason the request was blocked.</param>
    internal static void RecordBlocked(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "blocked");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    /// <summary>
    /// Records a warned outcome on an activity (enforcement mode = Warn, check failed but allowed).
    /// </summary>
    /// <param name="activity">The activity to mark as warned (may be <c>null</c>).</param>
    /// <param name="reason">The reason the warning was issued.</param>
    internal static void RecordWarned(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "warned");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
