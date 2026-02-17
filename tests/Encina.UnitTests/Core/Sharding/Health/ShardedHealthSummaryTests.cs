using Encina.Database;
using Encina.Sharding.Health;

namespace Encina.UnitTests.Core.Sharding.Health;

/// <summary>
/// Unit tests for <see cref="ShardedHealthSummary"/>.
/// </summary>
public sealed class ShardedHealthSummaryTests
{
    // ────────────────────────────────────────────────────────────
    //  Aggregate properties
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void HealthyCount_CountsOnlyHealthyShards()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Degraded("shard-2"),
            ShardHealthResult.Healthy("shard-3")
        };
        var summary = new ShardedHealthSummary(DatabaseHealthStatus.Degraded, results);

        // Assert
        summary.HealthyCount.ShouldBe(2);
    }

    [Fact]
    public void DegradedCount_CountsOnlyDegradedShards()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Degraded("shard-2"),
            ShardHealthResult.Unhealthy("shard-3")
        };
        var summary = new ShardedHealthSummary(DatabaseHealthStatus.Degraded, results);

        // Assert
        summary.DegradedCount.ShouldBe(1);
    }

    [Fact]
    public void UnhealthyCount_CountsOnlyUnhealthyShards()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Unhealthy("shard-1"),
            ShardHealthResult.Healthy("shard-2"),
            ShardHealthResult.Unhealthy("shard-3")
        };
        var summary = new ShardedHealthSummary(DatabaseHealthStatus.Unhealthy, results);

        // Assert
        summary.UnhealthyCount.ShouldBe(2);
    }

    [Fact]
    public void TotalShards_ReturnsTotalCount()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Degraded("shard-2"),
            ShardHealthResult.Unhealthy("shard-3")
        };
        var summary = new ShardedHealthSummary(DatabaseHealthStatus.Degraded, results);

        // Assert
        summary.TotalShards.ShouldBe(3);
    }

    [Fact]
    public void AllHealthy_WhenAllHealthy_ReturnsTrue()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Healthy("shard-2")
        };
        var summary = new ShardedHealthSummary(DatabaseHealthStatus.Healthy, results);

        // Assert
        summary.AllHealthy.ShouldBeTrue();
    }

    [Fact]
    public void AllHealthy_WhenNotAllHealthy_ReturnsFalse()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Degraded("shard-2")
        };
        var summary = new ShardedHealthSummary(DatabaseHealthStatus.Degraded, results);

        // Assert
        summary.AllHealthy.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  CalculateOverallStatus
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateOverallStatus_AllHealthy_ReturnsHealthy()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Healthy("shard-2"),
            ShardHealthResult.Healthy("shard-3")
        };

        // Act
        var status = ShardedHealthSummary.CalculateOverallStatus(results);

        // Assert
        status.ShouldBe(DatabaseHealthStatus.Healthy);
    }

    [Fact]
    public void CalculateOverallStatus_SomeDegraded_ReturnsDegraded()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Degraded("shard-2"),
            ShardHealthResult.Healthy("shard-3")
        };

        // Act
        var status = ShardedHealthSummary.CalculateOverallStatus(results);

        // Assert
        status.ShouldBe(DatabaseHealthStatus.Degraded);
    }

    [Fact]
    public void CalculateOverallStatus_MinorityUnhealthy_ReturnsDegraded()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Healthy("shard-1"),
            ShardHealthResult.Healthy("shard-2"),
            ShardHealthResult.Unhealthy("shard-3")
        };

        // Act
        var status = ShardedHealthSummary.CalculateOverallStatus(results);

        // Assert
        status.ShouldBe(DatabaseHealthStatus.Degraded);
    }

    [Fact]
    public void CalculateOverallStatus_MajorityUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var results = new[]
        {
            ShardHealthResult.Unhealthy("shard-1"),
            ShardHealthResult.Unhealthy("shard-2"),
            ShardHealthResult.Healthy("shard-3")
        };

        // Act
        var status = ShardedHealthSummary.CalculateOverallStatus(results);

        // Assert
        status.ShouldBe(DatabaseHealthStatus.Unhealthy);
    }

    [Fact]
    public void CalculateOverallStatus_Empty_ReturnsUnhealthy()
    {
        // Act
        var status = ShardedHealthSummary.CalculateOverallStatus([]);

        // Assert
        status.ShouldBe(DatabaseHealthStatus.Unhealthy);
    }

    [Fact]
    public void CalculateOverallStatus_NullResults_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ShardedHealthSummary.CalculateOverallStatus(null!));
    }

    [Fact]
    public void CalculateOverallStatus_SingleHealthy_ReturnsHealthy()
    {
        // Arrange
        var results = new[] { ShardHealthResult.Healthy("shard-1") };

        // Act
        var status = ShardedHealthSummary.CalculateOverallStatus(results);

        // Assert
        status.ShouldBe(DatabaseHealthStatus.Healthy);
    }

    [Fact]
    public void CalculateOverallStatus_SingleUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var results = new[] { ShardHealthResult.Unhealthy("shard-1") };

        // Act
        var status = ShardedHealthSummary.CalculateOverallStatus(results);

        // Assert
        status.ShouldBe(DatabaseHealthStatus.Unhealthy);
    }
}
