using System.Diagnostics;
using Encina.Sharding.Diagnostics;

namespace Encina.UnitTests.Core.Sharding.Diagnostics;

/// <summary>
/// Unit tests for <see cref="ShardingActivitySource"/>.
/// </summary>
public sealed class ShardingActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public ShardingActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Sharding",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    // ────────────────────────────────────────────────────────────
    //  StartRouting
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StartRouting_WithListener_ReturnsActivity()
    {
        var activity = ShardingActivitySource.StartRouting("key-123", "hash");
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Encina.Sharding.Route");
        activity.Dispose();
    }

    [Fact]
    public void StartRouting_SetsShardKeyTag()
    {
        var activity = ShardingActivitySource.StartRouting("key-123", "hash");
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.ShardKey).ShouldBe("key-123");
        activity.Dispose();
    }

    [Fact]
    public void StartRouting_SetsRouterTypeTag()
    {
        var activity = ShardingActivitySource.StartRouting("key-123", "range");
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.RouterType).ShouldBe("range");
        activity.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  RoutingCompleted
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RoutingCompleted_SetsShardIdAndOkStatus()
    {
        var activity = ShardingActivitySource.StartRouting("key-123", "hash");
        ShardingActivitySource.RoutingCompleted(activity, "shard-1");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.ShardId).ShouldBe("shard-1");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RoutingCompleted_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => ShardingActivitySource.RoutingCompleted(null, "shard-1"));
    }

    // ────────────────────────────────────────────────────────────
    //  RoutingFailed
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RoutingFailed_SetsErrorStatusAndCode()
    {
        var activity = ShardingActivitySource.StartRouting("key-123", "hash");
        ShardingActivitySource.RoutingFailed(activity, "encina.sharding.shard_not_found", "Not found");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.FailureCode).ShouldBe("encina.sharding.shard_not_found");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void RoutingFailed_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => ShardingActivitySource.RoutingFailed(null, "code", "msg"));
    }

    [Fact]
    public void RoutingFailed_NullErrorCode_DoesNotSetFailureCodeTag()
    {
        var activity = ShardingActivitySource.StartRouting("key-123", "hash");
        ShardingActivitySource.RoutingFailed(activity, null, "Error");

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.FailureCode).ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  StartScatterGather
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StartScatterGather_WithListener_ReturnsActivity()
    {
        var activity = ShardingActivitySource.StartScatterGather(3, "all");
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Encina.Sharding.ScatterGather");
        activity.Dispose();
    }

    [Fact]
    public void StartScatterGather_SetsShardCountTag()
    {
        var activity = ShardingActivitySource.StartScatterGather(3, "targeted");
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.ShardCount).ShouldBe(3);
        activity.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  CompleteScatterGather
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CompleteScatterGather_AllSuccess_SetsOkStatus()
    {
        var activity = ShardingActivitySource.StartScatterGather(2, "all");
        ShardingActivitySource.CompleteScatterGather(activity, 2, 0, 10);

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void CompleteScatterGather_WithFailures_SetsErrorStatus()
    {
        var activity = ShardingActivitySource.StartScatterGather(3, "all");
        ShardingActivitySource.CompleteScatterGather(activity, 2, 1, 5);

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void CompleteScatterGather_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => ShardingActivitySource.CompleteScatterGather(null, 1, 0, 5));
    }

    // ────────────────────────────────────────────────────────────
    //  StartShardQuery / CompleteShardQuery
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StartShardQuery_WithListener_ReturnsActivity()
    {
        var activity = ShardingActivitySource.StartShardQuery("shard-1");
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Encina.Sharding.ShardQuery");
        activity.GetTagItem(ActivityTagNames.ShardId).ShouldBe("shard-1");
        activity.Dispose();
    }

    [Fact]
    public void CompleteShardQuery_Success_SetsOkStatus()
    {
        var activity = ShardingActivitySource.StartShardQuery("shard-1");
        ShardingActivitySource.CompleteShardQuery(activity, isSuccess: true);

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void CompleteShardQuery_Failure_SetsErrorStatus()
    {
        var activity = ShardingActivitySource.StartShardQuery("shard-1");
        ShardingActivitySource.CompleteShardQuery(activity, isSuccess: false, "Query failed");

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void CompleteShardQuery_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => ShardingActivitySource.CompleteShardQuery(null, true));
    }

    // ────────────────────────────────────────────────────────────
    //  StartAggregation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StartAggregation_WithListener_ReturnsActivity()
    {
        var activity = ShardingActivitySource.StartAggregation("Count", 3);
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Encina.Sharding.Aggregation");
        activity.Dispose();
    }

    [Fact]
    public void StartAggregation_SetsOperationTypeTag()
    {
        var activity = ShardingActivitySource.StartAggregation("Sum", 5);
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.AggregationOperationType).ShouldBe("Sum");
        activity.Dispose();
    }

    [Fact]
    public void StartAggregation_SetsShardsQueriedTag()
    {
        var activity = ShardingActivitySource.StartAggregation("Avg", 4);
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.AggregationShardsQueried).ShouldBe(4);
        activity.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  StartShardAggregation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StartShardAggregation_WithListener_ReturnsActivity()
    {
        var activity = ShardingActivitySource.StartShardAggregation("shard-1", "Min");
        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe("Encina.Sharding.ShardAggregation");
        activity.Dispose();
    }

    [Fact]
    public void StartShardAggregation_SetsShardIdTag()
    {
        var activity = ShardingActivitySource.StartShardAggregation("shard-2", "Max");
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.ShardId).ShouldBe("shard-2");
        activity.Dispose();
    }

    [Fact]
    public void StartShardAggregation_SetsOperationTypeTag()
    {
        var activity = ShardingActivitySource.StartShardAggregation("shard-1", "Count");
        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.AggregationOperationType).ShouldBe("Count");
        activity.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  CompleteAggregation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CompleteAggregation_AllSuccess_SetsOkStatus()
    {
        var activity = ShardingActivitySource.StartAggregation("Count", 3);
        ShardingActivitySource.CompleteAggregation(activity, successCount: 3, failedCount: 0, resultValue: 42L);

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem(ActivityTagNames.AggregationShardsSucceeded).ShouldBe(3);
        activity.GetTagItem(ActivityTagNames.AggregationShardsFailed).ShouldBe(0);
        activity.GetTagItem(ActivityTagNames.AggregationIsPartial).ShouldBe(false);
        activity.GetTagItem(ActivityTagNames.AggregationResultValue).ShouldBe("42");
    }

    [Fact]
    public void CompleteAggregation_WithFailures_SetsErrorStatus()
    {
        var activity = ShardingActivitySource.StartAggregation("Sum", 3);
        ShardingActivitySource.CompleteAggregation(activity, successCount: 2, failedCount: 1, resultValue: 100m);

        activity.ShouldNotBeNull();
        activity!.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(ActivityTagNames.AggregationIsPartial).ShouldBe(true);
    }

    [Fact]
    public void CompleteAggregation_NullResultValue_DoesNotSetResultTag()
    {
        var activity = ShardingActivitySource.StartAggregation("Min", 2);
        ShardingActivitySource.CompleteAggregation(activity, successCount: 2, failedCount: 0, resultValue: null);

        activity.ShouldNotBeNull();
        activity!.GetTagItem(ActivityTagNames.AggregationResultValue).ShouldBeNull();
    }

    [Fact]
    public void CompleteAggregation_NullActivity_DoesNotThrow()
    {
        Should.NotThrow(() => ShardingActivitySource.CompleteAggregation(null, 1, 0, 42L));
    }
}
