using Encina.Marten.Health;
using Encina.Marten.IntegrationTests.Fixtures;
using Encina.Messaging.Health;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Marten.IntegrationTests.Health;

/// <summary>
/// Integration tests for MartenHealthCheck using a real PostgreSQL container with Marten.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Trait("EventSourcing", "Marten")]
public sealed class MartenHealthCheckIntegrationTests : IClassFixture<MartenFixture>
{
    private readonly MartenFixture _fixture;

    public MartenHealthCheckIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenMartenIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL/Marten container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MartenHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("connected");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL/Marten container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-marten" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MartenHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-marten");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL/Marten container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MartenHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("eventsourcing");
        healthCheck.Tags.Should().Contain("marten");
        healthCheck.Tags.Should().Contain("postgresql");
        healthCheck.Tags.Should().Contain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(_fixture.Store!);
        return services.BuildServiceProvider();
    }
}
