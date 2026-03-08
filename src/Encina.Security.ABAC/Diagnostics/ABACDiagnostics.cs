using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.ABAC.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina ABAC observability.
/// </summary>
internal static class ABACDiagnostics
{
    internal const string SourceName = "Encina.Security.ABAC";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ── Counters ────────────────────────────────────────────────────

    internal static readonly Counter<long> EvaluationTotal =
        Meter.CreateCounter<long>("abac.evaluation.total",
            description: "Total number of ABAC policy evaluations.");

    internal static readonly Counter<long> EvaluationPermitted =
        Meter.CreateCounter<long>("abac.evaluation.permitted",
            description: "Number of ABAC evaluations that resulted in Permit.");

    internal static readonly Counter<long> EvaluationDenied =
        Meter.CreateCounter<long>("abac.evaluation.denied",
            description: "Number of ABAC evaluations that resulted in Deny.");

    internal static readonly Counter<long> ObligationExecuted =
        Meter.CreateCounter<long>("abac.obligation.executed",
            description: "Total number of obligations executed.");

    internal static readonly Counter<long> EvaluationNotApplicable =
        Meter.CreateCounter<long>("abac.evaluation.not_applicable",
            description: "Number of ABAC evaluations that resulted in NotApplicable.");

    internal static readonly Counter<long> EvaluationIndeterminate =
        Meter.CreateCounter<long>("abac.evaluation.indeterminate",
            description: "Number of ABAC evaluations that resulted in Indeterminate.");

    internal static readonly Counter<long> ObligationFailed =
        Meter.CreateCounter<long>("abac.obligation.failed",
            description: "Number of obligation executions that failed.");

    internal static readonly Counter<long> ObligationNoHandler =
        Meter.CreateCounter<long>("abac.obligation.no_handler",
            description: "Number of obligations with no registered handler.");

    internal static readonly Counter<long> AdviceExecuted =
        Meter.CreateCounter<long>("abac.advice.executed",
            description: "Total number of advice expressions executed.");

    // ── Histograms ──────────────────────────────────────────────────

    internal static readonly Histogram<double> EvaluationDuration =
        Meter.CreateHistogram<double>("abac.evaluation.duration",
            unit: "ms",
            description: "Duration of ABAC policy evaluations in milliseconds.");

    internal static readonly Histogram<double> ObligationDuration =
        Meter.CreateHistogram<double>("abac.obligation.duration",
            unit: "ms",
            description: "Duration of individual obligation executions in milliseconds.");

    // ── Tag Names ───────────────────────────────────────────────────

    internal const string TagRequestType = "abac.request_type";
    internal const string TagEffect = "abac.effect";
    internal const string TagPolicyId = "abac.policy_id";
    internal const string TagEnforcementMode = "abac.enforcement_mode";
    internal const string TagObligationId = "abac.obligation_id";
    internal const string TagAdviceId = "abac.advice_id";

    // ── Activity Helpers ────────────────────────────────────────────

    internal static Activity? StartEvaluation(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ABAC.Evaluate", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    internal static void RecordPermitted(Activity? activity, string? policyId)
    {
        activity?.SetTag(TagEffect, "permit");
        activity?.SetTag(TagPolicyId, policyId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordDenied(Activity? activity, string? policyId, string reason)
    {
        activity?.SetTag(TagEffect, "deny");
        activity?.SetTag(TagPolicyId, policyId);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    internal static void RecordIndeterminate(Activity? activity, string reason)
    {
        activity?.SetTag(TagEffect, "indeterminate");
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    internal static void RecordNotApplicable(Activity? activity)
    {
        activity?.SetTag(TagEffect, "not_applicable");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
