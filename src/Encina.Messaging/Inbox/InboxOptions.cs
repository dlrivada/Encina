namespace Encina.Messaging.Inbox;

/// <summary>
/// Configuration options for the Inbox Pattern.
/// </summary>
/// <remarks>
/// These options are shared across all storage providers (EF Core, Dapper, ADO.NET, etc.)
/// to ensure consistent idempotent message processing behavior.
/// </remarks>
public sealed class InboxOptions
{
    /// <summary>
    /// Gets or sets the maximum number of handler attempts for a message.
    /// </summary>
    /// <remarks>
    /// For sequential deliveries the handler runs at most <c>MaxRetries</c> times (concurrent redeliveries
    /// of the same message are not serialized by the inbox): each attempt that throws increments the
    /// message's <c>RetryCount</c> once (in <c>IInboxStore.MarkAsFailedAsync</c>), and when
    /// <c>RetryCount</c> reaches <c>MaxRetries</c> the next delivery is rejected with
    /// <c>inbox.max_retries_exceeded</c> without running the handler. With the default of 3 the handler
    /// runs 3 times. A handler that returns a <c>Left</c> is a business outcome (ADR-001): the result is
    /// cached as the processed response and does not consume attempts.
    /// </remarks>
    /// <value>Default: 3</value>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets how long processed messages are retained in the inbox.
    /// </summary>
    /// <remarks>
    /// Messages are retained to handle delayed duplicates. After this period,
    /// they can be purged. Set to a value longer than your maximum expected
    /// duplicate delay (typically 7-30 days).
    /// </remarks>
    /// <value>Default: 30 days</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not greater than zero.</exception>
    public TimeSpan MessageRetentionPeriod
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, nameof(MessageRetentionPeriod));
            field = value;
        }
    } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the interval at which expired messages are purged.
    /// </summary>
    /// <remarks>
    /// The interval is awaited with <c>Task.Delay</c>, which accepts at most
    /// <c>uint.MaxValue - 1</c> milliseconds (about 49.7 days); a larger value is rejected here so the
    /// misconfiguration fails at configuration time instead of inside the purge loop.
    /// </remarks>
    /// <value>Default: 24 hours</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not greater than zero, or is greater than <c>uint.MaxValue - 1</c> milliseconds.
    /// </exception>
    public TimeSpan PurgeInterval
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, nameof(PurgeInterval));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxPurgeInterval, nameof(PurgeInterval));
            field = value;
        }
    } = TimeSpan.FromHours(24);

    /// <summary>The longest delay <c>Task.Delay</c> accepts: <c>uint.MaxValue - 1</c> milliseconds.</summary>
    private static readonly TimeSpan MaxPurgeInterval = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <summary>
    /// Gets or sets whether to enable automatic purging of expired messages.
    /// </summary>
    /// <value>Default: true</value>
    public bool EnableAutomaticPurge { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for purging expired messages.
    /// </summary>
    /// <remarks>
    /// There is no upper bound: the stores split a batch that exceeds a database limit.
    /// </remarks>
    /// <value>Default: 100</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int PurgeBatchSize
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(PurgeBatchSize));
            field = value;
        }
    } = 100;
}
