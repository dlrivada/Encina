using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Polly;

/// <summary>
/// Pipeline behavior that implements adaptive rate limiting.
/// Automatically rate limits requests based on <see cref="RateLimitAttribute"/> configuration.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior provides:
/// </para>
/// <list type="bullet">
/// <item><description>Sliding window rate limiting</description></item>
/// <item><description>Automatic outage detection via error rate monitoring</description></item>
/// <item><description>Adaptive throttling when errors exceed threshold</description></item>
/// <item><description>Gradual recovery with configurable ramp-up</description></item>
/// </list>
/// </remarks>
public sealed partial class RateLimitingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RateLimitingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IRateLimiter _rateLimiter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="rateLimiter">Rate limiter instance.</param>
    public RateLimitingPipelineBehavior(
        ILogger<RateLimitingPipelineBehavior<TRequest, TResponse>> logger,
        IRateLimiter rateLimiter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check if request has [RateLimit] attribute
        if (typeof(TRequest).GetCustomAttributes(typeof(RateLimitAttribute), true)
            .FirstOrDefault() is not RateLimitAttribute rateLimitAttribute)
        {
            // No rate limit policy configured, pass through
            return await nextStep().ConfigureAwait(false);
        }

        var requestType = typeof(TRequest).Name;
        var key = requestType;

        // Try to acquire a permit
        var acquireResult = await _rateLimiter.AcquireAsync(key, rateLimitAttribute, cancellationToken)
            .ConfigureAwait(false);

        if (!acquireResult.IsAllowed)
        {
            LogRateLimitExceeded(
                _logger,
                requestType,
                acquireResult.CurrentCount,
                acquireResult.CurrentLimit,
                acquireResult.CurrentState.ToString(),
                acquireResult.RetryAfter?.TotalSeconds ?? 0);

            var errorMessage = $"Rate limit exceeded for {requestType}. " +
                               $"Current: {acquireResult.CurrentCount}/{acquireResult.CurrentLimit}. " +
                               $"State: {acquireResult.CurrentState}. " +
                               $"Retry after: {acquireResult.RetryAfter?.TotalSeconds:F1}s.";

            return EncinaError.New(errorMessage);
        }

        LogRateLimitAcquired(
            _logger,
            requestType,
            acquireResult.CurrentCount,
            acquireResult.CurrentLimit,
            acquireResult.CurrentState.ToString());

        // Execute the handler
        Either<EncinaError, TResponse> result;
        try
        {
            result = await nextStep().ConfigureAwait(false);
        }
        catch
        {
            // Record failure for adaptive adjustment
            _rateLimiter.RecordFailure(key);
            throw;
        }

        // Record success or failure for adaptive adjustment
        if (result.IsRight)
        {
            _rateLimiter.RecordSuccess(key);
        }
        else
        {
            _rateLimiter.RecordFailure(key);

            // Log state change if error threshold might be reached
            var state = _rateLimiter.GetState(key);
            if (state == RateLimitState.Throttled)
            {
                LogStateTransitionToThrottled(_logger, requestType, acquireResult.ErrorRate);
            }
        }

        return result;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for {RequestType}. Current: {CurrentCount}/{CurrentLimit}. State: {State}. Retry after: {RetryAfterSeconds}s")]
    private static partial void LogRateLimitExceeded(
        ILogger logger,
        string requestType,
        int currentCount,
        int currentLimit,
        string state,
        double retryAfterSeconds);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Rate limit acquired for {RequestType}. Current: {CurrentCount}/{CurrentLimit}. State: {State}")]
    private static partial void LogRateLimitAcquired(
        ILogger logger,
        string requestType,
        int currentCount,
        int currentLimit,
        string state);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Rate limiter transitioned to Throttled state for {RequestType}. Error rate: {ErrorRate:F1}%")]
    private static partial void LogStateTransitionToThrottled(
        ILogger logger,
        string requestType,
        double errorRate);
}
