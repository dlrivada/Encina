using System.Diagnostics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Provides the activity source for audit trail distributed tracing.
/// </summary>
/// <remarks>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </remarks>
internal static class AuditActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Audit", "1.0");

    /// <summary>
    /// The activity source name for external registration.
    /// </summary>
    internal const string SourceName = "Encina.Audit";

    /// <summary>
    /// Starts an audit record activity.
    /// </summary>
    /// <param name="entityType">The audited entity type name.</param>
    /// <param name="action">The audit action (created, updated, deleted).</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartRecordAudit(string entityType, string action)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("encina.audit.record", ActivityKind.Internal);
        activity?.SetTag("audit.entity_type", entityType);
        activity?.SetTag("audit.action", action);
        return activity;
    }

    /// <summary>
    /// Starts an audit query activity.
    /// </summary>
    /// <param name="queryType">The query type (by_entity, by_date_range, all).</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartQueryAudit(string queryType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("encina.audit.query", ActivityKind.Internal);
        activity?.SetTag("audit.query_type", queryType);
        return activity;
    }

    /// <summary>
    /// Completes an audit activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
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
    /// Marks an audit activity as failed.
    /// </summary>
    /// <param name="activity">The activity to mark as failed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    internal static void Failed(Activity? activity, string? errorCode, string? errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            activity.SetTag("error.code", errorCode);
        }

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
