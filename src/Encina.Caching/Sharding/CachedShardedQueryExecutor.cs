using Encina.Caching.Sharding.Configuration;
using Encina.Sharding;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Sharding;

/// <summary>
/// Decorator that adds result caching to an <see cref="IShardedQueryExecutor"/>.
/// </summary>
/// <remarks>
/// <para>
/// The standard <see cref="IShardedQueryExecutor"/> methods (<see cref="ExecuteAsync{T}"/>
/// and <see cref="ExecuteAllAsync{T}"/>) pass through to the inner executor without caching.
/// Caching is only applied when using the explicit cache key methods:
/// <see cref="ExecuteCachedAsync{T}(IEnumerable{string}, string, Func{string, CancellationToken, Task{Either{EncinaError, IReadOnlyList{T}}}}, TimeSpan?, CancellationToken)"/>
/// and <see cref="ExecuteAllCachedAsync{T}"/>.
/// </para>
/// <para>
/// This design avoids the complexity of delegate hashing: callers provide an explicit
/// <c>cacheKey</c> string that deterministically identifies the query. Cache keys are
/// composed as <c>shard:scatter:{cacheKey}</c> using <see cref="ShardCacheKeyGenerator"/>.
/// </para>
/// <para>
/// Invalidation can target specific cache keys or entire shards. When
/// <see cref="IPubSubProvider"/> is available, invalidation messages are broadcast
/// to coordinate caches across application instances.
/// </para>
/// </remarks>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
public sealed class CachedShardedQueryExecutor : IShardedQueryExecutor
{
    private const string DefaultKeyPrefix = "shard:scatter";

