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
/// Decorator that adds resilience (retry, circuit breaker, timeout) to any <see cref="ISecretRotator"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps secret rotation operations with a Polly <see cref="ResiliencePipeline"/> that applies
/// retry, circuit breaker, and timeout strategies. Shares the same pipeline instance as
/// <see cref="ResilientSecretReaderDecorator"/> so circuit breaker state is unified.
/// </para>
/// </remarks>
public sealed class ResilientSecretRotatorDecorator : ISecretRotator
{
    private readonly ISecretRotator _inner;
    private readonly ResiliencePipeline _pipeline;
    private readonly SecretsResilienceOptions _options;
    private readonly ILogger<ResilientSecretRotatorDecorator> _logger;
    private readonly SecretsMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientSecretRotatorDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner secret rotator to wrap.</param>
    /// <param name="pipeline">The resilience pipeline to apply.</param>
    /// <param name="options">The resilience options for timeout error reporting.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics recorder for resilience telemetry.</param>
    public ResilientSecretRotatorDecorator(
        ISecretRotator inner,
        ResiliencePipeline pipeline,
        SecretsResilienceOptions options,
        ILogger<ResilientSecretRotatorDecorator> logger,
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
    public async ValueTask<Either<EncinaError, Unit>> RotateSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        using var activity = SecretsActivitySource.StartResilienceActivity(secretName, "rotate");
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await _pipeline.ExecuteAsync(async ct =>
            {
                var innerResult = await _inner.RotateSecretAsync(secretName, ct).ConfigureAwait(false);

                return innerResult.Match<Either<EncinaError, Unit>>(
                    Right: _ => Unit.Default,
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
            _metrics?.RecordResilienceDuration(secretName, result.IsRight, sw.Elapsed);
            if (result.IsRight)
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
