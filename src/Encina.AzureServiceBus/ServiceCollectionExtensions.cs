using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.AzureServiceBus;

/// <summary>
/// Extension methods for configuring Encina Azure Service Bus integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Azure Service Bus integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaAzureServiceBus(
        this IServiceCollection services,
        Action<EncinaAzureServiceBusOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EncinaAzureServiceBusOptions();
        configure.Invoke(options);

        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new ArgumentException("ConnectionString is required.", nameof(configure));
        }

        services.Configure<EncinaAzureServiceBusOptions>(opt =>
        {
            opt.ConnectionString = options.ConnectionString;
            opt.DefaultQueueName = options.DefaultQueueName;
            opt.DefaultTopicName = options.DefaultTopicName;
            opt.SubscriptionName = options.SubscriptionName;
            opt.UseSessions = options.UseSessions;
            opt.MaxConcurrentCalls = options.MaxConcurrentCalls;
            opt.PrefetchCount = options.PrefetchCount;
            opt.MaxAutoLockRenewalDuration = options.MaxAutoLockRenewalDuration;
        });

        services.TryAddSingleton(sp =>
            new ServiceBusClient(options.ConnectionString));

        services.TryAddScoped<IAzureServiceBusMessagePublisher, AzureServiceBusMessagePublisher>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, AzureServiceBusHealthCheck>();
        }

        return services;
    }
}
