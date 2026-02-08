using System.Collections.Concurrent;
using Encina.Database;
using Encina.Polly.Predicates;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Encina.Polly;

/// <summary>
/// Pipeline behavior that implements a database-aware circuit breaker using Polly.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/> which is attribute-driven
/// and per-request-type, this behavior integrates with the database resilience infrastructure
/// defined in <see cref="DatabaseResilienceOptions"/> and <see cref="IDatabaseHealthMonitor"/>.
/// </para>
/// <para>
/// The circuit breaker is configured via <see cref="DatabaseCircuitBreakerOptions"/> and uses
/// <see cref="DatabaseTransientErrorPredicate"/> to identify transient database errors.
/// Circuit breaker instances are cached per provider name using a static
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> for thread-safe reuse.
/// </para>
/// <para>
/// When the circuit is open, the behavior reports the state to <see cref="IDatabaseHealthMonitor"/>
/// and returns an <see cref="EncinaError"/> immediately without attempting the operation.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
/// <example>
/// <code>
/// // Register in DI
/// services.AddEncinaPolly();
/// services.AddDatabaseCircuitBreaker(options =>
/// {
///     options.FailureThreshold = 0.3;
///     options.BreakDuration = TimeSpan.FromMinutes(1);
///     options.MinimumThroughput = 20;
/// });
/// </code>
/// </example>
public sealed partial class DatabaseCircuitBreakerPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDatabaseHealthMonitor _healthMonitor;
    private readonly DatabaseCircuitBreakerOptions _options;
    private readonly DatabaseTransientErrorPredicate _predicate;
    private readonly ILogger<DatabaseCircuitBreakerPipelineBehavior<TRequest, TResponse>> _logger;

    private static readonly ConcurrentDictionary<string, ResiliencePipeline<Either<EncinaError, TResponse>>> _circuitBreakerCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseCircuitBreakerPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="healthMonitor">The database health monitor for circuit state reporting.</param>
    /// <param name="options">Circuit breaker configuration options.</param>
    /// <param name="predicate">Predicate to identify transient database errors.</param>
    /// <param name="logger">Logger instance.</param>
    public DatabaseCircuitBreakerPipelineBehavior(
        IDatabaseHealthMonitor healthMonitor,
        DatabaseCircuitBreakerOptions options,
        DatabaseTransientErrorPredicate predicate,
        ILogger<DatabaseCircuitBreakerPipelineBehavior<TRequest, TResponse>> logger)
    {
        _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Fast path: if the circuit is already open via the health monitor, fail fast
        if (_healthMonitor.IsCircuitOpen)
        {
            var providerName = _healthMonitor.ProviderName;
            LogCircuitBreakerOpenFastPath(_logger, typeof(TRequest).Name, providerName);
            return EncinaError.New(
                $"Database circuit breaker is open for provider '{providerName}'. " +
                "Service temporarily unavailable. Please retry later.");
        }

        // Get or create the resilience pipeline for this provider
        var pipeline = GetOrCreateCircuitBreaker();

        try
        {
            return await pipeline.ExecuteAsync(
                async ct => await nextStep().ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }
        catch (BrokenCircuitException ex)
        {
            var providerName = _healthMonitor.ProviderName;
            LogCircuitBreakerTripped(_logger, typeof(TRequest).Name, providerName, ex);
            return EncinaError.New(
                $"Database circuit breaker tripped for provider '{providerName}'. " +
                $"Too many failures detected. Break duration: {_options.BreakDuration.TotalSeconds}s.");
        }
        catch (Exception ex) when (_predicate.IsTransient(ex))
        {
            var providerName = _healthMonitor.ProviderName;
            LogTransientDatabaseError(_logger, typeof(TRequest).Name, providerName, ex);
            return EncinaError.New(ex);
        }
    }

    private ResiliencePipeline<Either<EncinaError, TResponse>> GetOrCreateCircuitBreaker()
    {
        var cacheKey = _healthMonitor.ProviderName;

        return _circuitBreakerCache.GetOrAdd(cacheKey, _ => BuildCircuitBreakerPipeline());
    }

    private ResiliencePipeline<Either<EncinaError, TResponse>> BuildCircuitBreakerPipeline()
    {
        var providerName = _healthMonitor.ProviderName;

        return new ResiliencePipelineBuilder<Either<EncinaError, TResponse>>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<Either<EncinaError, TResponse>>
            {
                FailureRatio = _options.FailureThreshold,
                MinimumThroughput = _options.MinimumThroughput,
                SamplingDuration = _options.SamplingDuration,
                BreakDuration = _options.BreakDuration,
                ShouldHandle = new PredicateBuilder<Either<EncinaError, TResponse>>()
                    .HandleResult(result => result.IsLeft)
                    .Handle<Exception>(ex => _predicate.IsTransient(ex)),
                OnOpened = args =>
                {
                    LogCircuitBreakerStateOpened(_logger, providerName, _options.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    LogCircuitBreakerStateClosed(_logger, providerName);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    LogCircuitBreakerStateHalfOpened(_logger, providerName);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Warning,
        Message = "Database circuit breaker is open for {RequestType} on provider '{ProviderName}'. Request blocked (fast path).")]
    private static partial void LogCircuitBreakerOpenFastPath(
        ILogger logger,
        string requestType,
        string providerName);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Database circuit breaker tripped for {RequestType} on provider '{ProviderName}'.")]
    private static partial void LogCircuitBreakerTripped(
        ILogger logger,
        string requestType,
        string providerName,
        Exception exception);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Warning,
        Message = "Transient database error for {RequestType} on provider '{ProviderName}'.")]
    private static partial void LogTransientDatabaseError(
        ILogger logger,
        string requestType,
        string providerName,
        Exception exception);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Warning,
        Message = "Database circuit breaker OPENED for provider '{ProviderName}'. Break duration: {BreakDurationSeconds}s.")]
    private static partial void LogCircuitBreakerStateOpened(
        ILogger logger,
        string providerName,
        double breakDurationSeconds);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Information,
        Message = "Database circuit breaker CLOSED for provider '{ProviderName}'. Database recovered.")]
    private static partial void LogCircuitBreakerStateClosed(
        ILogger logger,
        string providerName);

    [LoggerMessage(
        EventId = 105,
        Level = LogLevel.Information,
        Message = "Database circuit breaker HALF-OPEN for provider '{ProviderName}'. Testing if database recovered...")]
    private static partial void LogCircuitBreakerStateHalfOpened(
        ILogger logger,
        string providerName);
}
