using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.Encryption.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina field-level encryption observability.
/// </summary>
/// <remarks>
/// <para>
/// All tracing and metrics are emitted under the <c>Encina.Security.Encryption</c> source name.
/// Tracing and metrics are gated by <see cref="EncryptionOptions.EnableTracing"/> and
/// <see cref="EncryptionOptions.EnableMetrics"/> respectively.
/// </para>
/// <para>
/// <b>Traces</b>: Activities named <c>"Encryption.Process"</c> for each pipeline behavior invocation,
/// with events for individual encrypt/decrypt operations.
/// </para>
/// <para>
/// <b>Metrics</b>:
/// <list type="bullet">
/// <item><c>encryption.operations</c> — Total encryption/decryption operations</item>
/// <item><c>encryption.duration</c> — Duration of cryptographic operations in milliseconds</item>
/// <item><c>encryption.failures</c> — Total failed encryption/decryption operations</item>
/// </list>
/// </para>
/// </remarks>
internal static class EncryptionDiagnostics
{
    internal const string SourceName = "Encina.Security.Encryption";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters
    internal static readonly Counter<long> OperationsTotal =
        Meter.CreateCounter<long>("encryption.operations",
            description: "Total number of encryption and decryption operations.");

    internal static readonly Counter<long> FailuresTotal =
        Meter.CreateCounter<long>("encryption.failures",
            description: "Total number of failed encryption and decryption operations.");

    // Histogram
    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>("encryption.duration",
            unit: "ms",
            description: "Duration of encryption and decryption operations in milliseconds.");

    // Activity tag name constants
    internal const string TagRequestType = "encryption.request_type";
    internal const string TagOperation = "encryption.operation";
    internal const string TagAlgorithm = "encryption.algorithm";
    internal const string TagKeyId = "encryption.key_id";
    internal const string TagPurpose = "encryption.purpose";
    internal const string TagPropertyCount = "encryption.property_count";
    internal const string TagOutcome = "encryption.outcome";

    /// <summary>
    /// Starts a new activity for encryption pipeline processing.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type being processed.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartProcess(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encryption.Process", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    /// <summary>
    /// Records a successful encryption/decryption operation on the activity.
    /// </summary>
    internal static void RecordOperationEvent(Activity? activity, string operation, int propertyCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.AddEvent(new ActivityEvent($"Encryption.{operation}", tags: new ActivityTagsCollection
        {
            { TagOperation, operation },
            { TagPropertyCount, propertyCount }
        }));
    }

    /// <summary>
    /// Records that the pipeline completed successfully.
    /// </summary>
    internal static void RecordSuccess(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "success");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records that the pipeline completed with an error.
    /// </summary>
    internal static void RecordFailure(Activity? activity, string operation, string errorMessage)
    {
        activity?.SetTag(TagOutcome, "failure");
        activity?.SetTag(TagOperation, operation);
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }
}
