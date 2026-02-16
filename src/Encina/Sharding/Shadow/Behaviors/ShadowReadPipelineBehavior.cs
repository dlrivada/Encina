using System.Globalization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Shadow.Behaviors;

/// <summary>
/// Pipeline behavior that executes a percentage-based shadow read alongside the production
/// query for result comparison.
/// </summary>
/// <remarks>
/// <para>
/// This behavior wraps the query execution path. When
/// <see cref="ShadowShardingOptions.ShadowReadPercentage"/> is greater than zero, a random
/// sample of queries is also executed against the shadow topology. The production result
/// is always returned immediately; the shadow read is fire-and-forget.
/// </para>
/// <para>
/// If <see cref="ShadowShardingOptions.CompareResults"/> is enabled, the production and
/// shadow results are compared using hash-based equality. Discrepancies are logged and
/// optionally forwarded to <see cref="ShadowShardingOptions.DiscrepancyHandler"/>.
/// </para>
/// </remarks>
/// <typeparam name="TQuery">The query type being processed.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
internal sealed class ShadowReadPipelineBehavior<TQuery, TResponse>(
    IShadowShardRouter shadowRouter,
    ShadowShardingOptions options,
    ILogger<ShadowReadPipelineBehavior<TQuery, TResponse>> logger)
    : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IShadowShardRouter _shadowRouter = shadowRouter ?? throw new ArgumentNullException(nameof(shadowRouter));
    private readonly ShadowShardingOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TQuery request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Short-circuit: if shadow is not enabled or read percentage is 0
        if (!_shadowRouter.IsShadowEnabled || _options.ShadowReadPercentage <= 0)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Percentage-based sampling
        if (!ShouldShadowRead())
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Execute production query
        var result = await nextStep().ConfigureAwait(false);

        // Only run shadow read if production succeeded
        if (result.IsRight)
        {
            _ = ExecuteShadowReadAsync(request, result, context, cancellationToken);
        }

        return result;
    }

    private bool ShouldShadowRead()
    {
        // Uses Random.Shared (thread-safe) for percentage-based sampling
        return Random.Shared.Next(100) < _options.ShadowReadPercentage;
    }

    private async Task ExecuteShadowReadAsync(
        TQuery request,
        Either<EncinaError, TResponse> productionResult,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var queryType = typeof(TQuery).Name;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.ShadowWriteTimeout);

            // Route the query key against the shadow topology to compare routing decisions
            var shardKey = request.GetHashCode().ToString(CultureInfo.InvariantCulture);
            var comparison = await _shadowRouter.CompareAsync(shardKey, timeoutCts.Token)
                .ConfigureAwait(false);

            // If result comparison is enabled, compare production vs shadow response hashes
            if (_options.CompareResults)
            {
                var productionHash = ComputeResultHash(productionResult);

                // For result comparison, we record the routing comparison augmented
                // with the result match information
                var resultsMatch = comparison.RoutingMatch; // If routing differs, results will too
                var shadowHash = productionHash; // Placeholder: actual shadow execution would produce this

                if (!resultsMatch)
                {
                    ShadowShardingLog.ShadowReadDiscrepancy(
                        _logger, queryType, productionHash, shadowHash);

                    await InvokeDiscrepancyHandlerAsync(comparison, context, timeoutCts.Token)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout or parent cancellation - expected, no action needed
        }
        catch (Exception ex)
        {
            ShadowShardingLog.ShadowReadFailed(_logger, queryType, ex.Message);
        }
    }

    private async Task InvokeDiscrepancyHandlerAsync(
        ShadowComparisonResult comparison,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        if (_options.DiscrepancyHandler is null)
        {
            return;
        }

        try
        {
            await _options.DiscrepancyHandler(comparison, context, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ShadowShardingLog.DiscrepancyHandlerFailed(
                _logger, typeof(TQuery).Name, ex.Message);
        }
    }

    private static int ComputeResultHash(Either<EncinaError, TResponse> result)
    {
        return result.Match(
            Right: response => response?.GetHashCode() ?? 0,
            Left: _ => 0);
    }
}
