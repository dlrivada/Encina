using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Encina.ADO.MySQL.Health;

/// <summary>
/// Database health monitor for MySQL using ADO.NET (MySqlConnector).
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="MySqlConnection.ClearPool(MySqlConnection)"/> for pool clearing.
/// MySqlConnector has limited public pool statistics APIs, so pool statistics
/// are based on available connection state information.
/// </para>
/// </remarks>
public sealed class MySqlDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    private readonly Func<IDbConnection> _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public MySqlDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("ado-mysql", CreateConnectionFactory(serviceProvider), options)
    {
        _connectionFactory = CreateConnectionFactory(serviceProvider);
    }

    /// <inheritdoc />
    protected override ConnectionPoolStats GetPoolStatisticsCore()
    {
        // MySqlConnector has limited public pool statistics APIs.
        // Return estimated metrics from connection string configuration.
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
