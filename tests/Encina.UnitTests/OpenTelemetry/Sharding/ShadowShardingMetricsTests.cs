using Encina.OpenTelemetry.Sharding;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Sharding;

/// <summary>
/// Unit tests for <see cref="ShadowShardingMetrics"/>.
/// </summary>
public sealed class ShadowShardingMetricsTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var ex = Record.Exception(() => new ShadowShardingMetrics());
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordRouting_Match_DoesNotThrow()
    {
        var metrics = new ShadowShardingMetrics();
        var ex = Record.Exception(() => metrics.RecordRouting(true));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordRouting_Mismatch_DoesNotThrow()
    {
        var metrics = new ShadowShardingMetrics();
        var ex = Record.Exception(() => metrics.RecordRouting(false));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordRoutingMismatch_DoesNotThrow()
    {
        var metrics = new ShadowShardingMetrics();
        var ex = Record.Exception(() => metrics.RecordRoutingMismatch("customer"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordShadowWrite_Success_DoesNotThrow()
    {
        var metrics = new ShadowShardingMetrics();
        var ex = Record.Exception(() => metrics.RecordShadowWrite("shard-1", true, 12.5));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordShadowWrite_Failure_DoesNotThrow()
    {
        var metrics = new ShadowShardingMetrics();
        var ex = Record.Exception(() => metrics.RecordShadowWrite("shard-1", false, -5.0));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordShadowRead_Match_DoesNotThrow()
    {
        var metrics = new ShadowShardingMetrics();
        var ex = Record.Exception(() => metrics.RecordShadowRead(true, 3.2));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordShadowRead_Mismatch_DoesNotThrow()
    {
        var metrics = new ShadowShardingMetrics();
        var ex = Record.Exception(() => metrics.RecordShadowRead(false, -1.0));
        ex.ShouldBeNull();
    }
}
