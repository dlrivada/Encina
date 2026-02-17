using System.Diagnostics;

namespace Encina.Tenancy.Diagnostics;

/// <summary>
/// Provides the activity source for multi-tenancy distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Tenancy"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// tenancy traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class TenancyActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Tenancy", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.Tenancy";

    /// <summary>
    /// Starts a tenant resolution activity.
    /// </summary>
    /// <param name="strategy">The tenant resolution strategy name.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartResolution(string strategy)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.tenancy.resolve", ActivityKind.Internal);
        activity?.SetTag("tenancy.strategy", strategy);
        return activity;
    }

    /// <summary>
    /// Starts a tenant-scoped query activity.
    /// </summary>
    /// <param name="tenantId">The resolved tenant identifier.</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartTenantQuery(string tenantId, string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.tenancy.query", ActivityKind.Internal);
        activity?.SetTag("tenancy.tenant_id", tenantId);
        activity?.SetTag("tenancy.entity_type", entityType);
        return activity;
    }

    /// <summary>
    /// Completes a tenant resolution activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="tenantId">The resolved tenant identifier.</param>
    internal static void CompleteResolution(Activity? activity, string tenantId)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("tenancy.tenant_id", tenantId);
        activity.SetTag("tenancy.outcome", "success");
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Completes a tenancy activity successfully.
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
    /// Marks a tenancy activity as failed.
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

        activity.SetTag("tenancy.outcome", "error");
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
