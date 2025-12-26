using System.Data;
using Encina.ADO.PostgreSQL.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Encina.ADO.PostgreSQL.IntegrationTests.Health;

/// <summary>
/// Integration tests for ADO.NET <see cref="PostgreSqlHealthCheck"/> using a real PostgreSQL database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.PostgreSQL")]
public sealed class PostgreSqlHealthCheckIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlHealthCheckIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains("healthy", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-ado-postgres" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal("my-custom-ado-postgres", healthCheck.Name);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Assert
        Assert.Contains("encina", healthCheck.Tags);
        Assert.Contains("database", healthCheck.Tags);
        Assert.Contains("postgresql", healthCheck.Tags);
        Assert.Contains("ready", healthCheck.Tags);
    }

    [SkippableFact]
    public async Task CheckHealthAsync_ExecutesQuerySuccessfully()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Act - Multiple calls should all succeed
        var result1 = await healthCheck.CheckHealthAsync();
        var result2 = await healthCheck.CheckHealthAsync();
        var result3 = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, result1.Status);
        Assert.Equal(HealthStatus.Healthy, result2.Status);
        Assert.Equal(HealthStatus.Healthy, result3.Status);
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDbConnection>(_ => _fixture.CreateConnection());
        return services.BuildServiceProvider();
    }
}
