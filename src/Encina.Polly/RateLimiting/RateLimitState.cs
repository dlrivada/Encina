namespace Encina.Polly;

/// <summary>
/// Represents the current state of the adaptive rate limiter.
/// </summary>
public enum RateLimitState
{
    /// <summary>
    /// Normal operation. Full capacity is available.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Throttled due to high error rate. Capacity is reduced.
    /// </summary>
    Throttled = 1,

    /// <summary>
    /// Recovering from throttled state. Capacity is gradually increasing.
    /// </summary>
    Recovering = 2
}
