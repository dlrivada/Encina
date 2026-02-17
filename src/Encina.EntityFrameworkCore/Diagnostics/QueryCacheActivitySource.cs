using System.Diagnostics;

namespace Encina.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Provides the activity source for query caching distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.QueryCache"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// query cache traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class QueryCacheActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.QueryCache", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.QueryCache";

    /// <summary>
    /// Starts a cache lookup activity.
    /// </summary>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="queryHash">The hash of the query used as cache key.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartLookup(string entityType, string queryHash)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.querycache.lookup", ActivityKind.Internal);
        activity?.SetTag("querycache.entity_type", entityType);
        activity?.SetTag("querycache.query_hash", queryHash);
        return activity;
    }

    /// <summary>
    /// Starts a cache population activity.
    /// </summary>
    /// <param name="entityType">The entity type being cached.</param>
    /// <param name="queryHash">The hash of the query used as cache key.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartPopulate(string entityType, string queryHash)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.querycache.populate", ActivityKind.Internal);
        activity?.SetTag("querycache.entity_type", entityType);
        activity?.SetTag("querycache.query_hash", queryHash);
        return activity;
    }

    /// <summary>
    /// Starts a cache eviction activity.
    /// </summary>
    /// <param name="entityType">The entity type being evicted.</param>
    /// <param name="reason">The eviction reason (ttl, invalidation, manual).</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartEviction(string entityType, string reason)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.querycache.evict", ActivityKind.Internal);
        activity?.SetTag("querycache.entity_type", entityType);
        activity?.SetTag("querycache.eviction_reason", reason);
        return activity;
    }

    /// <summary>
    /// Completes a cache lookup activity with a hit result.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    internal static void CompleteHit(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("querycache.outcome", "hit");
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Completes a cache lookup activity with a miss result.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    internal static void CompleteMiss(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("querycache.outcome", "miss");
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Completes a cache activity successfully.
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
    /// Marks a cache activity as failed.
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
