using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Dapper.SqlServer.Health;

/// <summary>
/// Database health monitor for SQL Server using Dapper.
/// </summary>
/// <remarks>
/// <para>
/// Dapper uses the same underlying ADO.NET connection pool as <c>Microsoft.Data.SqlClient</c>.
/// This monitor provides identical pool statistics and health checks using
/// <see cref="SqlConnection.RetrieveStatistics()"/> and <see cref="SqlConnection.ClearAllPools()"/>.
/// </para>
/// <para>
/// When both ADO.NET and Dapper providers are registered, <see cref="IDatabaseHealthMonitor"/>
/// resolves to whichever was registered first (via <c>TryAddSingleton</c>), since they share
/// the same connection pool.
/// </para>
/// </remarks>
public sealed class DapperSqlServerDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    private readonly Func<IDbConnection> _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperSqlServerDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public DapperSqlServerDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("dapper-sqlserver", CreateConnectionFactory(serviceProvider), options)
    {
        _connectionFactory = CreateConnectionFactory(serviceProvider);
    }

    /// <inheritdoc />
    protected override ConnectionPoolStats GetPoolStatisticsCore()
    {
        try
        {
            using var connection = _connectionFactory() as SqlConnection;
            if (connection is null)
            {
                return ConnectionPoolStats.CreateEmpty();
            }

            connection.StatisticsEnabled = true;
            connection.Open();

            var stats = connection.RetrieveStatistics();

            var activeConnections = GetStatValue(stats, "NumberOfActiveConnections");
            var freeConnections = GetStatValue(stats, "NumberOfFreeConnections");
            var pooledConnections = GetStatValue(stats, "NumberOfPooledConnections");

            return new ConnectionPoolStats(
                ActiveConnections: activeConnections,
                IdleConnections: freeConnections,
                TotalConnections: pooledConnections,
                PendingRequests: 0, // Not directly available from RetrieveStatistics
                MaxPoolSize: GetMaxPoolSizeFromConnectionString(connection.ConnectionString));
        }
        catch
        {
            return ConnectionPoolStats.CreateEmpty();
        }
    }

    /// <inheritdoc />
    protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken)
    {
        SqlConnection.ClearAllPools();
        return Task.CompletedTask;
    }

    private static Func<IDbConnection> CreateConnectionFactory(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return () =>
        {
            var scope = serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IDbConnection>();
        };
    }

    private static int GetStatValue(System.Collections.IDictionary stats, string key)
    {
        if (stats.Contains(key) && stats[key] is long value)
        {
            return (int)value;
        }

        return 0;
    }

    private static int GetMaxPoolSizeFromConnectionString(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.MaxPoolSize;
        }
        catch
        {
            return 100; // SQL Server default
        }
    }
}
