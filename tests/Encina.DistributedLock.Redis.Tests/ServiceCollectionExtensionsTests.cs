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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
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
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaDistributedLockRedis(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("connectionMultiplexer");
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
        provider.Should().NotBeNull();
        provider.Should().BeOfType<RedisDistributedLockProvider>();
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
        provider.Should().NotBeNull();
    }
}
