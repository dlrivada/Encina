using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.NServiceBus;

/// <summary>
/// Extension methods for configuring Encina NServiceBus integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina NServiceBus integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaNServiceBus(
        this IServiceCollection services,
        Action<EncinaNServiceBusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaNServiceBusOptions();
        configure?.Invoke(options);

        services.Configure<EncinaNServiceBusOptions>(opt =>
        {
            opt.EndpointName = options.EndpointName;
            opt.UseOutbox = options.UseOutbox;
            opt.IncludeExceptionDetails = options.IncludeExceptionDetails;
            opt.DefaultTimeout = options.DefaultTimeout;
            opt.ImmediateRetries = options.ImmediateRetries;
            opt.DelayedRetries = options.DelayedRetries;
        });

        services.TryAddScoped<INServiceBusMessagePublisher, NServiceBusMessagePublisher>();

        return services;
    }
}
