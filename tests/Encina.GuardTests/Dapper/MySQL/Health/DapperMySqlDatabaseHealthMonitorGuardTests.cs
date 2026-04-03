using Encina.Dapper.MySQL.Health;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Health;

/// <summary>
/// Guard tests for <see cref="DapperMySqlDatabaseHealthMonitor"/> to verify null parameter handling.
/// </summary>
public class DapperMySqlDatabaseHealthMonitorGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DapperMySqlDatabaseHealthMonitor(null!);
        Should.Throw<ArgumentNullException>(act);
    }
}
