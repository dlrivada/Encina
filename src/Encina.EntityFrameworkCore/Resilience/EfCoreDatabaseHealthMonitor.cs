using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.EntityFrameworkCore.Resilience;

/// <summary>
/// Database health monitor for Entity Framework Core providers.
/// </summary>
/// <remarks>
/// <para>
/// EF Core shares the same underlying connection pool as the ADO.NET driver in use
/// (Microsoft.Data.SqlClient, Npgsql, MySqlConnector, or Microsoft.Data.Sqlite).
/// This monitor uses a scoped <see cref="DbContext"/> to obtain connections for health
/// checks, delegating to the ADO.NET provider's connection pool.
/// </para>
/// <para>
/// Pool statistics are estimated based on the underlying ADO.NET driver capabilities.
/// Since EF Core abstracts the connection management, detailed pool statistics may
/// not be available for all providers.
/// </para>
/// </remarks>
public sealed class EfCoreDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve DbContext from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public EfCoreDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("efcore", CreateConnectionFactory(serviceProvider), options)
    {
    }

    /// <inheritdoc />
    /// <returns>
    /// Returns <see cref="ConnectionPoolStats.CreateEmpty()"/> as EF Core does not expose
    /// direct pool statistics. The underlying ADO.NET driver manages the actual pool.
    /// </returns>
    protected override ConnectionPoolStats GetPoolStatisticsCore()
    {
        return ConnectionPoolStats.CreateEmpty();
    }

    /// <inheritdoc />
    /// <remarks>
    /// No-op for the generic EF Core monitor. Pool clearing should be handled by
    /// the specific ADO.NET driver registered alongside EF Core.
    /// </remarks>
    protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Func<IDbConnection> CreateConnectionFactory(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return () =>
        {
            var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            var connection = dbContext.Database.GetDbConnection();
            return connection;
        };
    }
}
