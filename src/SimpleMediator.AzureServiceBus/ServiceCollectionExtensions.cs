using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.AzureServiceBus;

/// <summary>
/// Extension methods for configuring SimpleMediator Azure Service Bus integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator Azure Service Bus integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorAzureServiceBus(
        this IServiceCollection services,
        Action<SimpleMediatorAzureServiceBusOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SimpleMediatorAzureServiceBusOptions();
        configure.Invoke(options);

        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new ArgumentException("ConnectionString is required.", nameof(configure));
        }

        services.Configure<SimpleMediatorAzureServiceBusOptions>(opt =>
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

        services.TryAddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient(options.ConnectionString));

        services.TryAddScoped<IAzureServiceBusMessagePublisher, AzureServiceBusMessagePublisher>();

        return services;
    }
}
