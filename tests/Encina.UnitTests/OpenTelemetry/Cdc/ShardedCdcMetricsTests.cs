using Encina.OpenTelemetry.Cdc;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Cdc;

/// <summary>
/// Unit tests for <see cref="ShardedCdcMetrics"/>.
/// </summary>
public sealed class ShardedCdcMetricsTests
{
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
        var ex = Record.Exception(() =>
            new ShardedCdcMetrics(() => 3, () => [("shard-1", 100.0)]));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordEventsProcessed_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.RecordEventsProcessed("shard-1", "insert", 10));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordEventsProcessed_DefaultCount_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.RecordEventsProcessed("shard-1", "update"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordPositionSave_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.RecordPositionSave("shard-1"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordError_DoesNotThrow()
    {
        var metrics = new ShardedCdcMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.RecordError("shard-1", "Timeout"));
        ex.ShouldBeNull();
    }
}
