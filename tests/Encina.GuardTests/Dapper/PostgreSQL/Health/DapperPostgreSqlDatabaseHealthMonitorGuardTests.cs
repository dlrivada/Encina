using Encina.Dapper.PostgreSQL.Health;
using Shouldly;

namespace Encina.GuardTests.Dapper.PostgreSQL.Health;

/// <summary>
/// Guard tests for <see cref="DapperPostgreSqlDatabaseHealthMonitor"/> to verify null parameter handling.
/// </summary>
public class DapperPostgreSqlDatabaseHealthMonitorGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DapperPostgreSqlDatabaseHealthMonitor(null!);
        Should.Throw<ArgumentNullException>(act);
    }
}
