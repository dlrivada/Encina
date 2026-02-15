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

    /// <summary>
    /// Starts a distributed aggregation activity spanning multiple shards.
    /// </summary>
    /// <param name="operationType">The aggregation type (e.g., "Count", "Sum", "Avg", "Min", "Max").</param>
    /// <param name="shardCount">The number of shards that will be queried.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartAggregation(string operationType, int shardCount)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Sharding.Aggregation", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.AggregationOperationType, operationType);
        activity?.SetTag(ActivityTagNames.AggregationShardsQueried, shardCount);
        return activity;
    }

    /// <summary>
    /// Starts a child activity for a single shard's aggregation query within a distributed aggregation.
    /// </summary>
    /// <param name="shardId">The shard identifier being queried.</param>
    /// <param name="operationType">The aggregation type (e.g., "Count", "Sum", "Avg", "Min", "Max").</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartShardAggregation(string shardId, string operationType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Sharding.ShardAggregation", ActivityKind.Client);
        activity?.SetTag(ActivityTagNames.ShardId, shardId);
        activity?.SetTag(ActivityTagNames.AggregationOperationType, operationType);
        return activity;
    }

    /// <summary>
    /// Starts a specification-based scatter-gather activity.
    /// </summary>
    /// <param name="specificationType">The specification type name.</param>
    /// <param name="operationKind">The operation kind (e.g., "query", "paged_query", "count").</param>
    /// <param name="shardCount">The number of shards that will be queried.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSpecificationScatterGather(
        string specificationType,
        string operationKind,
        int shardCount)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "Encina.Sharding.SpecificationScatterGather", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.SpecificationType, specificationType);
        activity?.SetTag(ActivityTagNames.SpecificationOperationKind, operationKind);
        activity?.SetTag(ActivityTagNames.ShardCount, shardCount);
        return activity;
    }

    /// <summary>
    /// Adds pagination context to a specification scatter-gather activity.
    /// </summary>
    /// <param name="activity">The specification scatter-gather activity.</param>
    /// <param name="strategy">The pagination strategy name.</param>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    internal static void SetPaginationContext(
        Activity? activity,
        string strategy,
        int page,
        int pageSize)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.PaginationStrategy, strategy);
        activity.SetTag(ActivityTagNames.PaginationPage, page);
        activity.SetTag(ActivityTagNames.PaginationPageSize, pageSize);
    }

    /// <summary>
    /// Completes a specification scatter-gather activity with result information.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="successCount">Number of successful shard queries.</param>
    /// <param name="failedCount">Number of failed shard queries.</param>
    /// <param name="totalItems">Total number of items in the result.</param>
    /// <param name="mergeDurationMs">Duration of the merge/pagination step in milliseconds.</param>
    internal static void CompleteSpecificationScatterGather(
        Activity? activity,
        int successCount,
        int failedCount,
        int totalItems,
        double mergeDurationMs)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("encina.sharding.scatter.success_count", successCount);
        activity.SetTag("encina.sharding.scatter.failed_count", failedCount);
        activity.SetTag(ActivityTagNames.SpecificationTotalItems, totalItems);
        activity.SetTag(ActivityTagNames.SpecificationMergeDurationMs, mergeDurationMs);

        var status = failedCount == 0
            ? ActivityStatusCode.Ok
            : ActivityStatusCode.Error;

        activity.SetStatus(status, failedCount > 0
            ? $"{failedCount} shard(s) failed during specification scatter-gather"
            : null);

        activity.Dispose();
    }

    /// <summary>
    /// Starts a read/write connection acquisition activity.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="intent">The routing intent (<c>"read"</c> or <c>"write"</c>).</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartReadWriteConnect(string shardId, string intent)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "Encina.Sharding.ReadWrite.Connect", ActivityKind.Client);
        activity?.SetTag(ActivityTagNames.ShardId, shardId);
        activity?.SetTag(ActivityTagNames.ReadWriteIntent, intent);
        return activity;
    }

    /// <summary>
    /// Completes a read/write connection acquisition activity with the selected replica.
    /// </summary>
    /// <param name="activity">The connection activity to complete.</param>
    /// <param name="replicaId">The selected replica identifier, or <c>null</c> if primary was used.</param>
    /// <param name="selectionStrategy">The selection strategy used.</param>
    internal static void ReadWriteConnectCompleted(
        Activity? activity, string? replicaId, string? selectionStrategy)
    {
        if (activity is null)
        {
            return;
        }

        if (replicaId is not null)
        {
            activity.SetTag(ActivityTagNames.ReplicaId, replicaId);
        }

        if (selectionStrategy is not null)
        {
            activity.SetTag(ActivityTagNames.ReplicaSelectionStrategy, selectionStrategy);
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks a read/write connection acquisition activity as failed.
    /// </summary>
    /// <param name="activity">The connection activity to mark as failed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    internal static void ReadWriteConnectFailed(
        Activity? activity, string? errorCode, string? errorMessage)
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
    /// Starts a time-based routing activity for resolving a timestamp to a shard.
    /// </summary>
    /// <param name="shardKey">The shard key (timestamp) being routed.</param>
    /// <param name="period">The period label (e.g., "2026-02").</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartTimeBasedRouting(string shardKey, string period)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "Encina.Sharding.TimeBasedRoute", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.ShardKey, shardKey);
        activity?.SetTag(ActivityTagNames.RouterType, "time-based");
        activity?.SetTag(ActivityTagNames.ShardPeriod, period);
        return activity;
    }

    /// <summary>
    /// Starts a tier transition activity.
    /// </summary>
    /// <param name="shardId">The shard being transitioned.</param>
    /// <param name="fromTier">The source tier name.</param>
    /// <param name="toTier">The target tier name.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartTierTransition(string shardId, string fromTier, string toTier)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "Encina.Sharding.TierTransition", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.ShardId, shardId);
        activity?.SetTag(ActivityTagNames.TierFrom, fromTier);
        activity?.SetTag(ActivityTagNames.TierTo, toTier);
        return activity;
    }

    /// <summary>
    /// Completes a distributed aggregation activity with result information.
    /// </summary>
    /// <param name="activity">The aggregation activity to complete.</param>
    /// <param name="successCount">Number of shards that returned results successfully.</param>
    /// <param name="failedCount">Number of shards that failed during aggregation.</param>
    /// <param name="resultValue">The final aggregated result value.</param>
    internal static void CompleteAggregation(
        Activity? activity,
        int successCount,
        int failedCount,
        object? resultValue)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.AggregationShardsSucceeded, successCount);
        activity.SetTag(ActivityTagNames.AggregationShardsFailed, failedCount);
        activity.SetTag(ActivityTagNames.AggregationIsPartial, failedCount > 0);

        if (resultValue is not null)
        {
            activity.SetTag(ActivityTagNames.AggregationResultValue, resultValue.ToString());
        }

        var status = failedCount == 0
            ? ActivityStatusCode.Ok
            : ActivityStatusCode.Error;

        activity.SetStatus(status, failedCount > 0
            ? $"{failedCount} shard(s) failed during aggregation"
            : null);

        activity.Dispose();
    }
}
