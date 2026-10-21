namespace Encina.Polly;

/// <summary>
/// Configures retry policy for a request using Polly.
/// Applied to request types to enable automatic retries on transient failures.
/// </summary>
/// <example>
/// <code>
/// [Retry(MaxAttempts = 3, BackoffType = BackoffType.Exponential)]
/// public record CallExternalApiQuery(string Endpoint) : IRequest&lt;ApiResponse&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RetryAttribute : Attribute
{
    /// <summary>
    /// Maximum number of retry attempts (including the initial attempt).
    /// </summary>
    /// <remarks>
    /// Default: 3 attempts (1 initial + 2 retries).
    /// </remarks>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Type of backoff strategy between retries.
    /// </summary>
    /// <remarks>
    /// - <see cref="BackoffType.Constant"/>: Fixed delay between retries
    /// - <see cref="BackoffType.Linear"/>: Linearly increasing delay
    /// - <see cref="BackoffType.Exponential"/>: Exponentially increasing delay (recommended for most cases)
    /// </remarks>
    public BackoffType BackoffType { get; init; } = BackoffType.Exponential;

    /// <summary>
    /// Base delay for the backoff calculation (in milliseconds).
    /// </summary>
    /// <remarks>
    /// - Constant: Uses this value as-is
    /// - Linear: Multiplied by attempt number (baseDelay * attemptNumber)
    /// - Exponential: Multiplied by 2^attemptNumber (baseDelay * 2^attemptNumber)
    /// Default: 1000ms (1 second)
    /// </remarks>
    public int BaseDelayMs { get; init; } = 1000;

    /// <summary>
    /// Maximum delay between retries (in milliseconds).
    /// Prevents exponential backoff from growing too large.
    /// </summary>
    /// <remarks>
    /// Default: 30000ms (30 seconds)
    /// </remarks>
    public int MaxDelayMs { get; init; } = 30000;

    /// <summary>
    /// Whether to retry on all exceptions or only on specific transient failures.
    /// </summary>
    /// <remarks>
    /// When false, retries exceptions that are classified as transient and the known transient types:
    /// TimeoutException, HttpRequestException, IOException.
    /// When true, also retries exceptions the <c>IErrorClassifier</c> cannot classify (<c>Unknown</c>).
    /// An exception classified <c>Permanent</c> is never retried, with or without this flag; use a custom
    /// <c>IErrorClassifier</c> registered in DI to change a classification.
    /// Default: false (only transient failures)
    /// </remarks>
    public bool RetryOnAllExceptions { get; init; }

    /// <summary>
    /// Whether an ambiguous failure of a non-idempotent send may be retried automatically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An outbound call with a non-idempotent method (POST, PATCH) that fails ambiguously (500, 502, 504, a timeout
    /// or a broken connection after the request was sent) may already have been processed, so retrying it can
    /// duplicate the effect. By default it is returned to the caller instead of being retried. Set this only for a
    /// provider that deduplicates; a request that carries an idempotency key is retried without it.
    /// </para>
    /// <para>
    /// 429 and the responses that prove the request was not processed (503, 408, a connection that never opened)
    /// are retried regardless. Default: false.
    /// </para>
    /// </remarks>
    public bool RetryNonIdempotent { get; init; }
}

/// <summary>
/// Backoff strategy for retry delays.
/// </summary>
public enum BackoffType
{
    /// <summary>
    /// Constant delay between retries (fixed interval).
    /// </summary>
    /// <example>
    /// Delay: 1s, 1s, 1s
    /// </example>
    Constant,

    /// <summary>
    /// Linearly increasing delay.
    /// </summary>
    /// <example>
    /// Delay: 1s, 2s, 3s
    /// </example>
    Linear,

    /// <summary>
    /// Exponentially increasing delay (recommended).
    /// </summary>
    /// <example>
    /// Delay: 1s, 2s, 4s, 8s
    /// </example>
    Exponential
}
