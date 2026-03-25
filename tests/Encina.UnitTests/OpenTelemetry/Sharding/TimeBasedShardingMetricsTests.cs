using Encina.OpenTelemetry.Sharding;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Sharding;

/// <summary>
/// Unit tests for <see cref="TimeBasedShardingMetrics"/>.
/// </summary>
public sealed class TimeBasedShardingMetricsTests
{
    [Fact]
    public void Constructor_NullCallbacks_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardingMetrics(null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var callbacks = new TimeBasedShardingMetricsCallbacks(
            () => [("Hot", 5)],
            () => 30.0);

        var ex = Record.Exception(() => new TimeBasedShardingMetrics(callbacks));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordTierTransition_DoesNotThrow()
    {
        var callbacks = new TimeBasedShardingMetricsCallbacks(
            () => [], () => null);
        var metrics = new TimeBasedShardingMetrics(callbacks);

        var ex = Record.Exception(() => metrics.RecordTierTransition("Hot", "Warm"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordAutoCreatedShard_DoesNotThrow()
    {
        var callbacks = new TimeBasedShardingMetricsCallbacks(
            () => [], () => null);
        var metrics = new TimeBasedShardingMetrics(callbacks);

        var ex = Record.Exception(() => metrics.RecordAutoCreatedShard());
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordQueryPerTier_DoesNotThrow()
    {
        var callbacks = new TimeBasedShardingMetricsCallbacks(
            () => [], () => null);
        var metrics = new TimeBasedShardingMetrics(callbacks);

        var ex = Record.Exception(() => metrics.RecordQueryPerTier("Hot"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordArchivalDuration_DoesNotThrow()
    {
        var callbacks = new TimeBasedShardingMetricsCallbacks(
            () => [], () => null);
        var metrics = new TimeBasedShardingMetrics(callbacks);

        var ex = Record.Exception(() => metrics.RecordArchivalDuration("orders-2025-01", 1234.5));
        ex.ShouldBeNull();
    }
}
