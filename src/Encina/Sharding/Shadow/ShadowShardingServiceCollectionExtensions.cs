using Encina.Sharding.Shadow.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Shadow;

/// <summary>
/// Extension methods for registering shadow sharding services in the dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// This class is used internally by the sharding registration pipeline. When
/// <see cref="Configuration.ShardingOptions{TEntity}.UseShadowSharding"/> is <c>true</c>,
/// <see cref="ShardingServiceCollectionExtensions.AddEncinaSharding{TEntity}"/> calls
/// <see cref="AddShadowSharding"/> to decorate the existing <see cref="IShardRouter"/>
/// with a <see cref="ShadowShardRouterDecorator"/> and register pipeline behaviors.
/// </para>
/// </remarks>
internal static class ShadowShardingServiceCollectionExtensions
{
    /// <summary>
    /// Registers shadow sharding services: decorates the existing <see cref="IShardRouter"/>
    /// with shadow routing capabilities and registers shadow pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The shadow sharding options.</param>
    internal static void AddShadowSharding(
        IServiceCollection services,
        ShadowShardingOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ShadowTopology is null)
        {
            throw new InvalidOperationException(
                "ShadowShardingOptions.ShadowTopology must be configured before enabling shadow sharding. " +
                "Set the ShadowTopology property in the WithShadowSharding configuration action.");
        }

        // Register shadow options as singleton
        services.AddSingleton(options);

        // Decorate IShardRouter with ShadowShardRouterDecorator
        DecorateShadowShardRouter(services, options);

        // Register pipeline behaviors as open generics
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(ICommandPipelineBehavior<,>),
            typeof(ShadowWritePipelineBehavior<,>)));

        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IQueryPipelineBehavior<,>),
            typeof(ShadowReadPipelineBehavior<,>)));
    }

    /// <summary>
    /// Decorates the existing <see cref="IShardRouter"/> registration with a
    /// <see cref="ShadowShardRouterDecorator"/> and registers <see cref="IShadowShardRouter"/>
    /// pointing to the decorated instance.
    /// </summary>
    private static void DecorateShadowShardRouter(
        IServiceCollection services,
        ShadowShardingOptions options)
    {
        var existingDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IShardRouter));

        if (existingDescriptor is null)
        {
            return;
        }

        services.Remove(existingDescriptor);

        // Build the shadow router from the shadow topology using the configured factory
        // or default to hash-based routing (ShadowTopology validated non-null above)
        var shadowTopology = options.ShadowTopology!;
        var shadowRouter = options.ShadowRouterFactory is not null
            ? options.ShadowRouterFactory(shadowTopology)
            : new Routing.HashShardRouter(shadowTopology, new Routing.HashShardRouterOptions());

        services.AddSingleton<IShardRouter>(sp =>
        {
            var inner = ResolveFromDescriptor<IShardRouter>(sp, existingDescriptor);
            var logger = sp.GetRequiredService<ILogger<ShadowShardRouterDecorator>>();

            return new ShadowShardRouterDecorator(inner, shadowRouter, options, logger);
        });

        // Register IShadowShardRouter pointing to the same decorated instance
        services.AddSingleton<IShadowShardRouter>(sp =>
            (IShadowShardRouter)sp.GetRequiredService<IShardRouter>());
    }

    /// <summary>
    /// Resolves a service instance from an existing <see cref="ServiceDescriptor"/>.
    /// </summary>
    private static T ResolveFromDescriptor<T>(IServiceProvider sp, ServiceDescriptor descriptor)
        where T : class
    {
        if (descriptor.ImplementationInstance is T instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (T)descriptor.ImplementationFactory(sp);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (T)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
        }

        throw new InvalidOperationException(
            $"Cannot resolve {typeof(T).Name} from existing service descriptor. " +
            "Ensure IShardRouter is registered before enabling shadow sharding.");
    }
}
