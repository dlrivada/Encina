using Encina.Dapper.MySQL.Health;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Health;

/// <summary>
/// Guard tests for <see cref="MySqlHealthCheck"/> to verify null parameter handling and constants.
/// </summary>
public class MySqlHealthCheckGuardTests
{
    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        MySqlHealthCheck.DefaultName.ShouldBe("encina-mysql");
    }

    [Fact]
    public void Constructor_ValidServiceProvider_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();

        // Act & Assert
        Should.NotThrow(() => new MySqlHealthCheck(serviceProvider, null));
    }
}
