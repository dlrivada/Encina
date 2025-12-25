using System.Data;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Dapper.Oracle.Health;

/// <summary>
/// Health check for Oracle database connectivity.
/// </summary>
public sealed class OracleHealthCheck : DatabaseHealthCheck
{
    /// <summary>
    /// The default name for the Oracle health check.
    /// </summary>
    public const string DefaultName = "encina-oracle";

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve connections from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public OracleHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(DefaultName, CreateConnectionFactory(serviceProvider), options)
    {
    }

    /// <inheritdoc/>
    protected override string GetHealthCheckQuery() => "SELECT 1 FROM DUAL";

    private static Func<IDbConnection> CreateConnectionFactory(IServiceProvider serviceProvider)
    {
        return () =>
        {
            var scope = serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IDbConnection>();
        };
    }
}
