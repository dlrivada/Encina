using Encina.ADO.SqlServer.Health;
using Shouldly;

namespace Encina.GuardTests.ADO.SqlServer.Health;

/// <summary>
/// Guard tests for <see cref="SqlServerDatabaseHealthMonitor"/> to verify null parameter handling.
/// </summary>
public class SqlServerDatabaseHealthMonitorGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SqlServerDatabaseHealthMonitor(null!);
        Should.Throw<ArgumentNullException>(act);
    }
}
