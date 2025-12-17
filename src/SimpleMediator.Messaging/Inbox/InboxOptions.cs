namespace SimpleMediator.Messaging.Inbox;

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
    /// Gets or sets the maximum number of retries for failed messages.
    /// </summary>
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
    public TimeSpan MessageRetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the interval at which expired messages are purged.
    /// </summary>
    /// <value>Default: 24 hours</value>
    public TimeSpan PurgeInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets whether to enable automatic purging of expired messages.
    /// </summary>
    /// <value>Default: true</value>
    public bool EnableAutomaticPurge { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for purging expired messages.
    /// </summary>
    /// <value>Default: 100</value>
    public int PurgeBatchSize { get; set; } = 100;
}
