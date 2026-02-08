using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Health;

/// <summary>
/// Database health monitor for PostgreSQL using ADO.NET (Npgsql).
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="NpgsqlConnection.ClearPool(NpgsqlConnection)"/> for pool clearing.
/// Pool statistics are estimated from connection state tracking, as Npgsql does not
/// expose detailed public pool metrics APIs.
/// </para>
/// </remarks>
public sealed class PostgreSqlDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    private readonly Func<IDbConnection> _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public PostgreSqlDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("ado-postgresql", CreateConnectionFactory(serviceProvider), options)
    {
        _connectionFactory = CreateConnectionFactory(serviceProvider);
    }

    /// <inheritdoc />
    protected override ConnectionPoolStats GetPoolStatisticsCore()
    {
        // Npgsql does not expose detailed public pool statistics APIs.
        // Return empty stats indicating limited visibility.
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
