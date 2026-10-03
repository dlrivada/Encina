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

        AddTimeoutLayer(builder, options, logger, metrics);

        if (options.MaxRetryAttempts > 0)
        {
            AddRetryLayer(builder, options, logger, metrics);
        }

        AddCircuitBreakerLayer(builder, options, circuitBreakerState, logger, metrics);

        return builder.Build();
    }

    private static PredicateBuilder<object> TransientFailures() =>
        new PredicateBuilder()
            .Handle<TransientSecretException>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>()
            .Handle<IOException>()
            .Handle<System.Net.Sockets.SocketException>();

    // Layer 1 (outermost): Total operation timeout
    private static void AddTimeoutLayer(
        ResiliencePipelineBuilder builder,
        SecretsResilienceOptions options,
        ILogger logger,
        SecretsMetrics? metrics)
    {
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = options.OperationTimeout,
            OnTimeout = _ =>
            {
                Log.ResilienceTimeoutExceeded(logger, options.OperationTimeout.TotalSeconds);
                metrics?.RecordTimeout(options.OperationTimeout.TotalSeconds);
                SecretsActivitySource.RecordTimeoutEvent(
                    System.Diagnostics.Activity.Current,
                    options.OperationTimeout.TotalSeconds);
                return default;
            }
        });
    }

    // Layer 2: Retry with exponential backoff and jitter
    private static void AddRetryLayer(
        ResiliencePipelineBuilder builder,
        SecretsResilienceOptions options,
        ILogger logger,
        SecretsMetrics? metrics)
    {
        builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = options.RetryBaseDelay,
            MaxDelay = options.RetryMaxDelay,
            ShouldHandle = TransientFailures(),
            OnRetry = args =>
            {
                // The exception type only: the exception message can carry personal data.
                var attemptNumber = args.AttemptNumber + 1;
                var reason = args.Outcome.Exception?.GetType().Name ?? "Transient error";

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
    private static void AddCircuitBreakerLayer(
        ResiliencePipelineBuilder builder,
        SecretsResilienceOptions options,
        SecretsCircuitBreakerState circuitBreakerState,
        ILogger logger,
        SecretsMetrics? metrics)
    {
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            SamplingDuration = options.CircuitBreakerSamplingDuration,
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            BreakDuration = options.CircuitBreakerBreakDuration,
            ShouldHandle = TransientFailures(),
            OnOpened = _ =>
            {
                circuitBreakerState.SetOpened();
                Log.ResilienceCircuitBreakerOpened(logger);
                RecordTransition(metrics, "opened");
                return default;
            },
            OnClosed = _ =>
            {
                circuitBreakerState.SetClosed();
                Log.ResilienceCircuitBreakerClosed(logger);
                RecordTransition(metrics, "closed");
                return default;
            },
            OnHalfOpened = _ =>
            {
                circuitBreakerState.SetHalfOpen();
                Log.ResilienceCircuitBreakerHalfOpen(logger);
                RecordTransition(metrics, "half_open");
                return default;
            }
        });
    }

    private static void RecordTransition(SecretsMetrics? metrics, string state)
    {
        metrics?.RecordCircuitBreakerTransition(state);
        SecretsActivitySource.RecordCircuitBreakerEvent(System.Diagnostics.Activity.Current, state);
    }
}
