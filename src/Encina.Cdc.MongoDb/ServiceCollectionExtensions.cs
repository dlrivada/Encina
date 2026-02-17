using Encina.Cdc.Abstractions;
using Encina.Cdc.MongoDb.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Cdc.MongoDb;

/// <summary>
/// Extension methods for configuring MongoDB Change Streams CDC services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MongoDB Change Streams CDC connector services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for MongoDB CDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaCdcMongoDb(
        this IServiceCollection services,
        Action<MongoCdcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton(TimeProvider.System);

        var options = new MongoCdcOptions();
        configure(options);

        services.AddSingleton(options);
        services.TryAddSingleton<ICdcConnector, MongoCdcConnector>();
        services.TryAddSingleton<MongoCdcHealthCheck>();

        return services;
    }
}
