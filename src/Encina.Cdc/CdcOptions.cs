namespace Encina.Cdc;

/// <summary>
/// Configuration options for the CDC processing infrastructure.
/// All CDC features are opt-in and disabled by default.
/// </summary>
public sealed class CdcOptions
{
    /// <summary>
    /// Gets or sets whether CDC processing is enabled.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the interval between polling cycles for providers that use polling.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum number of change events to process in a single batch.
    /// Default is 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of retries for transient failures.
    /// Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between retries (used with exponential backoff).
    /// Default is 1 second.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets table name filters to restrict which tables are captured.
    /// Empty array means all tables are captured.
    /// </summary>
    public string[] TableFilters { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to enable position tracking for resume after restart.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnablePositionTracking { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable the CDC-to-messaging bridge.
    /// When enabled, captured change events are published as <see cref="INotification"/>
    /// via <see cref="IEncina.Publish{TNotification}"/>.
    /// Default is <c>false</c>.
    /// </summary>
    public bool UseMessagingBridge { get; set; }

    /// <summary>
    /// Gets or sets whether to enable CDC-driven outbox processing.
    /// When enabled, CDC monitors the outbox table and republishes stored
    /// notifications instead of relying on polling-based outbox processing.
    /// Default is <c>false</c>.
    /// </summary>
    public bool UseOutboxCdc { get; set; }

    /// <summary>
    /// Gets or sets whether to enable sharded CDC capture.
    /// When enabled, the <c>ShardedCdcProcessor</c> is registered instead of
    /// the standard <c>CdcProcessor</c> to process events from multiple shards.
    /// Default is <c>false</c>.
    /// </summary>
    public bool UseShardedCapture { get; set; }
}
