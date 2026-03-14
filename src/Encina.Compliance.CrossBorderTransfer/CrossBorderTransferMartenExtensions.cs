using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.CrossBorderTransfer;

/// <summary>
/// Extension methods for registering cross-border transfer event-sourced aggregate repositories with Marten.
/// </summary>
public static class CrossBorderTransferMartenExtensions
{
    /// <summary>
    /// Registers the cross-border transfer aggregate repositories with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IAggregateRepository{TAggregate}"/> for each cross-border transfer aggregate:
    /// <list type="bullet">
    /// <item><see cref="TIAAggregate"/> — Transfer Impact Assessment lifecycle</item>
    /// <item><see cref="SCCAgreementAggregate"/> — SCC agreement lifecycle</item>
    /// <item><see cref="ApprovedTransferAggregate"/> — Approved transfer lifecycle</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaCrossBorderTransfer"/>
    /// when using Marten as the event store. The aggregate repositories are registered separately to allow
    /// use of cross-border transfer services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaCrossBorderTransfer(options =>
    /// {
    ///     options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
    /// });
    ///
    /// // Register Marten aggregate repositories
    /// services.AddCrossBorderTransferAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddCrossBorderTransferAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<TIAAggregate>();
        services.AddAggregateRepository<SCCAgreementAggregate>();
        services.AddAggregateRepository<ApprovedTransferAggregate>();

        return services;
    }
}
