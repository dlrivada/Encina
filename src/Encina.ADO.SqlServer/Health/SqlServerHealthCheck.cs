using System.Data;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.ADO.SqlServer.Health;

/// <summary>
/// Health check for SQL Server database connectivity (ADO.NET).
/// </summary>
public sealed class SqlServerHealthCheck : DatabaseHealthCheck
{
    /// <summary>
    /// The default name for the SQL Server health check.
    /// </summary>
    public const string DefaultName = "encina-ado-sqlserver";

    private static readonly string[] ProviderTags = ["sqlserver"];

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public SqlServerHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(DefaultName, CreateConnectionFactory(serviceProvider), options, ProviderTags)
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
