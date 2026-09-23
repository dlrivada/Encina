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
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1.</exception>
    public int BatchSize
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
        }
    } = 100;

    /// <summary>
    /// Gets or sets the maximum number of delivery attempts for a message, the first one included.
    /// </summary>
    /// <remarks>
    /// A message is fetched while its <see cref="IOutboxMessage.RetryCount"/> (the number of failed
    /// attempts) is less than <c>MaxRetries</c>. The failure that brings the count to <c>MaxRetries</c>
    /// exhausts the message: it is not fetched again until it is requeued. With the default of 10, a
    /// message is attempted at most 10 times.
    /// </remarks>
    /// <value>Default: 10</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1.</exception>
    public int MaxRetries
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
        }
    } = 10;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff retry strategy.
    /// </summary>
    /// <value>
    /// Default: 30 seconds. The delay before the next attempt is
    /// <c>min(BaseRetryDelay * 2^RetryCount, MaxRetryDelay)</c>, reduced by up to
    /// <see cref="RetryJitterRatio"/>, where <c>RetryCount</c> is the number of failures
    /// recorded before the current one (see <see cref="OutboxRetryBackoff"/>).
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is zero or negative.</exception>
    /// <remarks>
    /// It must not exceed <see cref="MaxRetryDelay"/>; the outbox processor and
    /// <see cref="OutboxOrchestrator"/> reject such options when they are created.
    /// </remarks>
    public TimeSpan BaseRetryDelay
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            field = value;
        }
    } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the upper bound of the retry delay, whatever the retry count.
    /// </summary>
    /// <value>Default: 1 hour.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    /// <remarks>
    /// It must be greater than or equal to <see cref="BaseRetryDelay"/>; the outbox processor and
    /// <see cref="OutboxOrchestrator"/> reject such options when they are created. A retry time beyond
    /// <see cref="DateTime.MaxValue"/> is saturated to <see cref="DateTime.MaxValue"/>.
    /// </remarks>
    public TimeSpan MaxRetryDelay
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            field = value;
        }
    } = TimeSpan.FromHours(1);

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

    /// <summary>
    /// Validates the rules that involve more than one property, which the setters cannot check
    /// because the properties may be assigned in any order.
    /// </summary>
    /// <param name="paramName">The name of the parameter that received these options.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="MaxRetryDelay"/> is less than <see cref="BaseRetryDelay"/>.
    /// </exception>
    internal void Validate(string paramName)
    {
        if (MaxRetryDelay < BaseRetryDelay)
        {
            throw new ArgumentException(
                $"{nameof(OutboxOptions)}.{nameof(MaxRetryDelay)} ({MaxRetryDelay}) must be greater than or equal to {nameof(BaseRetryDelay)} ({BaseRetryDelay}).",
                paramName);
        }
    }
}
