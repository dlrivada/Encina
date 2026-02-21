using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.Sanitization.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina sanitization and encoding observability.
/// </summary>
/// <remarks>
/// <para>
/// All tracing and metrics are emitted under the <c>Encina.Security.Sanitization</c> source name.
/// Tracing and metrics are gated by <see cref="SanitizationOptions.EnableTracing"/> and
/// <see cref="SanitizationOptions.EnableMetrics"/> respectively.
/// </para>
/// <para>
/// <b>Traces</b>: Activities named <c>"Sanitization.Input"</c> for input sanitization and
/// <c>"Sanitization.Output"</c> for output encoding pipeline invocations.
/// </para>
/// <para>
/// <b>Metrics</b>:
/// <list type="bullet">
/// <item><c>sanitization.operations</c> — Total sanitization and encoding operations</item>
/// <item><c>sanitization.properties.processed</c> — Total properties sanitized or encoded</item>
/// <item><c>sanitization.duration</c> — Duration of sanitization/encoding operations in milliseconds</item>
/// <item><c>sanitization.failures</c> — Total failed sanitization/encoding operations</item>
/// </list>
/// </para>
/// </remarks>
internal static class SanitizationDiagnostics
{
    internal const string SourceName = "Encina.Security.Sanitization";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters
    internal static readonly Counter<long> OperationsTotal =
        Meter.CreateCounter<long>("sanitization.operations",
            description: "Total number of sanitization and encoding operations.");

    internal static readonly Counter<long> PropertiesProcessed =
        Meter.CreateCounter<long>("sanitization.properties.processed",
            description: "Total number of properties sanitized or encoded.");

    internal static readonly Counter<long> FailuresTotal =
        Meter.CreateCounter<long>("sanitization.failures",
            description: "Total number of failed sanitization and encoding operations.");

    // Histogram
    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>("sanitization.duration",
            unit: "ms",
            description: "Duration of sanitization and encoding operations in milliseconds.");

    // Activity tag name constants
    internal const string TagRequestType = "sanitization.request_type";
    internal const string TagOperation = "sanitization.operation";
    internal const string TagSanitizationType = "sanitization.type";
    internal const string TagProfile = "sanitization.profile";
    internal const string TagPropertyCount = "sanitization.property_count";
    internal const string TagOutcome = "sanitization.outcome";

    /// <summary>
    /// Starts a new activity for input sanitization pipeline processing.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type being processed.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartInputSanitization(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Sanitization.Input", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagOperation, "sanitize");
        return activity;
    }

    /// <summary>
    /// Starts a new activity for output encoding pipeline processing.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type being processed.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartOutputEncoding(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Sanitization.Output", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagOperation, "encode");
        return activity;
    }

    /// <summary>
    /// Records a property sanitization/encoding event on the activity.
    /// </summary>
    internal static void RecordOperationEvent(Activity? activity, string operation, int propertyCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.AddEvent(new ActivityEvent($"Sanitization.{operation}", tags: new ActivityTagsCollection
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
