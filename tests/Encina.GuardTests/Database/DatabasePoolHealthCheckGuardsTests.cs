using Encina.Database;
using Encina.Messaging.Health;

using Shouldly;

namespace Encina.GuardTests.Database;

/// <summary>
/// Guard clause tests for <see cref="DatabasePoolHealthCheck"/>.
/// </summary>
public sealed class DatabasePoolHealthCheckGuardsTests
{
    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new DatabasePoolHealthCheck(null!));
        ex.ParamName.ShouldBe("monitor");
    }
}
