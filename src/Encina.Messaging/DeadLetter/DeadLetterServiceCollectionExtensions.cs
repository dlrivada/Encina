using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Extension methods for registering Dead Letter Queue services.
/// </summary>
public static class DeadLetterServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Dead Letter Queue pattern to the service collection.
    /// </summary>
    /// <typeparam name="TStore">The dead letter store implementation.</typeparam>
    /// <typeparam name="TFactory">The dead letter message factory implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// A thin wrapper over <see cref="MessagingServiceCollectionExtensions.AddDeadLetterQueueServices{TStore, TFactory}"/>,
    /// the registration body every provider shares. A store registered earlier is kept (<c>TryAdd</c>).
    /// </remarks>
    public static IServiceCollection AddEncinaDeadLetterQueue<TStore, TFactory>(
        this IServiceCollection services,
        Action<DeadLetterOptions>? configure = null)
        where TStore : class, IDeadLetterStore
        where TFactory : class, IDeadLetterMessageFactory
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DeadLetterOptions();
        configure?.Invoke(options);

        return services.AddDeadLetterQueueServices<TStore, TFactory>(useDeadLetterQueue: true, options);
    }

    /// <summary>
    /// Adds the Dead Letter Queue pattern with custom health check options.
    /// </summary>
    /// <typeparam name="TStore">The dead letter store implementation.</typeparam>
    /// <typeparam name="TFactory">The dead letter message factory implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for DLQ options.</param>
    /// <param name="healthCheckOptions">Health check options; they replace the default registration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDeadLetterQueue<TStore, TFactory>(
        this IServiceCollection services,
        Action<DeadLetterOptions>? configure,
        DeadLetterHealthCheckOptions? healthCheckOptions)
        where TStore : class, IDeadLetterStore
        where TFactory : class, IDeadLetterMessageFactory
    {
        services.AddEncinaDeadLetterQueue<TStore, TFactory>(configure);

        if (healthCheckOptions is not null)
        {
            services.Replace(ServiceDescriptor.Singleton(healthCheckOptions));
        }

        return services;
    }
}
