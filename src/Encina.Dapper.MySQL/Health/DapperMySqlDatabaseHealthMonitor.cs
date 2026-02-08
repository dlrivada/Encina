using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Encina.Dapper.MySQL.Health;

/// <summary>
/// Database health monitor for MySQL using Dapper.
/// </summary>
/// <remarks>
/// <para>
/// Dapper uses the same underlying ADO.NET connection pool as MySqlConnector.
/// This monitor provides pool statistics estimated from connection string configuration
/// and uses <see cref="MySqlConnection.ClearPoolAsync(MySqlConnection, CancellationToken)"/>
/// for pool clearing.
/// </para>
/// <para>
/// When both ADO.NET and Dapper providers are registered, <see cref="IDatabaseHealthMonitor"/>
/// resolves to whichever was registered first (via <c>TryAddSingleton</c>), since they share
/// the same connection pool.
/// </para>
/// </remarks>
public sealed class DapperMySqlDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    private readonly Func<IDbConnection> _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperMySqlDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public DapperMySqlDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("dapper-mysql", CreateConnectionFactory(serviceProvider), options)
    {
        _connectionFactory = CreateConnectionFactory(serviceProvider);
    }

    /// <inheritdoc />
    protected override ConnectionPoolStats GetPoolStatisticsCore()
    {
        try
        {
            var maxPoolSize = GetMaxPoolSizeFromConnectionString();
            return new ConnectionPoolStats(
                ActiveConnections: 0,
                IdleConnections: 0,
                TotalConnections: 0,
                PendingRequests: 0,
                MaxPoolSize: maxPoolSize);
        }
        catch
        {
            return ConnectionPoolStats.CreateEmpty();
        }
    }

    /// <inheritdoc />
    protected override async Task ClearPoolCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = _connectionFactory() as MySqlConnection;
            if (connection is not null)
            {
                await MySqlConnection.ClearPoolAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // Best effort - pool clearing is not critical
        }
    }

    private int GetMaxPoolSizeFromConnectionString()
    {
        try
        {
            using var connection = _connectionFactory() as MySqlConnection;
            if (connection is null)
            {
                return 100; // MySqlConnector default
            }

            var builder = new MySqlConnectionStringBuilder(connection.ConnectionString);
            return (int)builder.MaximumPoolSize;
        }
        catch
        {
            return 100; // MySqlConnector default
        }
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
}
