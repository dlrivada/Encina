using Encina.Sharding.Diagnostics;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardedDatabasePoolMetrics"/>.
/// </summary>
public sealed class ShardedDatabasePoolMetricsGuardTests
{
    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new ShardedDatabasePoolMetrics(null!));
        ex.ParamName.ShouldBe("monitor");
    }
}
