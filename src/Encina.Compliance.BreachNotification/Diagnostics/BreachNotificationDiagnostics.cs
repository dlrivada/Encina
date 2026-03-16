using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.BreachNotification.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Breach Notification observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.BreachNotification</c>)
/// for fine-grained trace filtering, and a dedicated <see cref="Meter"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>breach.severity</c>, <c>breach.outcome</c>,
/// <c>breach.detection_rule</c>, <c>breach.status</c>) to enable flexible dashboards
/// without creating separate counters per outcome.
/// </para>
/// <para>
/// Key metric: <c>breach.time_to_notification.hours</c> measures compliance with the
/// GDPR Article 33(1) 72-hour deadline for supervisory authority notification.
/// </para>
/// <para>
/// The breach notification module uses Marten event sourcing for aggregate persistence.
/// Audit trail data is derived from the event stream rather than a separate store.
/// Cache-aside pattern is used for read model queries via <c>ICacheProvider</c>.
/// </para>
/// </remarks>
internal static class BreachNotificationDiagnostics
{
    internal const string SourceName = "Encina.Compliance.BreachNotification";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    /// <summary>Severity of the breach: Low, Medium, High, Critical.</summary>
    internal const string TagSeverity = "breach.severity";

    /// <summary>Outcome of the operation: detected, passed, skipped, sent, failed, pending, completed.</summary>
    internal const string TagOutcome = "breach.outcome";

    /// <summary>Name of the detection rule that triggered the breach.</summary>
    internal const string TagDetectionRule = "breach.detection_rule";

    /// <summary>The unique identifier of the breach.</summary>
    internal const string TagBreachId = "breach.id";

    /// <summary>Current lifecycle status of the breach.</summary>
    internal const string TagStatus = "breach.status";

    /// <summary>Number of data subjects affected by the breach or notification.</summary>
    internal const string TagSubjectCount = "breach.subject_count";

    /// <summary>The request type name triggering the pipeline.</summary>
    internal const string TagRequestType = "breach.request_type";

    /// <summary>The notification type: authority, subjects.</summary>
    internal const string TagNotificationType = "breach.notification_type";

    /// <summary>The reason a check or operation failed.</summary>
    internal const string TagFailureReason = "breach.failure_reason";

    /// <summary>The cache key used for a cache-aside lookup.</summary>
    internal const string TagCacheKey = "breach.cache_key";

    /// <summary>The name of the service operation being executed.</summary>
    internal const string TagOperationName = "breach.operation";

    // ---- Counters ----

    /// <summary>
    /// Total breaches detected, tagged with <c>breach.severity</c> and <c>breach.detection_rule</c>.
    /// </summary>
    internal static readonly Counter<long> BreachesDetectedTotal =
        Meter.CreateCounter<long>("breach.detected.total",
            description: "Total number of breaches detected.");

    /// <summary>
    /// Total authority notifications sent, tagged with <c>breach.outcome</c> (sent, failed, pending).
    /// </summary>
    internal static readonly Counter<long> AuthorityNotificationsTotal =
        Meter.CreateCounter<long>("breach.notification.authority.total",
            description: "Total number of authority notification attempts.");

    /// <summary>
    /// Total data subject notifications sent, tagged with <c>breach.outcome</c> and <c>breach.subject_count</c>.
    /// </summary>
    internal static readonly Counter<long> SubjectNotificationsTotal =
        Meter.CreateCounter<long>("breach.notification.subjects.total",
            description: "Total number of data subject notification attempts.");

    /// <summary>
    /// Total pipeline executions, tagged with <c>breach.outcome</c> (detected, passed, skipped).
    /// </summary>
    internal static readonly Counter<long> PipelineExecutionsTotal =
        Meter.CreateCounter<long>("breach.pipeline.executions.total",
            description: "Total number of breach detection pipeline executions.");

    /// <summary>
    /// Total phased reports submitted, tagged with <c>breach.id</c>.
    /// </summary>
    internal static readonly Counter<long> PhasedReportsTotal =
        Meter.CreateCounter<long>("breach.phased_reports.total",
            description: "Total number of phased reports submitted.");

    /// <summary>
    /// Total breaches resolved, tagged with <c>breach.severity</c>.
    /// </summary>
    internal static readonly Counter<long> BreachesResolvedTotal =
        Meter.CreateCounter<long>("breach.resolved.total",
            description: "Total number of breaches resolved.");

    /// <summary>
    /// Total breaches assessed via the service.
    /// </summary>
    internal static readonly Counter<long> BreachesAssessedTotal =
        Meter.CreateCounter<long>("breach.assessed.total",
            description: "Total number of breaches assessed.");

    /// <summary>
    /// Total breaches closed via the service.
    /// </summary>
    internal static readonly Counter<long> BreachesClosedTotal =
        Meter.CreateCounter<long>("breach.closed.total",
            description: "Total number of breaches closed.");

