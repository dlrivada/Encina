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
        using var serviceProvider = CreateServiceProvider();
        var healthCheck = new MartenHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldContain("connected");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL/Marten container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-marten" };
        using var serviceProvider = CreateServiceProvider();
        var healthCheck = new MartenHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-marten");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL/Marten container not available");

        // Arrange
        using var serviceProvider = CreateServiceProvider();
        var healthCheck = new MartenHealthCheck(serviceProvider, null);

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldContain("encina");
        tags.ShouldContain("eventsourcing");
        tags.ShouldContain("marten");
        tags.ShouldContain("postgresql");
        tags.ShouldContain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(_fixture.Store!);
        return services.BuildServiceProvider();
    }
}
