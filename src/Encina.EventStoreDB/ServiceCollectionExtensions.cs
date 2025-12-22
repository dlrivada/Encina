using System.Reflection;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.EventStoreDB;

/// <summary>
/// Extension methods for configuring Encina with EventStoreDB integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina EventStoreDB integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The EventStoreDB connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaEventStoreDb(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddEncinaEventStoreDb(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Adds Encina EventStoreDB integration to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure EventStoreDB options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaEventStoreDb(
        this IServiceCollection services,
        Action<EventStoreDbOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EventStoreDbOptions();
        configure(options);

        services.Configure(configure);

        // Register EventStoreClient
        services.TryAddSingleton(_ =>
        {
            var settings = EventStoreClientSettings.Create(options.ConnectionString);
            return new EventStoreClient(settings);
        });

        // Register default event type resolver
        services.TryAddSingleton<IEventTypeResolver, DefaultEventTypeResolver>();

        // Register JSON event serializer
        services.TryAddSingleton<IEventSerializer, JsonEventSerializer>();

        // Register the open generic aggregate repository
        services.TryAddScoped(typeof(IAggregateRepository<>), typeof(EventStoreDbAggregateRepository<>));

        return services;
    }

    /// <summary>
    /// Adds Encina EventStoreDB integration with custom event assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure EventStoreDB options.</param>
    /// <param name="eventAssemblies">Assemblies containing event types.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaEventStoreDb(
        this IServiceCollection services,
        Action<EventStoreDbOptions> configure,
        params Assembly[] eventAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EventStoreDbOptions();
        configure(options);

        services.Configure(configure);

        // Register EventStoreClient
        services.TryAddSingleton(_ =>
        {
            var settings = EventStoreClientSettings.Create(options.ConnectionString);
            return new EventStoreClient(settings);
        });

        // Register event type resolver with assemblies
        services.TryAddSingleton<IEventTypeResolver>(_ =>
            new DefaultEventTypeResolver(eventAssemblies));

        // Register JSON event serializer
        services.TryAddSingleton<IEventSerializer, JsonEventSerializer>();

        // Register the open generic aggregate repository
        services.TryAddScoped(typeof(IAggregateRepository<>), typeof(EventStoreDbAggregateRepository<>));

        return services;
    }

    /// <summary>
    /// Adds a specific aggregate repository to the service collection.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAggregateRepository<TAggregate>(this IServiceCollection services)
        where TAggregate : AggregateBase, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IAggregateRepository<TAggregate>, EventStoreDbAggregateRepository<TAggregate>>();

        return services;
    }

    /// <summary>
    /// Registers event types from the specified assemblies for type resolution.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for event types.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventTypes(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Replace the default resolver with one that includes the assemblies
        services.RemoveAll<IEventTypeResolver>();
        services.AddSingleton<IEventTypeResolver>(_ => new DefaultEventTypeResolver(assemblies));

        return services;
    }
}
