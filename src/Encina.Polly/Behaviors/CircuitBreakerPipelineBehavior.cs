using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Encina.Polly;

/// <summary>
/// Pipeline behavior that implements circuit breaker pattern using Polly.
/// Prevents cascading failures by temporarily blocking requests after repeated failures.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed partial class CircuitBreakerPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<CircuitBreakerPipelineBehavior<TRequest, TResponse>> _logger;
    private static readonly Dictionary<Type, object> _circuitBreakerCache = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public CircuitBreakerPipelineBehavior(ILogger<CircuitBreakerPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check if request has [CircuitBreaker] attribute
        var circuitBreakerAttribute = typeof(TRequest).GetCustomAttributes(typeof(CircuitBreakerAttribute), true)
            .FirstOrDefault() as CircuitBreakerAttribute;

        if (circuitBreakerAttribute is null)
        {
            // No circuit breaker policy configured, pass through
            return await nextStep().ConfigureAwait(false);
        }

        // Get or create circuit breaker for this request type
        var pipeline = GetOrCreateCircuitBreaker(circuitBreakerAttribute);

        // Execute with circuit breaker
        try
        {
            return await pipeline.ExecuteAsync(async ct => await nextStep().ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        catch (BrokenCircuitException ex)
        {
            LogCircuitBreakerOpen(_logger, typeof(TRequest).Name, ex);
            return MediatorError.New(
                $"Circuit breaker is open for {typeof(TRequest).Name}. Service temporarily unavailable.");
        }
        catch (Exception ex)
        {
            LogCircuitBreakerExecutionFailed(_logger, typeof(TRequest).Name, ex);
            return MediatorError.New(ex);
        }
    }

    private ResiliencePipeline<Either<MediatorError, TResponse>> GetOrCreateCircuitBreaker(
        CircuitBreakerAttribute config)
    {
        var requestType = typeof(TRequest);

        // Check cache first (thread-safe read)
        if (_circuitBreakerCache.TryGetValue(requestType, out var cached))
        {
            return (ResiliencePipeline<Either<MediatorError, TResponse>>)cached;
        }

        // Create new circuit breaker (thread-safe write)
        lock (_lock)
        {
            // Double-check after acquiring lock
            if (_circuitBreakerCache.TryGetValue(requestType, out var cachedAfterLock))
            {
                return (ResiliencePipeline<Either<MediatorError, TResponse>>)cachedAfterLock;
            }

            var pipeline = BuildCircuitBreakerPipeline(config);
            _circuitBreakerCache[requestType] = pipeline;
            return pipeline;
        }
    }

    private ResiliencePipeline<Either<MediatorError, TResponse>> BuildCircuitBreakerPipeline(
        CircuitBreakerAttribute config)
    {
        var requestType = typeof(TRequest).Name;

        return new ResiliencePipelineBuilder<Either<MediatorError, TResponse>>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<Either<MediatorError, TResponse>>
            {
                FailureRatio = config.FailureRateThreshold,
                MinimumThroughput = config.MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(config.SamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(config.DurationOfBreakSeconds),
                ShouldHandle = new PredicateBuilder<Either<MediatorError, TResponse>>()
                    .HandleResult(result => result.IsLeft)
                    .Handle<Exception>(),
                OnOpened = args =>
                {
                    LogCircuitBreakerStateOpened(_logger, requestType, config.DurationOfBreakSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    LogCircuitBreakerStateClosed(_logger, requestType);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    LogCircuitBreakerStateHalfOpened(_logger, requestType);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Circuit breaker is open for {RequestType}. Request blocked.")]
    private static partial void LogCircuitBreakerOpen(
        ILogger logger,
        string requestType,
        Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Circuit breaker execution failed for {RequestType}")]
    private static partial void LogCircuitBreakerExecutionFailed(
        ILogger logger,
        string requestType,
        Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Circuit breaker OPENED for {RequestType}. Break duration: {BreakDuration}s")]
    private static partial void LogCircuitBreakerStateOpened(
        ILogger logger,
        string requestType,
        int breakDuration);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Circuit breaker CLOSED for {RequestType}. System recovered.")]
    private static partial void LogCircuitBreakerStateClosed(
        ILogger logger,
        string requestType);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Circuit breaker HALF-OPEN for {RequestType}. Testing if system recovered...")]
    private static partial void LogCircuitBreakerStateHalfOpened(
        ILogger logger,
        string requestType);
}
