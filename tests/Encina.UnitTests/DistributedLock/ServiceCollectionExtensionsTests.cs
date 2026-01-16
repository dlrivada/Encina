using Encina.Testing.Time;
using Encina.DistributedLock;
using Encina.DistributedLock.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.DistributedLock;

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
        Should.NotThrow(act);
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
        options.ShouldNotBeNull();
        options.Value.KeyPrefix.ShouldBe("test");
        options.Value.DefaultExpiry.ShouldBe(TimeSpan.FromMinutes(5));
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
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<InMemoryDistributedLockProvider>();
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
        provider.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDistributedLock_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddEncinaDistributedLock();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaDistributedLockInMemory_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddEncinaDistributedLockInMemory();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }
}
