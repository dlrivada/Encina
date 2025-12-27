using Encina.DistributedLock.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DistributedLock.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaDistributedLock_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaDistributedLock();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaDistributedLock_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDistributedLock(options =>
        {
            options.KeyPrefix = "test";
            options.DefaultExpiry = TimeSpan.FromMinutes(5);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<DistributedLockOptions>>();
        options.Should().NotBeNull();
        options!.Value.KeyPrefix.Should().Be("test");
        options.Value.DefaultExpiry.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddEncinaDistributedLockInMemory_ShouldRegisterProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockInMemory();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IDistributedLockProvider>();
        provider.Should().NotBeNull();
        provider.Should().BeOfType<InMemoryDistributedLockProvider>();
    }

    [Fact]
    public void AddEncinaDistributedLockInMemory_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockInMemory(options =>
        {
            options.KeyPrefix = "myapp";
            options.WarnOnUse = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IDistributedLockProvider>();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaDistributedLock_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddEncinaDistributedLock();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddEncinaDistributedLockInMemory_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddEncinaDistributedLockInMemory();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }
}
