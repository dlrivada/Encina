using Encina.Marten.Health;
using Encina.Marten.Projections;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Marten;

/// <summary>
/// Extension methods for configuring Encina with Marten integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Marten integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMarten(this IServiceCollection services)
    {
        return services.AddEncinaMarten(_ => { });
    }

    /// <summary>
    /// Adds Encina Marten integration to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure Marten options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMarten(
        this IServiceCollection services,
        Action<EncinaMartenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EncinaMartenOptions();
        configure.Invoke(options);

        services.Configure(configure);

        // Register the open generic aggregate repository
        services.TryAddScoped(typeof(IAggregateRepository<>), typeof(MartenAggregateRepository<>));

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, MartenHealthCheck>();
        }

        // Register projection infrastructure if enabled
        if (options.Projections.Enabled)
        {
            services.AddProjections(options.Projections);
        }

        return services;
    }

    /// <summary>
    /// Adds a specific aggregate repository to the service collection.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAggregateRepository<TAggregate>(this IServiceCollection services)
        where TAggregate : class, IAggregate
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IAggregateRepository<TAggregate>, MartenAggregateRepository<TAggregate>>();

        return services;
    }

    /// <summary>
    /// Adds projection infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The projection options.</param>
    /// <returns>The service collection for chaining.</returns>
    internal static IServiceCollection AddProjections(
        this IServiceCollection services,
        ProjectionOptions options)
    {
        // Register core projection services
        services.TryAddSingleton<ProjectionRegistry>();
        services.TryAddSingleton<IProjectionManager, MartenProjectionManager>();

        // Register read model repository (open generic)
        services.TryAddScoped(typeof(IReadModelRepository<>), typeof(MartenReadModelRepository<>));

        // Register inline projection dispatcher if enabled
        if (options.UseInlineProjections)
        {
            services.TryAddScoped<IInlineProjectionDispatcher, MartenInlineProjectionDispatcher>();
        }

        return services;
    }

    /// <summary>
    /// Registers a projection with the projection registry.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaMarten(options =>
    /// {
    ///     options.Projections.Enabled = true;
    /// });
    ///
    /// services.AddProjection&lt;OrderSummaryProjection, OrderSummary&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddProjection<TProjection, TReadModel>(this IServiceCollection services)
        where TProjection : class, IProjection<TReadModel>
        where TReadModel : class, IReadModel
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the projection type
        services.TryAddScoped<TProjection>();

        // Register the specific read model repository
        services.TryAddScoped<IReadModelRepository<TReadModel>, MartenReadModelRepository<TReadModel>>();

        // Add to registry - we need to do this at startup time
        services.AddSingleton<IProjectionRegistrar>(sp =>
            new ProjectionRegistrar<TProjection, TReadModel>());

        return services;
    }
}

/// <summary>
/// Interface for projection registration during startup.
/// </summary>
internal interface IProjectionRegistrar
{
    /// <summary>
    /// Registers the projection with the registry.
    /// </summary>
    /// <param name="registry">The projection registry.</param>
    void Register(ProjectionRegistry registry);
}

/// <summary>
/// Typed projection registrar.
/// </summary>
/// <typeparam name="TProjection">The projection type.</typeparam>
/// <typeparam name="TReadModel">The read model type.</typeparam>
internal sealed class ProjectionRegistrar<TProjection, TReadModel> : IProjectionRegistrar
    where TProjection : class, IProjection<TReadModel>
    where TReadModel : class, IReadModel
{
    /// <inheritdoc />
    public void Register(ProjectionRegistry registry)
    {
        registry.Register<TProjection, TReadModel>();
    }
}
