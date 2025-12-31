using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using Shouldly;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AzureFunctions.ContractTests.Durable;

/// <summary>
/// Contract tests to verify that DurableFunctionsHealthCheck properly implements IEncinaHealthCheck.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DurableFunctionsHealthCheckContractTests
{
    [Fact]
    public void DurableFunctionsHealthCheck_ImplementsIEncinaHealthCheck()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());

        // Act
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Assert
        healthCheck.ShouldBeAssignableTo<IEncinaHealthCheck>();
    }

    [Fact]
    public void Name_IsNotNullOrEmpty()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var name = healthCheck.Name;

        // Assert
        name.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Name_HasDefaultValue_WhenNotConfigured()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var name = healthCheck.Name;

        // Assert
        name.ShouldBe("encina-durable-functions");
    }

    [Fact]
    public void Name_ReturnsConfiguredValue()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            ProviderHealthCheck = { Name = "custom-durable-check" }
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var name = healthCheck.Name;

        // Assert
        name.ShouldBe("custom-durable-check");
    }

    [Fact]
    public void Tags_IsNotNull()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldNotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsValidResult()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsResultWithData()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldNotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_DataContainsConfigurationSettings()
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
        result.Data.ShouldContainKey("defaultMaxRetries");
        result.Data.ShouldContainKey("defaultBackoffCoefficient");
    }

    [Fact]
    public async Task CheckHealthAsync_SupportsCancellation()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions());
        var healthCheck = new DurableFunctionsHealthCheck(options);
        using var cts = new CancellationTokenSource();

        // Act - should not throw
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.Status.ShouldNotBe(default(HealthStatus));
    }

    [Fact]
    public async Task CheckHealthAsync_WithValidConfiguration_ReturnsHealthy()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultMaxRetries = 3,
            DefaultFirstRetryInterval = TimeSpan.FromSeconds(1),
            DefaultBackoffCoefficient = 2.0,
            DefaultMaxRetryInterval = TimeSpan.FromMinutes(5)
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidConfiguration_ReturnsDegraded()
    {
        // Arrange
        var options = Options.Create(new DurableFunctionsOptions
        {
            DefaultMaxRetries = -1 // Invalid
        });
        var healthCheck = new DurableFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }
}
