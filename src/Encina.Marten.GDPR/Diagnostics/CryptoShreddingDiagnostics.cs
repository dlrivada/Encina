using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Marten.GDPR.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Marten GDPR crypto-shredding observability.
/// </summary>
internal static class CryptoShreddingDiagnostics
{
    internal const string SourceName = "Encina.Marten.GDPR";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters
    internal static readonly Counter<long> EncryptionTotal =
        Meter.CreateCounter<long>("crypto.encryption.total",
            description: "Total number of PII field encryptions.");

    internal static readonly Counter<long> DecryptionTotal =
        Meter.CreateCounter<long>("crypto.decryption.total",
            description: "Total number of PII field decryptions.");

    internal static readonly Counter<long> EncryptionFailedTotal =
        Meter.CreateCounter<long>("crypto.encryption.failed",
            description: "Number of PII field encryption failures.");

    internal static readonly Counter<long> DecryptionFailedTotal =
        Meter.CreateCounter<long>("crypto.decryption.failed",
            description: "Number of PII field decryption failures.");

    internal static readonly Counter<long> ForgottenAccessTotal =
        Meter.CreateCounter<long>("crypto.forgotten_access.total",
            description: "Number of attempts to decrypt data for forgotten subjects.");

    internal static readonly Counter<long> KeyRotationTotal =
        Meter.CreateCounter<long>("crypto.key_rotation.total",
            description: "Total number of subject key rotations.");

    internal static readonly Counter<long> ForgetTotal =
        Meter.CreateCounter<long>("crypto.forget.total",
            description: "Total number of subject forget (crypto-shredding) operations.");

    internal static readonly Counter<long> MisconfiguredTotal =
        Meter.CreateCounter<long>("crypto.configuration.misconfigured.total",
            description: "Number of types rejected at runtime because a [CryptoShredded] shape is misconfigured.");

    // Histograms
    internal static readonly Histogram<double> EncryptionDuration =
        Meter.CreateHistogram<double>("crypto.encryption.duration",
            unit: "ms",
            description: "Duration of PII field encryption in milliseconds.");

    internal static readonly Histogram<double> DecryptionDuration =
        Meter.CreateHistogram<double>("crypto.decryption.duration",
            unit: "ms",
            description: "Duration of PII field decryption in milliseconds.");

    internal static readonly Histogram<double> ForgetDuration =
        Meter.CreateHistogram<double>("crypto.forget.duration",
            unit: "ms",
            description: "Duration of subject forget operations in milliseconds.");

    // Tag names
    internal const string TagEventType = "crypto.event_type";
    // Note: no subject-id tag is exposed here. A data subject's own identifier must never appear
    // as a trace or metric tag (it is high-cardinality personal data); correlate via the root
    // type, the failure reason or key version instead (#1429, following #1314).
    internal const string TagOutcome = "crypto.outcome";
    internal const string TagKeyProviderType = "crypto.key_provider_type";
    internal const string TagFailureReason = "crypto.failure_reason";

    internal static Activity? StartEncryption(string eventType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("CryptoShredding.Encrypt", ActivityKind.Internal);
        activity?.SetTag(TagEventType, eventType);
        return activity;
    }

    internal static Activity? StartDecryption(string eventType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("CryptoShredding.Decrypt", ActivityKind.Internal);
        activity?.SetTag(TagEventType, eventType);
        return activity;
    }

    // Note: none of these Start* methods take a subjectId parameter — the data subject's own
    // identifier must never become an activity tag (#1429, following #1314).
    internal static Activity? StartForget()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("CryptoShredding.Forget", ActivityKind.Internal);
    }

    internal static Activity? StartKeyRotation()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("CryptoShredding.KeyRotation", ActivityKind.Internal);
    }

    internal static Activity? StartErasure()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("CryptoShredding.Erasure", ActivityKind.Internal);
    }

    internal static void RecordSuccess(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "success");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordFailed(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    /// <summary>
    /// Builds the low-cardinality tag list of a failure counter: the failure reason (an enum name or an
    /// exception type name, never a message).
    /// </summary>
    internal static KeyValuePair<string, object?> FailureReasonTag(string reason) => new(TagFailureReason, reason);

    internal static void RecordForgottenAccess(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "forgotten");
        activity?.SetStatus(ActivityStatusCode.Ok, "Subject has been forgotten");
    }
}
