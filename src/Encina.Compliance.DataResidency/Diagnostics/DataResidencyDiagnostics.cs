using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.DataResidency.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Data Residency observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.DataResidency</c>)
/// for fine-grained trace filtering, and a dedicated <see cref="Meter"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>residency.outcome</c>, <c>residency.data_category</c>,
/// <c>residency.source_region</c>, <c>residency.target_region</c>) to enable flexible dashboards
/// without creating separate counters per outcome.
/// </para>
/// </remarks>
internal static class DataResidencyDiagnostics
{
    internal const string SourceName = "Encina.Compliance.DataResidency";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    /// <summary>Outcome of the residency check: allowed, blocked, skipped, warning.</summary>
    internal const string TagOutcome = "residency.outcome";

    /// <summary>The request type name triggering the pipeline.</summary>
    internal const string TagRequestType = "residency.request_type";

    /// <summary>The response type name being tracked.</summary>
    internal const string TagResponseType = "residency.response_type";

    /// <summary>The data category subject to residency policy.</summary>
    internal const string TagDataCategory = "residency.data_category";

    /// <summary>The source region code (where data originates).</summary>
    internal const string TagSourceRegion = "residency.source_region";

    /// <summary>The target/destination region code (where data is stored or transferred).</summary>
    internal const string TagTargetRegion = "residency.target_region";

    /// <summary>The enforcement mode: Block, Warn, Disabled.</summary>
    internal const string TagEnforcementMode = "residency.enforcement_mode";

    /// <summary>The legal basis for a cross-border transfer.</summary>
    internal const string TagLegalBasis = "residency.legal_basis";

    /// <summary>The action being performed (e.g., policy_check, transfer_validate, location_record).</summary>
    internal const string TagAction = "residency.action";

    /// <summary>The reason a check or transfer failed.</summary>
    internal const string TagFailureReason = "residency.failure_reason";

    // ---- Counters ----

    /// <summary>
    /// Total data residency pipeline executions, tagged with <c>residency.outcome</c>.
    /// </summary>
    internal static readonly Counter<long> PipelineExecutionsTotal =
        Meter.CreateCounter<long>("residency.pipeline.executions.total",
            description: "Total number of data residency pipeline executions.");

    /// <summary>
    /// Total residency policy checks, tagged with <c>residency.outcome</c> and <c>residency.data_category</c>.
    /// </summary>
    internal static readonly Counter<long> PolicyChecksTotal =
        Meter.CreateCounter<long>("residency.policy.checks.total",
            description: "Total number of residency policy checks performed.");

    /// <summary>
    /// Total cross-border transfer validations, tagged with <c>residency.source_region</c>
    /// and <c>residency.target_region</c>.
    /// </summary>
    internal static readonly Counter<long> CrossBorderTransfersTotal =
        Meter.CreateCounter<long>("residency.transfers.total",
            description: "Total number of cross-border transfer validations.");

    /// <summary>
    /// Total cross-border transfers that were blocked, tagged with <c>residency.failure_reason</c>.
    /// </summary>
    internal static readonly Counter<long> TransfersBlockedTotal =
        Meter.CreateCounter<long>("residency.transfers.blocked.total",
            description: "Total number of cross-border transfers blocked.");

    /// <summary>
    /// Total data location records created, tagged with <c>residency.target_region</c>.
    /// </summary>
    internal static readonly Counter<long> LocationRecordsTotal =
        Meter.CreateCounter<long>("residency.locations.recorded.total",
            description: "Total number of data location records created.");

    /// <summary>
    /// Total residency policy violations detected, tagged with <c>residency.data_category</c>.
    /// </summary>
    internal static readonly Counter<long> ViolationsTotal =
        Meter.CreateCounter<long>("residency.violations.total",
            description: "Total number of residency policy violations detected.");

    /// <summary>
    /// Total residency audit trail entries recorded, tagged with <c>residency.action</c>.
    /// </summary>
    internal static readonly Counter<long> AuditEntriesTotal =
        Meter.CreateCounter<long>("residency.audit.entries.total",
            description: "Total number of residency audit entries recorded.");

    // ---- Service-level counters ----

