using Encina.Database;

namespace Encina.UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="ConnectionPoolStats"/>.
/// </summary>
public sealed class ConnectionPoolStatsTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange & Act
        var stats = new ConnectionPoolStats(
            ActiveConnections: 5,
            IdleConnections: 10,
            TotalConnections: 15,
            PendingRequests: 2,
            MaxPoolSize: 100);

        // Assert
        stats.ActiveConnections.ShouldBe(5);
        stats.IdleConnections.ShouldBe(10);
        stats.TotalConnections.ShouldBe(15);
        stats.PendingRequests.ShouldBe(2);
        stats.MaxPoolSize.ShouldBe(100);
    }

    [Fact]
    public void PoolUtilization_WhenMaxPoolSizeIsPositive_ReturnsRatio()
    {
        // Arrange
        var stats = new ConnectionPoolStats(5, 5, 10, 0, 100);

        // Act
        var utilization = stats.PoolUtilization;

        // Assert
        utilization.ShouldBe(0.1);
    }

    [Fact]
    public void PoolUtilization_WhenMaxPoolSizeIsZero_ReturnsZero()
    {
        // Arrange
        var stats = new ConnectionPoolStats(5, 5, 10, 0, 0);

        // Act
        var utilization = stats.PoolUtilization;

        // Assert
        utilization.ShouldBe(0.0);
    }

    [Fact]
    public void PoolUtilization_WhenFullyUtilized_ReturnsOne()
    {
        // Arrange
        var stats = new ConnectionPoolStats(100, 0, 100, 0, 100);

        // Act
        var utilization = stats.PoolUtilization;

        // Assert
        utilization.ShouldBe(1.0);
    }

    [Fact]
    public void PoolUtilization_WhenTotalExceedsMax_ClampsToOne()
    {
        // Arrange — TotalConnections > MaxPoolSize (edge case)
        var stats = new ConnectionPoolStats(120, 0, 120, 0, 100);

        // Act
        var utilization = stats.PoolUtilization;

        // Assert
        utilization.ShouldBe(1.0);
    }

    [Fact]
    public void PoolUtilization_WhenHalfUtilized_ReturnsHalf()
    {
        // Arrange
        var stats = new ConnectionPoolStats(25, 25, 50, 0, 100);

        // Act
        var utilization = stats.PoolUtilization;

        // Assert
        utilization.ShouldBe(0.5);
    }

    [Fact]
    public void CreateEmpty_ReturnsAllZeroValues()
    {
        // Act
        var stats = ConnectionPoolStats.CreateEmpty();

        // Assert
        stats.ActiveConnections.ShouldBe(0);
        stats.IdleConnections.ShouldBe(0);
        stats.TotalConnections.ShouldBe(0);
        stats.PendingRequests.ShouldBe(0);
        stats.MaxPoolSize.ShouldBe(0);
    }

    [Fact]
    public void CreateEmpty_PoolUtilization_ReturnsZero()
    {
        // Act
        var stats = ConnectionPoolStats.CreateEmpty();

        // Assert
        stats.PoolUtilization.ShouldBe(0.0);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var stats1 = new ConnectionPoolStats(5, 10, 15, 2, 100);
        var stats2 = new ConnectionPoolStats(5, 10, 15, 2, 100);

        // Assert
        stats1.ShouldBe(stats2);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var stats1 = new ConnectionPoolStats(5, 10, 15, 2, 100);
        var stats2 = new ConnectionPoolStats(6, 10, 16, 2, 100);

        // Assert
        stats1.ShouldNotBe(stats2);
    }

    [Fact]
    public void With_CreatesModifiedCopy()
    {
        // Arrange
        var original = new ConnectionPoolStats(5, 10, 15, 2, 100);

        // Act
        var modified = original with { ActiveConnections = 20 };

        // Assert
        modified.ActiveConnections.ShouldBe(20);
        modified.IdleConnections.ShouldBe(10);
        original.ActiveConnections.ShouldBe(5);
    }
}
