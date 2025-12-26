using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Extension methods for registering Recoverability Pipeline services.
/// </summary>
public static class RecoverabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Recoverability Pipeline to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description><see cref="RecoverabilityOptions"/> as singleton</description></item>
    /// <item><description><see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> as scoped pipeline behavior</description></item>
    /// <item><description><see cref="DefaultErrorClassifier"/> as <see cref="IErrorClassifier"/> if not already registered</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// To enable delayed retries, you must also register:
    /// <list type="bullet">
    /// <item><description><see cref="IDelayedRetryStore"/> - Provider-specific implementation</description></item>
    /// <item><description><see cref="IDelayedRetryMessageFactory"/> - Provider-specific factory</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRecoverability(options =>
    /// {
    ///     options.ImmediateRetries = 3;
    ///     options.DelayedRetries = new[]
    ///     {
    ///         TimeSpan.FromSeconds(30),
    ///         TimeSpan.FromMinutes(5),
    ///         TimeSpan.FromMinutes(30)
    ///     };
    ///     options.OnPermanentFailure = async (message, ct) =>
    ///     {
    ///         // Log to DLQ, send alert, etc.
    ///         await _dlqService.AddAsync(message, ct);
    ///     };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRecoverability(
        this IServiceCollection services,
        Action<RecoverabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RecoverabilityOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register default error classifier if not already registered
        services.TryAddSingleton<IErrorClassifier>(options.ErrorClassifier ?? new DefaultErrorClassifier());

        // Register the pipeline behavior
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));

        // Register delayed retry infrastructure if delayed retries are enabled
        if (options.EnableDelayedRetries)
        {
            services.TryAddScoped<IDelayedRetryScheduler, DelayedRetryScheduler>();
            services.AddHostedService<DelayedRetryProcessor>();
        }

        return services;
    }

    /// <summary>
    /// Adds the Recoverability Pipeline with a custom error classifier.
    /// </summary>
    /// <typeparam name="TClassifier">The custom error classifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaRecoverability<TClassifier>(
        this IServiceCollection services,
        Action<RecoverabilityOptions>? configure = null)
        where TClassifier : class, IErrorClassifier
    {
        services.AddSingleton<IErrorClassifier, TClassifier>();
        return services.AddEncinaRecoverability(configure);
    }

    /// <summary>
    /// Adds delayed retry store implementation.
    /// </summary>
    /// <typeparam name="TStore">The store implementation type.</typeparam>
    /// <typeparam name="TFactory">The factory implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Call this method to register provider-specific implementations for delayed retries.
    /// </remarks>
    public static IServiceCollection AddEncinaDelayedRetryStore<TStore, TFactory>(
        this IServiceCollection services)
        where TStore : class, IDelayedRetryStore
        where TFactory : class, IDelayedRetryMessageFactory
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDelayedRetryStore, TStore>();
        services.AddScoped<IDelayedRetryMessageFactory, TFactory>();

        return services;
    }
}
