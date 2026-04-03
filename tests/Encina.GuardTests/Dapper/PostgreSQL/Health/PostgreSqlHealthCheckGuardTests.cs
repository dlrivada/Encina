using Encina.Dapper.PostgreSQL.Health;
using Shouldly;

namespace Encina.GuardTests.Dapper.PostgreSQL.Health;

/// <summary>
/// Guard tests for <see cref="PostgreSqlHealthCheck"/> to verify null parameter handling and constants.
/// </summary>
public class PostgreSqlHealthCheckGuardTests
{
    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        PostgreSqlHealthCheck.DefaultName.ShouldBe("encina-postgresql");
    }

    [Fact]
    public void Constructor_ValidServiceProvider_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();

        // Act & Assert
        Should.NotThrow(() => new PostgreSqlHealthCheck(serviceProvider, null));
    }
}
