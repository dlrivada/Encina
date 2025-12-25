using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Modules;

/// <summary>
/// Extension methods for registering modules in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ModuleServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina module support with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection to register modules into.</param>
    /// <param name="configure">The action to configure modules.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddEncinaModules(config =>
    /// {
    ///     config.AddModule&lt;OrderModule&gt;();
    ///     config.AddModule&lt;PaymentModule&gt;();
    ///     config.AddModule&lt;ShippingModule&gt;();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaModules(
        this IServiceCollection services,
        Action<ModuleConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new ModuleConfiguration();
        configure(configuration);

        return AddEncinaModulesCore(services, configuration);
    }

    /// <summary>
    /// Adds Encina module support with a preconfigured configuration.
    /// </summary>
    /// <param name="services">The service collection to register modules into.</param>
    /// <param name="configuration">The module configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddEncinaModules(
        this IServiceCollection services,
        ModuleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return AddEncinaModulesCore(services, configuration);
    }

    private static IServiceCollection AddEncinaModulesCore(
        IServiceCollection services,
        ModuleConfiguration configuration)
    {
        var modules = configuration.ModuleDescriptors.Select(d => d.Module).ToList();

        // Register the module registry as a singleton
        var registry = new ModuleRegistry(modules);
        services.TryAddSingleton<IModuleRegistry>(registry);

        // Register individual modules for direct injection
        foreach (var module in modules)
        {
            services.TryAddSingleton(module.GetType(), _ => module);
        }

        // Register module lifecycle hosted service if any modules implement IModuleLifecycle
        if (registry.GetLifecycleModules().Count > 0)
        {
            services.AddHostedService<ModuleLifecycleHostedService>();
        }

        // Let each module configure its services
        foreach (var module in modules)
        {
            module.ConfigureServices(services);
        }

        // Discover and register handlers from module assemblies
        if (configuration.EnableHandlerDiscovery)
        {
            var assemblies = configuration.ModuleDescriptors
                .Select(d => d.HandlerAssembly)
                .Distinct()
                .ToArray();

            if (assemblies.Length > 0)
            {
                services.AddEncina(assemblies);
            }
        }

        return services;
    }
}
