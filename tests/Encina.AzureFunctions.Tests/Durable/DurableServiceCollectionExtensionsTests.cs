using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class DurableServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaDurableFunctions_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<DurableFunctionsOptions>>();
        options.ShouldNotBeNull();
        options!.Value.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDurableFunctions_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions(options =>
        {
            options.DefaultMaxRetries = 10;
            options.DefaultFirstRetryInterval = TimeSpan.FromSeconds(30);
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>();
        options.Value.DefaultMaxRetries.ShouldBe(10);
        options.Value.DefaultFirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddEncinaDurableFunctions_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();
        using var provider = services.BuildServiceProvider();

        // Assert
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        healthChecks.ShouldContain(hc => hc is DurableFunctionsHealthCheck);
    }

    [Fact]
    public void AddEncinaDurableFunctions_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaDurableFunctions();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaDurableFunctions_CalledMultipleTimes_RegistersHealthCheckOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();
        services.AddEncinaDurableFunctions();
        using var provider = services.BuildServiceProvider();

        // Assert
        var healthChecks = provider.GetServices<IEncinaHealthCheck>()
            .OfType<DurableFunctionsHealthCheck>()
            .ToList();
        healthChecks.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaDurableFunctions_WithNullConfigure_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var action = () => services.AddEncinaDurableFunctions(null);
        Should.NotThrow(action);
    }

    [Fact]
    public void AddEncinaDurableFunctions_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var action = () => services!.AddEncinaDurableFunctions();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("services");
    }
}
