using LanguageExt;

namespace Encina.Messaging.Health;

/// <summary>
/// Represents a health check for Encina messaging components.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a provider-agnostic way to check the health of messaging components.
/// Implementations can verify database connectivity, message queue availability, and pattern-specific status.
/// </para>
/// <para>
/// This interface is designed to be wrapped by ASP.NET Core's <c>IHealthCheck</c> for Kubernetes
/// readiness/liveness probes and monitoring systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OutboxHealthCheck : IEncinaHealthCheck
/// {
///     public string Name => "outbox";
///     public string[] Tags => ["database", "messaging"];
///
///     public async Task&lt;HealthCheckResult&gt; CheckHealthAsync(CancellationToken cancellationToken)
///     {
///         // Check outbox table accessibility
///         var canConnect = await _store.CanConnectAsync(cancellationToken);
///         return canConnect
///             ? HealthCheckResult.Healthy("Outbox store is accessible")
///             : HealthCheckResult.Unhealthy("Cannot connect to outbox store");
///     }
/// }
/// </code>
/// </example>
public interface IEncinaHealthCheck
{
    /// <summary>
    /// Gets the unique name of this health check.
    /// </summary>
    /// <remarks>
    /// This name is used for identification in health check reports and monitoring dashboards.
    /// Should be lowercase, kebab-case (e.g., "outbox-store", "saga-state").
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the tags associated with this health check.
    /// </summary>
    /// <remarks>
    /// Tags allow filtering health checks by category. Common tags include:
    /// <list type="bullet">
    /// <item><description>"ready": Kubernetes readiness probe</description></item>
    /// <item><description>"live": Kubernetes liveness probe</description></item>
    /// <item><description>"database": Database-related checks</description></item>
    /// <item><description>"messaging": Messaging pattern checks</description></item>
    /// </list>
    /// </remarks>
    IReadOnlyCollection<string> Tags { get; }

    /// <summary>
    /// Performs the health check.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the health check.</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
