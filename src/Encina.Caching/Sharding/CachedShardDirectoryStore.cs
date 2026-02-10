using System.Collections.Concurrent;
using Encina.Caching.Sharding.Configuration;
using Encina.Sharding.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Sharding;

/// <summary>
/// Decorator that adds an L1 in-memory cache to an <see cref="IShardDirectoryStore"/>,
/// with optional distributed coordination via <see cref="ICacheProvider"/> and
/// <see cref="IPubSubProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Since <see cref="IShardDirectoryStore"/> is synchronous, this decorator uses a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> as an L1 read cache. The distributed
/// cache (<see cref="ICacheProvider"/>) is used only during background refresh operations
/// initiated by <c>TopologyRefreshHostedService</c>, avoiding sync-over-async on the hot path.
/// </para>
/// <para>
/// Write operations flow through to the inner store immediately and apply the configured
/// <see cref="CacheInvalidationStrategy"/>:
/// <list type="bullet">
///   <item><description><b>Immediate</b>: Removes the L1 entry, forcing the next read to query the inner store.</description></item>
///   <item><description><b>WriteThrough</b>: Updates the L1 entry in place after writing to the inner store.</description></item>
///   <item><description><b>Lazy</b>: Does not touch the L1 cache; stale entries expire via background refresh.</description></item>
/// </list>
/// </para>
/// </remarks>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
public sealed class CachedShardDirectoryStore : IShardDirectoryStore
{
    private readonly IShardDirectoryStore _inner;
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider? _pubSub;
    private readonly DirectoryCacheOptions _options;
    private readonly ILogger<CachedShardDirectoryStore> _logger;
    private readonly ConcurrentDictionary<string, string> _l1Cache = new(StringComparer.OrdinalIgnoreCase);
    private IAsyncDisposable? _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedShardDirectoryStore"/> class.
    /// </summary>
    /// <param name="inner">The inner directory store to decorate.</param>
    /// <param name="cache">The distributed cache provider for L2 coordination.</param>
    /// <param name="options">The directory cache configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="pubSub">Optional pub/sub provider for distributed invalidation.</param>
    public CachedShardDirectoryStore(
        IShardDirectoryStore inner,
        ICacheProvider cache,
        IOptions<DirectoryCacheOptions> options,
        ILogger<CachedShardDirectoryStore> logger,
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
    public string? GetMapping(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Check L1 first
        if (_l1Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // L1 miss: go to inner store
        var result = _inner.GetMapping(key);

        if (result is not null)
        {
            _l1Cache.TryAdd(key, result);
        }

        return result;
    }

    /// <inheritdoc />
    public void AddMapping(string key, string shardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        // Always write to inner store
        _inner.AddMapping(key, shardId);

        // Apply invalidation strategy
        switch (_options.InvalidationStrategy)
        {
            case CacheInvalidationStrategy.Immediate:
                _l1Cache.TryRemove(key, out _);
                break;

            case CacheInvalidationStrategy.WriteThrough:
                _l1Cache[key] = shardId;
                break;

            case CacheInvalidationStrategy.Lazy:
                // No-op: TTL-based refresh will pick it up
                break;
        }

        // Publish distributed invalidation if enabled
        PublishInvalidation(key, shardId, isRemoval: false);
    }

    /// <inheritdoc />
    public bool RemoveMapping(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var removed = _inner.RemoveMapping(key);

        if (_options.InvalidationStrategy != CacheInvalidationStrategy.Lazy)
        {
            _l1Cache.TryRemove(key, out _);
        }

        // Publish distributed invalidation if enabled
        PublishInvalidation(key, shardId: null, isRemoval: true);

        return removed;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetAllMappings()
    {
        // Bulk operations bypass the cache and go directly to inner store
        return _inner.GetAllMappings();
    }

    /// <summary>
    /// Refreshes the L1 cache from the inner store. Called by the background hosted service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task RefreshL1FromInnerAsync(CancellationToken cancellationToken)
    {
        try
        {
            var allMappings = _inner.GetAllMappings();

            // Replace L1 contents atomically: clear and repopulate
            _l1Cache.Clear();

            foreach (var (key, shardId) in allMappings)
            {
                _l1Cache[key] = shardId;
            }

            // Also update L2 distributed cache
            var cacheKey = ShardCacheKeyGenerator.ForDirectoryAll(_options.KeyPrefix);
            await _cache.SetAsync(
                cacheKey,
                allMappings,
                _options.CacheDuration,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Refreshed directory L1 cache with {Count} mappings",
                allMappings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh directory L1 cache");
        }
    }

    /// <summary>
    /// Subscribes to distributed invalidation messages. Called during startup by the hosted service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task SubscribeToInvalidationsAsync(CancellationToken cancellationToken)
    {
        if (_pubSub is null || !_options.EnableDistributedInvalidation)
        {
            return;
        }

        _subscription = await _pubSub.SubscribeAsync<DirectoryCacheInvalidationMessage>(
            _options.InvalidationChannel,
            message =>
            {
                if (message.IsRemoval)
                {
                    _l1Cache.TryRemove(message.Key, out _);
                }
                else if (message.ShardId is not null)
                {
                    _l1Cache[message.Key] = message.ShardId;
                }

                _logger.LogTrace(
                    "Applied distributed invalidation for key '{Key}', removal={IsRemoval}",
                    message.Key,
                    message.IsRemoval);

                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Subscribed to directory invalidation channel '{Channel}'",
            _options.InvalidationChannel);
    }

    /// <summary>
    /// Unsubscribes from distributed invalidation messages. Called during shutdown.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task UnsubscribeFromInvalidationsAsync()
    {
        if (_subscription is not null)
        {
            await _subscription.DisposeAsync().ConfigureAwait(false);
            _subscription = null;
        }
    }

    private void PublishInvalidation(string key, string? shardId, bool isRemoval)
    {
        if (_pubSub is null || !_options.EnableDistributedInvalidation)
        {
            return;
        }

        var message = new DirectoryCacheInvalidationMessage(key, shardId, isRemoval);

        // Fire-and-forget: pub/sub invalidation should not block the synchronous write path
        _ = Task.Run(async () =>
        {
            try
            {
                await _pubSub.PublishAsync(
                    _options.InvalidationChannel,
                    message,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish directory invalidation for key '{Key}'", key);
            }
        });
    }
}
#pragma warning restore CA1848
