namespace Encina.Database;

/// <summary>
/// Configuration options for the database-aware circuit breaker.
/// </summary>
/// <remarks>
/// <para>
/// The circuit breaker monitors database operation failures and opens the circuit
/// when the failure rate exceeds the configured threshold, preventing further
/// operations from reaching an unhealthy database.
/// </para>
/// <para>
/// This follows the same circuit breaker semantics as Polly and
/// <c>Microsoft.Extensions.Resilience</c>: when the failure ratio exceeds
/// <see cref="FailureThreshold"/> within <see cref="SamplingDuration"/>,
/// the circuit opens for <see cref="BreakDuration"/> before transitioning
/// to half-open for a probe attempt.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new DatabaseCircuitBreakerOptions
/// {
///     FailureThreshold = 0.3,                     // Open at 30% failures
///     SamplingDuration = TimeSpan.FromSeconds(15), // Measure over 15 seconds
///     BreakDuration = TimeSpan.FromMinutes(1),     // Stay open for 1 minute
///     MinimumThroughput = 20                       // Require 20 calls before evaluating
/// };
/// </code>
/// </example>
public sealed class DatabaseCircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the failure rate threshold at which the circuit breaker opens.
    /// </summary>
    /// <remarks>
    /// A value between 0 and 1 representing the percentage of failures.
    /// For example, 0.5 means the circuit opens when 50% of operations fail.
    /// </remarks>
    /// <value>Default: 0.5 (50% failure rate).</value>
    public double FailureThreshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the time window over which failure rates are measured.
    /// </summary>
    /// <value>Default: 10 seconds.</value>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets how long the circuit remains open before transitioning to half-open.
    /// </summary>
    /// <remarks>
    /// After this duration, a single probe operation is allowed through.
    /// If it succeeds, the circuit closes; if it fails, the circuit remains open.
    /// </remarks>
    /// <value>Default: 30 seconds.</value>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the minimum number of operations required within the
    /// <see cref="SamplingDuration"/> before the failure rate is evaluated.
    /// </summary>
    /// <remarks>
    /// This prevents the circuit from opening due to a small number of failures
    /// during low-traffic periods.
    /// </remarks>
    /// <value>Default: 10.</value>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether timeout exceptions count as failures for circuit breaker evaluation.
    /// </summary>
    /// <value>Default: true.</value>
    public bool IncludeTimeouts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether connection failures (e.g., <c>SocketException</c>,
    /// connection refused) count as failures for circuit breaker evaluation.
    /// </summary>
    /// <value>Default: true.</value>
    public bool IncludeConnectionFailures { get; set; } = true;
}
