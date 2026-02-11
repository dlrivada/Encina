using System.Diagnostics;
using LanguageExt;

namespace Encina.Sharding.Diagnostics;

/// <summary>
/// Decorator that adds OpenTelemetry tracing and metrics to an <see cref="IShardRouter"/>.
/// </summary>
/// <remarks>
/// Records routing decisions, latency, and tracing activities for each call to
/// <see cref="GetShardId(string)"/>. Delegates all other operations to the inner router.
/// </remarks>
internal sealed class InstrumentedShardRouter : IShardRouter
{
    private readonly IShardRouter _inner;
    private readonly ShardRoutingMetrics _metrics;
    private readonly string _routerType;

    /// <summary>
    /// Initializes a new instance of <see cref="InstrumentedShardRouter"/>.
    /// </summary>
    /// <param name="inner">The inner router to instrument.</param>
    /// <param name="metrics">The routing metrics to record to.</param>
    /// <param name="routerType">
    /// The router strategy name (e.g., <c>"hash"</c>, <c>"range"</c>, <c>"directory"</c>, <c>"geo"</c>).
    /// </param>
    internal InstrumentedShardRouter(IShardRouter inner, ShardRoutingMetrics metrics, string routerType)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentException.ThrowIfNullOrWhiteSpace(routerType);

        _inner = inner;
        _metrics = metrics;
        _routerType = routerType;
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey)
    {
        var activity = ShardingActivitySource.StartRouting(shardKey, _routerType);
        var start = Stopwatch.GetTimestamp();

        var result = _inner.GetShardId(shardKey);

        var elapsed = Stopwatch.GetElapsedTime(start);

        _ = result.Match(
            Right: shardId =>
            {
                ShardingActivitySource.RoutingCompleted(activity, shardId);
                _metrics.RecordRouteDecision(shardId, _routerType, elapsed.TotalNanoseconds);
                return shardId;
            },
            Left: error =>
            {
                ShardingActivitySource.RoutingFailed(activity, error.GetCode().IfNone(string.Empty), error.Message);
                return string.Empty;
            });

        return result;
    }

    /// <summary>
    /// Routes a compound shard key with instrumentation.
    /// </summary>
    public Either<EncinaError, string> GetShardId(CompoundShardKey key)
    {
        var activity = ShardingActivitySource.StartCompoundRouting(key, _routerType);
        var start = Stopwatch.GetTimestamp();

        _metrics.RecordCompoundKeyExtraction(key.ComponentCount, _routerType);

        var result = _inner.GetShardId(key);

        var elapsed = Stopwatch.GetElapsedTime(start);

        _ = result.Match(
            Right: shardId =>
            {
                ShardingActivitySource.RoutingCompleted(activity, shardId);
                _metrics.RecordRouteDecision(shardId, _routerType, elapsed.TotalNanoseconds);
                return shardId;
            },
            Left: error =>
            {
                ShardingActivitySource.RoutingFailed(activity, error.GetCode().IfNone(string.Empty), error.Message);
                return string.Empty;
            });

        return result;
    }

    /// <summary>
    /// Routes a partial compound key for scatter-gather with instrumentation.
    /// </summary>
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey)
    {
        var activity = ShardingActivitySource.StartCompoundRouting(partialKey, _routerType);

        _metrics.RecordPartialKeyRouting(partialKey.ComponentCount, _inner.GetAllShardIds().Count, _routerType);

        var result = _inner.GetShardIds(partialKey);

        _ = result.Match(
            Right: shardIds =>
            {
                activity?.SetTag(ActivityTagNames.ShardCount, shardIds.Count);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                return shardIds;
            },
            Left: error =>
            {
                ShardingActivitySource.RoutingFailed(activity, error.GetCode().IfNone(string.Empty), error.Message);
                return (IReadOnlyList<string>)[];
            });

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() => _inner.GetAllShardIds();

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId) =>
        _inner.GetShardConnectionString(shardId);

    /// <summary>
    /// Gets the underlying router type name used for metric tagging.
    /// </summary>
    internal string RouterType => _routerType;
}

/// <summary>
/// Decorator that adds OpenTelemetry tracing and metrics to an <see cref="IShardRouter{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type being routed.</typeparam>
internal sealed class InstrumentedShardRouter<TEntity> : IShardRouter<TEntity>
    where TEntity : notnull
{
    private readonly IShardRouter<TEntity> _inner;
    private readonly InstrumentedShardRouter _instrumentedBase;

    /// <summary>
    /// Initializes a new instance of <see cref="InstrumentedShardRouter{TEntity}"/>.
    /// </summary>
    /// <param name="inner">The inner entity router to instrument.</param>
    /// <param name="instrumentedBase">The instrumented base router that handles metrics/tracing.</param>
    internal InstrumentedShardRouter(IShardRouter<TEntity> inner, InstrumentedShardRouter instrumentedBase)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(instrumentedBase);

        _inner = inner;
        _instrumentedBase = instrumentedBase;
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(TEntity entity)
    {
        // Entity routing extracts the key and calls GetShardId(string), which
        // is already instrumented via _instrumentedBase. Delegate directly.
        return _inner.GetShardId(entity);
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyList<string>> GetShardIds(TEntity entity)
    {
        return _inner.GetShardIds(entity);
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardId(string shardKey) =>
        _instrumentedBase.GetShardId(shardKey);

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllShardIds() => _inner.GetAllShardIds();

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardConnectionString(string shardId) =>
        _inner.GetShardConnectionString(shardId);
}
