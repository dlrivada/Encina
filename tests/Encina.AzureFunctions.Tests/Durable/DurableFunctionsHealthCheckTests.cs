using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class DurableFunctionsHealthCheckTests
{
    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());

        // Act
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Assert
        healthCheck.Should().NotBeNull();
    }

    [Fact]
    public void Name_ReturnsConfiguredName()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            ProviderHealthCheck = new ProviderHealthCheckOptions
            {
                Name = "custom-durable-check"
            }
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act & Assert
        healthCheck.Name.Should().Be("custom-durable-check");
    }

    [Fact]
    public void Name_WhenNull_ReturnsDefaultName()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            ProviderHealthCheck = new ProviderHealthCheckOptions
            {
                Name = null
            }
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act & Assert
        healthCheck.Name.Should().Be("encina-durable-functions");
    }

    [Fact]
    public void Tags_ReturnsConfiguredTags()
    {
        // Arrange
        var expectedTags = new[] { "tag1", "tag2" };
        var options = Options.Create(new DurableFunctionsOptions
        {
            ProviderHealthCheck = new ProviderHealthCheckOptions
            {
                Tags = expectedTags
            }
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act & Assert
        healthCheck.Tags.Should().BeEquivalentTo(expectedTags);
    }

    [Fact]
    public async Task CheckHealthAsync_WithValidConfiguration_ReturnsHealthy()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("configured and ready");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesConfigurationInData()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultMaxRetries = 5,
            DefaultBackoffCoefficient = 2.5
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.Should().ContainKey("defaultMaxRetries");
        result.Data["defaultMaxRetries"].Should().Be(5);
        result.Data["defaultBackoffCoefficient"].Should().Be(2.5);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNegativeRetries_ReturnsDegraded()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultMaxRetries = -1
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("DefaultMaxRetries cannot be negative");
    }

    [Fact]
    public async Task CheckHealthAsync_WithZeroFirstRetryInterval_ReturnsDegraded()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultFirstRetryInterval = TimeSpan.Zero
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("DefaultFirstRetryInterval must be positive");
    }

    [Fact]
    public async Task CheckHealthAsync_WithZeroBackoffCoefficient_ReturnsDegraded()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultBackoffCoefficient = 0
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("DefaultBackoffCoefficient must be positive");
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidSagaTimeout_ReturnsDegraded()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultSagaTimeout = TimeSpan.FromSeconds(-1)
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("DefaultSagaTimeout must be positive");
    }

    [Fact]
    public async Task CheckHealthAsync_WithMultipleIssues_ReportsAllIssues()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultMaxRetries = -1,
            DefaultBackoffCoefficient = 0
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("DefaultMaxRetries cannot be negative");
        result.Description.Should().Contain("DefaultBackoffCoefficient must be positive");
    }

    [Fact]
    public async Task CheckHealthAsync_WithValidSagaTimeout_IncludesInData()
    {
        // Arrange
        var timeout = TimeSpan.FromHours(1);
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultSagaTimeout = timeout
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("defaultSagaTimeout");
    }
}
