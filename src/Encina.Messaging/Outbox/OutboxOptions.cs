namespace Encina.Messaging.Outbox;

/// <summary>
/// Configuration options for the Outbox Pattern.
/// </summary>
/// <remarks>
/// These options are shared across all storage providers (EF Core, Dapper, ADO.NET, etc.)
/// to ensure consistent behavior.
/// </remarks>
public sealed class OutboxOptions
{
    /// <summary>
    /// Gets or sets the interval at which the outbox processor runs.
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
    /// Default: 5 seconds. The delay before the next attempt is
    /// <c>min(BaseRetryDelay * 2^RetryCount, MaxRetryDelay)</c>, reduced by up to
    /// <see cref="RetryJitterRatio"/>, where <c>RetryCount</c> is the number of failures
    /// recorded before the current one (see <see cref="OutboxRetryBackoff"/>).
    /// </value>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the upper bound of the retry delay, whatever the retry count.
    /// </summary>
    /// <value>Default: 10 minutes.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    public TimeSpan MaxRetryDelay
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            field = value;
        }
    } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the fraction of the retry delay that is randomised, so that messages that
    /// failed together do not retry together.
    /// </summary>
    /// <remarks>
    /// The capped exponential delay is multiplied by a random factor in
    /// <c>(1 - RetryJitterRatio, 1]</c>. Set it to <c>0</c> for a deterministic delay.
    /// </remarks>
    /// <value>Default: 0.2 (the delay is reduced by up to 20%).</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not between 0 and 1.</exception>
    public double RetryJitterRatio
    {
        get;
        set
        {
            if (double.IsNaN(value) || value < 0 || value > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The retry jitter ratio must be between 0 and 1.");
            }

            field = value;
        }
    } = 0.2;

    /// <summary>
    /// Gets or sets whether to enable the outbox processor.
    /// </summary>
    /// <remarks>
    /// Set to false if you want to process outbox messages manually or with a separate worker.
    /// </remarks>
    /// <value>Default: true</value>
    public bool EnableProcessor { get; set; } = true;
}
