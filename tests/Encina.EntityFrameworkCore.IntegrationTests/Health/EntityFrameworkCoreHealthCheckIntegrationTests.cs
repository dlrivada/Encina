using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Encina.EntityFrameworkCore.IntegrationTests.Health;

/// <summary>
/// Integration tests for EntityFrameworkCoreHealthCheck using a real SQL Server container.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class EntityFrameworkCoreHealthCheckIntegrationTests : IClassFixture<EFCoreFixture>
{
    private readonly EFCoreFixture _fixture;

    public EntityFrameworkCoreHealthCheckIntegrationTests(EFCoreFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-efcore" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-efcore");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("database");
        healthCheck.Tags.Should().Contain("efcore");
        healthCheck.Tags.Should().Contain("ready");
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaEfcore()
    {
        EntityFrameworkCoreHealthCheck.DefaultName.Should().Be("encina-efcore");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<DbContext>(_ => _fixture.CreateDbContext());
        return services.BuildServiceProvider();
    }
}
