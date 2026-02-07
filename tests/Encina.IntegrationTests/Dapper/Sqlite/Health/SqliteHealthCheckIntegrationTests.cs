using System.Data;
using Encina.Dapper.Sqlite.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Dapper.Sqlite.Health;

/// <summary>
/// Integration tests for <see cref="SqliteHealthCheck"/> using a real SQLite database.
/// </summary>
[Collection("Dapper-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.Sqlite")]
public sealed class SqliteHealthCheckIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;

    public SqliteHealthCheckIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

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
        var options = new ProviderHealthCheckOptions { Name = "my-custom-dapper-sqlite" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new SqliteHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        Assert.Equal("my-custom-dapper-sqlite", healthCheck.Name);
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

    /// <summary>
    /// Creates a ServiceProvider that provides NEW disposable SQLite connections.
    /// DatabaseHealthCheck uses "using var connection = ..." which disposes the connection.
    /// We must NOT give it the fixture's shared in-memory connection, or it will be destroyed.
    /// </summary>
    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDbConnection>(_ =>
        {
            // Create a new independent connection each time (using the same shared DB via Cache=Shared)
            var conn = new SqliteConnection(_fixture.ConnectionString);
            conn.Open();
            return conn;
        });
        return services.BuildServiceProvider();
    }
}
