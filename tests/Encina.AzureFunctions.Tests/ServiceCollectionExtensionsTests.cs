using Encina.AzureFunctions.Health;
using Encina.Messaging.Health;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AzureFunctions.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAzureFunctions_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureFunctions();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<EncinaAzureFunctionsOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaAzureFunctions_WithConfiguration_AppliesSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAzureFunctions(options =>
        {
            options.CorrelationIdHeader = "X-Request-ID";
            options.EnableRequestContextEnrichment = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EncinaAzureFunctionsOptions>>();
        options.Value.CorrelationIdHeader.Should().Be("X-Request-ID");
        options.Value.EnableRequestContextEnrichment.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaAzureFunctions_RegistersMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAzureFunctions();
        var provider = services.BuildServiceProvider();

        // Assert
        var middleware = provider.GetService<EncinaFunctionMiddleware>();
        middleware.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaAzureFunctions_RegistersHealthCheck_WhenEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAzureFunctions(options =>
        {
            options.ProviderHealthCheck.Enabled = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheck = provider.GetService<IEncinaHealthCheck>();
        healthCheck.Should().NotBeNull();
        healthCheck.Should().BeOfType<AzureFunctionsHealthCheck>();
    }

    [Fact]
    public void AddEncinaAzureFunctions_DoesNotRegisterHealthCheck_WhenDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAzureFunctions(options =>
        {
            options.ProviderHealthCheck.Enabled = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheck = provider.GetService<IEncinaHealthCheck>();
        healthCheck.Should().BeNull();
    }

    [Fact]
    public void AddEncinaAzureFunctions_IsIdempotent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAzureFunctions();
        services.AddEncinaAzureFunctions();
        var provider = services.BuildServiceProvider();

        // Assert
        var middlewares = provider.GetServices<EncinaFunctionMiddleware>().ToList();
        middlewares.Should().HaveCount(1);
    }

    [Fact]
    public void AddEncinaAzureFunctions_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAzureFunctions();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaAzureFunctions_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAzureFunctions();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaAzureFunctions_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddEncinaAzureFunctions(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }
}
