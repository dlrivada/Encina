using Encina.Messaging.Health;
using Encina.NATS.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Encina.IntegrationTests.MessageBrokers.NATS;

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

    [Fact]
    public async Task CheckHealthAsync_WhenNATSIsRunning_ReturnsHealthy()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new NATSHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-nats" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new NATSHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-nats");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new NATSHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("nats");
        healthCheck.Tags.ShouldContain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INatsConnection>(_fixture.Connection!);
        return services.BuildServiceProvider();
    }
}
