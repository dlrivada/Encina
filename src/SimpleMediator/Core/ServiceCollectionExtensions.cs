using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator;

/// <summary>
/// Extensions for registering SimpleMediator in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Legacy alias for <see cref="AddSimpleMediator(IServiceCollection, Assembly[])"/>.
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddSimpleMediator(assemblies);

    /// <summary>
    /// Legacy alias for <see cref="AddSimpleMediator(IServiceCollection, Action{SimpleMediatorConfiguration}?, Assembly[])"/>.
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, Action<SimpleMediatorConfiguration>? configure, params Assembly[] assemblies)
        => services.AddSimpleMediator(configure, assemblies);

    /// <summary>
    /// Registers the mediator using the default configuration.
    /// </summary>
    /// <param name="services">Service container.</param>
    /// <param name="assemblies">Assemblies to scan for handlers and behaviors.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance to allow chaining.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, params Assembly[] assemblies)
        => AddSimpleMediator(services, configure: null, assemblies);

    /// <summary>
    /// Registers the mediator while allowing custom configuration.
    /// </summary>
    /// <param name="services">Service container.</param>
    /// <param name="configure">Optional action to adjust scanning and behaviors.</param>
    /// <param name="assemblies">Assemblies that contain contracts and handlers.</param>
    /// <returns>The <see cref="IServiceCollection"/> passed as input.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, Action<SimpleMediatorConfiguration>? configure, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuration = new SimpleMediatorConfiguration()
            .RegisterServicesFromAssemblies(assemblies);

        configure?.Invoke(configuration);

        if (configuration.Assemblies.Count == 0)
        {
            configuration.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        }

        var resolvedAssemblies = configuration.Assemblies.ToArray();

        services.TryAddScoped<IMediator, SimpleMediator>();
        services.TryAddSingleton<IMediatorMetrics, MediatorMetrics>();
        services.TryAddSingleton<IFunctionalFailureDetector>(NullFunctionalFailureDetector.Instance);

        foreach (var assembly in resolvedAssemblies.Distinct())
        {
            var registrations = MediatorAssemblyScanner.GetRegistrations(assembly);

            RegisterHandlers(services, registrations.HandlerRegistrations, configuration.HandlerLifetime);
            RegisterNotificationHandlers(services, registrations.NotificationRegistrations, configuration.HandlerLifetime);
            RegisterPipelineBehaviors(services, registrations.PipelineRegistrations);
            RegisterRequestPreProcessors(services, registrations.RequestPreProcessorRegistrations);
            RegisterRequestPostProcessors(services, registrations.RequestPostProcessorRegistrations);
        }

        configuration.RegisterConfiguredPipelineBehaviors(services);
        configuration.RegisterConfiguredRequestPreProcessors(services);
        configuration.RegisterConfiguredRequestPostProcessors(services);

        return services;
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
            var descriptor = ServiceDescriptor.Scoped(registration.ServiceType, registration.ImplementationType);
            services.TryAddEnumerable(descriptor);
        }
    }

    /// <summary>
    /// Registers pre-processors discovered during scanning.
    /// </summary>
    private static void RegisterRequestPreProcessors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            var descriptor = ServiceDescriptor.Scoped(registration.ServiceType, registration.ImplementationType);
            services.TryAddEnumerable(descriptor);
        }
    }

    /// <summary>
    /// Registers post-processors discovered during scanning.
    /// </summary>
    private static void RegisterRequestPostProcessors(IServiceCollection services, IEnumerable<TypeRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            var descriptor = ServiceDescriptor.Scoped(registration.ServiceType, registration.ImplementationType);
            services.TryAddEnumerable(descriptor);
        }
    }
}
