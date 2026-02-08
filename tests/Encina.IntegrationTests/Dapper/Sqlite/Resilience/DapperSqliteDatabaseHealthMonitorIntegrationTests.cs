using System.Data;
using Encina.Dapper.Sqlite.Health;
using Encina.Database;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Dapper.Sqlite.Resilience;

/// <summary>
/// Integration tests for <see cref="DapperSqliteDatabaseHealthMonitor"/> using a real SQLite database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.Sqlite")]
[Collection("Dapper-Sqlite")]
public sealed class DapperSqliteDatabaseHealthMonitorIntegrationTests
{
    private readonly SqliteFixture _fixture;

    public DapperSqliteDatabaseHealthMonitorIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ProviderName_ReturnsDapperSqlite()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new DapperSqliteDatabaseHealthMonitor(serviceProvider);

        // Act & Assert
        Assert.Equal("dapper-sqlite", monitor.ProviderName);
    }

    [Fact]
    public void IsCircuitOpen_InitiallyFalse()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new DapperSqliteDatabaseHealthMonitor(serviceProvider);

        // Act & Assert
        Assert.False(monitor.IsCircuitOpen);
    }

    [Fact]
    public void GetPoolStatistics_ReturnsEmptyStats()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new DapperSqliteDatabaseHealthMonitor(serviceProvider);

        // Act
        var stats = monitor.GetPoolStatistics();

        // Assert
        Assert.Equal(ConnectionPoolStats.CreateEmpty(), stats);
    }

    [Fact]
    public async Task CheckHealthAsync_WithRealDatabase_ReturnsHealthy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new DapperSqliteDatabaseHealthMonitor(serviceProvider);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        Assert.Equal(DatabaseHealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.False(monitor.IsCircuitOpen);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDataWithProviderName()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new DapperSqliteDatabaseHealthMonitor(serviceProvider);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal("dapper-sqlite", result.Data!["provider"]);
    }

    [Fact]
    public async Task ClearPoolAsync_CompletesSuccessfully()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new DapperSqliteDatabaseHealthMonitor(serviceProvider);

        // Act & Assert
        await monitor.ClearPoolAsync();
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDbConnection>(_ =>
        {
            var conn = new SqliteConnection(_fixture.ConnectionString);
            conn.Open();
            return conn;
        });
        return services.BuildServiceProvider();
    }
}
