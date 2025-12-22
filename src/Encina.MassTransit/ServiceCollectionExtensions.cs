using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.MassTransit;

/// <summary>
/// Extension methods for configuring Encina with MassTransit integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina MassTransit integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMassTransit(this IServiceCollection services)
    {
        return services.AddEncinaMassTransit(_ => { });
    }

    /// <summary>
    /// Adds Encina MassTransit integration to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure MassTransit options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMassTransit(
        this IServiceCollection services,
        Action<EncinaMassTransitOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // Register the message publisher
        services.TryAddTransient<IMassTransitMessagePublisher, MassTransitMessagePublisher>();

        // Register generic consumer types for open generic registration
        services.TryAddTransient(typeof(MassTransitRequestConsumer<,>));
        services.TryAddTransient(typeof(MassTransitNotificationConsumer<>));

        return services;
    }
}
