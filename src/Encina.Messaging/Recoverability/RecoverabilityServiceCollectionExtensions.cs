using Encina.Recoverability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
    /// Delayed retries are off by default. When <see cref="RecoverabilityOptions.EnableDelayedRetries"/> is on, you
    /// must also register (before or after this call, with <see cref="AddEncinaDelayedRetryStore{TStore, TFactory}"/>
    /// or through a database provider package), otherwise the application fails at startup:
    /// <list type="bullet">
    /// <item><description><see cref="IDelayedRetryStore"/> - Provider-specific implementation</description></item>
    /// <item><description><see cref="IDelayedRetryMessageFactory"/> - Provider-specific factory</description></item>
    /// <item><description><see cref="IEncina"/> - Registered with <c>AddEncina</c>; the processor re-dispatches through it</description></item>
    /// </list>
    /// An empty <see cref="RecoverabilityOptions.DelayedRetries"/> array with the flag on still requires the store.
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

        RegisterErrorClassifier(services, options);

        // Register the pipeline behavior
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));

        RegisterDelayedRetries(services, options);

        return services;
    }

    /// <summary>
    /// Registers the delayed retry startup check always and, when
    /// <see cref="RecoverabilityOptions.EnableDelayedRetries"/> is on, the scheduler and the processor.
    /// Shared by <c>AddEncinaRecoverability</c> and <c>UseRecoverability</c> so the two paths never drift.
    /// </summary>
    /// <remarks>
    /// The store and its factory may be registered before or after this call: the scheduler is built by a
    /// factory that fails with a clear message when either is missing, and the startup check
    /// (<see cref="DelayedRetryStartupCheck"/>) fails the host start for the same reason.
    /// </remarks>
    internal static void RegisterDelayedRetries(IServiceCollection services, RecoverabilityOptions options)
    {
        // The check goes before the processor so a failed start keeps the processor from running.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DelayedRetryStartupCheck>());

        if (!options.EnableDelayedRetries)
        {
            return;
        }

        // DelayedRetryScheduler and DelayedRetryProcessor serialize the request payload
        // through IMessageSerializer (so encryption applies to delayed-retry content).
        services.TryAddDefaultMessageSerializer();
        services.TryAddScoped<IDelayedRetryScheduler>(CreateScheduler);
        services.AddHostedService<DelayedRetryProcessor>();
    }

    private static DelayedRetryScheduler CreateScheduler(IServiceProvider provider)
    {
        var missing = DelayedRetryStartupCheck.DescribeMissing(
            provider.GetService<IDelayedRetryStore>() is not null,
            provider.GetService<IDelayedRetryMessageFactory>() is not null,
            hasEncina: true);
        if (missing is not null)
        {
            throw new InvalidOperationException(DelayedRetryStartupCheck.BuildMessage(missing));
        }

        return ActivatorUtilities.CreateInstance<DelayedRetryScheduler>(provider);
    }

    /// <summary>
    /// Registers the error classifier: a custom <see cref="RecoverabilityOptions.ErrorClassifier"/> replaces any
    /// registered one (the application's own choice wins over a default registered earlier, for example by
    /// <c>AddEncinaPolly</c>); without one the default is added only when none is registered, so registration
    /// order never matters.
    /// </summary>
    internal static void RegisterErrorClassifier(IServiceCollection services, RecoverabilityOptions options)
    {
        if (options.ErrorClassifier is { } custom)
        {
            services.Replace(ServiceDescriptor.Singleton(custom));
            return;
        }

        services.TryAddSingleton<IErrorClassifier>(new DefaultErrorClassifier());
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
