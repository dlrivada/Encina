namespace Encina.Polly;

/// <summary>
/// Configures automatic rate limiting with adaptive throttling for a request.
/// Applied to request types to enable rate limiting with outage detection and auto-throttle.
/// </summary>
/// <remarks>
/// <para>
/// The rate limiter uses a sliding window algorithm with adaptive capacity adjustment:
/// </para>
/// <list type="bullet">
/// <item><description><b>Normal state</b>: Full capacity (MaxRequestsPerWindow) is available.</description></item>
/// <item><description><b>Throttled state</b>: When error rate exceeds ErrorThresholdPercent, capacity is reduced to 10%.</description></item>
/// <item><description><b>Recovering state</b>: After CooldownSeconds, capacity gradually increases by RampUpFactor.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Basic rate limiting
/// [RateLimit(MaxRequestsPerWindow = 100, WindowSizeSeconds = 60)]
/// public record CallExternalApiQuery(string Endpoint) : IRequest&lt;ApiResponse&gt;;
///
/// // Adaptive rate limiting with custom thresholds
/// [RateLimit(
///     MaxRequestsPerWindow = 50,
///     WindowSizeSeconds = 30,
///     ErrorThresholdPercent = 25.0,
///     CooldownSeconds = 120,
///     RampUpFactor = 1.5)]
/// public record ProcessPaymentCommand(PaymentData Data) : ICommand&lt;PaymentResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Maximum number of requests allowed per time window.
    /// </summary>
    /// <remarks>
    /// Default: 100 requests.
    /// This is the full capacity in Normal state. In Throttled state, capacity is reduced to 10%.
    /// </remarks>
    public int MaxRequestsPerWindow { get; init; } = 100;

    /// <summary>
    /// Duration of the sliding window in seconds.
    /// </summary>
    /// <remarks>
    /// Default: 60 seconds (1 minute).
    /// Requests are counted within this sliding window.
    /// </remarks>
    public int WindowSizeSeconds { get; init; } = 60;

    /// <summary>
    /// Error rate percentage that triggers throttling (0-100).
    /// </summary>
    /// <remarks>
    /// Default: 50% (half of requests failing triggers throttle).
    /// When error rate exceeds this threshold, the limiter enters Throttled state.
    /// Set to 100 to disable adaptive throttling.
    /// </remarks>
    public double ErrorThresholdPercent { get; init; } = 50.0;

    /// <summary>
    /// Duration in seconds before attempting recovery from throttled state.
    /// </summary>
    /// <remarks>
    /// Default: 60 seconds.
    /// After this cooldown period in Throttled state, the limiter transitions to Recovering state.
    /// </remarks>
    public int CooldownSeconds { get; init; } = 60;

    /// <summary>
    /// Factor by which capacity increases during recovery.
    /// </summary>
    /// <remarks>
    /// Default: 2.0 (double capacity each recovery step).
    /// In Recovering state, capacity increases by this factor after each successful window.
    /// Once capacity reaches MaxRequestsPerWindow, the limiter returns to Normal state.
    /// </remarks>
    public double RampUpFactor { get; init; } = 2.0;

    /// <summary>
    /// Whether to enable adaptive throttling based on error rates.
    /// </summary>
    /// <remarks>
    /// Default: true.
    /// When disabled, only fixed rate limiting is applied (no outage detection).
    /// </remarks>
    public bool EnableAdaptiveThrottling { get; init; } = true;

    /// <summary>
    /// Minimum number of requests before error rate is calculated.
    /// </summary>
    /// <remarks>
    /// Default: 10 requests.
    /// Prevents premature throttling when sample size is too small.
    /// </remarks>
    public int MinimumThroughputForThrottling { get; init; } = 10;
}
