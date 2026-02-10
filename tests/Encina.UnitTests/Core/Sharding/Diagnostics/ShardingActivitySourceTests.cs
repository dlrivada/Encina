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
}
