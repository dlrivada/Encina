using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.GDPR.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina GDPR compliance observability.
/// </summary>
internal static class GDPRDiagnostics
{
    internal const string SourceName = "Encina.Compliance.GDPR";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters
    internal static readonly Counter<long> ComplianceCheckTotal =
        Meter.CreateCounter<long>("gdpr.compliance_check.total",
            description: "Total number of GDPR compliance evaluations.");

    internal static readonly Counter<long> ComplianceCheckPassed =
        Meter.CreateCounter<long>("gdpr.compliance_check.passed",
            description: "Number of GDPR compliance evaluations that passed.");

    internal static readonly Counter<long> ComplianceCheckFailed =
        Meter.CreateCounter<long>("gdpr.compliance_check.failed",
            description: "Number of GDPR compliance evaluations that failed.");

    internal static readonly Counter<long> ComplianceCheckSkipped =
        Meter.CreateCounter<long>("gdpr.compliance_check.skipped",
            description: "Number of requests skipped (no GDPR attributes).");

    // Histogram
    internal static readonly Histogram<double> ComplianceCheckDuration =
        Meter.CreateHistogram<double>("gdpr.compliance_check.duration",
            unit: "ms",
            description: "Duration of GDPR compliance evaluations in milliseconds.");

    // RoPA Export
    internal static readonly Counter<long> RoPAExportTotal =
        Meter.CreateCounter<long>("gdpr.ropa_export.total",
            description: "Total number of RoPA export operations.");

    internal static readonly Counter<long> RoPAExportFailed =
        Meter.CreateCounter<long>("gdpr.ropa_export.failed",
            description: "Number of failed RoPA export operations.");

    internal static readonly Histogram<double> RoPAExportDuration =
        Meter.CreateHistogram<double>("gdpr.ropa_export.duration",
            unit: "ms",
            description: "Duration of RoPA export operations in milliseconds.");

    // Tag names
    internal const string TagRequestType = "gdpr.request_type";
    internal const string TagOutcome = "gdpr.outcome";
    internal const string TagLawfulBasis = "gdpr.lawful_basis";
    internal const string TagFailureReason = "gdpr.failure_reason";
    internal const string TagExportFormat = "gdpr.export_format";
    internal const string TagActivityCount = "gdpr.activity_count";

    internal static Activity? StartComplianceCheck(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("GDPR.ComplianceCheck", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    internal static Activity? StartRoPAExport(string format, int activityCount)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("GDPR.RoPA.Export", ActivityKind.Internal);
        activity?.SetTag(TagExportFormat, format);
        activity?.SetTag(TagActivityCount, activityCount);
        return activity;
    }

    internal static void RecordExportCompleted(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordExportFailed(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
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

    internal static void SetLawfulBasis(Activity? activity, LawfulBasis lawfulBasis)
    {
        activity?.SetTag(TagLawfulBasis, lawfulBasis.ToString());
    }

}
