using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Extension methods for registering DPIA event-sourced aggregate repositories
/// and projections with Marten.
/// </summary>
public static class DPIAMartenExtensions
{
    /// <summary>
    /// Registers the DPIA aggregate repository and read model projection with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// <list type="bullet">
    /// <item><see cref="IAggregateRepository{TAggregate}"/> for <see cref="DPIAAggregate"/> — assessment lifecycle</item>
    /// <item><see cref="DPIAProjection"/> and <see cref="Marten.Projections.IReadModelRepository{TReadModel}"/>
    /// for <see cref="DPIAReadModel"/> — query-optimized read model</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <c>AddEncinaDPIA</c> when using Marten as the event store.
    /// The aggregate repositories and projections are registered separately to allow use of DPIA
    /// services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDPIA(options =>
    /// {
    ///     options.EnforcementMode = DPIAEnforcementMode.Block;
    ///     options.DPOName = "Jane Doe";
    ///     options.DPOEmail = "dpo@example.com";
    /// });
    ///
    /// // Register Marten aggregate repositories and projections
    /// services.AddDPIAAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddDPIAAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<DPIAAggregate>();
        services.AddProjection<DPIAProjection, DPIAReadModel>();

        return services;
    }
}
