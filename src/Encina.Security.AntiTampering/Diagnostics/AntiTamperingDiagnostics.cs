using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.AntiTampering.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina anti-tampering observability.
/// </summary>
/// <remarks>
/// <para>
/// All tracing and metrics are emitted under the <c>Encina.Security.AntiTampering</c> source name.
/// Tracing and metrics are gated by <see cref="AntiTamperingOptions.EnableTracing"/> and
/// <see cref="AntiTamperingOptions.EnableMetrics"/> respectively.
/// </para>
/// <para>
/// <b>Traces</b>: Activities for each pipeline behavior invocation (<c>AntiTampering.Verify</c>),
/// signing (<c>AntiTampering.Sign</c>), and sub-operations like timestamp and nonce validation.
/// </para>
/// <para>
/// <b>Metrics</b>:
/// <list type="bullet">
/// <item><c>antitampering.signature.validations.total</c> — Total signature verifications (tags: result, key_id)</item>
/// <item><c>antitampering.signature.failures.total</c> — Total failures by reason (tags: reason)</item>
/// <item><c>antitampering.nonce.rejections.total</c> — Total nonce rejections (replay attempts)</item>
/// <item><c>antitampering.signature.duration_ms</c> — Verification latency in milliseconds</item>
/// </list>
/// </para>
/// </remarks>
internal static class AntiTamperingDiagnostics
{
    internal const string SourceName = "Encina.Security.AntiTampering";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // --- Activity name constants ---

    internal const string ActivitySignRequest = "AntiTampering.Sign";
    internal const string ActivityVerifySignature = "AntiTampering.Verify";
    internal const string ActivityValidateTimestamp = "AntiTampering.ValidateTimestamp";
    internal const string ActivityValidateNonce = "AntiTampering.ValidateNonce";

    // --- Tag name constants ---

    internal const string TagRequestType = "antitampering.request_type";
    internal const string TagOperation = "antitampering.operation";
    internal const string TagAlgorithm = "antitampering.algorithm";
    internal const string TagKeyId = "antitampering.key_id";
    internal const string TagHasNonce = "antitampering.has_nonce";
    internal const string TagOutcome = "antitampering.outcome";
    internal const string TagFailureReason = "antitampering.failure_reason";
    internal const string TagResult = "result";

    // --- Counters ---

    internal static readonly Counter<long> ValidationsTotal =
        Meter.CreateCounter<long>("antitampering.signature.validations.total",
            description: "Total number of signature validation operations.");

    internal static readonly Counter<long> FailuresTotal =
        Meter.CreateCounter<long>("antitampering.signature.failures.total",
            description: "Total number of signature validation failures by reason.");

    internal static readonly Counter<long> NonceRejectionsTotal =
        Meter.CreateCounter<long>("antitampering.nonce.rejections.total",
            description: "Total number of nonce rejections (detected replay attempts).");

    // --- Histogram ---

    internal static readonly Histogram<double> VerificationDuration =
        Meter.CreateHistogram<double>("antitampering.signature.duration_ms",
            unit: "ms",
            description: "Duration of signature verification operations in milliseconds.");

    // --- Activity helpers ---

    /// <summary>
    /// Starts a new activity for the pipeline verification (parent span).
    /// </summary>
    internal static Activity? StartVerification(string requestTypeName, string algorithm, bool hasNonce)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(ActivityVerifySignature, ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagAlgorithm, algorithm);
        activity?.SetTag(TagHasNonce, hasNonce);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for signing an outgoing request.
    /// </summary>
    internal static Activity? StartSigning(string keyId, string algorithm)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(ActivitySignRequest, ActivityKind.Internal);
        activity?.SetTag(TagKeyId, keyId);
        activity?.SetTag(TagAlgorithm, algorithm);
        return activity;
    }

    /// <summary>
    /// Starts a child activity for timestamp validation.
    /// </summary>
    internal static Activity? StartTimestampValidation()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity(ActivityValidateTimestamp, ActivityKind.Internal);
    }

    /// <summary>
    /// Starts a child activity for nonce validation.
    /// </summary>
    internal static Activity? StartNonceValidation()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity(ActivityValidateNonce, ActivityKind.Internal);
    }

    /// <summary>
    /// Records that verification completed successfully.
    /// </summary>
    internal static void RecordSuccess(Activity? activity, string keyId)
    {
        activity?.SetTag(TagOutcome, "success");
        activity?.SetTag(TagKeyId, keyId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records that verification completed with a failure.
    /// </summary>
    internal static void RecordFailure(Activity? activity, string failureReason)
    {
        activity?.SetTag(TagOutcome, "failure");
        activity?.SetTag(TagFailureReason, failureReason);
        activity?.SetStatus(ActivityStatusCode.Error, failureReason);
    }
}
