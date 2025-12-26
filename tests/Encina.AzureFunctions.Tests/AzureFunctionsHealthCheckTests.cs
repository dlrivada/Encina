using Encina.AzureFunctions.Health;
using Encina.Messaging.Health;
using FluentAssertions;
using Microsoft.Extensions.Options;
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
        name.Should().Be("custom-health-check");
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
        name.Should().Be("encina-azure-functions");
    }

    [Fact]
    public void Tags_ReturnsConfiguredTags()
    {
        // Arrange
        var expectedTags = new[] { "custom", "tag" };
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
        tags.Should().BeEquivalentTo(expectedTags);
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
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("properly configured");
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
        result.Data.Should().ContainKey("requestContextEnrichment");
        result.Data["requestContextEnrichment"].Should().Be(true);
        result.Data.Should().ContainKey("correlationIdHeader");
        result.Data["correlationIdHeader"].Should().Be("X-Custom-Correlation");
        result.Data.Should().ContainKey("tenantIdHeader");
        result.Data["tenantIdHeader"].Should().Be("X-Custom-Tenant");
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
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("incomplete configuration");
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
        result.Status.Should().Be(HealthStatus.Degraded);
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
        result.Status.Should().Be(HealthStatus.Degraded);
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
        result.Status.Should().Be(HealthStatus.Degraded);
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
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act
        var action = () => new AzureFunctionsHealthCheck(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }
}
