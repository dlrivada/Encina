using Microsoft.Extensions.DependencyInjection;

namespace Encina.DistributedLock.Redis.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaDistributedLockRedis_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddEncinaDistributedLockRedis("localhost:6379");

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        string nullConnectionString = null!;

        // Act
        var act = () => services.AddEncinaDistributedLockRedis(nullConnectionString);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaDistributedLockRedis(string.Empty);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithConnectionMultiplexer_WithNullMultiplexer_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConnectionMultiplexer? multiplexer = null;

        // Act
        var act = () => services.AddEncinaDistributedLockRedis(multiplexer!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectionMultiplexer");
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithConnectionMultiplexer_ShouldRegisterProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();

        // Act
        services.AddEncinaDistributedLockRedis(multiplexer);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IDistributedLockProvider>();
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<RedisDistributedLockProvider>();
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();

        // Act
        services.AddEncinaDistributedLockRedis(multiplexer, options =>
        {
            options.Database = 5;
            options.KeyPrefix = "myapp";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IDistributedLockProvider>();
        provider.ShouldNotBeNull();
    }
}
