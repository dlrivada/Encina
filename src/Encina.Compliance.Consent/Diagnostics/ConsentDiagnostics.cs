using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.Consent.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina consent compliance observability.
/// </summary>
internal static class ConsentDiagnostics
{
    internal const string SourceName = "Encina.Compliance.Consent";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters
    internal static readonly Counter<long> ConsentCheckTotal =
        Meter.CreateCounter<long>("consent.checks.total",
            description: "Total number of consent compliance evaluations.");

    internal static readonly Counter<long> ConsentCheckPassed =
        Meter.CreateCounter<long>("consent.checks.passed",
            description: "Number of consent evaluations that passed.");

    internal static readonly Counter<long> ConsentCheckFailed =
        Meter.CreateCounter<long>("consent.checks.failed",
            description: "Number of consent evaluations that failed.");

    internal static readonly Counter<long> ConsentCheckSkipped =
        Meter.CreateCounter<long>("consent.checks.skipped",
            description: "Number of requests skipped (no [RequireConsent] attribute).");

    // Histogram
    internal static readonly Histogram<double> ConsentCheckDuration =
        Meter.CreateHistogram<double>("consent.check.duration",
            unit: "ms",
            description: "Duration of consent compliance evaluations in milliseconds.");

    // Tag names
    internal const string TagSubjectId = "consent.subject_id";
    internal const string TagPurpose = "consent.purpose";
    internal const string TagOutcome = "consent.outcome";
    internal const string TagRequestType = "consent.request_type";
    internal const string TagFailureReason = "consent.failure_reason";
    internal const string TagEnforcementMode = "consent.enforcement_mode";

    internal static Activity? StartConsentCheck(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Consent.Check", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
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
}
