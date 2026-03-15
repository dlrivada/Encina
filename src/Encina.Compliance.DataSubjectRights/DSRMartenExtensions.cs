using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Extension methods for registering DSR request event-sourced aggregate repositories and projections with Marten.
/// </summary>
public static class DSRMartenExtensions
{
    /// <summary>
    /// Registers the DSR request aggregate repository and read model projection with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following Marten infrastructure:
    /// <list type="bullet">
    /// <item><see cref="IAggregateRepository{TAggregate}"/> for <see cref="DSRRequestAggregate"/> — Event-sourced DSR request lifecycle</item>
    /// <item><see cref="DSRRequestProjection"/> → <see cref="DSRRequestReadModel"/> — Inline projection for read-side queries</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaDataSubjectRights"/>
    /// when using Marten as the event store. The aggregate repository and projection are registered
    /// separately to allow use of DSR services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDataSubjectRights(options =>
    /// {
    ///     options.RestrictionEnforcementMode = DSREnforcementMode.Block;
    ///     options.AddHealthCheck = true;
    /// });
    ///
    /// // Register Marten aggregate repository and projection
    /// services.AddDSRRequestAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddDSRRequestAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<DSRRequestAggregate>();
        services.AddProjection<DSRRequestProjection, DSRRequestReadModel>();

        return services;
    }
}
