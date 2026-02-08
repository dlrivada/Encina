using System.Data;
using Encina.ADO.Sqlite.Health;
using Encina.Database;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.ADO.Sqlite.Resilience;

/// <summary>
/// Integration tests for <see cref="SqliteDatabaseHealthMonitor"/> using a real SQLite database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.Sqlite")]
[Collection("ADO-Sqlite")]
public sealed class SqliteDatabaseHealthMonitorIntegrationTests
{
    private readonly SqliteFixture _fixture;

    public SqliteDatabaseHealthMonitorIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ProviderName_ReturnsAdoSqlite()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);

        // Act & Assert
        Assert.Equal("ado-sqlite", monitor.ProviderName);
    }

    [Fact]
    public void IsCircuitOpen_InitiallyFalse()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);

        // Act & Assert
        Assert.False(monitor.IsCircuitOpen);
    }

    [Fact]
    public void GetPoolStatistics_ReturnsEmptyStats()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);

        // Act
        var stats = monitor.GetPoolStatistics();

        // Assert - SQLite returns CreateEmpty()
        Assert.Equal(0, stats.ActiveConnections);
        Assert.Equal(0, stats.IdleConnections);
        Assert.Equal(0, stats.TotalConnections);
        Assert.Equal(0, stats.PendingRequests);
        Assert.Equal(0, stats.MaxPoolSize);
        Assert.Equal(0.0, stats.PoolUtilization);
    }

    [Fact]
    public async Task CheckHealthAsync_WithRealDatabase_ReturnsHealthy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        Assert.Equal(DatabaseHealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Null(result.Exception);
        Assert.False(monitor.IsCircuitOpen);
    }

    [Fact]
    public async Task CheckHealthAsync_MultipleChecks_AllReturnHealthy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);

        // Act
        var results = new List<DatabaseHealthResult>();
        for (var i = 0; i < 5; i++)
        {
            results.Add(await monitor.CheckHealthAsync());
        }

        // Assert
        Assert.All(results, r => Assert.Equal(DatabaseHealthStatus.Healthy, r.Status));
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDataDictionary()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.ContainsKey("provider"));
        Assert.Equal("ado-sqlite", result.Data["provider"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ReturnsUnhealthy()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - base class catches OperationCanceledException and returns Unhealthy
        var result = await monitor.CheckHealthAsync(cts.Token);

        // Assert
        Assert.Equal(DatabaseHealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ClearPoolAsync_CompletesSuccessfully()
    {
        // Arrange - SQLite ClearPool is a no-op
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider);

        // Act & Assert - should not throw
        await monitor.ClearPoolAsync();
    }

    [Fact]
    public async Task CheckHealthAsync_WithResilienceOptions_ReturnsHealthy()
    {
        // Arrange
        var options = new DatabaseResilienceOptions
        {
            EnablePoolMonitoring = true,
            EnableCircuitBreaker = true,
        };
        var serviceProvider = CreateServiceProvider();
        var monitor = new SqliteDatabaseHealthMonitor(serviceProvider, options);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        Assert.Equal(DatabaseHealthStatus.Healthy, result.Status);
    }

    /// <summary>
    /// Creates a ServiceProvider that provides NEW disposable SQLite connections.
    /// Health monitor uses "using var connection = ..." so we must NOT give it the fixture's
    /// shared in-memory connection (or it will be destroyed).
    /// </summary>
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
