using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.Marten;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Extension methods for registering breach notification event-sourced aggregate repositories
/// and projections with Marten.
/// </summary>
public static class BreachNotificationMartenExtensions
{
    /// <summary>
    /// Registers the breach notification aggregate repository and read model projection with Marten.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following Marten infrastructure:
    /// <list type="bullet">
    /// <item><see cref="IAggregateRepository{TAggregate}"/> for <see cref="BreachAggregate"/> — Event-sourced breach lifecycle</item>
    /// <item><see cref="BreachProjection"/> → <see cref="BreachReadModel"/> — Inline projection for read-side queries</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should be called alongside <see cref="ServiceCollectionExtensions.AddEncinaBreachNotification"/>
    /// when using Marten as the event store. The aggregate repository and projection are registered
    /// separately to allow use of breach notification services without Marten (e.g., during testing
    /// with custom implementations).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaBreachNotification(options =>
    /// {
    ///     options.EnforcementMode = BreachDetectionEnforcementMode.Block;
    ///     options.EnableDeadlineMonitoring = true;
    /// });
    ///
    /// // Register Marten aggregate repository and projection
    /// services.AddBreachNotificationAggregates();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddBreachNotificationAggregates(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAggregateRepository<BreachAggregate>();
        services.AddProjection<BreachProjection, BreachReadModel>();

        return services;
    }
}
