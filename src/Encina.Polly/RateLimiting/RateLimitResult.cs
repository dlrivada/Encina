namespace Encina.Polly;

/// <summary>
/// Result of a rate limit acquisition attempt.
/// </summary>
/// <param name="IsAllowed">Whether the request is allowed to proceed.</param>
/// <param name="CurrentState">Current state of the rate limiter.</param>
/// <param name="RetryAfter">Suggested time to wait before retrying (if denied).</param>
/// <param name="CurrentCount">Current request count in the window.</param>
/// <param name="CurrentLimit">Current effective limit (may be reduced during throttling).</param>
/// <param name="ErrorRate">Current error rate percentage (0-100).</param>
public readonly record struct RateLimitResult(
    bool IsAllowed,
    RateLimitState CurrentState,
    TimeSpan? RetryAfter,
    int CurrentCount,
    int CurrentLimit,
    double ErrorRate)
{
    /// <summary>
    /// Creates a successful (allowed) result.
    /// </summary>
    public static RateLimitResult Allowed(
        RateLimitState state,
        int currentCount,
        int currentLimit,
        double errorRate) =>
        new(
            IsAllowed: true,
            CurrentState: state,
            RetryAfter: null,
            CurrentCount: currentCount,
            CurrentLimit: currentLimit,
            ErrorRate: errorRate);

    /// <summary>
    /// Creates a denied result with retry information.
    /// </summary>
    public static RateLimitResult Denied(
        RateLimitState state,
        TimeSpan retryAfter,
        int currentCount,
        int currentLimit,
        double errorRate) =>
        new(
            IsAllowed: false,
            CurrentState: state,
            RetryAfter: retryAfter,
            CurrentCount: currentCount,
            CurrentLimit: currentLimit,
            ErrorRate: errorRate);
}
