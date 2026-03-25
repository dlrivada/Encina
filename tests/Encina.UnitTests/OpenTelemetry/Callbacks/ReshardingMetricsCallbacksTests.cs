using Encina.OpenTelemetry.Resharding;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Callbacks;

/// <summary>
/// Unit tests for <see cref="ReshardingMetricsCallbacks"/>.
/// </summary>
public sealed class ReshardingMetricsCallbacksTests
{
    [Fact]
    public void Constructor_NullRowsPerSecondCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(null!, () => 0.0, () => 0));
    }

    [Fact]
    public void Constructor_NullCdcLagMsCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(() => 0.0, null!, () => 0));
    }

    [Fact]
    public void Constructor_NullActiveReshardingCountCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(() => 0.0, () => 0.0, null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            new ReshardingMetricsCallbacks(() => 100.0, () => 50.0, () => 2));
        ex.ShouldBeNull();
    }
}
