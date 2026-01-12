using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Encina.Extensions.Resilience;

/// <summary>
/// Pipeline behavior that applies Microsoft's Standard Resilience Handler to requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Uses Microsoft.Extensions.Resilience standard resilience pipeline which includes:
/// - Rate limiter (1,000 permits by default)
/// - Total timeout (30 seconds by default)
/// - Retry with exponential backoff (3 attempts by default)
/// - Circuit breaker (10% failure threshold by default)
/// - Attempt timeout (10 seconds per attempt by default)
///
/// Configure via options in AddEncinaStandardResilience().
/// </remarks>
public sealed partial class StandardResiliencePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ILogger<StandardResiliencePipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardResiliencePipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="pipelineProvider">The resilience pipeline provider.</param>
    /// <param name="logger">The logger instance.</param>
    public StandardResiliencePipelineBehavior(
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<StandardResiliencePipelineBehavior<TRequest, TResponse>> logger)
    {
        _pipelineProvider = pipelineProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the request through the standard resilience pipeline.
    /// </summary>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var pipeline = _pipelineProvider.GetPipeline(requestType);

        try
        {
            LogExecutingWithResilience(requestType, context.CorrelationId);

            var result = await pipeline.ExecuteAsync(
                async ct =>
                {
                    var response = await nextStep().ConfigureAwait(false);

                    // If the response is a failure (Left), throw to trigger resilience strategies
                    return response.Match(
                        Right: r => r,
                        Left: error => throw new EncinaResilienceException(error)
                    );
                },
                cancellationToken).ConfigureAwait(false);

            LogResilienceSucceeded(requestType, context.CorrelationId);
            return result;
        }
        catch (EncinaResilienceException ex)
        {
            // This is an expected business error wrapped for resilience, return it as-is
            LogResilienceReturnedError(requestType, ex.Error.Message, context.CorrelationId);
            return ex.Error;
        }
        catch (BrokenCircuitException ex)
        {
            LogCircuitBreakerOpen(requestType, ex.Message, context.CorrelationId);
            return EncinaError.New(
                $"Circuit breaker is open for {requestType}. The service is temporarily unavailable.",
                ex);
        }
        catch (TimeoutRejectedException ex)
        {
            LogTimeoutOccurred(requestType, context.CorrelationId);
            return EncinaError.New(
                $"Request {requestType} timed out after exceeding the configured timeout period.",
                ex);
        }
        catch (Exception ex)
        {
            LogResilienceFailed(requestType, ex.Message, context.CorrelationId);
            return EncinaError.New(ex);
        }
    }

    #region Logging

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Executing request {RequestType} with standard resilience (CorrelationId: {CorrelationId})")]
    private partial void LogExecutingWithResilience(string requestType, string correlationId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Standard resilience succeeded for {RequestType} (CorrelationId: {CorrelationId})")]
    private partial void LogResilienceSucceeded(string requestType, string correlationId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Standard resilience returned error for {RequestType}: {ErrorMessage} (CorrelationId: {CorrelationId})")]
    private partial void LogResilienceReturnedError(string requestType, string errorMessage, string correlationId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Circuit breaker is open for {RequestType}: {Message} (CorrelationId: {CorrelationId})")]
    private partial void LogCircuitBreakerOpen(string requestType, string message, string correlationId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Request {RequestType} timed out (CorrelationId: {CorrelationId})")]
    private partial void LogTimeoutOccurred(string requestType, string correlationId);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Standard resilience failed for {RequestType}: {ErrorMessage} (CorrelationId: {CorrelationId})")]
    private partial void LogResilienceFailed(string requestType, string errorMessage, string correlationId);

    #endregion
}

/// <summary>
/// Exception thrown internally to propagate EncinaError through resilience strategies.
/// </summary>
/// <remarks>
/// This allows the resilience pipeline to treat business errors (Left in Either)
/// as exceptions that can be retried, circuit-broken, etc.
/// This exception is intentionally internal as it is an implementation detail
/// of the resilience pipeline and should not be exposed to consumers.
/// </remarks>
#pragma warning disable S3871 // Exception types should be "public" - Intentionally internal for encapsulation
internal sealed class EncinaResilienceException : Exception
{
    /// <summary>
    /// Gets the Encina error that caused this exception.
    /// </summary>
    public EncinaError Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaResilienceException"/> class.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    public EncinaResilienceException(EncinaError error)
        : base(error.Message)
    {
        Error = error;
    }
}
