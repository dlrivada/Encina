using System.Diagnostics;

namespace Encina.Security.Audit.Diagnostics;

/// <summary>
/// Provides the activity source for read audit distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.ReadAudit</c>)
/// for fine-grained trace filtering separate from write-audit tracing.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// <para>
/// Activity names follow the <c>ReadAudit.{Operation}</c> convention:
/// <list type="bullet">
/// <item><description><c>ReadAudit.LogRead</c> — recording a read access entry</description></item>
/// <item><description><c>ReadAudit.Query</c> — querying read audit entries</description></item>
/// <item><description><c>ReadAudit.Purge</c> — purging old audit entries</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class ReadAuditActivitySource
{
    /// <summary>
    /// The activity source name for external registration with OpenTelemetry.
    /// </summary>
    internal const string SourceName = "Encina.ReadAudit";

    /// <summary>
    /// The activity source version.
    /// </summary>
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    /// <summary>The entity type being read-audited.</summary>
    internal const string TagEntityType = "read_audit.entity_type";

    /// <summary>The method or operation that triggered the read audit.</summary>
    internal const string TagOperation = "read_audit.operation";

    /// <summary>The access method (Repository, DirectQuery, Api, Export, Custom).</summary>
    internal const string TagAccessMethod = "read_audit.access_method";

    /// <summary>The query type for query operations.</summary>
    internal const string TagQueryType = "read_audit.query_type";

    /// <summary>The number of entities affected by the operation.</summary>
    internal const string TagEntityCount = "read_audit.entity_count";

    /// <summary>The error message when an operation fails.</summary>
    internal const string TagErrorMessage = "read_audit.error_message";

    // ---- Activity starters ----

    /// <summary>
    /// Starts a <c>ReadAudit.LogRead</c> activity for recording a read access entry.
    /// </summary>
    /// <param name="entityType">The entity type being accessed.</param>
    /// <param name="operation">The repository method or operation name.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartLogRead(string entityType, string operation)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ReadAudit.LogRead", ActivityKind.Internal);
        activity?.SetTag(TagEntityType, entityType);
        activity?.SetTag(TagOperation, operation);
        return activity;
    }

    /// <summary>
    /// Starts a <c>ReadAudit.Query</c> activity for querying read audit entries.
    /// </summary>
    /// <param name="queryType">The query type (access_history, user_access_history, paginated_query).</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartQuery(string queryType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("ReadAudit.Query", ActivityKind.Internal);
        activity?.SetTag(TagQueryType, queryType);
        return activity;
    }

    /// <summary>
    /// Starts a <c>ReadAudit.Purge</c> activity for purging old audit entries.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartPurge()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("ReadAudit.Purge", ActivityKind.Internal);
    }

    // ---- Outcome recorders ----

    /// <summary>
    /// Records a successful completion on an activity.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    internal static void Complete(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks an activity as failed.
    /// </summary>
    /// <param name="activity">The activity to mark as failed (may be <c>null</c>).</param>
    /// <param name="errorMessage">The error message.</param>
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
