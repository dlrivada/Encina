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

    // ────────────────────────────────────────────────────────────
    //  RecordAggregationDuration — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordAggregationDuration_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordAggregationDuration("Count", 3, 150.0));
    }

    [Fact]
    public void RecordAggregationDuration_AllOperationTypes_DoNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() =>
        {
            metrics.RecordAggregationDuration("Count", 3, 50.0);
            metrics.RecordAggregationDuration("Sum", 3, 75.0);
            metrics.RecordAggregationDuration("Avg", 3, 100.0);
            metrics.RecordAggregationDuration("Min", 3, 60.0);
            metrics.RecordAggregationDuration("Max", 3, 65.0);
        });
    }

    // ────────────────────────────────────────────────────────────
    //  RecordAggregationPartialResult — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordAggregationPartialResult_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordAggregationPartialResult("Sum", 1, 3));
    }

    [Fact]
    public void RecordAggregationPartialResult_AllOperationTypes_DoNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() =>
        {
            metrics.RecordAggregationPartialResult("Count", 1, 5);
            metrics.RecordAggregationPartialResult("Sum", 2, 5);
            metrics.RecordAggregationPartialResult("Avg", 1, 3);
            metrics.RecordAggregationPartialResult("Min", 1, 4);
            metrics.RecordAggregationPartialResult("Max", 3, 5);
        });
    }

    // ────────────────────────────────────────────────────────────
    //  RecordCompoundKeyExtraction — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordCompoundKeyExtraction_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordCompoundKeyExtraction(2, "compound"));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordPartialKeyRouting — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordPartialKeyRouting_DoesNotThrow()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.NotThrow(() => metrics.RecordPartialKeyRouting(1, 2, "compound"));
    }
}
