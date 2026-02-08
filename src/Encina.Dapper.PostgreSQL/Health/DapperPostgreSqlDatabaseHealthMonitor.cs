using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Encina.Dapper.PostgreSQL.Health;

/// <summary>
/// Database health monitor for PostgreSQL using Dapper.
/// </summary>
/// <remarks>
/// <para>
/// Dapper uses the same underlying ADO.NET connection pool as Npgsql.
/// This monitor provides pool statistics estimated from connection string configuration
/// and uses <see cref="NpgsqlConnection.ClearPool(NpgsqlConnection)"/> for pool clearing.
/// </para>
/// <para>
/// When both ADO.NET and Dapper providers are registered, <see cref="IDatabaseHealthMonitor"/>
/// resolves to whichever was registered first (via <c>TryAddSingleton</c>), since they share
/// the same connection pool.
/// </para>
/// </remarks>
public sealed class DapperPostgreSqlDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    private readonly Func<IDbConnection> _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperPostgreSqlDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public DapperPostgreSqlDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("dapper-postgresql", CreateConnectionFactory(serviceProvider), options)
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
    protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = _connectionFactory() as NpgsqlConnection;
            if (connection is not null)
            {
                NpgsqlConnection.ClearPool(connection);
            }
        }
        catch
        {
            // Best effort - pool clearing is not critical
        }

        return Task.CompletedTask;
    }

    private int GetMaxPoolSizeFromConnectionString()
    {
        try
        {
            using var connection = _connectionFactory() as NpgsqlConnection;
            if (connection is null)
            {
                return 100; // Npgsql default
            }

            var builder = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            return builder.MaxPoolSize;
        }
        catch
        {
            return 100; // Npgsql default
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
