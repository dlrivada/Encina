using Encina.AzureFunctions.Health;
using Encina.Messaging.Health;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AzureFunctions.ContractTests;

/// <summary>
/// Contract tests to verify that AzureFunctionsHealthCheck properly implements IEncinaHealthCheck.
/// </summary>
public class HealthCheckContractTests
{
    [Fact]
    public void AzureFunctionsHealthCheck_ImplementsIEncinaHealthCheck()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());

        // Act
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Assert
        healthCheck.Should().BeAssignableTo<IEncinaHealthCheck>();
    }

    [Fact]
    public void Name_IsNotNullOrEmpty()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var name = healthCheck.Name;

        // Assert
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Tags_IsNotNull()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsValidResult()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsResultWithData()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_SupportsCancellation()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);
        using var cts = new CancellationTokenSource();

        // Act - should not throw
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.Status.Should().NotBe(default(HealthStatus));
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_CompletesNormally()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());
        var healthCheck = new AzureFunctionsHealthCheck(options);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Azure Functions health check is synchronous, so it completes even with cancelled token
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert - should complete without throwing
        result.Should().NotBe(default);
    }

    [Theory]
    [InlineData(HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded)]
    public async Task CheckHealthAsync_ReturnsExpectedStatus_BasedOnConfiguration(HealthStatus expectedMinStatus)
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions
        {
            CorrelationIdHeader = expectedMinStatus == HealthStatus.Degraded ? string.Empty : "X-Correlation-ID",
            TenantIdHeader = "X-Tenant-ID",
            UserIdClaimType = "sub",
            TenantIdClaimType = "tenant_id"
        });
        var healthCheck = new AzureFunctionsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(expectedMinStatus);
    }
}
