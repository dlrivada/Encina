using System.Data;
using Encina.ADO.MySQL.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Encina.ADO.MySQL.IntegrationTests.Health;

/// <summary>
/// Integration tests for ADO.NET <see cref="MySqlHealthCheck"/> using a real MySQL database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.MySQL")]
public sealed class MySqlHealthCheckIntegrationTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlHealthCheckIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);

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
        Skip.IfNot(_fixture.IsAvailable, "MySQL container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-ado-mysql" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal("my-custom-ado-mysql", healthCheck.Name);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);

        // Assert
        Assert.Contains("encina", healthCheck.Tags);
        Assert.Contains("database", healthCheck.Tags);
        Assert.Contains("mysql", healthCheck.Tags);
        Assert.Contains("ready", healthCheck.Tags);
    }

    [SkippableFact]
    public async Task CheckHealthAsync_ExecutesQuerySuccessfully()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);

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
