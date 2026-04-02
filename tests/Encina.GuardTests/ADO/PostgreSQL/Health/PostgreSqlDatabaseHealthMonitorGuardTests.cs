using Encina.ADO.PostgreSQL.Health;
using Shouldly;

namespace Encina.GuardTests.ADO.PostgreSQL.Health;

/// <summary>
/// Guard tests for <see cref="PostgreSqlDatabaseHealthMonitor"/> to verify null parameter handling.
/// </summary>
public class PostgreSqlDatabaseHealthMonitorGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PostgreSqlDatabaseHealthMonitor(null!);
        Should.Throw<ArgumentNullException>(act);
    }
}
