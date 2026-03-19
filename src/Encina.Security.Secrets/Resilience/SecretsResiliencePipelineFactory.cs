using Encina.Security.Secrets.Diagnostics;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Encina.Security.Secrets.Resilience;

/// <summary>
/// Factory that builds a <see cref="ResiliencePipeline"/> configured for secret operations.
/// </summary>
/// <remarks>
/// The pipeline applies strategies in the following order (outermost to innermost):
/// <list type="number">
/// <item><description>Total operation timeout — caps total execution time</description></item>
/// <item><description>Retry — exponential backoff with jitter for transient failures</description></item>
/// <item><description>Circuit breaker — prevents cascading failures when provider is down</description></item>
/// </list>
/// </remarks>
internal static class SecretsResiliencePipelineFactory
{
    /// <summary>
    /// Creates a <see cref="ResiliencePipeline"/> from the specified options.
    /// </summary>
    /// <param name="options">The resilience configuration.</param>
    /// <param name="circuitBreakerState">The circuit breaker state tracker for health check integration.</param>
    /// <param name="logger">The logger for resilience events.</param>
    /// <param name="metrics">Optional metrics recorder for resilience telemetry.</param>
    /// <returns>A configured <see cref="ResiliencePipeline"/>.</returns>
    public static ResiliencePipeline Create(
        SecretsResilienceOptions options,
        SecretsCircuitBreakerState circuitBreakerState,
        ILogger logger,
        SecretsMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(circuitBreakerState);
        ArgumentNullException.ThrowIfNull(logger);

        var builder = new ResiliencePipelineBuilder();

        // Layer 1 (outermost): Total operation timeout
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = options.OperationTimeout,
            OnTimeout = args =>
            {
                Log.ResilienceTimeoutExceeded(logger, options.OperationTimeout.TotalSeconds);
                metrics?.RecordTimeout(options.OperationTimeout.TotalSeconds);
                SecretsActivitySource.RecordTimeoutEvent(
                    System.Diagnostics.Activity.Current,
                    options.OperationTimeout.TotalSeconds);
                return default;
            }
        });

        // Layer 2: Retry with exponential backoff and jitter
        if (options.MaxRetryAttempts > 0)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = options.RetryBaseDelay,
                MaxDelay = options.RetryMaxDelay,
                ShouldHandle = new PredicateBuilder()
                    .Handle<TransientSecretException>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<IOException>()
                    .Handle<System.Net.Sockets.SocketException>(),
                OnRetry = args =>
                {
                    var attemptNumber = args.AttemptNumber + 1;
                    var reason = args.Outcome.Exception?.Message ?? "Transient error";

                    Log.ResilienceRetryAttempt(
                        logger,
                        attemptNumber,
                        options.MaxRetryAttempts,
                        args.RetryDelay.TotalMilliseconds,
                        reason);

                    metrics?.RecordRetry(attemptNumber, reason);
                    SecretsActivitySource.RecordRetryEvent(
                        System.Diagnostics.Activity.Current,
                        attemptNumber,
                        options.MaxRetryAttempts,
                        args.RetryDelay.TotalMilliseconds,
                        reason);
                    return default;
                }
            });
        }

        // Layer 3 (innermost): Circuit breaker
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            SamplingDuration = options.CircuitBreakerSamplingDuration,
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            BreakDuration = options.CircuitBreakerBreakDuration,
            ShouldHandle = new PredicateBuilder()
                .Handle<TransientSecretException>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>()
                .Handle<IOException>()
                .Handle<System.Net.Sockets.SocketException>(),
            OnOpened = args =>
            {
                circuitBreakerState.SetOpened();
                Log.ResilienceCircuitBreakerOpened(logger);
                metrics?.RecordCircuitBreakerTransition("opened");
                SecretsActivitySource.RecordCircuitBreakerEvent(
                    System.Diagnostics.Activity.Current, "opened");
                return default;
            },
            OnClosed = args =>
            {
                circuitBreakerState.SetClosed();
                Log.ResilienceCircuitBreakerClosed(logger);
                metrics?.RecordCircuitBreakerTransition("closed");
                SecretsActivitySource.RecordCircuitBreakerEvent(
                    System.Diagnostics.Activity.Current, "closed");
                return default;
            },
            OnHalfOpened = args =>
            {
                circuitBreakerState.SetHalfOpen();
                Log.ResilienceCircuitBreakerHalfOpen(logger);
                metrics?.RecordCircuitBreakerTransition("half_open");
                SecretsActivitySource.RecordCircuitBreakerEvent(
                    System.Diagnostics.Activity.Current, "half_open");
                return default;
            }
        });

        return builder.Build();
    }
}
