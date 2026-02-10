using Encina.Sharding;
using Encina.Sharding.Diagnostics;

namespace Encina.UnitTests.Core.Sharding.Diagnostics;

/// <summary>
/// Unit tests for <see cref="ShardRoutingMetrics"/>.
/// </summary>
public sealed class ShardRoutingMetricsTests
{
    private static ShardTopology CreateTopology(params string[] shardIds)
    {
        var shards = shardIds.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
        return new ShardTopology(shards);
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ShardRoutingMetrics(null!));
    }

    [Fact]
    public void Constructor_ValidTopology_DoesNotThrow()
    {
        Should.NotThrow(() => new ShardRoutingMetrics(CreateTopology("shard-1")));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordRouteDecision — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordRouteDecision_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordRouteDecision("shard-1", "hash", 500.0));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordScatterGatherDuration — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordScatterGatherDuration_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordScatterGatherDuration(3, 10, 150.0));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordShardQueryDuration — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordShardQueryDuration_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordShardQueryDuration("shard-1", 50.0));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordPartialFailure — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordPartialFailure_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordPartialFailure(1, 3));
    }

    // ────────────────────────────────────────────────────────────
    //  IncrementActiveQueries / DecrementActiveQueries
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IncrementActiveQueries_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.IncrementActiveQueries());
    }

    [Fact]
    public void DecrementActiveQueries_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        metrics.IncrementActiveQueries();
        Should.NotThrow(() => metrics.DecrementActiveQueries());
    }

    [Fact]
    public void IncrementAndDecrementActiveQueries_CanBeCalledMultipleTimes()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() =>
        {
            metrics.IncrementActiveQueries();
            metrics.IncrementActiveQueries();
            metrics.DecrementActiveQueries();
            metrics.DecrementActiveQueries();
        });
    }
}
