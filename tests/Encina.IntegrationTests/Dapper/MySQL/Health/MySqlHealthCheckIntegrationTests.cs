using System.Data;
using Encina.Dapper.MySQL.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Encina.IntegrationTests.Dapper.MySQL.Health;

/// <summary>
/// Integration tests for <see cref="MySqlHealthCheck"/> using a real MySQL database.
/// </summary>
[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.MySQL")]
public sealed class MySqlHealthCheckIntegrationTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;

    public MySqlHealthCheckIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {

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

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-mysql" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal("my-custom-mysql", healthCheck.Name);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);

        // Assert
        Assert.Contains("encina", healthCheck.Tags);
        Assert.Contains("database", healthCheck.Tags);
        Assert.Contains("mysql", healthCheck.Tags);
        Assert.Contains("ready", healthCheck.Tags);
    }

    [Fact]
    public async Task CheckHealthAsync_ExecutesQuerySuccessfully()
    {

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
