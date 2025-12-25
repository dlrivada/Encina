using System.Data;
using Encina.ADO.SqlServer.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Encina.ADO.SqlServer.IntegrationTests.Health;

/// <summary>
/// Integration tests for SqlServerHealthCheck using a real SQL Server container.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
public sealed class SqlServerHealthCheckIntegrationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SqlServerHealthCheckIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqlServerHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("healthy", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-sqlserver" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqlServerHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal("my-custom-sqlserver", healthCheck.Name);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void Tags_ContainsDatabaseTags()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqlServerHealthCheck(serviceProvider, null);

        // Assert
        Assert.Contains("encina", healthCheck.Tags);
        Assert.Contains("database", healthCheck.Tags);
        Assert.Contains("ready", healthCheck.Tags);
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDbConnection>(_ => _fixture.CreateConnection());
        return services.BuildServiceProvider();
    }
}
