using System.Diagnostics;

namespace Encina.Sharding.Diagnostics;

/// <summary>
/// Provides the activity source for sharding-related distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Sharding"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// sharding traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class ShardingActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Sharding", "1.0");

    /// <summary>
    /// Starts a routing activity for a shard key lookup.
    /// </summary>
    /// <param name="shardKey">The shard key being routed.</param>
    /// <param name="routerType">The router strategy name (e.g., "hash", "range", "directory", "geo").</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartRouting(string shardKey, string routerType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Sharding.Route", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.ShardKey, shardKey);
        activity?.SetTag(ActivityTagNames.RouterType, routerType);
        return activity;
    }

    /// <summary>
    /// Completes a routing activity with the resolved shard ID.
    /// </summary>
    /// <param name="activity">The routing activity to complete.</param>
    /// <param name="shardId">The resolved shard ID.</param>
    internal static void RoutingCompleted(Activity? activity, string shardId)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.ShardId, shardId);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks a routing activity as failed.
    /// </summary>
    /// <param name="activity">The routing activity to mark as failed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    internal static void RoutingFailed(Activity? activity, string? errorCode, string? errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            activity.SetTag(ActivityTagNames.FailureCode, errorCode);
        }

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }

    /// <summary>
    /// Starts a compound key routing activity.
    /// </summary>
    /// <param name="key">The compound shard key being routed.</param>
    /// <param name="routerType">The router strategy name (e.g., "compound").</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartCompoundRouting(CompoundShardKey key, string routerType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Sharding.CompoundRoute", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.ShardKey, key.ToString());
        activity?.SetTag(ActivityTagNames.RouterType, routerType);
        activity?.SetTag(ActivityTagNames.CompoundKeyComponents, key.ComponentCount);
        activity?.SetTag(ActivityTagNames.CompoundKeyPartial, key.ComponentCount < 2);
        return activity;
    }

    /// <summary>
    /// Adds per-component strategy information to a compound routing activity.
    /// </summary>
    /// <param name="activity">The compound routing activity.</param>
    /// <param name="strategyPerComponent">A description of strategies per component (e.g., "0:hash,1:range").</param>
    internal static void SetCompoundRoutingStrategies(Activity? activity, string strategyPerComponent)
    {
        activity?.SetTag(ActivityTagNames.CompoundKeyStrategyPerComponent, strategyPerComponent);
    }

    /// <summary>
    /// Starts a scatter-gather activity for a multi-shard query.
    /// </summary>
    /// <param name="shardCount">The number of shards to query.</param>
    /// <param name="strategy">The scatter-gather strategy (e.g., "all", "targeted").</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartScatterGather(int shardCount, string strategy)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Sharding.ScatterGather", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.ShardCount, shardCount);
        activity?.SetTag(ActivityTagNames.ScatterGatherStrategy, strategy);
        return activity;
    }

    /// <summary>
    /// Starts a child activity for a single shard query within a scatter-gather operation.
    /// </summary>
    /// <param name="shardId">The shard identifier being queried.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartShardQuery(string shardId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Sharding.ShardQuery", ActivityKind.Client);
        activity?.SetTag(ActivityTagNames.ShardId, shardId);
        return activity;
    }

    /// <summary>
    /// Completes a scatter-gather activity with result information.
    /// </summary>
    /// <param name="activity">The scatter-gather activity to complete.</param>
    /// <param name="successCount">Number of successful shard queries.</param>
    /// <param name="failedCount">Number of failed shard queries.</param>
    /// <param name="resultCount">Total number of result items.</param>
    internal static void CompleteScatterGather(
        Activity? activity,
        int successCount,
        int failedCount,
        int resultCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("encina.sharding.scatter.success_count", successCount);
        activity.SetTag("encina.sharding.scatter.failed_count", failedCount);
        activity.SetTag("encina.sharding.scatter.result_count", resultCount);

        var status = failedCount == 0
            ? ActivityStatusCode.Ok
            : ActivityStatusCode.Error;

        activity.SetStatus(status, failedCount > 0
            ? $"{failedCount} shard(s) failed"
            : null);

        activity.Dispose();
    }

    /// <summary>
    /// Completes a shard query child activity.
    /// </summary>
    /// <param name="activity">The shard query activity to complete.</param>
    /// <param name="isSuccess">Whether the query succeeded.</param>
    /// <param name="errorMessage">Optional error message if the query failed.</param>
    internal static void CompleteShardQuery(Activity? activity, bool isSuccess, string? errorMessage = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(isSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
