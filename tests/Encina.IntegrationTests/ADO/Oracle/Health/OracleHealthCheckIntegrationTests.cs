using System.Data;
using Encina.ADO.Oracle.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.ADO.Oracle.Health;

/// <summary>
/// Integration tests for ADO.NET <see cref="OracleHealthCheck"/> using a real Oracle database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.Oracle")]
public sealed class OracleHealthCheckIntegrationTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _fixture;

    public OracleHealthCheckIntegrationTests(OracleFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new OracleHealthCheck(serviceProvider, null);

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
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-ado-oracle" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new OracleHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal("my-custom-ado-oracle", healthCheck.Name);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new OracleHealthCheck(serviceProvider, null);

        // Assert
        Assert.Contains("encina", healthCheck.Tags);
        Assert.Contains("database", healthCheck.Tags);
        Assert.Contains("oracle", healthCheck.Tags);
        Assert.Contains("ready", healthCheck.Tags);
    }

    [SkippableFact]
    public async Task CheckHealthAsync_ExecutesQuerySuccessfully()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new OracleHealthCheck(serviceProvider, null);

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
