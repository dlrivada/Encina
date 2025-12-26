namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Configuration options for the Dead Letter Queue pattern.
/// </summary>
/// <remarks>
/// These options are shared across all storage providers (EF Core, Dapper, ADO.NET, etc.)
/// to ensure consistent behavior.
/// </remarks>
public sealed class DeadLetterOptions
{
    /// <summary>
    /// Gets or sets how long dead letter messages are retained before automatic cleanup.
    /// </summary>
    /// <value>Default: 7 days. Set to null to disable automatic expiration.</value>
    public TimeSpan? RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the interval at which expired messages are cleaned up.
    /// </summary>
    /// <value>Default: 1 hour.</value>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets whether to enable automatic cleanup of expired messages.
    /// </summary>
    /// <value>Default: true.</value>
    public bool EnableAutomaticCleanup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic integration with the Recoverability Pipeline.
    /// </summary>
    /// <remarks>
    /// When enabled, permanently failed messages from the Recoverability Pipeline
    /// are automatically stored in the DLQ.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithRecoverability { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic integration with the Outbox pattern.
    /// </summary>
    /// <remarks>
    /// When enabled, outbox messages that exceed max retries are automatically
    /// stored in the DLQ.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithOutbox { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic integration with the Inbox pattern.
    /// </summary>
    /// <remarks>
    /// When enabled, inbox messages that exceed max retries are automatically
    /// stored in the DLQ.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithInbox { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic integration with the Scheduling pattern.
    /// </summary>
    /// <remarks>
    /// When enabled, scheduled messages that exceed max retries are automatically
    /// stored in the DLQ.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithScheduling { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic integration with the Saga pattern.
    /// </summary>
    /// <remarks>
    /// When enabled, saga messages with no handler (saga not found) are automatically
    /// stored in the DLQ when moved to dead letter.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithSagas { get; set; } = true;

    /// <summary>
    /// Gets or sets a custom callback invoked when a message is added to the DLQ.
    /// </summary>
    /// <remarks>
    /// Use this for custom handling such as alerting, logging to external systems, etc.
    /// </remarks>
    public Func<IDeadLetterMessage, CancellationToken, Task>? OnDeadLetter { get; set; }
}
