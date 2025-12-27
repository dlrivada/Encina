using Microsoft.Extensions.DependencyInjection;

namespace Encina.DistributedLock.SqlServer.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddEncinaDistributedLockSqlServer("Server=.;Database=Test;");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaDistributedLockSqlServer(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaDistributedLockSqlServer(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_ShouldRegisterProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockSqlServer("Server=.;Database=Test;Trusted_Connection=True;");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IDistributedLockProvider>();
        provider.Should().NotBeNull();
        provider.Should().BeOfType<SqlServerDistributedLockProvider>();
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockSqlServer(
            "Server=.;Database=Test;Trusted_Connection=True;",
            options =>
            {
                options.KeyPrefix = "myapp";
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IDistributedLockProvider>();
        provider.Should().NotBeNull();
    }
}
