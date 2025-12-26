using Encina.Marten.Health;
using Encina.Marten.Projections;
using Encina.Marten.Snapshots;
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

        // Register snapshot infrastructure if enabled
        if (options.Snapshots.Enabled)
        {
            services.AddSnapshots(options.Snapshots);
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

    /// <summary>
    /// Adds snapshot infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The snapshot options.</param>
    /// <returns>The service collection for chaining.</returns>
    internal static IServiceCollection AddSnapshots(
        this IServiceCollection services,
        SnapshotOptions options)
    {
        // Register open generic snapshot store
        services.TryAddScoped(typeof(ISnapshotStore<>), typeof(MartenSnapshotStore<>));

        return services;
    }

    /// <summary>
    /// Registers a snapshotable aggregate with snapshot-aware repository.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type (must implement <see cref="ISnapshotable{TAggregate}"/>).</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Call this method for each aggregate that should use snapshot-optimized loading.
    /// The aggregate must implement <see cref="ISnapshotable{TAggregate}"/> and have
    /// a parameterless constructor.
    /// </para>
    /// <para>
    /// When registered, the aggregate repository will:
    /// <list type="bullet">
    /// <item>Load from the most recent snapshot when available</item>
    /// <item>Replay only events after the snapshot version</item>
    /// <item>Automatically create snapshots based on configuration</item>
    /// <item>Prune old snapshots to prevent storage bloat</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Enable snapshots globally
    /// services.AddEncinaMarten(options =>
    /// {
    ///     options.Snapshots.Enabled = true;
    ///     options.Snapshots.SnapshotEvery = 100;
    /// });
    ///
    /// // Register specific aggregate for snapshot-aware loading
    /// services.AddSnapshotableAggregate&lt;Order&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddSnapshotableAggregate<TAggregate>(this IServiceCollection services)
        where TAggregate : class, IAggregate, ISnapshotable<TAggregate>, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register snapshot store for this aggregate type
        services.TryAddScoped<ISnapshotStore<TAggregate>, MartenSnapshotStore<TAggregate>>();

        // Override the default repository with snapshot-aware one
        services.AddScoped<IAggregateRepository<TAggregate>, SnapshotAwareAggregateRepository<TAggregate>>();

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
