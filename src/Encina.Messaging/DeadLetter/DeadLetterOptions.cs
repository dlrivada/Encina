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
    /// <exception cref="ArgumentOutOfRangeException">The value is set and not greater than zero.</exception>
    public TimeSpan? RetentionPeriod
    {
        get;
        set
        {
            if (value is { } period)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(period, TimeSpan.Zero, nameof(RetentionPeriod));
            }

            field = value;
        }
    } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the interval at which expired messages are cleaned up.
    /// </summary>
    /// <value>Default: 1 hour.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not greater than zero.</exception>
    public TimeSpan CleanupInterval
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, nameof(CleanupInterval));
            field = value;
        }
    } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets how long a replay claim excludes other replays of the same message.
    /// </summary>
    /// <remarks>
    /// A replay first claims the message (<c>ReplayClaimedAtUtc</c>); a claim older than this timeout is
    /// treated as abandoned by a crashed host and can be taken over.
    /// </remarks>
    /// <value>Default: 5 minutes.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not greater than zero.</exception>
    public TimeSpan ReplayClaimTimeout
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, nameof(ReplayClaimTimeout));
            field = value;
        }
    } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether to enable automatic cleanup of expired messages.
    /// </summary>
    /// <value>Default: true.</value>
    public bool EnableAutomaticCleanup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether permanent failures of the Recoverability Pipeline are captured in the DLQ.
    /// </summary>
    /// <remarks>
    /// When enabled, a request that fails permanently is stored once, keyed by its retry chain id
    /// (<c>FailedMessage.Id</c>, source pattern <see cref="DeadLetterSourcePatterns.Recoverability"/>): in
    /// <c>RecoverabilityPipelineBehavior</c> after a permanent error or when immediate retries are exhausted and
    /// no delayed retry is scheduled, and in <c>DelayedRetryProcessor</c> when the last delayed retry fails or a
    /// row cannot be re-dispatched (then its stored type name and content are kept). A failed capture is
    /// logged (EventId 2996 or 2997); the request already returns its failure.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithRecoverability { get; set; } = true;

    /// <summary>
    /// Gets or sets whether outbox messages that use up their retries are captured in the DLQ.
    /// </summary>
    /// <remarks>
    /// When enabled, the failed delivery that brings a message to <c>OutboxOptions.MaxRetries</c> stores its
    /// notification type and content once, keyed by the outbox message id (source pattern
    /// <see cref="DeadLetterSourcePatterns.Outbox"/>), before the exhausted state is recorded. When the capture
    /// fails, the message is not marked exhausted and is delivered again in a later cycle. A replay publishes
    /// the notification again.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithOutbox { get; set; } = true;

    /// <summary>
    /// Gets or sets whether inbox messages that use up their retries are captured in the DLQ.
    /// </summary>
    /// <remarks>
    /// When enabled, the failed attempt (a thrown exception) that brings a message to
    /// <c>InboxOptions.MaxRetries</c> stores its request once, keyed by the inbox message id (source pattern
    /// <see cref="DeadLetterSourcePatterns.Inbox"/>); a redelivery rejected afterwards captures it again
    /// idempotently. When the capture fails, the attempt returns the capture's error.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithInbox { get; set; } = true;

    /// <summary>
    /// Gets or sets whether scheduled messages that use up their retries are captured in the DLQ.
    /// </summary>
    /// <remarks>
    /// When enabled, the failure that the scheduling retry policy dead-letters stores the message's request
    /// type and content once, keyed by the scheduled message id (source pattern
    /// <see cref="DeadLetterSourcePatterns.Scheduling"/>), before that state is recorded. When the capture
    /// fails, the message keeps its state and runs again in a later cycle.
    /// </remarks>
    /// <value>Default: true.</value>
    public bool IntegrateWithScheduling { get; set; } = true;

    /// <summary>
    /// Gets or sets whether failed sagas are captured in the DLQ.
    /// </summary>
    /// <remarks>
    /// When enabled, a saga that <c>SagaOrchestrator.FailAsync</c> ends <c>Failed</c> (a failed compensation, a
    /// cancelled run or an unexpected exception in <c>SagaRunner</c>) stores its saga type and data once,
    /// keyed by the saga id (source pattern <see cref="DeadLetterSourcePatterns.Saga"/>); a failed capture is
    /// returned by <c>FailAsync</c>. A saga that ends <c>Compensated</c> is not captured. The record is kept for
    /// inspection: saga data is not a request, so a replay is recorded as failed.
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
