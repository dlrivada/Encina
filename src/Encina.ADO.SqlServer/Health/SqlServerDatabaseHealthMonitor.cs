using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.ADO.SqlServer.Health;

/// <summary>
/// Database health monitor for SQL Server using ADO.NET.
/// </summary>
/// <remarks>
/// <para>
/// Leverages <see cref="SqlConnection.RetrieveStatistics()"/> to access connection statistics
/// and <see cref="SqlConnection.ClearPool(SqlConnection)"/> / <see cref="SqlConnection.ClearAllPools()"/>
/// for pool management.
/// </para>
/// <para>
/// Pool statistics from <c>RetrieveStatistics()</c> include counters such as
/// <c>NumberOfActiveConnections</c>, <c>NumberOfFreeConnections</c>, and
/// <c>NumberOfPooledConnections</c>.
/// </para>
/// </remarks>
public sealed class SqlServerDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    private readonly Func<IDbConnection> _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public SqlServerDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("ado-sqlserver", CreateConnectionFactory(serviceProvider), options)
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
