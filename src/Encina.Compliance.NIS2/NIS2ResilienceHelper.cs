using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.Registry;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Internal helper that wraps external service calls with resilience protection.
/// </summary>
/// <remarks>
/// <para>
/// When a <see cref="ResiliencePipelineProvider{TKey}"/> is registered in DI with a pipeline
/// named <c>"nis2-external"</c>, all external calls are executed through that pipeline
/// (supporting retry, circuit breaker, timeout, rate limiting, etc.).
/// </para>
/// <para>
/// When no resilience pipeline is configured, a simple timeout fallback is applied using
/// <see cref="NIS2Options.ExternalCallTimeout"/> (default 5 seconds) to prevent external
/// calls from blocking compliance evaluation indefinitely.
/// </para>
/// </remarks>
internal static class NIS2ResilienceHelper
{
    /// <summary>
    /// The well-known pipeline key for NIS2 external calls.
    /// Register a resilience pipeline with this key to enable retry, circuit breaker, etc.
    /// </summary>
    internal const string PipelineKey = "nis2-external";

    /// <summary>
    /// Executes an external call with resilience protection. Uses the registered
    /// <c>"nis2-external"</c> resilience pipeline if available; otherwise falls back
    /// to a simple timeout. Returns <paramref name="fallback"/> on any failure.
    /// </summary>
    /// <typeparam name="T">The return type of the external call.</typeparam>
    /// <param name="serviceProvider">The service provider to resolve resilience infrastructure.</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="fallback">The value to return if the operation fails or times out.</param>
    /// <param name="timeout">The timeout to apply when no resilience pipeline is configured.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result, or <paramref name="fallback"/> on failure.</returns>
    internal static async ValueTask<T> ExecuteAsync<T>(
        IServiceProvider serviceProvider,
        Func<CancellationToken, ValueTask<T>> operation,
        T fallback,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        // Option 1: Use registered resilience pipeline if available
        var pipelineProvider = serviceProvider.GetService<ResiliencePipelineProvider<string>>();
        if (pipelineProvider is not null)
        {
            try
            {
                var pipeline = pipelineProvider.GetPipeline(PipelineKey);
                return await pipeline.ExecuteAsync(
                    async ct => await operation(ct).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (KeyNotFoundException)
            {
                // Pipeline key "nis2-external" not registered — fall through to timeout fallback
            }
            catch (Exception)
            {
                // Resilience pipeline execution failed (circuit open, timeout, etc.)
                return fallback;
            }
        }

        // Option 2: Timeout fallback — prevents external calls from blocking indefinitely
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            return await operation(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    /// <summary>
    /// Executes an external call with resilience protection (no return value).
    /// Uses the registered <c>"nis2-external"</c> resilience pipeline if available;
    /// otherwise falls back to a simple timeout. Silently swallows failures.
    /// </summary>
    internal static async ValueTask ExecuteAsync(
        IServiceProvider serviceProvider,
        Func<CancellationToken, ValueTask> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        // Option 1: Use registered resilience pipeline if available
        var pipelineProvider = serviceProvider.GetService<ResiliencePipelineProvider<string>>();
        if (pipelineProvider is not null)
        {
            try
            {
                var pipeline = pipelineProvider.GetPipeline(PipelineKey);
                await pipeline.ExecuteAsync(
                    async ct => { await operation(ct).ConfigureAwait(false); },
                    cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (KeyNotFoundException)
            {
                // Pipeline key "nis2-external" not registered — fall through to timeout fallback
            }
            catch (Exception)
            {
                return;
            }
        }

        // Option 2: Timeout fallback
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            await operation(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Silently swallow — external call failures never block compliance evaluation
        }
    }
}
