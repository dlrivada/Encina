using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.AIAct.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina AI Act compliance observability.
/// </summary>
/// <remarks>
/// Event IDs: 9500-9599 (see <c>EventIdRanges.ComplianceAIAct</c>).
/// </remarks>
internal static class AIActDiagnostics
{
    internal const string SourceName = "Encina.Compliance.AIAct";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ═══════════════════════════════════════════════════════════════════════
    // Counters
    // ═══════════════════════════════════════════════════════════════════════

    internal static readonly Counter<long> ComplianceCheckTotal =
        Meter.CreateCounter<long>("aiact.compliance_check.total",
            description: "Total number of AI Act compliance evaluations.");

    internal static readonly Counter<long> ComplianceCheckPassed =
        Meter.CreateCounter<long>("aiact.compliance_check.passed",
            description: "Number of AI Act compliance evaluations that passed.");

    internal static readonly Counter<long> ComplianceCheckFailed =
        Meter.CreateCounter<long>("aiact.compliance_check.failed",
            description: "Number of AI Act compliance evaluations that failed.");

    internal static readonly Counter<long> ComplianceCheckSkipped =
        Meter.CreateCounter<long>("aiact.compliance_check.skipped",
            description: "Number of requests skipped (no AI Act attributes).");

    internal static readonly Counter<long> ProhibitedUseBlocked =
        Meter.CreateCounter<long>("aiact.prohibited_use.blocked",
            description: "Number of requests blocked due to prohibited AI practices (Art. 5).");

    // ═══════════════════════════════════════════════════════════════════════
    // Histogram
    // ═══════════════════════════════════════════════════════════════════════

    internal static readonly Histogram<double> ComplianceCheckDuration =
        Meter.CreateHistogram<double>("aiact.compliance_check.duration",
            unit: "ms",
            description: "Duration of AI Act compliance evaluations in milliseconds.");

    // ═══════════════════════════════════════════════════════════════════════
    // Tag names
    // ═══════════════════════════════════════════════════════════════════════

    internal const string TagRequestType = "aiact.request_type";
    internal const string TagOutcome = "aiact.outcome";
    internal const string TagRiskLevel = "aiact.risk_level";
    internal const string TagSystemId = "aiact.system_id";
    internal const string TagFailureReason = "aiact.failure_reason";
    internal const string TagEnforcementMode = "aiact.enforcement_mode";

    // ═══════════════════════════════════════════════════════════════════════
    // Activity helpers
    // ═══════════════════════════════════════════════════════════════════════

    internal static Activity? StartComplianceCheck(string requestTypeName, string enforcementMode)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("AIAct.ComplianceCheck", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagEnforcementMode, enforcementMode);
        return activity;
    }

    internal static void RecordPassed(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "passed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordFailed(Activity? activity, string failureReason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, failureReason);
        activity?.SetStatus(ActivityStatusCode.Error, failureReason);
    }

    internal static void RecordSkipped(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "skipped");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void SetRiskLevel(Activity? activity, string riskLevel)
    {
        activity?.SetTag(TagRiskLevel, riskLevel);
    }

    internal static void SetSystemId(Activity? activity, string systemId)
    {
        activity?.SetTag(TagSystemId, systemId);
    }
}
