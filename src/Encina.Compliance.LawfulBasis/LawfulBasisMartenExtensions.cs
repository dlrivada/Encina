using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.LawfulBasis;

/// <summary>
/// Extension methods for registering lawful basis event-sourced aggregate repositories with Marten.
/// </summary>
public static class LawfulBasisMartenExtensions
{
    /// <summary>
    /// Registers the lawful basis aggregate repositories with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IAggregateRepository{TAggregate}"/> for each lawful basis aggregate:
    /// <list type="bullet">
    /// <item><see cref="LawfulBasisAggregate"/> — Lawful basis registration lifecycle</item>
    /// <item><see cref="LIAAggregate"/> — Legitimate Interest Assessment lifecycle</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaLawfulBasis"/>
    /// when using Marten as the event store. The aggregate repositories are registered separately to allow
    /// use of lawful basis services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaLawfulBasis(options =>
    /// {
    ///     options.EnforcementMode = LawfulBasisEnforcementMode.Block;
    /// });
    ///
    /// // Register Marten aggregate repositories
    /// services.AddLawfulBasisAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddLawfulBasisAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<LawfulBasisAggregate>();
        services.AddAggregateRepository<LIAAggregate>();

        return services;
    }
}
