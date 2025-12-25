using Encina.Messaging.Scheduling;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check for the Scheduling pattern.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>Scheduled message store is accessible</description></item>
/// <item><description>Number of overdue messages is within acceptable thresholds</description></item>
/// </list>
/// </para>
/// <para>
/// Returns degraded if overdue messages exceed the warning threshold,
/// or unhealthy if they exceed the critical threshold.
/// </para>
/// </remarks>
public class SchedulingHealthCheck : EncinaHealthCheck
{
    private readonly IScheduledMessageStore _store;
    private readonly SchedulingHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingHealthCheck"/> class.
    /// </summary>
    /// <param name="store">The scheduled message store to check.</param>
    /// <param name="options">Health check options.</param>
    public SchedulingHealthCheck(IScheduledMessageStore store, SchedulingHealthCheckOptions? options = null)
        : base("encina-scheduling", ["ready", "database", "messaging"])
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _options = options ?? new SchedulingHealthCheckOptions();
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        // Get due messages to check for overdue ones
        var dueMessages = await _store.GetDueMessagesAsync(
            batchSize: _options.OverdueCriticalThreshold + 1,
            maxRetries: int.MaxValue,
            cancellationToken).ConfigureAwait(false);

        var overdueCount = 0;
        var now = DateTime.UtcNow;

        foreach (var message in dueMessages)
        {
            // A message is overdue if it was due more than the tolerance ago
            if (message.ScheduledAtUtc.Add(_options.OverdueTolerance) < now)
            {
                overdueCount++;
            }
        }

        var data = new Dictionary<string, object>
        {
            ["due_count"] = dueMessages.Count(),
            ["overdue_count"] = overdueCount,
            ["overdue_tolerance_minutes"] = _options.OverdueTolerance.TotalMinutes,
            ["warning_threshold"] = _options.OverdueWarningThreshold,
            ["critical_threshold"] = _options.OverdueCriticalThreshold
        };

        if (overdueCount >= _options.OverdueCriticalThreshold)
        {
            return HealthCheckResult.Unhealthy(
                $"Scheduler has {overdueCount} overdue messages (critical threshold: {_options.OverdueCriticalThreshold})",
                data: data);
        }

        if (overdueCount >= _options.OverdueWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                $"Scheduler has {overdueCount} overdue messages (warning threshold: {_options.OverdueWarningThreshold})",
                data: data);
        }

        return HealthCheckResult.Healthy("Scheduling store is accessible and healthy", data);
    }
}

/// <summary>
/// Configuration options for the Scheduling health check.
/// </summary>
public sealed class SchedulingHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the tolerance before a due message is considered overdue.
    /// </summary>
    /// <value>Default: 5 minutes.</value>
    public TimeSpan OverdueTolerance { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the number of overdue messages that triggers a warning (degraded) status.
    /// </summary>
    /// <value>Default: 10 messages.</value>
    public int OverdueWarningThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of overdue messages that triggers an unhealthy status.
    /// </summary>
    /// <value>Default: 50 messages.</value>
    public int OverdueCriticalThreshold { get; set; } = 50;
}
