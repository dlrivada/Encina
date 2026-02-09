using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Cdc.MySql;

/// <summary>
/// Extension methods for configuring MySQL Binary Log Replication CDC services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MySQL Binary Log Replication CDC connector services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for MySQL CDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaCdcMySql(
        this IServiceCollection services,
        Action<MySqlCdcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MySqlCdcOptions();
        configure(options);

        services.AddSingleton(options);
        services.TryAddSingleton<ICdcConnector, MySqlCdcConnector>();
        services.TryAddSingleton<MySqlCdcHealthCheck>();

        return services;
    }
}
