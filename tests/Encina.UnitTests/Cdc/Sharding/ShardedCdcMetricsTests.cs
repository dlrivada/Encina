using Encina.OpenTelemetry.Cdc;
using Shouldly;

namespace Encina.UnitTests.Cdc.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardedCdcMetrics"/>.
/// Verifies constructor validation and method invocability.
/// Metrics recording does not throw exceptions even without a listener.
/// </summary>
public sealed class ShardedCdcMetricsTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullActiveConnectorsCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcMetrics(null!, () => []));
    }

    [Fact]
    public void Constructor_NullLagCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcMetrics(() => 0, null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        Should.NotThrow(() =>
            new ShardedCdcMetrics(() => 3, () => [("shard-1", 100.0)]));
    }

    #endregion

    #region RecordEventsProcessed

    [Fact]
    public void RecordEventsProcessed_DefaultCount_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 1, () => []);

        Should.NotThrow(() => metrics.RecordEventsProcessed("shard-1", "insert"));
    }

    [Fact]
    public void RecordEventsProcessed_WithCount_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 1, () => []);

        Should.NotThrow(() => metrics.RecordEventsProcessed("shard-1", "update", 10));
    }

    [Fact]
    public void RecordEventsProcessed_MultipleCalls_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 1, () => []);

        for (var i = 0; i < 100; i++)
        {
            Should.NotThrow(() => metrics.RecordEventsProcessed($"shard-{i % 3}", "insert", 1));
        }
    }

    #endregion

    #region RecordPositionSave

    [Fact]
    public void RecordPositionSave_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 1, () => []);

        Should.NotThrow(() => metrics.RecordPositionSave("shard-1"));
    }

    #endregion

    #region RecordError

    [Fact]
    public void RecordError_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 1, () => []);

        Should.NotThrow(() => metrics.RecordError("shard-1", "Timeout"));
    }

    [Fact]
    public void RecordError_DifferentErrorTypes_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 1, () => []);

        Should.NotThrow(() => metrics.RecordError("shard-1", "Timeout"));
        Should.NotThrow(() => metrics.RecordError("shard-2", "ConnectionReset"));
        Should.NotThrow(() => metrics.RecordError("shard-1", "Deserialization"));
    }

    #endregion
}
