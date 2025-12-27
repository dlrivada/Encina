using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<DurableFunctionsOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
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
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>();
        options.Value.DefaultMaxRetries.Should().Be(10);
        options.Value.DefaultFirstRetryInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddEncinaDurableFunctions_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();
        var provider = services.BuildServiceProvider();

        // Assert
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        healthChecks.Should().ContainSingle(hc => hc is DurableFunctionsHealthCheck);
    }

    [Fact]
    public void AddEncinaDurableFunctions_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaDurableFunctions();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaDurableFunctions_CalledMultipleTimes_RegistersHealthCheckOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();
        services.AddEncinaDurableFunctions();
        var provider = services.BuildServiceProvider();

        // Assert
        var healthChecks = provider.GetServices<IEncinaHealthCheck>()
            .OfType<DurableFunctionsHealthCheck>()
            .ToList();
        healthChecks.Should().HaveCount(1);
    }

    [Fact]
    public void AddEncinaDurableFunctions_WithNullConfigure_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var action = () => services.AddEncinaDurableFunctions(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaDurableFunctions_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        var action = () => services!.AddEncinaDurableFunctions();
        action.Should().Throw<ArgumentNullException>();
    }
}
