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
    public static IServiceCollection AddEncinaDeadLetterQueue<TStore, TFactory>(
        this IServiceCollection services,
        Action<DeadLetterOptions>? configure = null)
        where TStore : class, IDeadLetterStore
        where TFactory : class, IDeadLetterMessageFactory
    {
        var options = new DeadLetterOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register store and factory
        services.TryAddScoped<IDeadLetterStore, TStore>();
        services.TryAddScoped<IDeadLetterMessageFactory, TFactory>();

        // Register orchestrator and manager
        services.TryAddScoped<DeadLetterOrchestrator>();
        services.TryAddScoped<IDeadLetterManager, DeadLetterManager>();

        // Register health check
        services.TryAddScoped<DeadLetterHealthCheck>();
        services.TryAddScoped<IEncinaHealthCheck>(sp => sp.GetRequiredService<DeadLetterHealthCheck>());

        // Register cleanup processor if enabled
        if (options.EnableAutomaticCleanup && options.RetentionPeriod.HasValue)
        {
            services.AddHostedService<DeadLetterCleanupProcessor>();
        }

        return services;
    }

    /// <summary>
    /// Adds the Dead Letter Queue pattern with custom health check options.
    /// </summary>
    /// <typeparam name="TStore">The dead letter store implementation.</typeparam>
    /// <typeparam name="TFactory">The dead letter message factory implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for DLQ options.</param>
    /// <param name="healthCheckOptions">Health check options.</param>
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
            services.AddSingleton(healthCheckOptions);
        }

        return services;
    }
}
