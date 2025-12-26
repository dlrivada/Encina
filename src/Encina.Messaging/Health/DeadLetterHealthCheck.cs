using Encina.Messaging.DeadLetter;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check for the Dead Letter Queue.
/// </summary>
/// <remarks>
/// Reports:
/// <list type="bullet">
/// <item><description><b>Healthy</b>: DLQ message count is below warning threshold</description></item>
/// <item><description><b>Degraded</b>: DLQ message count exceeds warning threshold</description></item>
/// <item><description><b>Unhealthy</b>: DLQ message count exceeds critical threshold</description></item>
/// </list>
/// </remarks>
public sealed class DeadLetterHealthCheck : EncinaHealthCheck
{
    private readonly IDeadLetterStore _store;
    private readonly DeadLetterHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterHealthCheck"/> class.
    /// </summary>
    /// <param name="store">The dead letter store.</param>
    /// <param name="options">Health check options.</param>
    public DeadLetterHealthCheck(
        IDeadLetterStore store,
        DeadLetterHealthCheckOptions? options = null)
        : base("encina-deadletter", ["encina", "messaging", "deadletter"])
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _options = options ?? new DeadLetterHealthCheckOptions();
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(
        CancellationToken cancellationToken)
    {
        var pendingCount = await _store.GetCountAsync(
            new DeadLetterFilter { ExcludeReplayed = true },
            cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["pending_count"] = pendingCount,
            ["warning_threshold"] = _options.PendingMessageWarningThreshold,
            ["critical_threshold"] = _options.PendingMessageCriticalThreshold
        };

        // Check for old messages
        if (_options.OldMessageThreshold.HasValue)
        {
            var oldMessages = await _store.GetMessagesAsync(
                new DeadLetterFilter
                {
                    ExcludeReplayed = true,
                    DeadLetteredBeforeUtc = DateTime.UtcNow.Subtract(_options.OldMessageThreshold.Value)
                },
                skip: 0,
                take: 1,
                cancellationToken);

            var hasOldMessages = oldMessages.Any();
            data["has_old_messages"] = hasOldMessages;
            data["old_message_threshold"] = _options.OldMessageThreshold.Value.ToString();

            if (hasOldMessages && pendingCount < _options.PendingMessageWarningThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"DLQ contains messages older than {_options.OldMessageThreshold.Value}",
                    data: data);
            }
        }

        if (pendingCount >= _options.PendingMessageCriticalThreshold)
        {
            return HealthCheckResult.Unhealthy(
                $"DLQ has {pendingCount} pending messages (critical threshold: {_options.PendingMessageCriticalThreshold})",
                data: data);
        }

        if (pendingCount >= _options.PendingMessageWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                $"DLQ has {pendingCount} pending messages (warning threshold: {_options.PendingMessageWarningThreshold})",
                data: data);
        }

        return HealthCheckResult.Healthy(
            pendingCount == 0
                ? "DLQ is empty"
                : $"DLQ has {pendingCount} pending messages",
            data: data);
    }
}

/// <summary>
/// Configuration options for the Dead Letter Queue health check.
/// </summary>
public sealed class DeadLetterHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the pending message count that triggers a warning.
    /// </summary>
    /// <value>Default: 10 messages.</value>
    public int PendingMessageWarningThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the pending message count that triggers a critical alert.
    /// </summary>
    /// <value>Default: 100 messages.</value>
    public int PendingMessageCriticalThreshold { get; set; } = 100;

    /// <summary>
    /// Gets or sets the age threshold for old messages that triggers a warning.
    /// </summary>
    /// <value>Default: 24 hours. Set to null to disable.</value>
    public TimeSpan? OldMessageThreshold { get; set; } = TimeSpan.FromHours(24);
}
