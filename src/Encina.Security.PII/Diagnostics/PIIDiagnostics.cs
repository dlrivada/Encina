using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.PII.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina PII masking observability.
/// </summary>
/// <remarks>
/// <para>
/// All tracing and metrics are emitted under the <c>Encina.Security.PII</c> source name.
/// Tracing and metrics are gated by <see cref="PIIOptions.EnableTracing"/> and
/// <see cref="PIIOptions.EnableMetrics"/> respectively.
/// </para>
/// <para>
/// <b>Traces</b>: Activities named <c>"PII.MaskObject"</c>, <c>"PII.MaskProperty"</c>,
/// and <c>"PII.ApplyStrategy"</c> for PII masking pipeline invocations.
/// </para>
/// <para>
/// <b>Metrics</b>:
/// <list type="bullet">
/// <item><c>pii.masking.operations</c> — Total masking operations</item>
/// <item><c>pii.masking.duration</c> — Duration of masking operations in milliseconds</item>
/// <item><c>pii.masking.properties</c> — Total properties masked</item>
/// <item><c>pii.masking.errors</c> — Total masking errors</item>
/// </list>
/// </para>
/// </remarks>
internal static class PIIDiagnostics
{
    internal const string SourceName = "Encina.Security.PII";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Activity name constants
    internal const string ActivityMaskObject = "PII.MaskObject";
    internal const string ActivityMaskProperty = "PII.MaskProperty";
    internal const string ActivityApplyStrategy = "PII.ApplyStrategy";

    // Counters
    internal static readonly Counter<long> OperationsTotal =
        Meter.CreateCounter<long>("pii.masking.operations",
            description: "Total number of PII masking operations.");

    internal static readonly Counter<long> PropertiesMasked =
        Meter.CreateCounter<long>("pii.masking.properties",
            description: "Total number of properties masked.");

    internal static readonly Counter<long> ErrorsTotal =
        Meter.CreateCounter<long>("pii.masking.errors",
            description: "Total number of PII masking errors.");

    // Histogram
    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>("pii.masking.duration",
            unit: "ms",
            description: "Duration of PII masking operations in milliseconds.");

    // Activity tag name constants
    internal const string TagTypeName = "pii.type_name";
    internal const string TagPropertyCount = "pii.property_count";
    internal const string TagMaskedCount = "pii.masked_count";
    internal const string TagPiiType = "pii.pii_type";
    internal const string TagMaskingMode = "pii.masking_mode";
    internal const string TagPropertyName = "pii.property_name";
    internal const string TagStrategy = "pii.strategy";
    internal const string TagOutcome = "pii.outcome";
    internal const string TagErrorType = "pii.error_type";

    /// <summary>
    /// Starts a new activity for a MaskObject operation.
    /// </summary>
    /// <param name="typeName">The name of the type being masked.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartMaskObject(string typeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(ActivityMaskObject, ActivityKind.Internal);
        activity?.SetTag(TagTypeName, typeName);
        return activity;
    }

    /// <summary>
    /// Starts a child activity for strategy application on a specific property.
    /// </summary>
    /// <param name="propertyName">The name of the property being masked.</param>
    /// <param name="piiType">The PII type of the property.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartApplyStrategy(string propertyName, string piiType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(ActivityApplyStrategy, ActivityKind.Internal);
        activity?.SetTag(TagPropertyName, propertyName);
        activity?.SetTag(TagPiiType, piiType);
        return activity;
    }

    /// <summary>
    /// Records that the masking operation completed successfully.
    /// </summary>
    internal static void RecordSuccess(Activity? activity, int maskedCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(TagMaskedCount, maskedCount);
        activity.SetTag(TagOutcome, "success");
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records that the masking operation completed with an error.
    /// </summary>
    internal static void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(TagOutcome, "failure");
        activity.SetTag(TagErrorType, exception.GetType().Name);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }

    /// <summary>
    /// Records metrics for a completed masking operation.
    /// </summary>
    internal static void RecordOperationMetrics(
        string typeName,
        string maskingMode,
        bool success,
        int maskedCount,
        double elapsedMs)
    {
        var tags = new TagList
        {
            { TagPiiType, typeName },
            { TagMaskingMode, maskingMode },
            { TagOutcome, success ? "success" : "failure" }
        };

        OperationsTotal.Add(1, tags);
        OperationDuration.Record(elapsedMs, tags);

        if (maskedCount > 0)
        {
            PropertiesMasked.Add(maskedCount, tags);
        }
    }

    /// <summary>
    /// Records an error metric.
    /// </summary>
    internal static void RecordErrorMetric(string errorType)
    {
        ErrorsTotal.Add(1, new TagList { { TagErrorType, errorType } });
    }

    // Pipeline counter
    internal static readonly Counter<long> PipelineOperationsTotal =
        Meter.CreateCounter<long>("pii.pipeline.operations",
            description: "Total number of PII pipeline masking operations.");

    /// <summary>
    /// Records metrics for a pipeline masking operation.
    /// </summary>
    internal static void RecordPipelineMetrics(string responseType, string outcome)
    {
        PipelineOperationsTotal.Add(1, new TagList
        {
            { TagTypeName, responseType },
            { TagOutcome, outcome }
        });
    }
}
