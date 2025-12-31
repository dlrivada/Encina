using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

namespace Encina.Extensions.Resilience;

/// <summary>
/// Configuration options for Encina standard resilience behavior.
/// </summary>
/// <remarks>
/// Provides configuration for the standard resilience pipeline which includes:
/// - Rate limiter (default: 1,000 permits)
/// - Total timeout (default: 30 seconds)
/// - Retry with exponential backoff (default: 3 attempts)
/// - Circuit breaker (default: 10% failure threshold)
/// - Attempt timeout (default: 10 seconds per attempt)
/// </remarks>
public sealed class StandardResilienceOptions
{
    private RateLimiterStrategyOptions _rateLimiter = new()
    {
        // 1000 requests per 30 seconds
    };

    private TimeoutStrategyOptions _totalRequestTimeout = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private RetryStrategyOptions _retry = new()
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = Polly.DelayBackoffType.Exponential,
        UseJitter = true
    };

    private CircuitBreakerStrategyOptions _circuitBreaker = new()
    {
        FailureRatio = 0.1, // 10%
        MinimumThroughput = 10,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(5)
    };

    private TimeoutStrategyOptions _attemptTimeout = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// Gets or sets the rate limiter options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public RateLimiterStrategyOptions RateLimiter
    {
        get => _rateLimiter;
        set => _rateLimiter = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the total request timeout options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public TimeoutStrategyOptions TotalRequestTimeout
    {
        get => _totalRequestTimeout;
        set => _totalRequestTimeout = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the retry strategy options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public RetryStrategyOptions Retry
    {
        get => _retry;
        set => _retry = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public CircuitBreakerStrategyOptions CircuitBreaker
    {
        get => _circuitBreaker;
        set => _circuitBreaker = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the per-attempt timeout options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public TimeoutStrategyOptions AttemptTimeout
    {
        get => _attemptTimeout;
        set => _attemptTimeout = value ?? throw new ArgumentNullException(nameof(value));
    }
}
