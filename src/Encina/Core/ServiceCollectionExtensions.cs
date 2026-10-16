using System.Reflection;
using Encina.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina;

/// <summary>
/// Extensions for registering Encina in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Legacy alias for <see cref="AddEncina(IServiceCollection, Assembly[])"/>.
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddEncina(assemblies);

    /// <summary>
    /// Legacy alias for <see cref="AddEncina(IServiceCollection, Action{EncinaConfiguration}?, Assembly[])"/>.
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, Action<EncinaConfiguration>? configure, params Assembly[] assemblies)
        => services.AddEncina(configure, assemblies);

    /// <summary>
    /// Registers the Encina using the default configuration.
    /// </summary>
    /// <param name="services">Service container.</param>
    /// <param name="assemblies">Assemblies to scan for handlers and behaviors.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance to allow chaining.</returns>
    public static IServiceCollection AddEncina(this IServiceCollection services, params Assembly[] assemblies)
        => AddEncina(services, configure: null, assemblies);

    /// <summary>
    /// Registers the Encina while allowing custom configuration.
    /// </summary>
    /// <param name="services">Service container.</param>
    /// <param name="configure">Optional action to adjust scanning and behaviors.</param>
    /// <param name="assemblies">Assemblies that contain contracts and handlers.</param>
    /// <returns>The <see cref="IServiceCollection"/> passed as input.</returns>
    public static IServiceCollection AddEncina(this IServiceCollection services, Action<EncinaConfiguration>? configure, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuration = new EncinaConfiguration()
            .RegisterServicesFromAssemblies(assemblies);

        configure?.Invoke(configuration);

        if (configuration.Assemblies.Count == 0)
        {
            configuration.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        }

        var resolvedAssemblies = configuration.Assemblies.ToArray();

        services.TryAddScoped<IEncina, Encina>();
        services.AddEncinaRequestIdentity();
        services.TryAddSingleton<IEncinaMetrics, EncinaMetrics>();
        var stageRegistry = GetOrAddStageRegistry(services);
        services.TryAddSingleton(new PipelineRegistrationSnapshot(services));
        services.TryAddSingleton(static provider => new PipelineOrderResolver(
            provider.GetRequiredService<PipelineStageRegistry>(),
            provider.GetService<ILogger<PipelineOrderResolver>>() ?? NullLogger<PipelineOrderResolver>.Instance,
            provider.GetRequiredService<PipelineRegistrationSnapshot>()));
        services.TryAddScoped<IEncinaPipelineInspector, EncinaPipelineInspector>();
        services.TryAddSingleton<IFunctionalFailureDetector>(NullFunctionalFailureDetector.Instance);

        // Register null module handler registry for when modules are not used
        // (will be overwritten if AddEncinaModules is called)
        services.TryAddSingleton<IModuleHandlerRegistry>(NullModuleHandlerRegistry.Instance);

        // Register notification dispatch options
        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = configuration.NotificationDispatch.Strategy;
            options.MaxDegreeOfParallelism = configuration.NotificationDispatch.MaxDegreeOfParallelism;
        });

        foreach (var assembly in resolvedAssemblies.Distinct())
        {
            var registrations = EncinaAssemblyScanner.GetRegistrations(assembly);

            RegisterHandlers(services, registrations.HandlerRegistrations, configuration.HandlerLifetime);
            RegisterNotificationHandlers(services, registrations.NotificationRegistrations, configuration.HandlerLifetime);
            RegisterPipelineBehaviors(services, registrations.PipelineRegistrations);
            RegisterRequestPreProcessors(services, registrations.RequestPreProcessorRegistrations);
            RegisterRequestPostProcessors(services, registrations.RequestPostProcessorRegistrations);
            RegisterRequestErrorProcessors(services, registrations.RequestErrorProcessorRegistrations);
            RegisterRequestPostCommitProcessors(services, registrations.RequestPostCommitProcessorRegistrations);
            RegisterStreamHandlers(services, registrations.StreamHandlerRegistrations, configuration.HandlerLifetime);
            RegisterStreamPipelineBehaviors(services, registrations.StreamPipelineRegistrations);
        }

        configuration.RegisterConfiguredPipelineBehaviors(services);
        configuration.RegisterConfiguredRequestPreProcessors(services);
        configuration.RegisterConfiguredRequestPostProcessors(services);
        configuration.RegisterConfiguredRequestErrorProcessors(services);
        configuration.RegisterConfiguredRequestPostCommitProcessors(services);
        configuration.RegisterConfiguredStreamPipelineBehaviors(services);
        configuration.ApplyStageDeclarations(stageRegistry);

        return services;
    }

    /// <summary>
    /// Returns the single <see cref="PipelineStageRegistry"/> of the collection, adding it on the first call, so every
    /// <c>AddEncina</c> call records its stage declarations in the same registry the resolver reads.
    /// </summary>
    private static PipelineStageRegistry GetOrAddStageRegistry(IServiceCollection services)
    {
        var existing = services.FirstOrDefault(static descriptor => descriptor.ServiceType == typeof(PipelineStageRegistry));
        if (existing?.ImplementationInstance is PipelineStageRegistry registered)
        {
            return registered;
        }

        var created = new PipelineStageRegistry();
        services.TryAddSingleton(created);
        return created;
    }

    /// <summary>
    /// Registers discovered request handlers honoring the configured lifetime.
    /// </summary>
    private static void RegisterHandlers(IServiceCollection services, IEnumerable<TypeRegistration> registrations, ServiceLifetime lifetime)
    {
        foreach (var registration in registrations)
        {
            var descriptor = ServiceDescriptor.Describe(registration.ServiceType, registration.ImplementationType, lifetime);
            services.TryAdd(descriptor);
        }
    }

    /// <summary>
    /// Registers discovered notification handlers.
    /// </summary>
    private static void RegisterNotificationHandlers(IServiceCollection services, IEnumerable<TypeRegistration> registrations, ServiceLifetime lifetime)
    {
        foreach (var registration in registrations)
        {
            var descriptor = ServiceDescriptor.Describe(registration.ServiceType, registration.ImplementationType, lifetime);
            services.TryAddEnumerable(descriptor);
        }
    }

    /// <summary>
    /// Registers generic pipeline behaviors.
    /// </summary>
    private static void RegisterPipelineBehaviors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            TryAddEnumerableByImplementationType(services, registration.ServiceType, registration.ImplementationType);
        }
    }

    /// <summary>
    /// Registers pre-processors discovered during scanning.
    /// </summary>
    private static void RegisterRequestPreProcessors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            TryAddEnumerableByImplementationType(services, registration.ServiceType, registration.ImplementationType);
        }
    }

    /// <summary>
    /// Registers post-processors discovered during scanning.
    /// </summary>
    private static void RegisterRequestPostProcessors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            TryAddEnumerableByImplementationType(services, registration.ServiceType, registration.ImplementationType);
        }
    }

    /// <summary>
    /// Registers error processors discovered during scanning.
    /// </summary>
    private static void RegisterRequestErrorProcessors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            TryAddEnumerableByImplementationType(services, registration.ServiceType, registration.ImplementationType);
        }
    }

    /// <summary>
    /// Registers post-commit processors discovered during scanning.
    /// </summary>
    private static void RegisterRequestPostCommitProcessors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            TryAddEnumerableByImplementationType(services, registration.ServiceType, registration.ImplementationType);
        }
    }

    /// <summary>
    /// Registers discovered stream request handlers honoring the configured lifetime.
    /// </summary>
    private static void RegisterStreamHandlers(IServiceCollection services, IEnumerable<TypeRegistration> registrations, ServiceLifetime lifetime)
    {
        foreach (var registration in registrations)
        {
            var descriptor = ServiceDescriptor.Describe(registration.ServiceType, registration.ImplementationType, lifetime);
            services.TryAdd(descriptor);
        }
    }

    /// <summary>
    /// Registers stream pipeline behaviors discovered during scanning.
    /// </summary>
    private static void RegisterStreamPipelineBehaviors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            TryAddEnumerableByImplementationType(services, registration.ServiceType, registration.ImplementationType);
        }
    }

    /// <summary>
    /// Adds a service to the enumerable collection only if no existing registration
    /// has the same implementation type. This prevents duplicate behaviors when
    /// the same type is registered both via assembly scanning and manually.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ServiceCollectionDescriptorExtensions"/>.TryAddEnumerable,
    /// this method checks by implementation type regardless of how the service was registered
    /// (direct type, factory, or instance).
    /// </remarks>
    private static void TryAddEnumerableByImplementationType(IServiceCollection services, Type serviceType, Type implementationType)
    {
        // Check if any existing registration for this service type uses the same implementation type
        var alreadyRegistered = services.Any(descriptor =>
            descriptor.ServiceType == serviceType &&
            GetImplementationType(descriptor) == implementationType);

        if (!alreadyRegistered)
        {
            services.AddScoped(serviceType, implementationType);
        }
    }

    /// <summary>
    /// Extracts the implementation type from a service descriptor,
    /// handling both direct type registrations and factory-based registrations.
    /// </summary>
    private static Type? GetImplementationType(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType is not null)
        {
            return descriptor.ImplementationType;
        }

        // For factory-based registrations, try to infer from the instance if available
        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance.GetType();
        }

        // For factory delegates, we cannot determine the implementation type at registration time
        return null;
    }
}
