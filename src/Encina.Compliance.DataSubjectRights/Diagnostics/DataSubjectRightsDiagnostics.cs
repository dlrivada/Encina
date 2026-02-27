using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.DataSubjectRights.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina Data Subject Rights observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.DataSubjectRights</c>)
/// for fine-grained trace filtering, and a dedicated <see cref="Meter"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>right_type</c>, <c>outcome</c>, <c>format</c>)
/// to enable flexible dashboards without creating separate counters per outcome.
/// </para>
/// </remarks>
internal static class DataSubjectRightsDiagnostics
{
    internal const string SourceName = "Encina.Compliance.DataSubjectRights";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    internal const string TagRightType = "dsr.right_type";
    internal const string TagOutcome = "dsr.outcome";
    internal const string TagSubjectId = "dsr.subject_id";
    internal const string TagFormat = "dsr.format";
    internal const string TagFailureReason = "dsr.failure_reason";
    internal const string TagRequestType = "dsr.request_type";
    internal const string TagEnforcementMode = "dsr.enforcement_mode";

    // ---- Counters ----

    /// <summary>
    /// Total DSR requests processed, tagged with <c>right_type</c> and <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> RequestsTotal =
        Meter.CreateCounter<long>("dsr.requests.total",
            description: "Total number of data subject rights requests processed.");

    /// <summary>
    /// Total erasure fields erased.
    /// </summary>
    internal static readonly Counter<long> ErasureFieldsErasedTotal =
        Meter.CreateCounter<long>("dsr.erasure.fields_erased.total",
            description: "Total number of personal data fields erased.");

    /// <summary>
    /// Total erasure fields retained (legal retention or non-erasable).
    /// </summary>
    internal static readonly Counter<long> ErasureFieldsRetainedTotal =
        Meter.CreateCounter<long>("dsr.erasure.fields_retained.total",
            description: "Total number of personal data fields retained due to legal requirements.");

    /// <summary>
    /// Total portability exports, tagged with <c>format</c> and <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> PortabilityExportsTotal =
        Meter.CreateCounter<long>("dsr.portability.exports.total",
            description: "Total number of data portability export operations.");

    /// <summary>
    /// Total processing restriction checks, tagged with <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> RestrictionChecksTotal =
        Meter.CreateCounter<long>("dsr.restriction.checks.total",
            description: "Total number of processing restriction checks.");

    // ---- Histograms ----

    /// <summary>
    /// Duration of DSR request handling in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("dsr.request.duration",
            unit: "ms",
            description: "Duration of data subject rights request handling in milliseconds.");

    /// <summary>
    /// Duration of erasure operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> ErasureDuration =
        Meter.CreateHistogram<double>("dsr.erasure.duration",
            unit: "ms",
            description: "Duration of data erasure operations in milliseconds.");

    /// <summary>
    /// Duration of portability export operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PortabilityDuration =
        Meter.CreateHistogram<double>("dsr.portability.duration",
            unit: "ms",
            description: "Duration of data portability export operations in milliseconds.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>DSR.Request</c> activity for a data subject right operation.
    /// </summary>
    /// <param name="rightType">The type of data subject right being exercised.</param>
    /// <param name="subjectId">The data subject identifier.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartDSRRequest(DataSubjectRight rightType, string subjectId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DSR.Request", ActivityKind.Internal);
        activity?.SetTag(TagRightType, rightType.ToString());
        activity?.SetTag(TagSubjectId, subjectId);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>DSR.Erasure</c> activity for a data erasure operation.
    /// </summary>
    /// <param name="subjectId">The data subject identifier.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartErasure(string subjectId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DSR.Erasure", ActivityKind.Internal);
        activity?.SetTag(TagSubjectId, subjectId);
        return activity;
    }

    /// <summary>
    /// Starts a new <c>DSR.Portability.Export</c> activity for a data portability export.
    /// </summary>
    /// <param name="subjectId">The data subject identifier.</param>
    /// <param name="format">The export format requested.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPortabilityExport(string subjectId, ExportFormat format)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DSR.Portability.Export", ActivityKind.Internal);
        activity?.SetTag(TagSubjectId, subjectId);
        activity?.SetTag(TagFormat, format.ToString());
        return activity;
    }

    /// <summary>
    /// Starts a new <c>DSR.Restriction.Check</c> activity for a processing restriction check.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type being checked.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartRestrictionCheck(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("DSR.Restriction.Check", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

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
    /// Records a skipped outcome on an activity.
    /// </summary>
    /// <param name="activity">The activity to mark as skipped (may be <c>null</c>).</param>
    internal static void RecordSkipped(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "skipped");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a blocked outcome on an activity (restriction enforcement).
    /// </summary>
    /// <param name="activity">The activity to mark as blocked (may be <c>null</c>).</param>
    /// <param name="subjectId">The restricted data subject identifier.</param>
    internal static void RecordBlocked(Activity? activity, string subjectId)
    {
        activity?.SetTag(TagOutcome, "blocked");
        activity?.SetTag(TagSubjectId, subjectId);
        activity?.SetStatus(ActivityStatusCode.Error, "Processing restriction active");
    }

    /// <summary>
    /// Records a warned outcome on an activity (restriction in warn mode).
    /// </summary>
    /// <param name="activity">The activity to mark as warned (may be <c>null</c>).</param>
    /// <param name="subjectId">The restricted data subject identifier.</param>
    internal static void RecordWarned(Activity? activity, string subjectId)
    {
        activity?.SetTag(TagOutcome, "warned");
        activity?.SetTag(TagSubjectId, subjectId);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
