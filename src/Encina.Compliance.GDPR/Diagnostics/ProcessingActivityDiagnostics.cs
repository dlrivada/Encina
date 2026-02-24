using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.GDPR.Diagnostics;

/// <summary>
/// Provides the dedicated activity source and metrics for processing activity registry observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.GDPR.ProcessingActivity</c>) for
/// fine-grained trace filtering, while reusing the shared <c>Encina.Compliance.GDPR</c> meter
/// from <see cref="GDPRDiagnostics"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>operation</c>, <c>outcome</c>) to enable flexible
/// dashboards without creating separate counters per outcome.
/// </para>
/// <para>
/// Counter increments always fire regardless of whether an <see cref="Activity"/> listener is attached,
/// ensuring metric collection is independent of distributed tracing configuration.
/// </para>
/// </remarks>
internal static class ProcessingActivityDiagnostics
{
    internal const string SourceName = "Encina.Compliance.GDPR.ProcessingActivity";
    internal const string SourceVersion = "1.0";

    /// <summary>
    /// Dedicated activity source for processing activity registry traces.
    /// </summary>
    internal static readonly ActivitySource Source = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    internal const string TagOperation = "operation";
    internal const string TagOutcome = "outcome";
    internal const string TagRequestType = "request.type";
    internal const string TagActivityCount = "activity.count";
    internal const string TagFailureReason = "failure_reason";
    internal const string TagProvider = "provider";

    // ---- Counters (reuse existing GDPR meter) ----

    /// <summary>
    /// Total processing activity registry operations, tagged with <c>operation</c> and <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> OperationsTotal =
        GDPRDiagnostics.Meter.CreateCounter<long>(
            "processing_activity_operations_total",
            description: "Total number of processing activity registry operations.");

    /// <summary>
    /// Total failed processing activity registry operations, tagged with <c>operation</c> and <c>failure_reason</c>.
    /// </summary>
    internal static readonly Counter<long> OperationsFailedTotal =
        GDPRDiagnostics.Meter.CreateCounter<long>(
            "processing_activity_operations_failed_total",
            description: "Total number of failed processing activity registry operations.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>ProcessingActivity.Register</c> activity with initial tags.
    /// </summary>
    /// <param name="requestType">The request type being registered.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartRegistration(Type requestType)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("ProcessingActivity.Register", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "register");
        activity?.SetTag(TagRequestType, requestType.Name);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>ProcessingActivity.Update</c> activity with initial tags.
    /// </summary>
    /// <param name="requestType">The request type being updated.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartUpdate(Type requestType)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("ProcessingActivity.Update", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "update");
        activity?.SetTag(TagRequestType, requestType.Name);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>ProcessingActivity.GetByRequestType</c> activity with initial tags.
    /// </summary>
    /// <param name="requestType">The request type being retrieved.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartGetByRequestType(Type requestType)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("ProcessingActivity.GetByRequestType", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "get_by_request_type");
        activity?.SetTag(TagRequestType, requestType.Name);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>ProcessingActivity.GetAll</c> activity.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartGetAll()
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("ProcessingActivity.GetAll", ActivityKind.Internal);
        activity?.SetTag(TagOperation, "get_all");
        return activity;
    }

    /// <summary>
    /// Records a successful operation outcome on the activity and increments the operations counter.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="operation">The operation name for counter tagging.</param>
    internal static void RecordSuccess(Activity? activity, string operation = "unknown")
    {
        activity?.SetTag(TagOutcome, "success");
        activity?.SetStatus(ActivityStatusCode.Ok);

        OperationsTotal.Add(1,
            new KeyValuePair<string, object?>(TagOperation, operation),
            new KeyValuePair<string, object?>(TagOutcome, "success"));
    }

    /// <summary>
    /// Records a successful retrieval with the count of returned activities and increments the operations counter.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="count">The number of activities returned.</param>
    /// <param name="operation">The operation name for counter tagging.</param>
    internal static void RecordSuccess(Activity? activity, int count, string operation = "unknown")
    {
        activity?.SetTag(TagOutcome, "success");
        activity?.SetTag(TagActivityCount, count);
        activity?.SetStatus(ActivityStatusCode.Ok);

        OperationsTotal.Add(1,
            new KeyValuePair<string, object?>(TagOperation, operation),
            new KeyValuePair<string, object?>(TagOutcome, "success"));
    }

    /// <summary>
    /// Records a failed operation outcome on the activity and increments the failure counter.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="operation">The operation name for counter tagging.</param>
    /// <param name="reason">The failure reason.</param>
    internal static void RecordFailure(Activity? activity, string operation = "unknown", string? reason = null)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, reason ?? "unknown");
        activity?.SetStatus(ActivityStatusCode.Error, reason);

        OperationsTotal.Add(1,
            new KeyValuePair<string, object?>(TagOperation, operation),
            new KeyValuePair<string, object?>(TagOutcome, "failed"));

        OperationsFailedTotal.Add(1,
            new KeyValuePair<string, object?>(TagOperation, operation),
            new KeyValuePair<string, object?>(TagFailureReason, reason ?? "unknown"));
    }
}