    /// <summary>
    /// Total cache hits for breach read model queries.
    /// </summary>
    internal static readonly Counter<long> CacheHitsTotal =
        Meter.CreateCounter<long>("breach.cache.hits.total",
            description: "Total cache hits for breach read model queries.");

    /// <summary>
    /// Total cache misses for breach read model queries.
    /// </summary>
    internal static readonly Counter<long> CacheMissesTotal =
        Meter.CreateCounter<long>("breach.cache.misses.total",
            description: "Total cache misses for breach read model queries.");

    /// <summary>
    /// Total aggregate load operations from the event store.
    /// </summary>
    internal static readonly Counter<long> AggregateLoadsTotal =
        Meter.CreateCounter<long>("breach.aggregate.loads.total",
            description: "Total breach aggregate loads from event store.");

    /// <summary>
    /// Total aggregate save operations to the event store.
    /// </summary>
    internal static readonly Counter<long> AggregateSavesTotal =
        Meter.CreateCounter<long>("breach.aggregate.saves.total",
            description: "Total breach aggregate saves to event store.");

    // ---- Histograms ----

    /// <summary>
    /// Time from breach detection to authority notification in hours.
    /// Key compliance metric for the GDPR Article 33(1) 72-hour deadline.
    /// </summary>
    internal static readonly Histogram<double> TimeToNotification =
        Meter.CreateHistogram<double>("breach.time_to_notification.hours",
            unit: "h",
            description: "Time from breach detection to authority notification in hours.");

    /// <summary>
    /// Duration of detection rule evaluation in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> DetectionDuration =
        Meter.CreateHistogram<double>("breach.detection.duration.ms",
            unit: "ms",
            description: "Duration of detection rule evaluation in milliseconds.");

    /// <summary>
    /// Duration of pipeline behavior execution in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PipelineDuration =
        Meter.CreateHistogram<double>("breach.pipeline.duration.ms",
            unit: "ms",
            description: "Duration of breach detection pipeline behavior execution in milliseconds.");

    /// <summary>
    /// Duration of service operations (aggregate load/save, queries) in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> ServiceOperationDuration =
        Meter.CreateHistogram<double>("breach.service.duration.ms",
            unit: "ms",
            description: "Duration of breach notification service operations in milliseconds.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>BreachNotification.Detection</c> activity for a detection rule evaluation.
    /// </summary>
    /// <param name="ruleName">The name of the detection rule being evaluated.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartBreachDetection(string ruleName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("BreachNotification.Detection", ActivityKind.Internal);
        activity?.SetTag(TagDetectionRule, ruleName);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>BreachNotification.Notification</c> activity for a notification operation.
    /// </summary>
    /// <param name="type">The notification type (e.g., "authority", "subjects").</param>
    /// <param name="breachId">The identifier of the breach being notified.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartNotification(string type, Guid breachId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("BreachNotification.Notification", ActivityKind.Internal);
        activity?.SetTag(TagNotificationType, type);
        activity?.SetTag(TagBreachId, breachId.ToString());
        return activity;
    }

    /// <summary>
    /// Starts a new <c>BreachNotification.Pipeline</c> activity for a pipeline behavior execution.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type triggering the pipeline.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPipelineExecution(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("BreachNotification.Pipeline", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>BreachNotification.DeadlineCheck</c> activity for a deadline monitoring cycle.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartDeadlineCheck()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("BreachNotification.DeadlineCheck", ActivityKind.Internal);
    }

    /// <summary>
    /// Starts a new <c>BreachNotification.ServiceOperation</c> activity for a service-level operation
    /// (e.g., RecordBreach, AssessBreach, ReportToDPA).
    /// </summary>
    /// <param name="operationName">The name of the service operation.</param>
    /// <param name="breachId">The breach identifier, if applicable.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartServiceOperation(string operationName, Guid? breachId = null)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("BreachNotification.ServiceOperation", ActivityKind.Internal);
        activity?.SetTag(TagOperationName, operationName);
        if (breachId.HasValue)
        {
            activity?.SetTag(TagBreachId, breachId.Value.ToString());
        }

        return activity;
    }

    // ---- Outcome recorders ----

    /// <summary>
    /// Records a successful completion on an activity.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    internal static void RecordCompleted(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a failure on an activity.
    /// </summary>
    /// <param name="activity">The activity to mark as failed (may be <c>null</c>).</param>
    /// <param name="reason">The failure reason.</param>
    internal static void RecordFailed(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "failed");
        activity?.SetTag(TagFailureReason, reason);
        activity?.SetStatus(ActivityStatusCode.Error, reason);
    }

    /// <summary>
    /// Records a skipped outcome on an activity (enforcement disabled or no attributes found).
    /// </summary>
    /// <param name="activity">The activity to mark as skipped (may be <c>null</c>).</param>
    internal static void RecordSkipped(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "skipped");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
