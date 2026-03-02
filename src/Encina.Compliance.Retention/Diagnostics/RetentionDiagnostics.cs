using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.Retention.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Retention observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.Retention</c>)
/// for fine-grained trace filtering, and a dedicated <see cref="Meter"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>retention.outcome</c>, <c>retention.data_category</c>,
/// <c>retention.entity_id</c>) to enable flexible dashboards without creating separate counters per outcome.
/// </para>
/// </remarks>
internal static class RetentionDiagnostics
{
    internal const string SourceName = "Encina.Compliance.Retention";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    internal const string TagOutcome = "retention.outcome";
    internal const string TagEntityId = "retention.entity_id";
    internal const string TagDataCategory = "retention.data_category";
    internal const string TagRequestType = "retention.request_type";
    internal const string TagResponseType = "retention.response_type";
    internal const string TagEnforcementMode = "retention.enforcement_mode";
    internal const string TagFailureReason = "retention.failure_reason";
    internal const string TagAction = "retention.action";
    internal const string TagHoldId = "retention.hold_id";
    internal const string TagPolicyId = "retention.policy_id";
    internal const string TagDeletionOutcome = "retention.deletion_outcome";

    // ---- Counters ----

    /// <summary>
    /// Total retention pipeline executions, tagged with <c>retention.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> PipelineExecutionsTotal =
        Meter.CreateCounter<long>("retention.pipeline.executions.total",
            description: "Total number of retention pipeline executions.");

    /// <summary>
    /// Total retention enforcement cycles, tagged with <c>retention.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> EnforcementCyclesTotal =
        Meter.CreateCounter<long>("retention.enforcement.cycles.total",
            description: "Total number of retention enforcement cycles.");

    /// <summary>
    /// Total retention records created via the pipeline, tagged with <c>retention.data_category</c>.
    /// </summary>
    internal static readonly Counter<long> RecordsCreatedTotal =
        Meter.CreateCounter<long>("retention.records.created.total",
            description: "Total number of retention records created.");

    /// <summary>
    /// Total retention records deleted during enforcement, tagged with <c>retention.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> RecordsDeletedTotal =
        Meter.CreateCounter<long>("retention.records.deleted.total",
            description: "Total number of retention records deleted.");

    /// <summary>
    /// Total retention records skipped due to legal hold, tagged with <c>retention.entity_id</c>.
    /// </summary>
    internal static readonly Counter<long> RecordsHeldTotal =
        Meter.CreateCounter<long>("retention.records.held.total",
            description: "Total number of retention records held due to legal hold.");

    /// <summary>
    /// Total retention record deletion failures, tagged with <c>retention.failure_reason</c>.
    /// </summary>
    internal static readonly Counter<long> RecordsFailedTotal =
        Meter.CreateCounter<long>("retention.records.failed.total",
            description: "Total number of retention record deletion failures.");

    /// <summary>
    /// Total legal holds applied, tagged with <c>retention.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> LegalHoldsAppliedTotal =
        Meter.CreateCounter<long>("retention.legal_holds.applied.total",
            description: "Total number of legal holds applied.");

    /// <summary>
    /// Total legal holds released, tagged with <c>retention.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> LegalHoldsReleasedTotal =
        Meter.CreateCounter<long>("retention.legal_holds.released.total",
            description: "Total number of legal holds released.");

    /// <summary>
    /// Total retention policy resolutions, tagged with <c>retention.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> PolicyResolutionsTotal =
        Meter.CreateCounter<long>("retention.policies.resolved.total",
            description: "Total number of retention policy resolutions.");

    /// <summary>
    /// Total audit trail entries recorded, tagged with <c>retention.action</c>.
    /// </summary>
    internal static readonly Counter<long> AuditEntriesTotal =
        Meter.CreateCounter<long>("retention.audit.entries.total",
            description: "Total number of retention audit entries recorded.");

    // ---- Histograms ----

    /// <summary>
    /// Duration of retention enforcement cycle in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> EnforcementDuration =
        Meter.CreateHistogram<double>("retention.enforcement.duration",
            unit: "ms",
            description: "Duration of retention enforcement cycle in milliseconds.");

    /// <summary>
    /// Duration of retention pipeline execution in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PipelineDuration =
        Meter.CreateHistogram<double>("retention.pipeline.duration",
            unit: "ms",
            description: "Duration of retention pipeline execution in milliseconds.");

    /// <summary>
    /// Duration of individual record deletion in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> DeletionDuration =
        Meter.CreateHistogram<double>("retention.deletion.duration",
            unit: "ms",
            description: "Duration of individual record deletion in milliseconds.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>Retention.Pipeline</c> activity for a pipeline behavior execution.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type triggering the pipeline.</param>
    /// <param name="responseTypeName">The name of the response type being tracked.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPipelineExecution(string requestTypeName, string responseTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Retention.Pipeline", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagResponseType, responseTypeName);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Retention.Enforcement</c> activity for an enforcement cycle.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartEnforcementCycle()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("Retention.Enforcement", ActivityKind.Internal);
    }

    /// <summary>
    /// Starts a new <c>Retention.Deletion</c> activity for an individual record deletion.
    /// </summary>
    /// <param name="entityId">The entity identifier being deleted.</param>
    /// <param name="dataCategory">The data category of the record.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartRecordDeletion(string entityId, string dataCategory)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Retention.Deletion", ActivityKind.Internal);
        activity?.SetTag(TagEntityId, entityId);
        activity?.SetTag(TagDataCategory, dataCategory);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Retention.LegalHold</c> activity for a legal hold operation (apply or release).
    /// </summary>
    /// <param name="holdId">The legal hold identifier.</param>
    /// <param name="entityId">The entity identifier targeted by the hold.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartLegalHoldOperation(string holdId, string entityId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Retention.LegalHold", ActivityKind.Internal);
        activity?.SetTag(TagHoldId, holdId);
        activity?.SetTag(TagEntityId, entityId);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Retention.PolicyResolution</c> activity for a retention policy lookup.
    /// </summary>
    /// <param name="dataCategory">The data category for which the policy is resolved.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPolicyResolution(string dataCategory)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Retention.PolicyResolution", ActivityKind.Internal);
        activity?.SetTag(TagDataCategory, dataCategory);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Retention.Audit</c> activity for an audit trail recording.
    /// </summary>
    /// <param name="action">The audit action being recorded.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartAuditRecording(string action)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Retention.Audit", ActivityKind.Internal);
        activity?.SetTag(TagAction, action);
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
    /// Records a successful completion on an activity with a record count.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="recordsProcessed">Number of records processed.</param>
    internal static void RecordCompleted(Activity? activity, int recordsProcessed)
    {
        activity?.SetTag(TagOutcome, "completed");
        activity?.SetTag("retention.records_processed", recordsProcessed);
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

    /// <summary>
    /// Records a held outcome on an activity (entity under legal hold, deletion skipped).
    /// </summary>
    /// <param name="activity">The activity to mark as held (may be <c>null</c>).</param>
    /// <param name="entityId">The entity ID under hold.</param>
    internal static void RecordHeld(Activity? activity, string entityId)
    {
        activity?.SetTag(TagOutcome, "held");
        activity?.SetTag(TagEntityId, entityId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
