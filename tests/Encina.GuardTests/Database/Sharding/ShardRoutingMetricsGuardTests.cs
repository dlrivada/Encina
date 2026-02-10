using Encina.Sharding.Diagnostics;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardRoutingMetrics"/>.
/// </summary>
public sealed class ShardRoutingMetricsGuardTests
{
    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new ShardRoutingMetrics(null!));
        ex.ParamName.ShouldBe("topology");
    }
}
