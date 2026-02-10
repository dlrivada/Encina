using Encina.Database;
using Encina.Sharding.Diagnostics;
using Encina.Sharding.Health;

namespace Encina.UnitTests.Core.Sharding.Diagnostics;

/// <summary>
/// Unit tests for <see cref="ShardedDatabasePoolMetrics"/>.
/// </summary>
public sealed class ShardedDatabasePoolMetricsTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ShardedDatabasePoolMetrics(null!));
    }

    [Fact]
    public void Constructor_ValidMonitor_DoesNotThrow()
    {
        var monitor = Substitute.For<IShardedDatabaseHealthMonitor>();
        monitor.GetAllShardPoolStatistics().Returns(new Dictionary<string, ConnectionPoolStats>());
        Should.NotThrow(() => new ShardedDatabasePoolMetrics(monitor));
    }

    // ────────────────────────────────────────────────────────────
    //  UpdateHealthSnapshot
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateHealthSnapshot_NullResults_ThrowsArgumentNullException()
    {
        var monitor = Substitute.For<IShardedDatabaseHealthMonitor>();
        monitor.GetAllShardPoolStatistics().Returns(new Dictionary<string, ConnectionPoolStats>());
        var metrics = new ShardedDatabasePoolMetrics(monitor);

        Should.Throw<ArgumentNullException>(() => metrics.UpdateHealthSnapshot(null!));
    }

    [Fact]
    public void UpdateHealthSnapshot_ValidResults_DoesNotThrow()
    {
        var monitor = Substitute.For<IShardedDatabaseHealthMonitor>();
        monitor.GetAllShardPoolStatistics().Returns(new Dictionary<string, ConnectionPoolStats>());
        var metrics = new ShardedDatabasePoolMetrics(monitor);
        var results = new[] { ShardHealthResult.Healthy("shard-1") };

        Should.NotThrow(() => metrics.UpdateHealthSnapshot(results));
    }

    [Fact]
    public void UpdateHealthSnapshot_EmptyResults_DoesNotThrow()
    {
        var monitor = Substitute.For<IShardedDatabaseHealthMonitor>();
        monitor.GetAllShardPoolStatistics().Returns(new Dictionary<string, ConnectionPoolStats>());
        var metrics = new ShardedDatabasePoolMetrics(monitor);

        Should.NotThrow(() => metrics.UpdateHealthSnapshot([]));
    }

    [Fact]
    public void UpdateHealthSnapshot_CalledMultipleTimes_DoesNotThrow()
    {
        var monitor = Substitute.For<IShardedDatabaseHealthMonitor>();
        monitor.GetAllShardPoolStatistics().Returns(new Dictionary<string, ConnectionPoolStats>());
        var metrics = new ShardedDatabasePoolMetrics(monitor);

        metrics.UpdateHealthSnapshot([ShardHealthResult.Healthy("shard-1")]);
        metrics.UpdateHealthSnapshot([ShardHealthResult.Unhealthy("shard-1")]);
        metrics.UpdateHealthSnapshot([]);
    }
}
