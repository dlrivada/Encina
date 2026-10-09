using Encina.Messaging.DeadLetter;

using LanguageExt;

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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterHealthCheck"/> class.
    /// </summary>
    /// <param name="store">The dead letter store.</param>
    /// <param name="options">Health check options.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public DeadLetterHealthCheck(
        IDeadLetterStore store,
        DeadLetterHealthCheckOptions? options = null,
        TimeProvider? timeProvider = null)
        : base("encina-deadletter", ["encina", "messaging", "deadletter"])
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _options = options ?? new DeadLetterHealthCheckOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(
        CancellationToken cancellationToken)
    {
        var pendingCountResult = await _store.GetCountAsync(
            new DeadLetterFilter { ExcludeReplayed = true },
            cancellationToken);

        if (pendingCountResult.IsLeft)
        {
            return StoreFailure(pendingCountResult.LeftToArray()[0]);
        }

        var pendingCount = pendingCountResult.RightToArray()[0];

        var data = new Dictionary<string, object>
        {
            ["pending_count"] = pendingCount,
            ["warning_threshold"] = _options.PendingMessageWarningThreshold,
            ["critical_threshold"] = _options.PendingMessageCriticalThreshold
        };

        // Check for old messages
        if (_options.OldMessageThreshold is { } threshold)
        {
            var oldMessagesOutcome = await CheckOldMessagesAsync(pendingCount, threshold, data, cancellationToken);
            if (oldMessagesOutcome is not null)
            {
                return oldMessagesOutcome.Value;
            }
        }

        return ClassifyByCount(pendingCount, data);
    }

    private async Task<HealthCheckResult?> CheckOldMessagesAsync(
        int pendingCount,
        TimeSpan threshold,
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        var oldMessagesResult = await _store.GetMessagesAsync(
            new DeadLetterFilter
            {
                ExcludeReplayed = true,
                DeadLetteredBeforeUtc = _timeProvider.GetUtcNow().UtcDateTime.Subtract(threshold)
            },
            skip: 0,
            take: 1,
            cancellationToken);

        if (oldMessagesResult.IsLeft)
        {
            return StoreFailure(oldMessagesResult.LeftToArray()[0]);
        }

        var hasOldMessages = oldMessagesResult.RightToArray()[0].Any();
        data["has_old_messages"] = hasOldMessages;
        data["old_message_threshold"] = threshold.ToString();

        if (hasOldMessages && pendingCount < _options.PendingMessageWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                $"DLQ contains messages older than {threshold}",
                data: data);
        }

        return null;
    }

    private HealthCheckResult ClassifyByCount(int pendingCount, Dictionary<string, object> data)
    {
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

    // Fail closed: a store error is Unhealthy. Only the error code is reported, never the
    // error message (AGENTS.md §3, #1168).
    private static HealthCheckResult StoreFailure(EncinaError error)
    {
        var code = error.GetCode().IfNone("encina.unknown");
        return HealthCheckResult.Unhealthy(
            $"DLQ store failed: {code}",
            data: new Dictionary<string, object> { ["error_code"] = code });
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
