using Encina.Cdc.Abstractions;
using Encina.Cdc.PostgreSql.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Cdc.PostgreSql;

/// <summary>
/// Extension methods for configuring PostgreSQL Logical Replication CDC services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PostgreSQL Logical Replication CDC connector services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for PostgreSQL CDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaCdcPostgreSql(
        this IServiceCollection services,
        Action<PostgresCdcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton(TimeProvider.System);

        var options = new PostgresCdcOptions();
        configure(options);

        services.AddSingleton(options);
        services.TryAddSingleton<ICdcConnector, PostgresCdcConnector>();
        services.TryAddSingleton<PostgresCdcHealthCheck>();

        return services;
    }
}
