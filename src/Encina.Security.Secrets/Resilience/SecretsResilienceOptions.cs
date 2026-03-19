namespace Encina.Security.Secrets.Resilience;

/// <summary>
/// Configuration options for the secrets resilience pipeline (retry, circuit breaker, timeout).
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="ResilientSecretReaderDecorator"/>,
/// <see cref="ResilientSecretWriterDecorator"/>, and <see cref="ResilientSecretRotatorDecorator"/>.
/// </para>
/// <para>
/// The resilience pipeline applies strategies in the following order:
/// <list type="number">
/// <item><description>Total operation timeout</description></item>
/// <item><description>Retry with exponential backoff and jitter</description></item>
/// <item><description>Circuit breaker for cascading failure prevention</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSecrets(options =>
/// {
///     options.EnableResilience = true;
///     options.Resilience.MaxRetryAttempts = 5;
///     options.Resilience.OperationTimeout = TimeSpan.FromSeconds(60);
/// });
/// </code>
/// </example>
public sealed class SecretsResilienceOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    /// <remarks>
    /// Default is 3. Set to 0 to disable retries while keeping circuit breaker and timeout.
    /// Only transient errors (e.g., <c>secrets.provider_unavailable</c>) trigger retries.
    /// </remarks>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff between retry attempts.
    /// </summary>
    /// <remarks>
    /// Default is 2 seconds. Actual delay includes jitter to prevent thundering herd.
    /// Formula: <c>baseDelay * 2^(attempt-1) + jitter</c>, capped at <see cref="RetryMaxDelay"/>.
    /// </remarks>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts.
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds. Caps the exponential backoff to prevent excessively long waits.
    /// </remarks>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the failure ratio threshold that triggers the circuit breaker.
    /// </summary>
    /// <remarks>
    /// Default is 0.5 (50%). When the failure rate exceeds this threshold within the
    /// <see cref="CircuitBreakerSamplingDuration"/>, the circuit opens and subsequent
    /// requests are rejected immediately.
    /// </remarks>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the sampling window for the circuit breaker failure rate calculation.
    /// </summary>
    /// <remarks>
    /// Default is 60 seconds. Failures are tracked within this rolling window.
    /// </remarks>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the minimum number of requests within the sampling window
    /// before the circuit breaker can trip.
    /// </summary>
    /// <remarks>
    /// Default is 10. Prevents the circuit from opening on a small number of requests
    /// (e.g., 1 out of 2 failing = 50% but not statistically significant).
    /// </remarks>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets how long the circuit breaker stays open before allowing a test request.
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds. After this duration, the circuit moves to half-open state
    /// and allows a single request through to test if the provider has recovered.
    /// </remarks>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum time allowed for a single secret operation (including retries).
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds. If the total operation exceeds this duration, it is cancelled
    /// and a <c>secrets.resilience_timeout</c> error is returned.
    /// </remarks>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum staleness duration for serving cached secrets when the provider is unavailable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to a value greater than <see cref="TimeSpan.Zero"/>, the cache will retain a
    /// "last-known-good" copy of each secret beyond the normal cache TTL. If the provider becomes
    /// unavailable (circuit breaker open, timeout, transient error), the stale cached value is
    /// returned instead of an error, provided it is not older than this duration.
    /// </para>
    /// <para>
    /// Default is 1 hour. Set to <see cref="TimeSpan.Zero"/> to disable stale fallback.
    /// Only effective when both <see cref="SecretsOptions.EnableResilience"/> and
    /// <see cref="SecretsOptions.EnableCaching"/> are <c>true</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSecrets(options =>
    /// {
    ///     options.EnableResilience = true;
    ///     options.EnableCaching = true;
    ///     options.Resilience.MaxStaleDuration = TimeSpan.FromHours(4);
    /// });
    /// </code>
    /// </example>
    public TimeSpan MaxStaleDuration { get; set; } = TimeSpan.FromHours(1);
}
