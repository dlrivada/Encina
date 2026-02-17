using System.Diagnostics;

namespace Encina.OpenTelemetry.Enrichers;

/// <summary>
/// Enriches OpenTelemetry activities with audit trail information.
/// </summary>
public static class AuditActivityEnricher
{
    /// <summary>
    /// Enriches an activity with audit entry recording details.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="entityType">The audited entity type.</param>
    /// <param name="action">The audit action (e.g., "created", "updated", "deleted").</param>
    public static void EnrichWithAuditRecord(
        Activity? activity,
        string entityType,
        string action)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Audit.EntityType, entityType);
        activity.SetTag(ActivityTagNames.Audit.Action, action);
    }

    /// <summary>
    /// Enriches an activity with audit query details.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="queryType">The audit query type (e.g., "by_entity", "by_user", "by_correlation_id").</param>
    /// <param name="entityType">The entity type being queried, if applicable.</param>
    public static void EnrichWithAuditQuery(
        Activity? activity,
        string queryType,
        string? entityType = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Audit.QueryType, queryType);

        if (entityType is not null)
        {
            activity.SetTag(ActivityTagNames.Audit.EntityType, entityType);
        }
    }
}
