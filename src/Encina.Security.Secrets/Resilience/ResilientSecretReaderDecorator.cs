using System.Diagnostics;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Encina.Security.Secrets.Resilience;

/// <summary>
/// Decorator that adds resilience (retry, circuit breaker, timeout) to any <see cref="ISecretReader"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps secret read operations with a Polly <see cref="ResiliencePipeline"/> that applies:
/// <list type="bullet">
/// <item><description>Total operation timeout</description></item>
/// <item><description>Retry with exponential backoff and jitter for transient failures</description></item>
/// <item><description>Circuit breaker to prevent cascading failures</description></item>
/// </list>
/// </para>
/// <para>
/// <b>ROP bridge:</b> Transient errors (<c>secrets.provider_unavailable</c>) are converted to
/// <see cref="TransientSecretException"/> internally to trigger Polly retries. Non-transient errors
/// (<c>secrets.not_found</c>, <c>secrets.access_denied</c>) pass through without retry.
/// </para>
/// <para>
/// <b>Observability:</b> When OpenTelemetry tracing is enabled, each resilient operation emits
/// an <c>Secrets.Resilience</c> activity with retry, circuit breaker, and timeout events.
/// When metrics are enabled, counters track retries, circuit transitions, and timeouts.
/// </para>
/// </remarks>
public sealed class ResilientSecretReaderDecorator : ISecretReader
{
    private readonly ISecretReader _inner;
    private readonly ResiliencePipeline _pipeline;
    private readonly SecretsResilienceOptions _options;
    private readonly ILogger<ResilientSecretReaderDecorator> _logger;
    private readonly SecretsMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientSecretReaderDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner secret reader to wrap.</param>
    /// <param name="pipeline">The resilience pipeline to apply.</param>
    /// <param name="options">The resilience options for timeout error reporting.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics recorder for resilience telemetry.</param>
    public ResilientSecretReaderDecorator(
        ISecretReader inner,
        ResiliencePipeline pipeline,
        SecretsResilienceOptions options,
        ILogger<ResilientSecretReaderDecorator> logger,
        SecretsMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _pipeline = pipeline;
        _options = options;
        _logger = logger;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        using var activity = SecretsActivitySource.StartResilienceActivity(secretName, "get");
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await _pipeline.ExecuteAsync(async ct =>
            {
                var innerResult = await _inner.GetSecretAsync(secretName, ct).ConfigureAwait(false);

                return innerResult.Match<Either<EncinaError, string>>(
                    Right: value => value,
                    Left: error =>
                    {
                        if (SecretsTransientErrorDetector.IsTransient(error))
                        {
                            throw new TransientSecretException(error);
                        }

                        return error;
                    });
            }, cancellationToken).ConfigureAwait(false);

            sw.Stop();
            var isRight = result.IsRight;
            _metrics?.RecordResilienceDuration(secretName, isRight, sw.Elapsed);
            if (isRight)
            {
                SecretsActivitySource.RecordSuccess(activity);
            }

            return result;
        }
        catch (TransientSecretException ex)
        {
            sw.Stop();
            _metrics?.RecordResilienceDuration(secretName, false, sw.Elapsed);
            SecretsActivitySource.RecordFailure(activity, SecretsErrors.ProviderUnavailableCode);
            return ex.Error;
        }
        catch (BrokenCircuitException)
        {
            sw.Stop();
            _metrics?.RecordResilienceDuration(secretName, false, sw.Elapsed);
            SecretsActivitySource.RecordFailure(activity, SecretsErrors.CircuitBreakerOpenCode);
            return SecretsErrors.CircuitBreakerOpen("secrets");
        }
        catch (TimeoutRejectedException)
        {
            sw.Stop();
            _metrics?.RecordResilienceDuration(secretName, false, sw.Elapsed);
            SecretsActivitySource.RecordFailure(activity, SecretsErrors.ResilienceTimeoutCode);
            return SecretsErrors.ResilienceTimeout(secretName, _options.OperationTimeout);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        using var activity = SecretsActivitySource.StartResilienceActivity(secretName, "get");
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await _pipeline.ExecuteAsync(async ct =>
            {
                var innerResult = await _inner.GetSecretAsync<T>(secretName, ct).ConfigureAwait(false);

                return innerResult.Match<Either<EncinaError, T>>(
                    Right: value => value,
                    Left: error =>
                    {
                        if (SecretsTransientErrorDetector.IsTransient(error))
                        {
                            throw new TransientSecretException(error);
                        }

                        return error;
                    });
            }, cancellationToken).ConfigureAwait(false);

            sw.Stop();
            var isRight = result.IsRight;
            _metrics?.RecordResilienceDuration(secretName, isRight, sw.Elapsed);
            if (isRight)
            {
                SecretsActivitySource.RecordSuccess(activity);
            }

            return result;
        }
        catch (TransientSecretException ex)
        {
            sw.Stop();
            _metrics?.RecordResilienceDuration(secretName, false, sw.Elapsed);
            SecretsActivitySource.RecordFailure(activity, SecretsErrors.ProviderUnavailableCode);
            return ex.Error;
        }
        catch (BrokenCircuitException)
        {
            sw.Stop();
            _metrics?.RecordResilienceDuration(secretName, false, sw.Elapsed);
            SecretsActivitySource.RecordFailure(activity, SecretsErrors.CircuitBreakerOpenCode);
            return SecretsErrors.CircuitBreakerOpen("secrets");
        }
        catch (TimeoutRejectedException)
        {
            sw.Stop();
            _metrics?.RecordResilienceDuration(secretName, false, sw.Elapsed);
            SecretsActivitySource.RecordFailure(activity, SecretsErrors.ResilienceTimeoutCode);
            return SecretsErrors.ResilienceTimeout(secretName, _options.OperationTimeout);
        }
    }
}
