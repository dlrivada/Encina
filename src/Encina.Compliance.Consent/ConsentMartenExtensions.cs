using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.ReadModels;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.Consent;

/// <summary>
/// Extension methods for registering consent event-sourced aggregate repositories and projections with Marten.
/// </summary>
public static class ConsentMartenExtensions
{
    /// <summary>
    /// Registers the consent aggregate repository and read model projection with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following Marten infrastructure:
    /// <list type="bullet">
    /// <item><see cref="IAggregateRepository{TAggregate}"/> for <see cref="ConsentAggregate"/> — Event-sourced consent lifecycle</item>
    /// <item><see cref="ConsentProjection"/> → <see cref="ConsentReadModel"/> — Inline projection for read-side queries</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaConsent"/>
    /// when using Marten as the event store. The aggregate repository and projection are registered
    /// separately to allow use of consent services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaConsent(options =>
    /// {
    ///     options.EnforcementMode = ConsentEnforcementMode.Block;
    ///     options.DefinePurpose(ConsentPurposes.Marketing, p =>
    ///     {
    ///         p.Description = "Email marketing communications";
    ///         p.RequiresExplicitOptIn = true;
    ///     });
    /// });
    ///
    /// // Register Marten aggregate repository and projection
    /// services.AddConsentAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddConsentAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<ConsentAggregate>();
        services.AddProjection<ConsentProjection, ConsentReadModel>();

        return services;
    }
}
