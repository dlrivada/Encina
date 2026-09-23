using Encina.Messaging.Diagnostics;
using Encina.Messaging.Outbox;
using LanguageExt;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check for the Outbox pattern.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>Outbox store is accessible</description></item>
/// <item><description>The pending backlog (messages not processed and with retries left) is within its thresholds</description></item>
/// <item><description>The exhausted messages (not processed, retries used up) are within their thresholds</description></item>
/// </list>
/// </para>
/// <para>
/// Each count is compared with its warning and critical thresholds from <see cref="OutboxHealthCheckOptions"/>;
/// the result is the worse of the two statuses. Both counts are exact queries on the store, using
/// <see cref="OutboxOptions.MaxRetries"/> to tell pending from exhausted messages.
/// </para>
/// </remarks>
public class OutboxHealthCheck : EncinaHealthCheck
{
    private readonly IOutboxStore _store;
    private readonly OutboxOptions _outboxOptions;
    private readonly OutboxHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxHealthCheck"/> class.
    /// </summary>
    /// <param name="store">The outbox store to check.</param>
    /// <param name="outboxOptions">The outbox options; <see cref="OutboxOptions.MaxRetries"/> separates pending from exhausted messages.</param>
    /// <param name="options">Health check options.</param>
    public OutboxHealthCheck(IOutboxStore store, OutboxOptions outboxOptions, OutboxHealthCheckOptions? options = null)
        : base("encina-outbox", ["ready", "database", "messaging"])
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(outboxOptions);
        _store = store;
        _outboxOptions = outboxOptions;
        _options = options ?? new OutboxHealthCheckOptions();
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var maxRetries = _outboxOptions.MaxRetries;

        var pendingResult = await _store.GetPendingCountAsync(maxRetries, cancellationToken).ConfigureAwait(false);
        if (pendingResult.IsLeft)
        {
            return StoreFailure(pendingResult.LeftToArray()[0]);
        }

        var exhaustedResult = await _store.GetExhaustedCountAsync(maxRetries, cancellationToken).ConfigureAwait(false);
        if (exhaustedResult.IsLeft)
        {
            return StoreFailure(exhaustedResult.LeftToArray()[0]);
        }

        var pendingCount = pendingResult.RightToArray()[0];
        var exhaustedCount = exhaustedResult.RightToArray()[0];
        OutboxProcessorMetrics.Instance.SetExhaustedCount(exhaustedCount);

        var data = new Dictionary<string, object>
        {
            ["pending_count"] = pendingCount,
            ["exhausted_count"] = exhaustedCount,
            ["max_retries"] = maxRetries,
            ["pending_warning_threshold"] = _options.PendingMessageWarningThreshold,
            ["pending_critical_threshold"] = _options.PendingMessageCriticalThreshold,
            ["exhausted_warning_threshold"] = _options.ExhaustedMessageWarningThreshold,
            ["exhausted_critical_threshold"] = _options.ExhaustedMessageCriticalThreshold
        };

        var pendingStatus = Classify(pendingCount, _options.PendingMessageWarningThreshold, _options.PendingMessageCriticalThreshold);
        var exhaustedStatus = Classify(exhaustedCount, _options.ExhaustedMessageWarningThreshold, _options.ExhaustedMessageCriticalThreshold);

        var problems = new List<string>(2);
        if (pendingStatus != HealthStatus.Healthy)
        {
            problems.Add($"{pendingCount} pending messages (warning threshold: {_options.PendingMessageWarningThreshold}, critical threshold: {_options.PendingMessageCriticalThreshold})");
        }

        if (exhaustedStatus != HealthStatus.Healthy)
        {
            problems.Add($"{exhaustedCount} messages with exhausted retries awaiting requeue (warning threshold: {_options.ExhaustedMessageWarningThreshold}, critical threshold: {_options.ExhaustedMessageCriticalThreshold})");
        }

        // HealthStatus orders Unhealthy < Degraded < Healthy, so the worse status is the minimum.
        var status = (HealthStatus)Math.Min((int)pendingStatus, (int)exhaustedStatus);

        return status switch
        {
            HealthStatus.Unhealthy => HealthCheckResult.Unhealthy($"Outbox has {string.Join("; ", problems)}", data: data),
            HealthStatus.Degraded => HealthCheckResult.Degraded($"Outbox has {string.Join("; ", problems)}", data: data),
            _ => HealthCheckResult.Healthy("Outbox store is accessible and healthy", data)
        };
    }

    private static HealthStatus Classify(int count, int warningThreshold, int criticalThreshold)
    {
        if (count >= criticalThreshold)
        {
            return HealthStatus.Unhealthy;
        }

        return count >= warningThreshold ? HealthStatus.Degraded : HealthStatus.Healthy;
    }

    private static HealthCheckResult StoreFailure(EncinaError error)
        => HealthCheckResult.Unhealthy(
            $"Failed to query outbox store: {error.Message}",
            data: new Dictionary<string, object> { ["error"] = error.Message });
}

/// <summary>
/// Configuration options for the Outbox health check.
/// </summary>
public sealed class OutboxHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the number of pending messages that triggers a warning (degraded) status.
    /// </summary>
    /// <value>Default: 100 messages.</value>
    public int PendingMessageWarningThreshold { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of pending messages that triggers an unhealthy status.
    /// </summary>
    /// <value>Default: 1000 messages.</value>
    public int PendingMessageCriticalThreshold { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the number of messages with exhausted retries that triggers a warning (degraded) status.
    /// </summary>
    /// <remarks>
    /// An exhausted message is a delivery that failed <see cref="OutboxOptions.MaxRetries"/> times and will not be
    /// attempted again until it is requeued, so the default reports the first one. Set it to
    /// <see cref="int.MaxValue"/> to never report degraded for exhausted messages.
    /// </remarks>
    /// <value>Default: 1 message.</value>
    public int ExhaustedMessageWarningThreshold { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of messages with exhausted retries that triggers an unhealthy status.
    /// </summary>
    /// <remarks>
    /// Set it to <see cref="int.MaxValue"/> to never report unhealthy for exhausted messages, for example when the
    /// check is used as a readiness probe and exhausted messages should not take the instance out of rotation.
    /// </remarks>
    /// <value>Default: 100 messages.</value>
    public int ExhaustedMessageCriticalThreshold { get; set; } = 100;
}
