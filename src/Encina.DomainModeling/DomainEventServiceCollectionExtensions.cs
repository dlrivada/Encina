using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.DomainModeling;

/// <summary>
/// Extension methods for registering domain event services in an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// These extensions register the domain event collection and dispatch infrastructure
/// for non-EF Core providers (ADO.NET, Dapper, MongoDB).
/// </para>
/// <para>
/// For EF Core, use the <c>UseDomainEvents</c> configuration option instead, which
/// provides automatic dispatch via a SaveChanges interceptor.
/// </para>
/// </remarks>
public static class DomainEventServiceCollectionExtensions
{
    /// <summary>
    /// Adds domain event collection and dispatch services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description><see cref="IDomainEventCollector"/> as scoped (<see cref="DomainEventCollector"/>)</description></item>
    /// <item><description><see cref="DomainEventDispatchHelper"/> as scoped</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Prerequisites</b>: <see cref="IEncina"/> must be registered before calling this method.
    /// Use <c>services.AddEncina(...)</c> first.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register Encina first
    /// services.AddEncina(typeof(MyHandler).Assembly);
    ///
    /// // Then add domain event services
    /// services.AddDomainEventServices();
    ///
    /// // Now you can inject IDomainEventCollector and DomainEventDispatchHelper
    /// public class MyUnitOfWork(
    ///     IDomainEventCollector collector,
    ///     DomainEventDispatchHelper dispatchHelper)
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddDomainEventServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register collector as scoped (one per request/unit of work)
        services.TryAddScoped<IDomainEventCollector, DomainEventCollector>();

        // Register dispatch helper as scoped
        services.TryAddScoped<DomainEventDispatchHelper>();

        return services;
    }

    /// <summary>
    /// Adds only the domain event collector to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this method if you only need the collector and want to handle dispatch
    /// yourself, or if you're using a custom dispatch mechanism.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDomainEventCollector(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IDomainEventCollector, DomainEventCollector>();

        return services;
    }
}
