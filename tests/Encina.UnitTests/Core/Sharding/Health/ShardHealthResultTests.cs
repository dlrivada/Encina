using Encina.Database;
using Encina.Sharding.Health;

namespace Encina.UnitTests.Core.Sharding.Health;

/// <summary>
/// Unit tests for <see cref="ShardHealthResult"/>.
/// </summary>
public sealed class ShardHealthResultTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullShardId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ShardHealthResult(null!, DatabaseHealthStatus.Healthy, ConnectionPoolStats.CreateEmpty()));
    }

    [Fact]
    public void Constructor_EmptyShardId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ShardHealthResult("", DatabaseHealthStatus.Healthy, ConnectionPoolStats.CreateEmpty()));
    }

    [Fact]
    public void Constructor_WhitespaceShardId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ShardHealthResult("  ", DatabaseHealthStatus.Healthy, ConnectionPoolStats.CreateEmpty()));
    }

    // ────────────────────────────────────────────────────────────
    //  Status properties
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsHealthy_WhenHealthy_ReturnsTrue()
    {
        var result = new ShardHealthResult("shard-1", DatabaseHealthStatus.Healthy, ConnectionPoolStats.CreateEmpty());
        result.IsHealthy.ShouldBeTrue();
        result.IsDegraded.ShouldBeFalse();
        result.IsUnhealthy.ShouldBeFalse();
    }

    [Fact]
    public void IsDegraded_WhenDegraded_ReturnsTrue()
    {
        var result = new ShardHealthResult("shard-1", DatabaseHealthStatus.Degraded, ConnectionPoolStats.CreateEmpty());
        result.IsHealthy.ShouldBeFalse();
        result.IsDegraded.ShouldBeTrue();
        result.IsUnhealthy.ShouldBeFalse();
    }

    [Fact]
    public void IsUnhealthy_WhenUnhealthy_ReturnsTrue()
    {
        var result = new ShardHealthResult("shard-1", DatabaseHealthStatus.Unhealthy, ConnectionPoolStats.CreateEmpty());
        result.IsHealthy.ShouldBeFalse();
        result.IsDegraded.ShouldBeFalse();
        result.IsUnhealthy.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Factory methods
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Healthy_CreatesHealthyResult()
    {
        var result = ShardHealthResult.Healthy("shard-1");
        result.Status.ShouldBe(DatabaseHealthStatus.Healthy);
        result.ShardId.ShouldBe("shard-1");
        result.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    public void Healthy_WithPoolStats_SetsPoolStats()
    {
        var stats = new ConnectionPoolStats(5, 10, 15, 0, 100);
        var result = ShardHealthResult.Healthy("shard-1", stats);
        result.PoolStats.ShouldBeSameAs(stats);
    }

    [Fact]
    public void Healthy_WithDescription_SetsDescription()
    {
        var result = ShardHealthResult.Healthy("shard-1", description: "All good");
        result.Description.ShouldBe("All good");
    }

    [Fact]
    public void Degraded_CreatesDegradedResult()
    {
        var result = ShardHealthResult.Degraded("shard-1", "Slow responses");
        result.Status.ShouldBe(DatabaseHealthStatus.Degraded);
        result.IsDegraded.ShouldBeTrue();
        result.Description.ShouldBe("Slow responses");
    }

    [Fact]
    public void Degraded_WithException_SetsException()
    {
        var ex = new TimeoutException("Timed out");
        var result = ShardHealthResult.Degraded("shard-1", exception: ex);
        result.Exception.ShouldBeSameAs(ex);
    }

    [Fact]
    public void Unhealthy_CreatesUnhealthyResult()
    {
        var result = ShardHealthResult.Unhealthy("shard-1", "Connection refused");
        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.IsUnhealthy.ShouldBeTrue();
        result.Description.ShouldBe("Connection refused");
    }

    [Fact]
    public void Unhealthy_WithException_SetsException()
    {
        var ex = new InvalidOperationException("DB error");
        var result = ShardHealthResult.Unhealthy("shard-1", exception: ex);
        result.Exception.ShouldBeSameAs(ex);
    }

    // ────────────────────────────────────────────────────────────
    //  Record equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var stats = ConnectionPoolStats.CreateEmpty();
        var r1 = new ShardHealthResult("shard-1", DatabaseHealthStatus.Healthy, stats);
        var r2 = new ShardHealthResult("shard-1", DatabaseHealthStatus.Healthy, stats);
        r1.ShouldBe(r2);
    }

    [Fact]
    public void RecordEquality_DifferentStatus_AreNotEqual()
    {
        var stats = ConnectionPoolStats.CreateEmpty();
        var r1 = new ShardHealthResult("shard-1", DatabaseHealthStatus.Healthy, stats);
        var r2 = new ShardHealthResult("shard-1", DatabaseHealthStatus.Unhealthy, stats);
        r1.ShouldNotBe(r2);
    }
}
