using System.Diagnostics;

namespace Encina.Cdc.Caching.Diagnostics;

/// <summary>
/// Provides the activity source for CDC-driven cache invalidation tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Cdc.CacheInvalidation"</c>)
/// to allow independent subscription filtering for cache invalidation traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class CacheInvalidationActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Cdc.CacheInvalidation", "1.0");

    private const string TagTableName = "encina.cdc.cache.table_name";
    private const string TagEntityType = "encina.cdc.cache.entity_type";
    private const string TagCacheKeyPattern = "encina.cdc.cache.key_pattern";
    private const string TagOperation = "encina.cdc.cache.operation";
    private const string TagPubSubChannel = "encina.cdc.cache.pubsub_channel";

    /// <summary>
    /// Starts an invalidation activity for a CDC-detected change.
    /// </summary>
    /// <param name="tableName">The source table name from CDC.</param>
    /// <param name="operation">The change operation type.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartInvalidation(string tableName, string operation)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "Encina.Cdc.CacheInvalidation.Invalidate", ActivityKind.Internal);
        activity?.SetTag(TagTableName, tableName);
        activity?.SetTag(TagOperation, operation);
        return activity;
    }

    /// <summary>
    /// Records the resolved entity type on an invalidation activity.
    /// </summary>
    /// <param name="activity">The invalidation activity.</param>
    /// <param name="entityType">The resolved entity type name.</param>
    /// <param name="cacheKeyPattern">The generated cache key pattern.</param>
    internal static void SetResolution(Activity? activity, string entityType, string cacheKeyPattern)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(TagEntityType, entityType);
        activity.SetTag(TagCacheKeyPattern, cacheKeyPattern);
    }

    /// <summary>
    /// Completes an invalidation activity successfully.
    /// </summary>
    /// <param name="activity">The invalidation activity to complete.</param>
    /// <param name="broadcastSent">Whether a pub/sub broadcast was sent.</param>
    internal static void InvalidationCompleted(Activity? activity, bool broadcastSent)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("encina.cdc.cache.broadcast_sent", broadcastSent);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks an invalidation activity as failed.
    /// </summary>
    /// <param name="activity">The invalidation activity to mark as failed.</param>
    /// <param name="errorMessage">The error message.</param>
    internal static void InvalidationFailed(Activity? activity, string? errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }

    /// <summary>
    /// Marks an invalidation activity as skipped due to table filtering.
    /// </summary>
    /// <param name="activity">The invalidation activity to complete.</param>
    internal static void InvalidationSkipped(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("encina.cdc.cache.skipped", true);
        activity.SetStatus(ActivityStatusCode.Ok, "Table filtered out");
        activity.Dispose();
    }

    /// <summary>
    /// Starts a pub/sub broadcast child activity.
    /// </summary>
    /// <param name="channel">The pub/sub channel name.</param>
    /// <param name="pattern">The invalidation pattern being broadcast.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartBroadcast(string channel, string pattern)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "Encina.Cdc.CacheInvalidation.Broadcast", ActivityKind.Producer);
        activity?.SetTag(TagPubSubChannel, channel);
        activity?.SetTag(TagCacheKeyPattern, pattern);
        return activity;
    }

    /// <summary>
    /// Completes a broadcast activity.
    /// </summary>
    /// <param name="activity">The broadcast activity to complete.</param>
    /// <param name="isSuccess">Whether the broadcast succeeded.</param>
    /// <param name="errorMessage">Optional error message if the broadcast failed.</param>
    internal static void CompleteBroadcast(Activity? activity, bool isSuccess, string? errorMessage = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(isSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
