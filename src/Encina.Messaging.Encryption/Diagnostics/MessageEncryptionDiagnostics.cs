using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Messaging.Encryption.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina message encryption observability.
/// </summary>
/// <remarks>
/// <para>
/// All tracing and metrics are emitted under the <c>Encina.Messaging.Encryption</c> source name.
/// Tracing and metrics are gated by <see cref="MessageEncryptionOptions.EnableTracing"/> and
/// <see cref="MessageEncryptionOptions.EnableMetrics"/> respectively.
/// </para>
/// <para>
/// <b>Traces</b>: Activities named <c>"MessageEncryption.Encrypt"</c> and
/// <c>"MessageEncryption.Decrypt"</c> for each serialize/deserialize operation,
/// with tags for message type, key ID, algorithm, and outcome.
/// </para>
/// <para>
/// <b>Metrics</b>:
/// <list type="bullet">
/// <item><c>messaging.encryption.operations</c> — Total encrypt/decrypt operations (dimensional: operation, outcome, message_type)</item>
/// <item><c>messaging.encryption.duration</c> — Duration of cryptographic operations in milliseconds</item>
/// <item><c>messaging.encryption.payload_size</c> — Encrypted payload size in bytes</item>
/// <item><c>messaging.encryption.failures</c> — Total failed operations (dimensional: operation, message_type)</item>
/// </list>
/// </para>
/// </remarks>
internal static class MessageEncryptionDiagnostics
{
    internal const string SourceName = "Encina.Messaging.Encryption";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ========================================================================
    // Counters
    // ========================================================================

    internal static readonly Counter<long> OperationsTotal =
        Meter.CreateCounter<long>("messaging.encryption.operations",
            description: "Total number of message encryption and decryption operations.");

    internal static readonly Counter<long> FailuresTotal =
        Meter.CreateCounter<long>("messaging.encryption.failures",
            description: "Total number of failed message encryption and decryption operations.");

    // ========================================================================
    // Histograms
    // ========================================================================

    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>("messaging.encryption.duration",
            unit: "ms",
            description: "Duration of message encryption and decryption operations in milliseconds.");

    internal static readonly Histogram<long> PayloadSize =
        Meter.CreateHistogram<long>("messaging.encryption.payload_size",
            unit: "By",
            description: "Encrypted message payload size in bytes.");

    // ========================================================================
    // Tag name constants
    // ========================================================================

    internal const string TagOperation = "messaging.encryption.operation";
    internal const string TagOutcome = "messaging.encryption.outcome";
    internal const string TagMessageType = "messaging.encryption.message_type";
    internal const string TagKeyId = "messaging.encryption.key_id";
    internal const string TagAlgorithm = "messaging.encryption.algorithm";

    // ========================================================================
    // Activity helpers
    // ========================================================================

    /// <summary>
    /// Starts a new activity for a message encryption operation.
    /// </summary>
    /// <param name="messageType">The message type being encrypted.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartEncrypt(string messageType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("MessageEncryption.Encrypt", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "encrypt");
        activity?.SetTag(TagMessageType, messageType);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for a message decryption operation.
    /// </summary>
    /// <param name="keyId">The key ID used for decryption.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartDecrypt(string keyId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("MessageEncryption.Decrypt", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "decrypt");
        activity?.SetTag(TagKeyId, keyId);
        return activity;
    }

    /// <summary>
    /// Records a successful operation on the activity.
    /// </summary>
    internal static void RecordSuccess(Activity? activity, string keyId, string algorithm)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(TagOutcome, "success");
        activity.SetTag(TagKeyId, keyId);
        activity.SetTag(TagAlgorithm, algorithm);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a failed operation on the activity.
    /// </summary>
    internal static void RecordFailure(Activity? activity, string errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(TagOutcome, "failure");
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
    }

    // ========================================================================
    // Metrics helpers
    // ========================================================================

    /// <summary>
    /// Records a successful operation in metrics counters and histograms.
    /// </summary>
    internal static void RecordOperationMetrics(
        string operation,
        string messageType,
        string keyId,
        double durationMs,
        long payloadSizeBytes)
    {
        if (!OperationsTotal.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { TagOperation, operation },
            { TagOutcome, "success" },
            { TagMessageType, messageType },
            { TagKeyId, keyId }
        };

        OperationsTotal.Add(1, tags);
        OperationDuration.Record(durationMs, tags);

        if (payloadSizeBytes > 0)
        {
            PayloadSize.Record(payloadSizeBytes, tags);
        }
    }

    /// <summary>
    /// Records a failed operation in metrics counters.
    /// </summary>
    internal static void RecordFailureMetrics(string operation, string messageType)
    {
        if (!FailuresTotal.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { TagOperation, operation },
            { TagMessageType, messageType }
        };

        FailuresTotal.Add(1, tags);
    }
}
