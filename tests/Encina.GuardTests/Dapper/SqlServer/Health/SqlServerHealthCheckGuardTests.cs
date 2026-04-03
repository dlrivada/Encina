using Encina.Dapper.SqlServer.Health;
using Shouldly;

namespace Encina.GuardTests.Dapper.SqlServer.Health;

/// <summary>
/// Guard tests for <see cref="SqlServerHealthCheck"/> to verify null parameter handling and constants.
/// </summary>
public class SqlServerHealthCheckGuardTests
{
    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        SqlServerHealthCheck.DefaultName.ShouldBe("encina-sqlserver");
    }

    [Fact]
    public void Constructor_ValidServiceProvider_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();

        // Act & Assert
        Should.NotThrow(() => new SqlServerHealthCheck(serviceProvider, null));
    }
}
