using Encina.OpenTelemetry.Resharding;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingMetrics"/>.
/// </summary>
public sealed class ReshardingMetricsTests
{
    [Fact]
    public void Constructor_NullCallbacks_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ReshardingMetrics(null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 0.0, () => 0.0, () => 0);

        var ex = Record.Exception(() => new ReshardingMetrics(callbacks));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordPhaseDuration_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 0.0, () => 0.0, () => 0);
        var metrics = new ReshardingMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordPhaseDuration(Guid.NewGuid(), "Copying", 12345.6));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordRowsCopied_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 0.0, () => 0.0, () => 0);
        var metrics = new ReshardingMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordRowsCopied("shard-1", "shard-3", 5000));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordVerificationMismatch_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 0.0, () => 0.0, () => 0);
        var metrics = new ReshardingMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordVerificationMismatch(Guid.NewGuid()));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordCutoverDuration_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 0.0, () => 0.0, () => 0);
        var metrics = new ReshardingMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordCutoverDuration(Guid.NewGuid(), 250.0));
        ex.ShouldBeNull();
    }
}
