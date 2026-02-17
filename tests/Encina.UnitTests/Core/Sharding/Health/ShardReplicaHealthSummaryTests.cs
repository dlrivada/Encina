using Encina.Database;
using Encina.Sharding.Health;

namespace Encina.UnitTests.Core.Sharding.Health;

public sealed class ShardReplicaHealthSummaryTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsProperties()
    {
        var results = new List<ShardReplicaHealthResult>
        {
            new("shard-0", DatabaseHealthStatus.Healthy, 2, 2),
            new("shard-1", DatabaseHealthStatus.Degraded, 1, 2),
        };

        var summary = new ShardReplicaHealthSummary(DatabaseHealthStatus.Degraded, results);

        summary.OverallStatus.ShouldBe(DatabaseHealthStatus.Degraded);
        summary.ShardResults.Count.ShouldBe(2);
    }

    // ────────────────────────────────────────────────────────────
    //  Computed Properties
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AllHealthy_WhenOverallHealthy_ReturnsTrue()
    {
        var results = new List<ShardReplicaHealthResult>
        {
            new("shard-0", DatabaseHealthStatus.Healthy, 2, 2),
        };

        var summary = new ShardReplicaHealthSummary(DatabaseHealthStatus.Healthy, results);
        summary.AllHealthy.ShouldBeTrue();
    }

    [Fact]
    public void AllHealthy_WhenOverallDegraded_ReturnsFalse()
    {
        var results = new List<ShardReplicaHealthResult>
        {
            new("shard-0", DatabaseHealthStatus.Degraded, 1, 2),
        };

        var summary = new ShardReplicaHealthSummary(DatabaseHealthStatus.Degraded, results);
        summary.AllHealthy.ShouldBeFalse();
    }

    [Fact]
    public void ShardCount_ReturnsNumberOfResults()
    {
        var results = new List<ShardReplicaHealthResult>
        {
            new("shard-0", DatabaseHealthStatus.Healthy, 2, 2),
            new("shard-1", DatabaseHealthStatus.Healthy, 1, 1),
            new("shard-2", DatabaseHealthStatus.Degraded, 0, 2),
        };

        var summary = new ShardReplicaHealthSummary(DatabaseHealthStatus.Degraded, results);
        summary.ShardCount.ShouldBe(3);
    }

    [Fact]
    public void DegradedCount_CountsDegradedShards()
    {
        var results = new List<ShardReplicaHealthResult>
        {
            new("shard-0", DatabaseHealthStatus.Healthy, 2, 2),
            new("shard-1", DatabaseHealthStatus.Degraded, 1, 3),
            new("shard-2", DatabaseHealthStatus.Degraded, 1, 2),
        };

        var summary = new ShardReplicaHealthSummary(DatabaseHealthStatus.Degraded, results);
        summary.DegradedCount.ShouldBe(2);
    }

    [Fact]
    public void UnhealthyCount_CountsUnhealthyShards()
    {
        var results = new List<ShardReplicaHealthResult>
        {
            new("shard-0", DatabaseHealthStatus.Healthy, 2, 2),
            new("shard-1", DatabaseHealthStatus.Unhealthy, 0, 2),
            new("shard-2", DatabaseHealthStatus.Unhealthy, 0, 3),
        };

        var summary = new ShardReplicaHealthSummary(DatabaseHealthStatus.Unhealthy, results);
        summary.UnhealthyCount.ShouldBe(2);
    }

    [Fact]
    public void EmptyResults_AllCountsZero()
    {
        var summary = new ShardReplicaHealthSummary(
            DatabaseHealthStatus.Healthy,
            Array.Empty<ShardReplicaHealthResult>());

        summary.ShardCount.ShouldBe(0);
        summary.DegradedCount.ShouldBe(0);
        summary.UnhealthyCount.ShouldBe(0);
    }
}
