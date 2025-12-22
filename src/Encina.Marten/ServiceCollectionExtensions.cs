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

        services.Configure(configure);

        // Register the open generic aggregate repository
        services.TryAddScoped(typeof(IAggregateRepository<>), typeof(MartenAggregateRepository<>));

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
}
