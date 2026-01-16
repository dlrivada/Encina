using Encina.AzureFunctions;
using Encina.AzureFunctions.Health;
using Encina.Messaging.Health;

namespace Encina.UnitTests.AzureFunctions;

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
        options.ShouldNotBeNull();
        options!.Value.ShouldNotBeNull();
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
        options.Value.CorrelationIdHeader.ShouldBe("X-Request-ID");
        options.Value.EnableRequestContextEnrichment.ShouldBeFalse();
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
        middleware.ShouldNotBeNull();
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
        healthCheck.ShouldNotBeNull();
        healthCheck.ShouldBeOfType<AzureFunctionsHealthCheck>();
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
        healthCheck.ShouldBeNull();
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
        middlewares.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddEncinaAzureFunctions_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAzureFunctions();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAzureFunctions_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAzureFunctions();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAzureFunctions_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddEncinaAzureFunctions(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("configureOptions");
    }
}
