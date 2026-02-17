using System.Diagnostics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Provides the activity source for repository pattern distributed tracing.
/// </summary>
/// <remarks>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </remarks>
internal static class RepositoryActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Repository", "1.0");

    /// <summary>
    /// The activity source name for external registration.
    /// </summary>
    internal const string SourceName = "Encina.Repository";

    /// <summary>
    /// Starts a repository operation activity.
    /// </summary>
    /// <param name="operation">The operation name (e.g., "get_by_id", "find", "add", "update", "remove").</param>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="provider">The data access provider (e.g., "ef_core", "dapper", "ado", "mongodb").</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartOperation(string operation, string entityType, string provider)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            $"encina.repository.{operation}", ActivityKind.Internal);
        activity?.SetTag("repository.operation", operation);
        activity?.SetTag("repository.entity_type", entityType);
        activity?.SetTag("repository.provider", provider);
        return activity;
    }

    /// <summary>
    /// Completes a repository operation activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="resultCount">The optional number of results returned.</param>
    internal static void Complete(Activity? activity, int? resultCount = null)
    {
        if (activity is null)
        {
            return;
        }

        if (resultCount.HasValue)
        {
            activity.SetTag("repository.result_count", resultCount.Value);
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks a repository operation activity as failed.
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
