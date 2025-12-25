using Encina.Messaging.Health;
using Encina.NATS.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Encina.NATS.IntegrationTests.Health;

/// <summary>
/// Integration tests for NATSHealthCheck using a real NATS container.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Messaging", "NATS")]
public sealed class NATSHealthCheckIntegrationTests : IClassFixture<NatsFixture>
{
    private readonly NatsFixture _fixture;

    public NATSHealthCheckIntegrationTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenNATSIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new NATSHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("connected");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-nats" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new NATSHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-nats");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "NATS container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new NATSHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("messaging");
        healthCheck.Tags.Should().Contain("nats");
        healthCheck.Tags.Should().Contain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INatsConnection>(_fixture.Connection!);
        return services.BuildServiceProvider();
    }
}
