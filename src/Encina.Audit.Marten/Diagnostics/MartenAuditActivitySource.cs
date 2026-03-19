using System.Diagnostics;

namespace Encina.Audit.Marten.Diagnostics;

/// <summary>
/// Provides the activity source for Marten event-sourced audit distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="System.Diagnostics.ActivitySource"/> (<c>Encina.Audit.Marten</c>)
/// for fine-grained trace filtering. All methods guard with
/// <see cref="System.Diagnostics.ActivitySource.HasListeners()"/> for zero-cost when no
/// OpenTelemetry listener is attached.
/// </para>
/// <para>
/// Activity names follow the <c>AuditMarten.{Operation}</c> convention:
/// <list type="bullet">
/// <item><c>AuditMarten.Record</c> — recording an encrypted audit entry</item>
/// <item><c>AuditMarten.Query</c> — querying projected read models</item>
/// <item><c>AuditMarten.Purge</c> — crypto-shredding temporal keys</item>
/// <item><c>AuditMarten.Encrypt</c> — encrypting PII fields</item>
/// <item><c>AuditMarten.Decrypt</c> — decrypting PII fields (in projections)</item>
/// </list>
/// </para>
/// </remarks>
internal static class MartenAuditActivitySource
{
    /// <summary>
    /// The activity source name for external registration with OpenTelemetry.
    /// </summary>
    internal const string SourceName = "Encina.Audit.Marten";

    /// <summary>
    /// The activity source version.
    /// </summary>
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);

    // ── Tag constants ────────────────────────────────────────────────────

    internal const string TagEntityType = "audit.marten.entity_type";
    internal const string TagAction = "audit.marten.action";
    internal const string TagStreamId = "audit.marten.stream_id";
    internal const string TagPeriod = "audit.marten.period";
    internal const string TagQueryType = "audit.marten.query_type";
    internal const string TagResultCount = "audit.marten.result_count";
    internal const string TagDestroyedCount = "audit.marten.destroyed_count";
    internal const string TagErrorMessage = "audit.marten.error_message";

    // ── Activity starters ────────────────────────────────────────────────

    /// <summary>
    /// Starts an <c>AuditMarten.Record</c> activity for recording an encrypted audit entry.
    /// </summary>
    internal static Activity? StartRecord(string entityType, string action, string streamId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("AuditMarten.Record", ActivityKind.Internal);
        activity?.SetTag(TagEntityType, entityType);
        activity?.SetTag(TagAction, action);
        activity?.SetTag(TagStreamId, streamId);
        return activity;
    }

    /// <summary>
    /// Starts an <c>AuditMarten.Query</c> activity for querying projected read models.
    /// </summary>
    internal static Activity? StartQuery(string queryType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("AuditMarten.Query", ActivityKind.Internal);
        activity?.SetTag(TagQueryType, queryType);
        return activity;
    }

    /// <summary>
    /// Starts an <c>AuditMarten.Purge</c> activity for crypto-shredding temporal keys.
    /// </summary>
    internal static Activity? StartPurge(string granularity)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("AuditMarten.Purge", ActivityKind.Internal);
        activity?.SetTag("audit.marten.granularity", granularity);
        return activity;
    }

    /// <summary>
    /// Starts an <c>AuditMarten.Encrypt</c> activity for PII field encryption.
    /// </summary>
    internal static Activity? StartEncrypt(string period)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("AuditMarten.Encrypt", ActivityKind.Internal);
        activity?.SetTag(TagPeriod, period);
        return activity;
    }

    /// <summary>
    /// Starts an <c>AuditMarten.Decrypt</c> activity for PII field decryption in projections.
    /// </summary>
    internal static Activity? StartDecrypt(string period)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("AuditMarten.Decrypt", ActivityKind.Internal);
        activity?.SetTag(TagPeriod, period);
        return activity;
    }

    // ── Outcome recorders ────────────────────────────────────────────────

    /// <summary>Records a successful completion on an activity.</summary>
    internal static void Complete(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>Marks an activity as failed.</summary>
    internal static void Failed(Activity? activity, string? errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            activity.SetTag(TagErrorMessage, errorMessage);
        }

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
