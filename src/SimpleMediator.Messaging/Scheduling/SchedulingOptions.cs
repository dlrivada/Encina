namespace SimpleMediator.Messaging.Scheduling;

/// <summary>
/// Configuration options for the Scheduling Pattern.
/// </summary>
/// <remarks>
/// These options are shared across all storage providers (EF Core, Dapper, ADO.NET, etc.)
/// to ensure consistent scheduled message processing behavior.
/// </remarks>
public sealed class SchedulingOptions
{
    /// <summary>
    /// Gets or sets the interval at which the scheduling processor runs.
    /// </summary>
    /// <value>Default: 30 seconds</value>
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of messages to process in a single batch.
    /// </summary>
    /// <value>Default: 100</value>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed messages.
    /// </summary>
    /// <value>Default: 3</value>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff retry strategy.
    /// </summary>
    /// <value>
    /// Default: 5 seconds. Actual delay is <c>BaseRetryDelay * 2^retryCount</c>.
    /// </value>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether to enable the scheduling processor.
    /// </summary>
    /// <remarks>
    /// Set to false if you want to process scheduled messages manually or with a separate worker.
    /// </remarks>
    /// <value>Default: true</value>
    public bool EnableProcessor { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to support recurring messages with cron expressions.
    /// </summary>
    /// <value>Default: true</value>
    public bool EnableRecurringMessages { get; set; } = true;
}
