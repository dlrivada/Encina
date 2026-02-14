using Encina.Database;
using Encina.Sharding.Health;

namespace Encina.UnitTests.Core.Sharding.Health;

public sealed class ShardReplicaHealthResultTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var result = new ShardReplicaHealthResult(
            "shard-0",
            DatabaseHealthStatus.Degraded,
            HealthyReplicaCount: 1,
            TotalReplicaCount: 3,
            Description: "Test description");

        result.ShardId.ShouldBe("shard-0");
        result.Status.ShouldBe(DatabaseHealthStatus.Degraded);
        result.HealthyReplicaCount.ShouldBe(1);
        result.TotalReplicaCount.ShouldBe(3);
        result.Description.ShouldBe("Test description");
    }

    [Fact]
    public void Constructor_DescriptionDefaultsToNull()
    {
        var result = new ShardReplicaHealthResult(
            "shard-0",
            DatabaseHealthStatus.Healthy,
            HealthyReplicaCount: 2,
            TotalReplicaCount: 2);

        result.Description.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  Computed Properties
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsHealthy_WhenStatusHealthy_ReturnsTrue()
    {
        var result = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Healthy, 2, 2);
        result.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_WhenStatusDegraded_ReturnsFalse()
    {
        var result = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Degraded, 1, 2);
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    public void IsDegraded_WhenStatusDegraded_ReturnsTrue()
    {
        var result = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Degraded, 1, 2);
        result.IsDegraded.ShouldBeTrue();
    }

    [Fact]
    public void IsDegraded_WhenStatusHealthy_ReturnsFalse()
    {
        var result = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Healthy, 2, 2);
        result.IsDegraded.ShouldBeFalse();
    }

    [Fact]
    public void IsUnhealthy_WhenStatusUnhealthy_ReturnsTrue()
    {
        var result = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Unhealthy, 0, 2);
        result.IsUnhealthy.ShouldBeTrue();
    }

    [Fact]
    public void IsUnhealthy_WhenStatusHealthy_ReturnsFalse()
    {
        var result = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Healthy, 2, 2);
        result.IsUnhealthy.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  Record Equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Healthy, 2, 2);
        var b = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Healthy, 2, 2);
        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentStatus_AreNotEqual()
    {
        var a = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Healthy, 2, 2);
        var b = new ShardReplicaHealthResult("shard-0", DatabaseHealthStatus.Degraded, 1, 2);
        a.ShouldNotBe(b);
    }
}
