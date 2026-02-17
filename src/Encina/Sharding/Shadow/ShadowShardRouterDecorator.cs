using System.Diagnostics;
using Encina.Sharding.Colocation;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Shadow;

/// <summary>
/// Decorator that wraps a production <see cref="IShardRouter"/> with shadow routing capabilities
/// for testing new shard topologies under real traffic.
/// </summary>
/// <remarks>
/// <para>
/// All standard <see cref="IShardRouter"/> methods delegate directly to the primary (production)
/// router, keeping the production path unchanged. Shadow-specific operations
/// (<see cref="RouteShadowAsync(string, CancellationToken)"/>, <see cref="CompareAsync"/>)
/// route against the shadow topology and measure latency for comparison.
/// </para>
/// <para>
/// Shadow operations never affect the production path. Errors from the shadow router are
/// logged but not propagated.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var decorator = new ShadowShardRouterDecorator(
///     primary: productionRouter,
///     shadowRouter: shadowRouter,
///     options: shadowOptions,
///     logger: logger);
///
/// // Production routing is unchanged
/// var result = decorator.GetShardId("customer-123");
///
/// // Shadow comparison
/// var comparison = await decorator.CompareAsync("customer-123", cancellationToken);
/// </code>
/// </example>
internal sealed class ShadowShardRouterDecorator : IShadowShardRouter
{
    private readonly IShardRouter _primary;
    private readonly IShardRouter _shadowRouter;
    private readonly ShadowShardingOptions _options;
    private readonly ILogger<ShadowShardRouterDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ShadowShardRouterDecorator"/>.
    /// </summary>
    /// <param name="primary">The production shard router that handles all production routing.</param>
    /// <param name="shadowRouter">The shadow shard router built from the shadow topology.</param>
    /// <param name="options">The shadow sharding configuration options.</param>
    /// <param name="logger">The logger for shadow routing diagnostics.</param>
    internal ShadowShardRouterDecorator(
        IShardRouter primary,
        IShardRouter shadowRouter,
        ShadowShardingOptions options,
        ILogger<ShadowShardRouterDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(shadowRouter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _primary = primary;
        _shadowRouter = shadowRouter;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsShadowEnabled => true;

    /// <inheritdoc />
    public ShardTopology ShadowTopology => _options.ShadowTopology!;

    // ── IShardRouter delegation to primary ──────────────────────────────

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey) =>
        _primary.GetShardId(shardKey);

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(CompoundShardKey key) =>
        _primary.GetShardId(key);

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey) =>
        _primary.GetShardIds(partialKey);

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() =>
        _primary.GetAllShardIds();

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId) =>
        _primary.GetShardConnectionString(shardId);

    /// <inheritdoc />
    public IColocationGroup? GetColocationGroup(Type entityType) =>
        _primary.GetColocationGroup(entityType);

    // ── Shadow-specific operations ──────────────────────────────────────

    /// <inheritdoc />
    public Task<Either<EncinaError, string>> RouteShadowAsync(string shardKey, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        try
        {
            var result = _shadowRouter.GetShardId(shardKey);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            ShadowShardingLog.ShadowRoutingFailed(_logger, shardKey, ex.Message);
            return Task.FromResult(
                Either<EncinaError, string>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.ShadowRoutingFailed,
                        $"Shadow routing failed for shard key '{shardKey}'.",
                        ex)));
        }
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, string>> RouteShadowAsync(CompoundShardKey key, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var result = _shadowRouter.GetShardId(key);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            var keyString = key.ToString();
            ShadowShardingLog.ShadowRoutingFailed(_logger, keyString, ex.Message);
            return Task.FromResult(
                Either<EncinaError, string>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.ShadowRoutingFailed,
                        $"Shadow routing failed for compound key '{keyString}'.",
                        ex)));
        }
    }

    /// <inheritdoc />
    public Task<ShadowComparisonResult> CompareAsync(string shardKey, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(shardKey);

        // Measure production routing
        var prodStart = Stopwatch.GetTimestamp();
        var prodResult = _primary.GetShardId(shardKey);
        var prodLatency = Stopwatch.GetElapsedTime(prodStart);

        // Measure shadow routing
        string shadowShardId;
        TimeSpan shadowLatency;

        try
        {
            var shadowStart = Stopwatch.GetTimestamp();
            var shadowResult = _shadowRouter.GetShardId(shardKey);
            shadowLatency = Stopwatch.GetElapsedTime(shadowStart);

            shadowShardId = shadowResult.Match(
                Right: id => id,
                Left: error =>
                {
                    ShadowShardingLog.ShadowRoutingFailed(_logger, shardKey, error.Message);
                    return string.Empty;
                });
        }
        catch (Exception ex)
        {
            ShadowShardingLog.ShadowRoutingFailed(_logger, shardKey, ex.Message);
            shadowShardId = string.Empty;
            shadowLatency = TimeSpan.Zero;
        }

        var prodShardId = prodResult.Match(
            Right: id => id,
            Left: _ => string.Empty);

        var routingMatch = !string.IsNullOrEmpty(prodShardId)
            && !string.IsNullOrEmpty(shadowShardId)
            && string.Equals(prodShardId, shadowShardId, StringComparison.Ordinal);

        var comparison = new ShadowComparisonResult(
            ShardKey: shardKey,
            ProductionShardId: prodShardId,
            ShadowShardId: shadowShardId,
            RoutingMatch: routingMatch,
            ProductionLatency: prodLatency,
            ShadowLatency: shadowLatency,
            ResultsMatch: null,
            ComparedAt: DateTimeOffset.UtcNow);

        if (!routingMatch && !string.IsNullOrEmpty(prodShardId))
        {
            ShadowShardingLog.RoutingMismatch(
                _logger, shardKey, prodShardId, shadowShardId);
        }

        return Task.FromResult(comparison);
    }
}
