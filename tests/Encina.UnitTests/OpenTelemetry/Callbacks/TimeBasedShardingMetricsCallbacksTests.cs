using Encina.OpenTelemetry.Sharding;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Callbacks;

/// <summary>
/// Unit tests for <see cref="TimeBasedShardingMetricsCallbacks"/>.
/// </summary>
public sealed class TimeBasedShardingMetricsCallbacksTests
{
    [Fact]
    public void Constructor_NullShardsPerTierCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardingMetricsCallbacks(null!, () => 10.0));
    }

    [Fact]
    public void Constructor_NullOldestHotShardAgeDaysCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardingMetricsCallbacks(
                () => Enumerable.Empty<(string, int)>(),
                null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            new TimeBasedShardingMetricsCallbacks(
                () => [("Hot", 5), ("Warm", 3)],
                () => 30.0));
        ex.ShouldBeNull();
    }
}
