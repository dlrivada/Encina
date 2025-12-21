using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.NServiceBus;

/// <summary>
/// Extension methods for configuring SimpleMediator NServiceBus integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator NServiceBus integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorNServiceBus(
        this IServiceCollection services,
        Action<SimpleMediatorNServiceBusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorNServiceBusOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorNServiceBusOptions>(opt =>
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
