using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Modules;

/// <summary>
/// Extension methods for registering module-scoped pipeline behaviors.
/// </summary>
public static class ModuleBehaviorServiceCollectionExtensions
{
    /// <summary>
    /// Adds a module-scoped pipeline behavior that only runs for handlers within the specified module.
    /// </summary>
    /// <typeparam name="TModule">The module type this behavior applies to.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TBehavior">
    /// The behavior type. Must implement <see cref="IModulePipelineBehavior{TModule, TRequest, TResponse}"/>.
    /// </typeparam>
    /// <param name="services">The service collection to add the behavior to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The behavior is wrapped in a <see cref="ModuleBehaviorAdapter{TModule, TRequest, TResponse}"/>
    /// that filters execution based on module ownership.
    /// </para>
    /// <para>
    /// This method should be called after AddEncinaModules to ensure the module is registered.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register module-scoped audit behavior for a specific request type
    /// services.AddEncinaModuleBehavior&lt;OrderModule, CreateOrderCommand, OrderId, CreateOrderAuditBehavior&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaModuleBehavior<TModule, TRequest, TResponse, TBehavior>(
        this IServiceCollection services)
        where TModule : class, IModule
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IModulePipelineBehavior<TModule, TRequest, TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the behavior implementation
        services.TryAddScoped<TBehavior>();

        // Register the module behavior interface
        services.TryAddScoped<IModulePipelineBehavior<TModule, TRequest, TResponse>>(
            sp => sp.GetRequiredService<TBehavior>());

        // Register the adapter as IPipelineBehavior
        services.AddScoped<IPipelineBehavior<TRequest, TResponse>>(sp =>
        {
            var innerBehavior = sp.GetRequiredService<IModulePipelineBehavior<TModule, TRequest, TResponse>>();
            var module = sp.GetRequiredService<TModule>();
            var handlerRegistry = sp.GetRequiredService<IModuleHandlerRegistry>();

            return new ModuleBehaviorAdapter<TModule, TRequest, TResponse>(
                innerBehavior,
                module,
                handlerRegistry);
        });

        return services;
    }

    /// <summary>
    /// Adds a module-scoped pipeline behavior with a specific lifetime.
    /// </summary>
    /// <typeparam name="TModule">The module type this behavior applies to.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <param name="services">The service collection to add the behavior to.</param>
    /// <param name="lifetime">The service lifetime for the behavior.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddEncinaModuleBehavior<TModule, TRequest, TResponse, TBehavior>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where TModule : class, IModule
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IModulePipelineBehavior<TModule, TRequest, TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the behavior with specified lifetime
        var descriptor = ServiceDescriptor.Describe(
            typeof(TBehavior),
            typeof(TBehavior),
            lifetime);
        services.TryAdd(descriptor);

        // Register the module behavior interface
        services.TryAdd(ServiceDescriptor.Describe(
            typeof(IModulePipelineBehavior<TModule, TRequest, TResponse>),
            sp => sp.GetRequiredService<TBehavior>(),
            lifetime));

        // Register the adapter as IPipelineBehavior with same lifetime
        services.Add(ServiceDescriptor.Describe(
            typeof(IPipelineBehavior<TRequest, TResponse>),
            sp =>
            {
                var innerBehavior = sp.GetRequiredService<IModulePipelineBehavior<TModule, TRequest, TResponse>>();
                var module = sp.GetRequiredService<TModule>();
                var handlerRegistry = sp.GetRequiredService<IModuleHandlerRegistry>();

                return new ModuleBehaviorAdapter<TModule, TRequest, TResponse>(
                    innerBehavior,
                    module,
                    handlerRegistry);
            },
            lifetime));

        return services;
    }
}
