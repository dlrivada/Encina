using System.Data;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Dapper.PostgreSQL.Health;

/// <summary>
/// Health check for PostgreSQL database connectivity.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that the PostgreSQL database is accessible by:
/// <list type="number">
/// <item>Opening a connection</item>
/// <item>Executing a simple <c>SELECT 1</c> query</item>
/// </list>
/// </para>
/// <para>
/// The health check respects the configured timeout and failure status from
/// <see cref="ProviderHealthCheckOptions"/>.
/// </para>
/// </remarks>
public sealed class PostgreSqlHealthCheck : DatabaseHealthCheck
{
    /// <summary>
    /// The default name for the PostgreSQL health check.
    /// </summary>
    public const string DefaultName = "encina-postgresql";

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
            // Create a scope to properly resolve scoped services
            var scope = serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IDbConnection>();
        };
    }
}
