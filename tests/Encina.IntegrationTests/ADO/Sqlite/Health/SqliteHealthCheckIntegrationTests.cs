using System.Data;
using Encina.ADO.Sqlite.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.ADO.Sqlite.Health;

/// <summary>
/// Integration tests for ADO.NET <see cref="SqliteHealthCheck"/> using a real SQLite database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.Sqlite")]
public sealed class SqliteHealthCheckIntegrationTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;

    public SqliteHealthCheckIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqliteHealthCheck(serviceProvider, null);

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
        var options = new ProviderHealthCheckOptions { Name = "my-custom-ado-sqlite" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqliteHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal("my-custom-ado-sqlite", healthCheck.Name);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqliteHealthCheck(serviceProvider, null);

        // Assert
        Assert.Contains("encina", healthCheck.Tags);
        Assert.Contains("database", healthCheck.Tags);
        Assert.Contains("sqlite", healthCheck.Tags);
        Assert.Contains("ready", healthCheck.Tags);
    }

    [Fact]
    public async Task CheckHealthAsync_ExecutesQuerySuccessfully()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqliteHealthCheck(serviceProvider, null);

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
