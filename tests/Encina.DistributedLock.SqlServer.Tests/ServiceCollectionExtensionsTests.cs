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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaDistributedLockSqlServer(null!);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("connectionString");
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaDistributedLockSqlServer(string.Empty);

        // Assert
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("connectionString");
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
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<SqlServerDistributedLockProvider>();
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
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<SqlServerDistributedLockProvider>();
    }
}
