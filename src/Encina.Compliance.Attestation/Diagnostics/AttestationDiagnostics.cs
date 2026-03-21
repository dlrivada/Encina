using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.Attestation.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina attestation compliance observability.
/// </summary>
/// <remarks>
/// Event IDs: 9600-9699 (see <c>EventIdRanges.ComplianceAttestation</c>).
/// </remarks>
internal static class AttestationDiagnostics
{
    internal const string SourceName = "Encina.Compliance.Attestation";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ═══════════════════════════════════════════════════════════════════════
    // Counters
    // ═══════════════════════════════════════════════════════════════════════

    internal static readonly Counter<long> AttestationTotal =
        Meter.CreateCounter<long>("attestation.attest.total",
            description: "Total number of attestation operations.");

    internal static readonly Counter<long> AttestationSucceeded =
        Meter.CreateCounter<long>("attestation.attest.succeeded",
            description: "Number of attestation operations that succeeded.");

    internal static readonly Counter<long> AttestationFailed =
        Meter.CreateCounter<long>("attestation.attest.failed",
            description: "Number of attestation operations that failed.");

    internal static readonly Counter<long> VerificationTotal =
        Meter.CreateCounter<long>("attestation.verify.total",
            description: "Total number of verification operations.");

    // ═══════════════════════════════════════════════════════════════════════
    // Histogram
    // ═══════════════════════════════════════════════════════════════════════

    internal static readonly Histogram<double> AttestationDuration =
        Meter.CreateHistogram<double>("attestation.attest.duration",
            unit: "ms",
            description: "Duration of attestation operations in milliseconds.");

    // ═══════════════════════════════════════════════════════════════════════
    // Tag names
    // ═══════════════════════════════════════════════════════════════════════

    internal const string TagProviderName = "attestation.provider";
    internal const string TagOutcome = "attestation.outcome";
    internal const string TagRecordType = "attestation.record_type";

    // ═══════════════════════════════════════════════════════════════════════
    // Activity helpers
    // ═══════════════════════════════════════════════════════════════════════

    internal static Activity? StartAttestation(string providerName, string recordType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Attestation.Attest", ActivityKind.Internal);
        activity?.SetTag(TagProviderName, providerName);
        activity?.SetTag(TagRecordType, recordType);
        return activity;
    }

    internal static Activity? StartVerification(string providerName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Attestation.Verify", ActivityKind.Internal);
        activity?.SetTag(TagProviderName, providerName);
        return activity;
    }

    internal static void RecordSuccess(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "success");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordFailure(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "failure");
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }
}
