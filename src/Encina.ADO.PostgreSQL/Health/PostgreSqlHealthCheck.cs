using System.Data;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.ADO.PostgreSQL.Health;

/// <summary>
/// Health check for PostgreSQL database connectivity (ADO.NET).
/// </summary>
public sealed class PostgreSqlHealthCheck : DatabaseHealthCheck
{
    /// <summary>
    /// The default name for the PostgreSQL health check.
    /// </summary>
    public const string DefaultName = "encina-ado-postgresql";

    private static readonly string[] ProviderTags = ["postgresql"];

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public PostgreSqlHealthCheck(
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