    /// <summary>
    /// Total residency policies created via <c>IResidencyPolicyService</c>,
    /// tagged with <c>residency.data_category</c>.
    /// </summary>
    internal static readonly Counter<long> PoliciesCreatedTotal =
        Meter.CreateCounter<long>("residency.policies.created.total",
            description: "Total number of residency policies created.");

    /// <summary>
    /// Total residency policies updated via <c>IResidencyPolicyService</c>,
    /// tagged with <c>residency.data_category</c>.
    /// </summary>
    internal static readonly Counter<long> PoliciesUpdatedTotal =
        Meter.CreateCounter<long>("residency.policies.updated.total",
            description: "Total number of residency policies updated.");

    /// <summary>
    /// Total residency policies deleted via <c>IResidencyPolicyService</c>,
    /// tagged with <c>residency.data_category</c>.
    /// </summary>
    internal static readonly Counter<long> PoliciesDeletedTotal =
        Meter.CreateCounter<long>("residency.policies.deleted.total",
            description: "Total number of residency policies deleted.");

    /// <summary>
    /// Total data locations registered via <c>IDataLocationService</c>,
    /// tagged with <c>residency.target_region</c> and <c>residency.data_category</c>.
    /// </summary>
    internal static readonly Counter<long> LocationsRegisteredTotal =
        Meter.CreateCounter<long>("residency.locations.registered.total",
            description: "Total number of data locations registered.");

    /// <summary>
    /// Total data location migrations via <c>IDataLocationService</c>,
    /// tagged with <c>residency.source_region</c> and <c>residency.target_region</c>.
    /// </summary>
    internal static readonly Counter<long> LocationsMigratedTotal =
        Meter.CreateCounter<long>("residency.locations.migrated.total",
            description: "Total number of data location migrations.");

    /// <summary>
    /// Total data locations removed via <c>IDataLocationService</c>.
    /// </summary>
    internal static readonly Counter<long> LocationsRemovedTotal =
        Meter.CreateCounter<long>("residency.locations.removed.total",
            description: "Total number of data locations removed.");

    // ---- Histograms ----

    /// <summary>
    /// Duration of the data residency pipeline execution in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PipelineDuration =
        Meter.CreateHistogram<double>("residency.pipeline.duration",
            unit: "ms",
            description: "Duration of data residency pipeline execution in milliseconds.");

    /// <summary>
    /// Duration of cross-border transfer validation in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> TransferValidationDuration =
        Meter.CreateHistogram<double>("residency.transfer.validation.duration",
            unit: "ms",
            description: "Duration of cross-border transfer validation in milliseconds.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>Residency.Pipeline</c> activity for a pipeline behavior execution.
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

        var activity = ActivitySource.StartActivity("Residency.Pipeline", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagResponseType, responseTypeName);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Residency.TransferValidation</c> activity for a cross-border transfer check.
    /// </summary>
    /// <param name="sourceRegion">The source region code.</param>
    /// <param name="destinationRegion">The destination region code.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartTransferValidation(string sourceRegion, string destinationRegion)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Residency.TransferValidation", ActivityKind.Internal);
        activity?.SetTag(TagSourceRegion, sourceRegion);
        activity?.SetTag(TagTargetRegion, destinationRegion);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>Residency.LocationRecord</c> activity for recording a data location.
    /// </summary>
    /// <param name="entityId">The entity identifier being tracked.</param>
    /// <param name="regionCode">The region code where data is being stored.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartLocationRecord(string entityId, string regionCode)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Residency.LocationRecord", ActivityKind.Internal);
        activity?.SetTag("residency.entity_id", entityId);
        activity?.SetTag(TagTargetRegion, regionCode);
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
    /// Records a blocked outcome on an activity (residency enforcement denied the operation).
    /// </summary>
    /// <param name="activity">The activity to mark as blocked (may be <c>null</c>).</param>
    /// <param name="reason">The reason for blocking.</param>
    internal static void RecordBlocked(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "blocked");
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
    /// Records a warning outcome on an activity (residency violation detected in Warn mode).
    /// </summary>
    /// <param name="activity">The activity to mark as warned (may be <c>null</c>).</param>
    /// <param name="reason">The reason for the warning.</param>
    internal static void RecordWarning(Activity? activity, string reason)
    {
        activity?.SetTag(TagOutcome, "warning");
        activity?.SetTag(TagFailureReason, reason);
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
}
