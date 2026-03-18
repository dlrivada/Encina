using Encina.Compliance.DataResidency.Aggregates;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Extension methods for registering data residency event-sourced aggregate repositories with Marten.
/// </summary>
public static class DataResidencyMartenExtensions
{
    /// <summary>
    /// Registers the data residency aggregate repositories with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IAggregateRepository{TAggregate}"/> for each data residency aggregate:
    /// <list type="bullet">
    /// <item><see cref="ResidencyPolicyAggregate"/> — Residency policy lifecycle</item>
    /// <item><see cref="DataLocationAggregate"/> — Data location lifecycle</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaDataResidency"/>
    /// when using Marten as the event store. The aggregate repositories are registered separately to allow
    /// use of data residency services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDataResidency(options =>
    /// {
    ///     options.DefaultRegion = RegionRegistry.DE;
    ///     options.EnforcementMode = DataResidencyEnforcementMode.Block;
    /// });
    ///
    /// // Register Marten aggregate repositories
    /// services.AddDataResidencyAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddDataResidencyAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<ResidencyPolicyAggregate>();
        services.AddAggregateRepository<DataLocationAggregate>();

        return services;
    }
}
