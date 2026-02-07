using System.Data;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.ADO.MySQL.Health;

/// <summary>
/// Health check for MySQL database connectivity (ADO.NET).
/// </summary>
public sealed class MySqlHealthCheck : DatabaseHealthCheck
{
    /// <summary>
    /// The default name for the MySQL health check.
    /// </summary>
    public const string DefaultName = "encina-ado-mysql";

    private static readonly string[] ProviderTags = ["mysql"];

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public MySqlHealthCheck(
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
