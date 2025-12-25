using Encina.Messaging.Sagas;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check for the Saga pattern.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>Saga store is accessible</description></item>
/// <item><description>Number of stuck sagas is within acceptable thresholds</description></item>
/// <item><description>Number of expired sagas is within acceptable thresholds</description></item>
/// </list>
/// </para>
/// <para>
/// Returns degraded if stuck or expired sagas exceed warning thresholds,
/// or unhealthy if they exceed critical thresholds.
/// </para>
/// </remarks>
public class SagaHealthCheck : EncinaHealthCheck
{
    private readonly ISagaStore _store;
    private readonly SagaHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaHealthCheck"/> class.
    /// </summary>
    /// <param name="store">The saga store to check.</param>
    /// <param name="options">Health check options.</param>
    public SagaHealthCheck(ISagaStore store, SagaHealthCheckOptions? options = null)
        : base("encina-saga", ["ready", "database", "messaging"])
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _options = options ?? new SagaHealthCheckOptions();
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        // Check for stuck sagas
        var stuckSagas = await _store.GetStuckSagasAsync(
            _options.StuckSagaThreshold,
            _options.SagaCriticalThreshold + 1,
            cancellationToken).ConfigureAwait(false);

        var stuckCount = stuckSagas.Count();

        // Check for expired sagas
        var expiredSagas = await _store.GetExpiredSagasAsync(
            _options.SagaCriticalThreshold + 1,
            cancellationToken).ConfigureAwait(false);

        var expiredCount = expiredSagas.Count();

        var data = new Dictionary<string, object>
        {
            ["stuck_count"] = stuckCount,
            ["expired_count"] = expiredCount,
            ["stuck_threshold"] = _options.StuckSagaThreshold.TotalMinutes,
            ["warning_threshold"] = _options.SagaWarningThreshold,
            ["critical_threshold"] = _options.SagaCriticalThreshold
        };

        var totalProblematic = stuckCount + expiredCount;

        if (totalProblematic >= _options.SagaCriticalThreshold)
        {
            return HealthCheckResult.Unhealthy(
                $"Saga store has {stuckCount} stuck and {expiredCount} expired sagas (critical threshold: {_options.SagaCriticalThreshold})",
                data: data);
        }

        if (totalProblematic >= _options.SagaWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                $"Saga store has {stuckCount} stuck and {expiredCount} expired sagas (warning threshold: {_options.SagaWarningThreshold})",
                data: data);
        }

        return HealthCheckResult.Healthy("Saga store is accessible and healthy", data);
    }
}

/// <summary>
/// Configuration options for the Saga health check.
/// </summary>
public sealed class SagaHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the time threshold after which a saga is considered stuck.
    /// </summary>
    /// <value>Default: 30 minutes.</value>
    public TimeSpan StuckSagaThreshold { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the number of problematic sagas that triggers a warning (degraded) status.
    /// </summary>
    /// <value>Default: 10 sagas.</value>
    public int SagaWarningThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of problematic sagas that triggers an unhealthy status.
    /// </summary>
    /// <value>Default: 50 sagas.</value>
    public int SagaCriticalThreshold { get; set; } = 50;
}