    private readonly IShardedQueryExecutor _inner;
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider? _pubSub;
    private readonly ScatterGatherCacheOptions _options;
    private readonly ILogger<CachedShardedQueryExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedShardedQueryExecutor"/> class.
    /// </summary>
    /// <param name="inner">The inner query executor to decorate.</param>
    /// <param name="cache">The distributed cache provider.</param>
    /// <param name="options">The scatter-gather cache configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="pubSub">Optional pub/sub provider for distributed invalidation.</param>
    public CachedShardedQueryExecutor(
        IShardedQueryExecutor inner,
        ICacheProvider cache,
        IOptions<ScatterGatherCacheOptions> options,
        ILogger<CachedShardedQueryExecutor> logger,
        IPubSubProvider? pubSub = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _pubSub = pubSub;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Pass-through to inner executor without caching. Use
    /// <see cref="ExecuteCachedAsync{T}(IEnumerable{string}, string, Func{string, CancellationToken, Task{Either{EncinaError, IReadOnlyList{T}}}}, TimeSpan?, CancellationToken)"/>
    /// for cached execution.
    /// </remarks>
    public Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteAsync<T>(
        IEnumerable<string> shardIds,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        CancellationToken cancellationToken = default)
    {
        return _inner.ExecuteAsync(shardIds, queryFactory, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Pass-through to inner executor without caching. Use
    /// <see cref="ExecuteAllCachedAsync{T}"/> for cached execution.
    /// </remarks>
    public Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteAllAsync<T>(
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        CancellationToken cancellationToken = default)
    {
        return _inner.ExecuteAllAsync(queryFactory, cancellationToken);
    }

    /// <summary>
    /// Executes a query across the specified shards with result caching.
    /// </summary>
    /// <typeparam name="T">The type of the result items.</typeparam>
    /// <param name="shardIds">The shard IDs to query.</param>
    /// <param name="cacheKey">A deterministic key identifying this query for caching.</param>
    /// <param name="queryFactory">A factory that creates a query task for each shard.</param>
    /// <param name="cacheDuration">
    /// Optional duration override. Defaults to <see cref="ScatterGatherCacheOptions.DefaultCacheDuration"/>.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{T}"/> (from cache or inner executor);
    /// Left with an error if the operation fails.
    /// </returns>
    public async Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteCachedAsync<T>(
        IEnumerable<string> shardIds,
        string cacheKey,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(queryFactory);

        var composedKey = ShardCacheKeyGenerator.ForScatterGather(DefaultKeyPrefix, cacheKey);
        var duration = cacheDuration ?? _options.DefaultCacheDuration;

        // Check cache first
        var cached = await _cache.GetAsync<ShardedQueryResult<T>>(composedKey, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for scatter-gather query '{CacheKey}'", cacheKey);
            return Either<EncinaError, ShardedQueryResult<T>>.Right(cached);
        }

        // Cache miss: execute the query
        var result = await _inner.ExecuteAsync(shardIds, queryFactory, cancellationToken)
            .ConfigureAwait(false);

        // Cache successful results that don't exceed the size limit
        if (result.IsRight)
        {
            var queryResult = result.Match(Right: r => r, Left: _ => default!);

            if (queryResult.Results.Count <= _options.MaxCachedResultSize)
            {
                await _cache.SetAsync(composedKey, queryResult, duration, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug(
                    "Cached scatter-gather result for '{CacheKey}' ({Count} items, TTL {Duration})",
                    cacheKey,
                    queryResult.Results.Count,
                    duration);
            }
            else
            {
                _logger.LogDebug(
                    "Scatter-gather result for '{CacheKey}' exceeds max size ({Count} > {Max}), not caching",
                    cacheKey,
                    queryResult.Results.Count,
                    _options.MaxCachedResultSize);
            }
        }

        return result;
    }

    /// <summary>
    /// Executes a query across all active shards with result caching.
    /// </summary>
    /// <typeparam name="T">The type of the result items.</typeparam>
    /// <param name="cacheKey">A deterministic key identifying this query for caching.</param>
    /// <param name="queryFactory">A factory that creates a query task for each shard.</param>
    /// <param name="cacheDuration">
    /// Optional duration override. Defaults to <see cref="ScatterGatherCacheOptions.DefaultCacheDuration"/>.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{T}"/> (from cache or inner executor);
    /// Left with an error if the operation fails.
    /// </returns>
    public async Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteAllCachedAsync<T>(
        string cacheKey,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(queryFactory);

        var composedKey = ShardCacheKeyGenerator.ForScatterGather(DefaultKeyPrefix, cacheKey);
        var duration = cacheDuration ?? _options.DefaultCacheDuration;

        // Check cache first
        var cached = await _cache.GetAsync<ShardedQueryResult<T>>(composedKey, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for scatter-gather all-shards query '{CacheKey}'", cacheKey);
            return Either<EncinaError, ShardedQueryResult<T>>.Right(cached);
        }

        // Cache miss: execute the query
        var result = await _inner.ExecuteAllAsync(queryFactory, cancellationToken)
            .ConfigureAwait(false);

        // Cache successful results that don't exceed the size limit
        if (result.IsRight)
        {
            var queryResult = result.Match(Right: r => r, Left: _ => default!);

            if (queryResult.Results.Count <= _options.MaxCachedResultSize)
            {
                await _cache.SetAsync(composedKey, queryResult, duration, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return result;
    }

    /// <summary>
    /// Invalidates cached scatter-gather results matching a cache key pattern.
    /// </summary>
    /// <param name="cacheKeyPattern">
    /// The pattern to match (e.g., <c>"orders:*"</c>). The prefix <c>shard:scatter:</c>
    /// is prepended automatically.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvalidateAsync(string cacheKeyPattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKeyPattern);

        var fullPattern = $"{DefaultKeyPrefix}:{cacheKeyPattern}";
        await _cache.RemoveByPatternAsync(fullPattern, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Invalidated scatter-gather cache entries matching '{Pattern}'", fullPattern);

        if (_pubSub is not null)
        {
            await _pubSub.PublishAsync(
                _options.InvalidationChannel,
                fullPattern,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invalidates all cached scatter-gather results associated with a specific shard.
    /// </summary>
    /// <param name="shardId">The shard identifier whose cached results should be invalidated.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvalidateShardAsync(string shardId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        var pattern = ShardCacheKeyGenerator.InvalidationPattern(DefaultKeyPrefix, shardId);
        await _cache.RemoveByPatternAsync(pattern, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Invalidated scatter-gather cache entries for shard '{ShardId}'", shardId);

        if (_pubSub is not null)
        {
            await _pubSub.PublishAsync(
                _options.InvalidationChannel,
                pattern,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
#pragma warning restore CA1848
