using Encina.Recoverability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
    /// <item><description><see cref="RecoverabilityOptions"/> through the options pattern (<c>Configure</c>; read it with <c>IOptions&lt;RecoverabilityOptions&gt;</c>), so an application <c>Configure</c> made before this call composes into one value</description></item>
    /// <item><description><see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> as scoped pipeline behavior</description></item>
    /// <item><description><see cref="DefaultErrorClassifier"/> as <see cref="IErrorClassifier"/> if not already registered</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Recoverability can be registered once per host (ADR-050): a second call, a
    /// <see cref="MessagingConfiguration.UseRecoverability"/> registration next to this one, or a
    /// <see cref="RecoverabilityOptions"/> instance registered directly throws an
    /// <see cref="InvalidOperationException"/> that names the duplicate and the fix. A duplicate added directly to the
    /// service collection after this call fails the host start instead. Only the open generic registration of
    /// <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> is checked.
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
    /// <para>
    /// Injecting <c>IErrorClassifier</c> into a configuration action for <see cref="RecoverabilityOptions"/> is not
    /// supported, because the registered classifier is itself decided from these options.
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

        // Registering Recoverability a second time is an error (ADR-050): fail before adding anything.
        RecoverabilityRegistrationCheck.ThrowIfAlreadyRegistered(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<RecoverabilityOptions>();
        }

        RegisterErrorClassifier(services);

        // Register the pipeline behavior
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));

        RegisterDelayedRetries(services);

        return services;
    }

    /// <summary>
    /// Registers the delayed retry startup check, the scheduler and the processor, whatever the flag says.
    /// Shared by <c>AddEncinaRecoverability</c> and <c>UseRecoverability</c> so the two paths never drift.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The options are only known when the container resolves them, so each piece decides then: with
    /// <see cref="RecoverabilityOptions.EnableDelayedRetries"/> off the scheduler factory yields no scheduler, the
    /// processor does no work and the startup check does nothing.
    /// </para>
    /// <para>
    /// The store and its factory may be registered before or after this call: with the flag on, the scheduler is
    /// built by a factory that fails with a clear message when either is missing, and the startup check
    /// (<see cref="DelayedRetryStartupCheck"/>) fails the host start for the same reason.
    /// </para>
    /// </remarks>
    internal static void RegisterDelayedRetries(IServiceCollection services)
    {
        // The checks go before the processor so a failed start keeps the processor from running. The duplicate
        // check always runs; it sees registrations added after this call because it holds the live collection.
        services.TryAddSingleton(new RecoverabilityDescriptorSnapshot(services));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RecoverabilityRegistrationCheck>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DelayedRetryStartupCheck>());

        // DelayedRetryScheduler and DelayedRetryProcessor serialize the request payload
        // through IMessageSerializer (so encryption applies to delayed-retry content).
        services.TryAddDefaultMessageSerializer();

        // The factory yields null (no scheduler) while delayed retries are off: the behavior takes the scheduler
        // as an optional constructor parameter and the container passes the null through; nothing requires it then.
        services.TryAddScoped<IDelayedRetryScheduler>(provider => CreateScheduler(provider)!);
        services.AddHostedService<DelayedRetryProcessor>();
    }

    private static DelayedRetryScheduler? CreateScheduler(IServiceProvider provider)
    {
        if (!provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value.EnableDelayedRetries)
        {
            return null;
        }

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
    /// Registers the error classifier, decided when the container resolves it: a custom
    /// <see cref="RecoverabilityOptions.ErrorClassifier"/> replaces any registered one (the application's own
    /// choice wins over a default registered earlier, for example by <c>AddEncinaPolly</c>); without one the
    /// classifier registered earlier is kept, or <see cref="DefaultErrorClassifier"/> when there is none. A
    /// registration made later by <c>TryAdd</c> finds this one and does nothing, so registration order never
    /// matters.
    /// </summary>
    internal static void RegisterErrorClassifier(IServiceCollection services)
    {
        var previous = services.LastOrDefault(IsClassifierDescriptor);
        var lifetime = ServiceLifetime.Singleton;
        if (previous is not null)
        {
            lifetime = previous.Lifetime;
            services.Remove(previous);
        }

        services.Add(new ServiceDescriptor(typeof(IErrorClassifier), new OptionsClassifierFactory(previous).Create, lifetime));
    }

    private static bool IsClassifierDescriptor(ServiceDescriptor descriptor)
        => descriptor.ServiceType == typeof(IErrorClassifier) && !descriptor.IsKeyedService;

    // Resolves the classifier: the one the options carry, else the registration that was in place before,
    // else the default. A scoped or transient earlier registration keeps its lifetime.
    private sealed class OptionsClassifierFactory(ServiceDescriptor? previous)
    {
        // Guards only against recursion: a configure action that injects IErrorClassifier re-enters this factory,
        // and the re-entrant call answers from the earlier registration. That pattern is not supported.
        private readonly AsyncLocal<bool> _creating = new();

        public IErrorClassifier Create(IServiceProvider provider)
        {
            if (_creating.Value)
            {
                return FromPrevious(provider);
            }

            _creating.Value = true;
            try
            {
                var fromOptions = provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value.ErrorClassifier;
                return fromOptions is null ? FromPrevious(provider) : External(fromOptions);
            }
            finally
            {
                _creating.Value = false;
            }
        }

        // Instances the options or an earlier registration own must not be disposed by the container.
        private static IErrorClassifier External(IErrorClassifier instance)
            => instance is IDisposable or IAsyncDisposable ? new NonDisposingClassifier(instance) : instance;

        // crap-exempt: single-question switch — which earlier registration supplies the classifier
        private IErrorClassifier FromPrevious(IServiceProvider provider) => previous switch
        {
            null => new DefaultErrorClassifier(),
            { ImplementationInstance: IErrorClassifier instance } => External(instance),
            { ImplementationFactory: { } factory } => (IErrorClassifier)factory(provider),
            _ => (IErrorClassifier)ActivatorUtilities.CreateInstance(provider, previous.ImplementationType!)
        };
    }

    // Forwards to an instance somebody else owns, without being disposable itself so the container leaves it alone.
    private sealed class NonDisposingClassifier(IErrorClassifier inner) : IErrorClassifier
    {
        public ErrorClassification Classify(EncinaError encinaError, Exception? exception)
            => inner.Classify(encinaError, exception);
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
        ArgumentNullException.ThrowIfNull(services);

        // Fail before the classifier is added, so a rejected call leaves the collection unchanged.
        RecoverabilityRegistrationCheck.ThrowIfAlreadyRegistered(services);

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
