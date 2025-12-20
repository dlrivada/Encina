namespace SimpleMediator.Polly;

/// <summary>
/// Configures circuit breaker policy for a request using Polly.
/// Prevents cascading failures by temporarily blocking requests after repeated failures.
/// </summary>
/// <remarks>
/// Circuit breaker states:
/// - Closed: Normal operation, requests flow through
/// - Open: Circuit is broken, requests fail immediately
/// - Half-Open: Testing if the system has recovered
/// </remarks>
/// <example>
/// <code>
/// [CircuitBreaker(FailureThreshold = 5, DurationOfBreakSeconds = 60)]
/// public record CallUnreliableServiceQuery(string ServiceId) : IRequest&lt;ServiceResponse&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CircuitBreakerAttribute : Attribute
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    /// <remarks>
    /// Default: 5 failures
    /// </remarks>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>
    /// Sampling duration for calculating failure rate (in seconds).
    /// </summary>
    /// <remarks>
    /// Circuit breaker tracks failures within this window.
    /// Default: 60 seconds
    /// </remarks>
    public int SamplingDurationSeconds { get; init; } = 60;

    /// <summary>
    /// Minimum number of requests in sampling duration before circuit can break.
    /// Prevents opening circuit on low traffic.
    /// </summary>
    /// <remarks>
    /// Default: 10 requests
    /// </remarks>
    public int MinimumThroughput { get; init; } = 10;

    /// <summary>
    /// Duration the circuit stays open before transitioning to half-open (in seconds).
    /// </summary>
    /// <remarks>
    /// After this period, the circuit allows one request through to test if the system recovered.
    /// Default: 30 seconds
    /// </remarks>
    public int DurationOfBreakSeconds { get; init; } = 30;

    /// <summary>
    /// Failure rate threshold (0.0 to 1.0) to open the circuit.
    /// </summary>
    /// <remarks>
    /// If FailureRate > FailureRateThreshold, circuit opens.
    /// Default: 0.5 (50% failure rate)
    /// </remarks>
    public double FailureRateThreshold { get; init; } = 0.5;
}
