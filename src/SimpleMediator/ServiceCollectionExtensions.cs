using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator;

/// <summary>
/// Extensiones para registrar SimpleMediator en un <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Alias legacy para <see cref="AddSimpleMediator(IServiceCollection, Assembly[])"/>.
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, params Assembly[] assemblies)
        => services.AddSimpleMediator(assemblies);

    /// <summary>
    /// Alias legacy para <see cref="AddSimpleMediator(IServiceCollection, Action{SimpleMediatorConfiguration}?, Assembly[])"/>.
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, Action<SimpleMediatorConfiguration>? configure, params Assembly[] assemblies)
        => services.AddSimpleMediator(configure, assemblies);

    /// <summary>
    /// Registra el mediador usando la configuración por defecto.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <param name="assemblies">Ensamblados que se escanearán en busca de handlers y behaviors.</param>
    /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar registros.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, params Assembly[] assemblies)
        => AddSimpleMediator(services, configure: null, assemblies);

    /// <summary>
    /// Registra el mediador permitiendo personalizar la configuración.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <param name="configure">Acción opcional para ajustar el escaneo y behaviors.</param>
    /// <param name="assemblies">Ensamblados que contienen contratos y handlers.</param>
    /// <returns>La colección de servicios pasada como argumento.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, Action<SimpleMediatorConfiguration>? configure, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuration = new SimpleMediatorConfiguration()
            .RegisterServicesFromAssemblies(assemblies);

        configure?.Invoke(configuration);

        if (!configuration.Assemblies.Any())
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
    /// Registra los handlers encontrados en el escaneo respetando el ciclo de vida configurado.
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
    /// Registra los handlers de notificación encontrados.
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
    /// Registra behaviors genéricos aplicables al pipeline.
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
    /// Registra los pre-procesadores descubiertos durante el escaneo.
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
    /// Registra los post-procesadores descubiertos durante el escaneo.
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
