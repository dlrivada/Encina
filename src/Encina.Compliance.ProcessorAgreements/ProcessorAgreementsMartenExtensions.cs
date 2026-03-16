using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Extension methods for registering processor agreement event-sourced aggregate repositories with Marten.
/// </summary>
public static class ProcessorAgreementsMartenExtensions
{
    /// <summary>
    /// Registers the processor agreement aggregate repositories with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IAggregateRepository{TAggregate}"/> for each processor agreement aggregate:
    /// <list type="bullet">
    /// <item><see cref="ProcessorAggregate"/> — Processor identity and sub-processor hierarchy lifecycle</item>
    /// <item><see cref="DPAAggregate"/> — Data Processing Agreement contractual lifecycle</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaProcessorAgreements"/>
    /// when using Marten as the event store. The aggregate repositories are registered separately to allow
    /// use of processor agreement services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaProcessorAgreements(options =>
    /// {
    ///     options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
    /// });
    ///
    /// // Register Marten aggregate repositories
    /// services.AddProcessorAgreementAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddProcessorAgreementAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<ProcessorAggregate>();
        services.AddAggregateRepository<DPAAggregate>();

        return services;
    }
}
