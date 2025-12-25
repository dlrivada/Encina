using System.Data;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.ADO.Sqlite.Health;

/// <summary>
/// Health check for SQLite database connectivity (ADO.NET).
/// </summary>
public sealed class SqliteHealthCheck : DatabaseHealthCheck
{
    /// <summary>
    /// The default name for the SQLite health check.
    /// </summary>
    public const string DefaultName = "encina-ado-sqlite";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public SqliteHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(DefaultName, CreateConnectionFactory(serviceProvider), options)
    {
    }

    private static Func<IDbConnection> CreateConnectionFactory(IServiceProvider serviceProvider)
    {
        return () =>
        {
            var scope = serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IDbConnection>();
        };
    }
}
