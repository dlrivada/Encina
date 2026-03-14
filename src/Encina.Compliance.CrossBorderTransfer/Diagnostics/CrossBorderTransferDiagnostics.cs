using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.CrossBorderTransfer.Diagnostics;

/// <summary>
/// Provides the activity source and meter for cross-border transfer compliance observability.
/// </summary>
internal static class CrossBorderTransferDiagnostics
{
    internal const string SourceName = "Encina.Compliance.CrossBorderTransfer";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters
    internal static readonly Counter<long> TransferCheckTotal =
        Meter.CreateCounter<long>("crossborder.checks.total",
            description: "Total number of cross-border transfer compliance evaluations.");

    internal static readonly Counter<long> TransferCheckPassed =
        Meter.CreateCounter<long>("crossborder.checks.passed",
            description: "Number of cross-border transfer evaluations that passed (transfer allowed).");

    internal static readonly Counter<long> TransferCheckBlocked =
        Meter.CreateCounter<long>("crossborder.checks.blocked",
            description: "Number of cross-border transfer evaluations that blocked the transfer.");

    internal static readonly Counter<long> TransferCheckWarned =
        Meter.CreateCounter<long>("crossborder.checks.warned",
            description: "Number of cross-border transfer evaluations that warned but allowed the transfer.");

    internal static readonly Counter<long> TransferCheckSkipped =
        Meter.CreateCounter<long>("crossborder.checks.skipped",
            description: "Number of requests skipped (no [RequiresCrossBorderTransfer] attribute).");

    // Histogram
    internal static readonly Histogram<double> TransferCheckDuration =
        Meter.CreateHistogram<double>("crossborder.check.duration",
            unit: "ms",
            description: "Duration of cross-border transfer compliance evaluations in milliseconds.");

    // Service-level counters
    internal static readonly Counter<long> TIACreated =
        Meter.CreateCounter<long>("crossborder.tia.created",
            description: "Number of Transfer Impact Assessments created.");

    internal static readonly Counter<long> TIACompleted =
        Meter.CreateCounter<long>("crossborder.tia.completed",
            description: "Number of Transfer Impact Assessments completed.");

    internal static readonly Counter<long> SCCRegistered =
        Meter.CreateCounter<long>("crossborder.scc.registered",
            description: "Number of SCC agreements registered.");

    internal static readonly Counter<long> SCCRevoked =
        Meter.CreateCounter<long>("crossborder.scc.revoked",
            description: "Number of SCC agreements revoked.");

    internal static readonly Counter<long> TransferApproved =
        Meter.CreateCounter<long>("crossborder.transfer.approved",
            description: "Number of transfers approved.");

    internal static readonly Counter<long> TransferRevoked =
        Meter.CreateCounter<long>("crossborder.transfer.revoked",
            description: "Number of transfers revoked.");

    // Tag names
    internal const string TagSource = "crossborder.source";
    internal const string TagDestination = "crossborder.destination";
    internal const string TagDataCategory = "crossborder.data_category";
    internal const string TagOutcome = "crossborder.outcome";
    internal const string TagBasis = "crossborder.basis";
    internal const string TagRequestType = "crossborder.request_type";
    internal const string TagFailureReason = "crossborder.failure_reason";
    internal const string TagEnforcementMode = "crossborder.enforcement_mode";

    internal static Activity? StartTransferCheck(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("CrossBorderTransfer.Check", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    internal static void RecordPassed(Activity? activity, string basis)
    {
        activity?.SetTag(TagOutcome, "passed");
        activity?.SetTag(TagBasis, basis);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordBlocked(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "blocked");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    internal static void RecordWarned(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "warned");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordSkipped(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "skipped");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
