using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.ReadModels;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.Retention;

/// <summary>
/// Extension methods for registering retention event-sourced aggregate repositories and projections with Marten.
/// </summary>
public static class RetentionMartenExtensions
{
    /// <summary>
    /// Registers the retention aggregate repositories and projections with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IAggregateRepository{TAggregate}"/> for each retention aggregate:
    /// <list type="bullet">
    /// <item><see cref="RetentionPolicyAggregate"/> — Retention policy lifecycle</item>
    /// <item><see cref="RetentionRecordAggregate"/> — Retention record lifecycle</item>
    /// <item><see cref="LegalHoldAggregate"/> — Legal hold lifecycle</item>
    /// </list>
    /// </para>
    /// <para>
    /// Also registers projections and their read model repositories:
    /// <list type="bullet">
    /// <item><see cref="RetentionPolicyProjection"/> → <see cref="RetentionPolicyReadModel"/></item>
    /// <item><see cref="RetentionRecordProjection"/> → <see cref="RetentionRecordReadModel"/></item>
    /// <item><see cref="LegalHoldProjection"/> → <see cref="LegalHoldReadModel"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaRetention"/>
    /// when using Marten as the event store. The aggregate repositories are registered separately to allow
    /// use of retention services without Marten (e.g., during testing with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRetention(options =>
    /// {
    ///     options.EnforcementMode = RetentionEnforcementMode.Block;
    /// });
    ///
    /// // Register Marten aggregate repositories and projections
    /// services.AddRetentionAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddRetentionAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Aggregate repositories
        services.AddAggregateRepository<RetentionPolicyAggregate>();
        services.AddAggregateRepository<RetentionRecordAggregate>();
        services.AddAggregateRepository<LegalHoldAggregate>();

        // Projections and read model repositories
        services.AddProjection<RetentionPolicyProjection, RetentionPolicyReadModel>();
        services.AddProjection<RetentionRecordProjection, RetentionRecordReadModel>();
        services.AddProjection<LegalHoldProjection, LegalHoldReadModel>();

        return services;
    }
}
