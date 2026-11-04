using Encina.AzureFunctions;
using Encina.AzureFunctions.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.Options;
using static Encina.IntegrationTests.Cloud.AzureFunctions.AzureFunctionsTestSupport;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Checks the <c>AddEncinaAzureFunctions</c> registration and its health check on a provider built with
/// <c>ValidateOnBuild</c> and <c>ValidateScopes</c>, next to the real Encina pipeline.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class AzureFunctionsRegistrationIntegrationTests
{
    [Fact]
    public async Task AddEncinaAzureFunctions_BuildsAValidProviderWithTheOptionsAndTheMiddleware()
    {
        // Arrange
        await using var provider = CreateServiceProvider(options => options.CorrelationIdHeader = "X-Request-ID");

        // Act
        var options = provider.GetRequiredService<IOptions<EncinaAzureFunctionsOptions>>().Value;
        var middleware = provider.GetRequiredService<EncinaFunctionMiddleware>();

        // Assert
        options.CorrelationIdHeader.ShouldBe("X-Request-ID");
        middleware.ShouldBeSameAs(provider.GetRequiredService<EncinaFunctionMiddleware>());
    }

    [Fact]
    public async Task AddEncinaAzureFunctions_WithoutConfiguration_BuildsAValidProviderWithTheDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddEncinaAzureFunctions();

        // Act
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert
        provider.GetRequiredService<IOptions<EncinaAzureFunctionsOptions>>().Value.EnableRequestContextEnrichment.ShouldBeTrue();
        provider.GetRequiredService<EncinaFunctionMiddleware>().ShouldNotBeNull();
    }

    [Fact]
    public async Task AddEncinaAzureFunctions_RegistersTheHealthCheckOnlyWhenItIsEnabled()
    {
        // Arrange
        await using var enabled = CreateServiceProvider(options => options.ProviderHealthCheck.Enabled = true);
        await using var disabled = CreateServiceProvider(options => options.ProviderHealthCheck.Enabled = false);

        // Act
        var enabledChecks = enabled.GetServices<IEncinaHealthCheck>().ToList();
        var disabledChecks = disabled.GetServices<IEncinaHealthCheck>().ToList();

        // Assert
        enabledChecks.ShouldHaveSingleItem().ShouldBeOfType<AzureFunctionsHealthCheck>();
        disabledChecks.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_WithTheDefaultConfiguration_ReportsHealthy()
    {
        // Arrange
        await using var provider = CreateServiceProvider(options => options.ProviderHealthCheck.Enabled = true);
        var healthCheck = provider.GetServices<IEncinaHealthCheck>().Single();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("encina-azure-functions");
        healthCheck.Tags.ShouldNotBeNull();
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["correlationIdHeader"].ShouldBe("X-Correlation-ID");
        result.Data["tenantIdHeader"].ShouldBe("X-Tenant-ID");
        result.Data["requestContextEnrichment"].ShouldBe(true);
    }

    [Fact]
    public async Task CheckHealthAsync_WithAnIncompleteConfiguration_ReportsDegraded()
    {
        // Arrange
        await using var provider = CreateServiceProvider(options =>
        {
            options.ProviderHealthCheck.Enabled = true;
            options.TenantIdClaimType = string.Empty;
        });
        var healthCheck = provider.GetServices<IEncinaHealthCheck>().Single();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }
}
