using System.Data;
using Encina.Database;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Dapper.Sqlite.Health;

/// <summary>
/// Database health monitor for SQLite using Dapper.
/// </summary>
/// <remarks>
/// <para>
/// SQLite in-memory databases do not use traditional connection pooling.
/// Pool statistics are not applicable and <see cref="ConnectionPoolStats.CreateEmpty()"/>
/// is always returned.
/// </para>
/// <para>
/// The <see cref="IDatabaseHealthMonitor.ClearPoolAsync"/> method is a no-op for SQLite since there is no
/// pool to clear.
/// </para>
/// </remarks>
public sealed class DapperSqliteDatabaseHealthMonitor : DatabaseHealthMonitorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DapperSqliteDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public DapperSqliteDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
        : base("dapper-sqlite", CreateConnectionFactory(serviceProvider), options)
    {
    }

    /// <inheritdoc />
    /// <returns>
    /// Always returns <see cref="ConnectionPoolStats.CreateEmpty()"/> since SQLite
    /// does not use traditional connection pooling.
    /// </returns>
    protected override ConnectionPoolStats GetPoolStatisticsCore()
    {
        return ConnectionPoolStats.CreateEmpty();
    }

    /// <inheritdoc />
    /// <remarks>
    /// No-op for SQLite. In-memory databases do not have a pool to clear.
    /// File-based SQLite databases use Microsoft.Data.Sqlite's internal pooling
    /// which does not expose a public clear API.
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
            return scope.ServiceProvider.GetRequiredService<IDbConnection>();
        };
    }
}
