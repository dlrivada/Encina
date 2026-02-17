using System.Diagnostics;

namespace Encina.OpenTelemetry.Enrichers;

/// <summary>
/// Enriches OpenTelemetry activities with repository pattern information.
/// </summary>
public static class RepositoryActivityEnricher
{
    /// <summary>
    /// Enriches an activity with repository operation details.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="operation">The repository operation (e.g., "get_by_id", "find", "add").</param>
    /// <param name="entityType">The entity type being accessed.</param>
    /// <param name="provider">The data access provider (e.g., "ef_core", "dapper", "ado").</param>
    /// <param name="resultCount">The number of results returned, if applicable.</param>
    public static void EnrichWithOperation(
        Activity? activity,
        string operation,
        string entityType,
        string provider,
        int? resultCount = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Repository.Operation, operation);
        activity.SetTag(ActivityTagNames.Repository.EntityType, entityType);
        activity.SetTag(ActivityTagNames.Repository.Provider, provider);

        if (resultCount.HasValue)
        {
            activity.SetTag(ActivityTagNames.Repository.ResultCount, resultCount.Value);
        }
    }

    /// <summary>
    /// Enriches an activity with repository error information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="operation">The repository operation that failed.</param>
    /// <param name="entityType">The entity type being accessed.</param>
    /// <param name="errorCode">The error code.</param>
    public static void EnrichWithError(
        Activity? activity,
        string operation,
        string entityType,
        string errorCode)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Repository.Operation, operation);
        activity.SetTag(ActivityTagNames.Repository.EntityType, entityType);
        activity.SetTag(ActivityTagNames.Repository.ErrorCode, errorCode);
    }
}
