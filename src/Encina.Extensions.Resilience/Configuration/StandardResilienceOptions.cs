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
    /// <summary>
    /// Gets or sets the rate limiter options.
    /// </summary>
    public RateLimiterStrategyOptions RateLimiter { get; set; } = new()
    {
        // 1000 requests per 30 seconds
    };

    /// <summary>
    /// Gets or sets the total request timeout options.
    /// </summary>
    public TimeoutStrategyOptions TotalRequestTimeout { get; set; } = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Gets or sets the retry strategy options.
    /// </summary>
    public RetryStrategyOptions Retry { get; set; } = new()
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = Polly.DelayBackoffType.Exponential,
        UseJitter = true
    };

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    public CircuitBreakerStrategyOptions CircuitBreaker { get; set; } = new()
    {
        FailureRatio = 0.1, // 10%
        MinimumThroughput = 10,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(5)
    };

    /// <summary>
    /// Gets or sets the per-attempt timeout options.
    /// </summary>
    public TimeoutStrategyOptions AttemptTimeout { get; set; } = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
}
