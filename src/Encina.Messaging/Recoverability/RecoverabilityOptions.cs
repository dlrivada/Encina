using Encina.Recoverability;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Configuration options for the Recoverability Pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The Recoverability Pipeline provides a unified retry strategy with two phases:
/// <list type="number">
/// <item><description><b>Immediate retries</b>: Fast, in-memory retries for transient failures</description></item>
/// <item><description><b>Delayed retries</b>: Persistent, scheduled retries for longer recovery</description></item>
/// </list>
/// </para>
/// <para>
/// After all retries are exhausted, the message is sent to the Dead Letter Queue (DLQ)
/// for manual inspection and recovery.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaRecoverability(config =>
/// {
///     config.ImmediateRetries = 3;
///     config.DelayedRetries = new[]
///     {
///         TimeSpan.FromSeconds(30),
///         TimeSpan.FromMinutes(5),
///         TimeSpan.FromMinutes(30)
///     };
/// });
/// </code>
/// </example>
public sealed class RecoverabilityOptions
{
    /// <summary>
    /// Gets or sets the number of immediate retries before moving to delayed retries.
    /// </summary>
    /// <remarks>
    /// Immediate retries are fast, in-memory retries executed right after a failure.
    /// They are ideal for transient failures like network hiccups.
    /// </remarks>
    /// <value>Default: 3</value>
    public int ImmediateRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between immediate retries.
    /// </summary>
    /// <value>Default: 100ms</value>
    public TimeSpan ImmediateRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets whether to use exponential backoff for immediate retries.
    /// </summary>
    /// <remarks>
    /// When enabled, delays are calculated as <c>ImmediateRetryDelay * 2^attempt</c>.
    /// </remarks>
    /// <value>Default: true</value>
    public bool UseExponentialBackoffForImmediateRetries { get; set; } = true;

    /// <summary>
    /// Gets or sets the delays for delayed retries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Delayed retries are persistent and scheduled for future execution.
    /// Each element in the array represents the delay for that retry attempt.
    /// </para>
    /// <para>
    /// After all delayed retries are exhausted, the message goes to the DLQ.
    /// </para>
    /// </remarks>
    /// <value>Default: 30s, 5m, 30m, 2h</value>
    public TimeSpan[] DelayedRetries { get; set; } =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2)
    ];

    /// <summary>
    /// Gets or sets the callback invoked when a message permanently fails.
    /// </summary>
    /// <remarks>
    /// This callback is invoked after all retries (immediate and delayed) are exhausted.
    /// Use it to log, alert, or implement custom dead letter handling.
    /// </remarks>
    public Func<FailedMessage, CancellationToken, Task>? OnPermanentFailure { get; set; }

    /// <summary>
    /// Gets or sets the error classifier to determine retry behavior.
    /// </summary>
    /// <remarks>
    /// The classifier determines whether an error is transient (retriable)
    /// or permanent (should go directly to DLQ).
    /// </remarks>
    public IErrorClassifier? ErrorClassifier { get; set; }

    /// <summary>
    /// Gets or sets whether to enable delayed retries.
    /// </summary>
    /// <remarks>
    /// When disabled, only immediate retries are used, and messages go to DLQ
    /// after immediate retries are exhausted. Requires scheduling to be enabled
    /// in <see cref="MessagingConfiguration.UseScheduling"/>.
    /// </remarks>
    /// <value>Default: true</value>
    public bool EnableDelayedRetries { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add jitter to retry delays.
    /// </summary>
    /// <remarks>
    /// Jitter adds randomness to retry delays to prevent thundering herd problems
    /// when multiple failing messages retry at the same time.
    /// </remarks>
    /// <value>Default: true</value>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum jitter percentage.
    /// </summary>
    /// <value>Default: 20 (±20%)</value>
    public int MaxJitterPercent { get; set; } = 20;

    /// <summary>
    /// Gets or sets the ceiling of a <c>Retry-After</c> that a delayed retry honours.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <c>Retry-After</c> at or below the ceiling replaces the configured delay of the next delayed retry and is
    /// never shortened. A <c>Retry-After</c> strictly above it schedules nothing: the message is dead-lettered
    /// through the permanent-failure path with the error code
    /// <see cref="RecoverabilityErrorCodes.RetryAfterExceedsCeiling"/>, a warning (EventIds 5454 and 5455) is
    /// logged and the <c>encina.retry.skipped_total</c> counter records the reason
    /// <see cref="RetryMetrics.ReasonRetryAfterExceedsCeiling"/>.
    /// </para>
    /// <para>
    /// A positive <see cref="TimeSpan.MaxValue"/> is the explicit "no ceiling" choice.
    /// </para>
    /// </remarks>
    /// <value>Default: 24 hours.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is zero or negative.</exception>
    public TimeSpan MaxRetryAfter
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            field = value;
        }
    } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets the total number of retry attempts (immediate + delayed).
    /// </summary>
    public int TotalRetryAttempts => ImmediateRetries + (EnableDelayedRetries ? DelayedRetries.Length : 0);
}
