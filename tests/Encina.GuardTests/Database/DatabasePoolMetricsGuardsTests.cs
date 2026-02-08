using Encina.Diagnostics;

using Shouldly;

namespace Encina.GuardTests.Database;

/// <summary>
/// Guard clause tests for <see cref="DatabasePoolMetrics"/>.
/// </summary>
public sealed class DatabasePoolMetricsGuardsTests
{
    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new DatabasePoolMetrics(null!));
        ex.ParamName.ShouldBe("monitor");
    }
}
