namespace Encina.Messaging.Health;

/// <summary>
/// Base class for Encina health checks with common functionality.
/// </summary>
/// <remarks>
/// <para>
/// This base class provides a convenient starting point for implementing health checks.
/// It handles exception catching and converts exceptions to unhealthy results automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OutboxHealthCheck : EncinaHealthCheck
/// {
///     private readonly IOutboxStore _store;
///
///     public OutboxHealthCheck(IOutboxStore store)
///         : base("outbox-store", ["database", "messaging", "ready"])
///     {
///         _store = store;
///     }
///
///     protected override async Task&lt;HealthCheckResult&gt; CheckHealthCoreAsync(CancellationToken cancellationToken)
///     {
///         var pendingCount = await _store.GetPendingCountAsync(cancellationToken);
///         var data = new Dictionary&lt;string, object&gt; { ["pending_messages"] = pendingCount };
///         return HealthCheckResult.Healthy("Outbox store is accessible", data);
///     }
/// }
/// </code>
/// </example>
public abstract class EncinaHealthCheck : IEncinaHealthCheck
{
    private readonly string _name;
    private readonly IReadOnlyCollection<string> _tags;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaHealthCheck"/> class.
    /// </summary>
    /// <param name="name">The unique name of the health check.</param>
    /// <param name="tags">The tags associated with this health check.</param>
    protected EncinaHealthCheck(string name, IReadOnlyCollection<string>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _name = name;
        _tags = tags ?? [];
    }

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Tags => _tags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await CheckHealthCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Health check was cancelled");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Performs the actual health check logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the health check.</returns>
    /// <remarks>
    /// Implementers should focus on the health check logic. Exception handling
    /// is provided by the base class.
    /// </remarks>
    protected abstract Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken);
}
