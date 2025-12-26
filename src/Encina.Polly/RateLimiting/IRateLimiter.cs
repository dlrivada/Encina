namespace Encina.Polly;

/// <summary>
/// Abstraction for rate limiting with adaptive throttling capabilities.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to acquire a permit for the specified key.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit bucket (typically request type name).</param>
    /// <param name="config">Rate limit configuration from attribute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the request is allowed and current state.</returns>
    ValueTask<RateLimitResult> AcquireAsync(
        string key,
        RateLimitAttribute config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful request execution for adaptive adjustment.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit bucket.</param>
    void RecordSuccess(string key);

    /// <summary>
    /// Records a failed request execution for adaptive adjustment.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit bucket.</param>
    void RecordFailure(string key);

    /// <summary>
    /// Gets the current state of the rate limiter for the specified key.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit bucket.</param>
    /// <returns>Current state, or null if no state exists for the key.</returns>
    RateLimitState? GetState(string key);

    /// <summary>
    /// Resets the rate limiter state for the specified key.
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit bucket.</param>
    void Reset(string key);
}
