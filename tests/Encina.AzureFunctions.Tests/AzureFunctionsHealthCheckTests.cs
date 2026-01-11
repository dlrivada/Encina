using Encina.AzureFunctions.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.AzureFunctions.Tests;

public class AzureFunctionsHealthCheckTests
{
    [Fact]
    public void Name_ReturnsConfiguredName()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            ProviderHealthCheck = new ProviderHealthCheckOptions
            {
                Name = "custom-health-check"
            }
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var name = healthCheck.Name;

        // Assert
        name.ShouldBe("custom-health-check");
    }

    [Fact]
    public void Name_ReturnsDefaultName_WhenNotConfigured()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            ProviderHealthCheck = new ProviderHealthCheckOptions
            {
                Name = null
            }
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var name = healthCheck.Name;

        // Assert
        name.ShouldBe("encina-azure-functions");
    }

    [Fact]
    public void Tags_ReturnsConfiguredTags()
    {
        // Arrange
        string[] expectedTags = ["custom", "tag"];
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            ProviderHealthCheck = new ProviderHealthCheckOptions
            {
                Tags = expectedTags
            }
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldBe(expectedTags);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenConfigurationIsValid()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("properly configured");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesConfigurationData()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            EnableRequestContextEnrichment = true,
            CorrelationIdHeader = "X-Custom-Correlation",
            TenantIdHeader = "X-Custom-Tenant"
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("requestContextEnrichment");
        result.Data["requestContextEnrichment"].ShouldBe(true);
        result.Data.ShouldContainKey("correlationIdHeader");
        result.Data["correlationIdHeader"].ShouldBe("X-Custom-Correlation");
        result.Data.ShouldContainKey("tenantIdHeader");
        result.Data["tenantIdHeader"].ShouldBe("X-Custom-Tenant");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenCorrelationIdHeaderIsEmpty()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            CorrelationIdHeader = string.Empty
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("incomplete configuration");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenTenantIdHeaderIsEmpty()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            TenantIdHeader = string.Empty
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenUserIdClaimTypeIsEmpty()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            UserIdClaimType = string.Empty
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenTenantIdClaimTypeIsEmpty()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            TenantIdClaimType = string.Empty
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_SupportsCancellation()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);
        var cts = new CancellationTokenSource();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert - Should complete without throwing
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act
        var action = () => new AzureFunctionsHealthCheck(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("options");
    }
}
