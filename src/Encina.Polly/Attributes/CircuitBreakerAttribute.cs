namespace Encina.Polly;

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
    private const string MustBeGreaterThanZero = "Value must be greater than 0.";

    private int _failureThreshold = 5;
    private int _samplingDurationSeconds = 60;
    private int _minimumThroughput = 10;
    private int _durationOfBreakSeconds = 30;
    private double _failureRateThreshold = 0.5;

    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    /// <remarks>
    /// Default: 5 failures. Must be greater than 0.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0.</exception>
    public int FailureThreshold
    {
        get => _failureThreshold;
        init => _failureThreshold = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(FailureThreshold), value, MustBeGreaterThanZero);
    }

    /// <summary>
    /// Sampling duration for calculating failure rate (in seconds).
    /// </summary>
    /// <remarks>
    /// Circuit breaker tracks failures within this window.
    /// Default: 60 seconds. Must be greater than 0.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0.</exception>
    public int SamplingDurationSeconds
    {
        get => _samplingDurationSeconds;
        init => _samplingDurationSeconds = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(SamplingDurationSeconds), value, MustBeGreaterThanZero);
    }

    /// <summary>
    /// Minimum number of requests in sampling duration before circuit can break.
    /// Prevents opening circuit on low traffic.
    /// </summary>
    /// <remarks>
    /// Default: 10 requests. Must be greater than 0.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0.</exception>
    public int MinimumThroughput
    {
        get => _minimumThroughput;
        init => _minimumThroughput = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MinimumThroughput), value, MustBeGreaterThanZero);
    }

    /// <summary>
    /// Duration the circuit stays open before transitioning to half-open (in seconds).
    /// </summary>
    /// <remarks>
    /// After this period, the circuit allows one request through to test if the system recovered.
    /// Default: 30 seconds. Must be greater than 0.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0.</exception>
    public int DurationOfBreakSeconds
    {
        get => _durationOfBreakSeconds;
        init => _durationOfBreakSeconds = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(DurationOfBreakSeconds), value, MustBeGreaterThanZero);
    }

    /// <summary>
    /// Failure rate threshold (0.0 to 1.0) to open the circuit.
    /// </summary>
    /// <remarks>
    /// If FailureRate > FailureRateThreshold, circuit opens.
    /// Default: 0.5 (50% failure rate). Must be between 0.0 and 1.0 inclusive.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside [0.0, 1.0] range.</exception>
    public double FailureRateThreshold
    {
        get => _failureRateThreshold;
        init => _failureRateThreshold = value is >= 0.0 and <= 1.0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(FailureRateThreshold), value, "Value must be between 0.0 and 1.0.");
    }
}
