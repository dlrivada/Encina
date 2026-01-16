using System.Data;
using Encina.Dapper.SqlServer.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Encina.IntegrationTests.Dapper.SqlServer.Health;

/// <summary>
/// Integration tests for <see cref="SqlServerHealthCheck"/> using a real SQL Server database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.SqlServer")]
public sealed class SqlServerHealthCheckIntegrationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SqlServerHealthCheckIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqlServerHealthCheck(serviceProvider, null);

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
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

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

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqlServerHealthCheck(serviceProvider, null);

        // Assert
        Assert.Contains("encina", healthCheck.Tags);
        Assert.Contains("database", healthCheck.Tags);
        Assert.Contains("sqlserver", healthCheck.Tags);
        Assert.Contains("ready", healthCheck.Tags);
    }

    [SkippableFact]
    public async Task CheckHealthAsync_ExecutesQuerySuccessfully()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqlServerHealthCheck(serviceProvider, null);

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

