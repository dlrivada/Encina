using Encina.OpenTelemetry.Cdc;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Callbacks;

/// <summary>
/// Unit tests for <see cref="ShardedCdcMetricsCallbacks"/>.
/// </summary>
public sealed class ShardedCdcMetricsCallbacksTests
{
    [Fact]
    public void Constructor_NullActiveConnectorsCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcMetricsCallbacks(
                null!,
                () => Enumerable.Empty<(string, double)>()));
    }

    [Fact]
    public void Constructor_NullLagCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcMetricsCallbacks(() => 0, null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            new ShardedCdcMetricsCallbacks(
                () => 5,
                () => [("shard-1", 100.0)]));
        ex.ShouldBeNull();
    }
}
