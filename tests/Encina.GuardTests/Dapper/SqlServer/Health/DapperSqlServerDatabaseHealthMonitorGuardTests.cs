using Encina.Dapper.SqlServer.Health;
using Shouldly;

namespace Encina.GuardTests.Dapper.SqlServer.Health;

/// <summary>
/// Guard tests for <see cref="DapperSqlServerDatabaseHealthMonitor"/> to verify null parameter handling.
/// </summary>
public class DapperSqlServerDatabaseHealthMonitorGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DapperSqlServerDatabaseHealthMonitor(null!);
        Should.Throw<ArgumentNullException>(act);
    }
}
