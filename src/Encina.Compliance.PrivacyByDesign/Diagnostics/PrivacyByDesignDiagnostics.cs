using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.PrivacyByDesign.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Privacy by Design compliance observability.
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
/// Metric names follow the <c>pbd.*</c> prefix convention.
/// Tag names follow the <c>pbd.*</c> prefix convention.
/// </para>
/// </remarks>
internal static class PrivacyByDesignDiagnostics
{
    internal const string SourceName = "Encina.Compliance.PrivacyByDesign";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ========================================================================
    // Counters — Pipeline
    // ========================================================================

    /// <summary>Total number of PbD pipeline checks executed.</summary>
    internal static readonly Counter<long> PipelineCheckTotal =
        Meter.CreateCounter<long>("pbd.pipeline.checks.total",
            description: "Total number of Privacy by Design pipeline compliance checks.");

    /// <summary>Number of PbD pipeline checks that passed (request is compliant).</summary>
    internal static readonly Counter<long> PipelineCheckPassed =
        Meter.CreateCounter<long>("pbd.pipeline.checks.passed",
            description: "Number of Privacy by Design pipeline checks that passed.");

    /// <summary>Number of PbD pipeline checks that failed (violations detected).</summary>
    internal static readonly Counter<long> PipelineCheckFailed =
        Meter.CreateCounter<long>("pbd.pipeline.checks.failed",
            description: "Number of Privacy by Design pipeline checks that failed.");

    /// <summary>Number of requests skipped (no [EnforceDataMinimization] attribute).</summary>
    internal static readonly Counter<long> PipelineCheckSkipped =
        Meter.CreateCounter<long>("pbd.pipeline.checks.skipped",
            description: "Number of requests skipped (no [EnforceDataMinimization] attribute).");

    // ========================================================================
    // Counters — Violations & Validations
    // ========================================================================

    /// <summary>Total number of data minimization violations detected.</summary>
    internal static readonly Counter<long> MinimizationViolationsTotal =
        Meter.CreateCounter<long>("pbd.minimization.violations.total",
            description: "Total number of data minimization violations detected (GDPR Art. 25).");

    /// <summary>Total number of purpose limitation validations executed.</summary>
    internal static readonly Counter<long> PurposeValidationsTotal =
        Meter.CreateCounter<long>("pbd.purpose.validations.total",
            description: "Total number of purpose limitation validations executed (GDPR Art. 5(1)(b)).");

    /// <summary>Total number of purpose limitation violations detected.</summary>
    internal static readonly Counter<long> PurposeViolationsTotal =
        Meter.CreateCounter<long>("pbd.purpose.violations.total",
            description: "Total number of purpose limitation violations detected.");

    /// <summary>Total number of privacy default overrides detected.</summary>
    internal static readonly Counter<long> DefaultOverridesTotal =
        Meter.CreateCounter<long>("pbd.defaults.overrides.total",
            description: "Total number of privacy default overrides detected (GDPR Art. 25(2)).");

    /// <summary>Total number of violation notifications published.</summary>
    internal static readonly Counter<long> NotificationsPublishedTotal =
        Meter.CreateCounter<long>("pbd.notifications.published.total",
            description: "Total number of PbD violation notifications published.");

    /// <summary>Total number of notification publish failures (non-blocking).</summary>
    internal static readonly Counter<long> NotificationsFailedTotal =
        Meter.CreateCounter<long>("pbd.notifications.failed.total",
            description: "Total number of PbD notification publish failures.");

    // ========================================================================
    // Counters — Purpose Registry
    // ========================================================================

    /// <summary>Total number of purposes registered at startup.</summary>
    internal static readonly Counter<long> PurposeRegistrationsTotal =
        Meter.CreateCounter<long>("pbd.purpose.registrations.total",
            description: "Total number of purpose definitions registered.");

    /// <summary>Total number of purpose registration failures.</summary>
    internal static readonly Counter<long> PurposeRegistrationFailuresTotal =
        Meter.CreateCounter<long>("pbd.purpose.registrations.failed.total",
            description: "Total number of purpose registration failures.");

    // ========================================================================
    // Histograms
    // ========================================================================

    /// <summary>Duration of PbD pipeline check in milliseconds.</summary>
    internal static readonly Histogram<double> PipelineCheckDuration =
        Meter.CreateHistogram<double>("pbd.pipeline.check.duration",
            unit: "ms",
            description: "Duration of Privacy by Design pipeline compliance check in milliseconds.");

    /// <summary>Duration of full PbD validation (orchestrator) in milliseconds.</summary>
    internal static readonly Histogram<double> ValidationDuration =
        Meter.CreateHistogram<double>("pbd.validation.duration",
            unit: "ms",
            description: "Duration of Privacy by Design full validation in milliseconds.");

    /// <summary>Duration of data minimization analysis in milliseconds.</summary>
    internal static readonly Histogram<double> AnalysisDuration =
        Meter.CreateHistogram<double>("pbd.analysis.duration",
            unit: "ms",
            description: "Duration of data minimization analysis in milliseconds.");

    // ========================================================================
    // Tag names
    // ========================================================================

    internal const string TagOutcome = "pbd.outcome";
    internal const string TagRequestType = "pbd.request_type";
    internal const string TagFailureReason = "pbd.failure_reason";
    internal const string TagEnforcementMode = "pbd.enforcement_mode";
    internal const string TagViolationType = "pbd.violation_type";
    internal const string TagModuleId = "pbd.module_id";
    internal const string TagPurposeName = "pbd.purpose_name";
    internal const string TagMinimizationScore = "pbd.minimization_score";

    // ========================================================================
    // Activity helpers
    // ========================================================================

    /// <summary>
    /// Starts an activity for a Privacy by Design pipeline check.
    /// </summary>
    /// <param name="requestTypeName">The request type being checked.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartPipelineCheck(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("PbD.PipelineCheck", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    /// <summary>
    /// Starts an activity for a full Privacy by Design validation.
    /// </summary>
    /// <param name="requestTypeName">The request type being validated.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartValidation(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("PbD.Validation", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    /// <summary>
    /// Starts an activity for data minimization analysis.
    /// </summary>
    /// <param name="requestTypeName">The request type being analyzed.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartAnalysis(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("PbD.Analysis", ActivityKind.Internal);
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

    /// <summary>Records a warned outcome on the activity (Warn enforcement mode).</summary>
    internal static void RecordWarned(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "warned");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
