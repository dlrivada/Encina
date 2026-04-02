using Encina.ADO.MySQL.Health;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL.Health;

/// <summary>
/// Guard tests for <see cref="MySqlDatabaseHealthMonitor"/> to verify null parameter handling.
/// </summary>
public class MySqlDatabaseHealthMonitorGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MySqlDatabaseHealthMonitor(null!);
        Should.Throw<ArgumentNullException>(act);
    }
}
