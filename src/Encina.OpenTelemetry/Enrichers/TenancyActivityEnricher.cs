using System.Diagnostics;

namespace Encina.OpenTelemetry.Enrichers;

/// <summary>
/// Enriches OpenTelemetry activities with multi-tenancy information.
/// </summary>
public static class TenancyActivityEnricher
{
    /// <summary>
    /// Enriches an activity with tenant resolution details.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="tenantId">The resolved tenant identifier.</param>
    /// <param name="strategy">The resolution strategy used (e.g., "header", "claim", "subdomain").</param>
    /// <param name="outcome">The resolution outcome (e.g., "success", "not_found", "error").</param>
    public static void EnrichWithResolution(
        Activity? activity,
        string? tenantId,
        string strategy,
        string outcome)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Tenancy.Strategy, strategy);
        activity.SetTag(ActivityTagNames.Tenancy.Outcome, outcome);

        if (tenantId is not null)
        {
            activity.SetTag(ActivityTagNames.Tenancy.TenantId, tenantId);
        }
    }

    /// <summary>
    /// Enriches an activity with tenant-scoped entity access details.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="tenantId">The tenant identifier for the scoped query.</param>
    /// <param name="entityType">The entity type being accessed in tenant scope.</param>
    public static void EnrichWithTenantScope(
        Activity? activity,
        string tenantId,
        string entityType)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Tenancy.TenantId, tenantId);
        activity.SetTag(ActivityTagNames.Tenancy.EntityType, entityType);
    }
}
