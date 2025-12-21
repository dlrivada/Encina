using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SimpleMediator.Caching.Hybrid;

/// <summary>
/// Hybrid cache implementation of <see cref="ICacheProvider"/> using Microsoft's HybridCache.
/// </summary>
/// <remarks>
/// <para>
/// HybridCache provides a two-tier caching solution combining:
/// </para>
/// <list type="bullet">
/// <item><description>L1: Fast in-memory cache (per-instance)</description></item>
/// <item><description>L2: Distributed cache (Redis, SQL Server, etc.) for cross-instance sharing</description></item>
/// </list>
/// <para>
/// Key features:
/// </para>
/// <list type="bullet">
/// <item><description>Built-in stampede protection - concurrent requests for the same key share a single factory execution</description></item>
/// <item><description>Tag-based invalidation - invalidate groups of related cache entries efficiently</description></item>
/// <item><description>Automatic serialization - uses configurable serializers for L2 storage</description></item>
/// </list>
/// </remarks>
public sealed partial class HybridCacheProvider : ICacheProvider
{
    private readonly HybridCache _cache;
    private readonly HybridCacheProviderOptions _options;
    private readonly ILogger<HybridCacheProvider> _logger;

    // Track keys for pattern-based invalidation (best-effort for hybrid scenarios)
    private readonly ConcurrentDictionary<string, HashSet<string>> _tagToKeys = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _keyToTags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridCacheProvider"/> class.
    /// </summary>
    /// <param name="cache">The underlying HybridCache instance.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="logger">The logger.</param>
    public HybridCacheProvider(
        HybridCache cache,
        IOptions<HybridCacheProviderOptions> options,
        ILogger<HybridCacheProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        // HybridCache doesn't have a simple Get without factory
        // We use GetOrCreateAsync and track factory invocation via captured variable
        var factoryCalled = false;
        var result = await _cache.GetOrCreateAsync<T?>(
            key,
            _ =>
            {
                // Factory being called means cache miss
                factoryCalled = true;
                return ValueTask.FromResult(default(T?));
            },
            new HybridCacheEntryOptions
            {
                // Minimal expiration to not cache the miss result
                Expiration = TimeSpan.FromSeconds(1),
                LocalCacheExpiration = TimeSpan.FromSeconds(1),
                Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (factoryCalled)
        {
            LogCacheMiss(_logger, key);
            return default;
        }

        LogCacheHit(_logger, key);
        return result;
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _options.DefaultExpiration,
            LocalCacheExpiration = _options.LocalCacheExpiration ?? expiration ?? _options.DefaultExpiration
        };

        await _cache.SetAsync(key, value, entryOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
        LogCacheSet(_logger, key, entryOptions.Expiration ?? _options.DefaultExpiration);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

        // Clean up tag tracking
        if (_keyToTags.TryRemove(key, out var tags))
        {
            foreach (var tag in tags)
            {
                if (_tagToKeys.TryGetValue(tag, out var keys))
                {
                    lock (keys)
                    {
                        keys.Remove(key);
                    }
                }
            }
        }

        LogCacheRemove(_logger, key);
    }

    /// <inheritdoc/>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        // HybridCache supports tag-based invalidation natively
        // For pattern-based, we check if it matches our tag pattern
        if (IsTagPattern(pattern, out var tag))
        {
            await RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
            return;
        }

        // For glob patterns, we need to rely on our key tracking
        var regex = GlobToRegex(pattern);
        var keysToRemove = _keyToTags.Keys.Where(k => regex.IsMatch(k)).ToList();
        var removedCount = 0;

        foreach (var key in keysToRemove)
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            _keyToTags.TryRemove(key, out _);
            removedCount++;
        }

        LogCacheRemovePattern(_logger, pattern, removedCount);
    }

    /// <summary>
    /// Removes all cache entries associated with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to invalidate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This leverages HybridCache's native tag-based invalidation for efficient bulk removal.
    /// </remarks>
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tag);
        cancellationToken.ThrowIfCancellationRequested();

        await _cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);

        // Clean up local tracking
        if (_tagToKeys.TryRemove(tag, out var keys))
        {
            foreach (var key in keys)
            {
                if (_keyToTags.TryGetValue(key, out var keyTags))
                {
                    lock (keyTags)
                    {
                        keyTags.Remove(tag);
                        if (keyTags.Count == 0)
                        {
                            _keyToTags.TryRemove(key, out _);
                        }
                    }
                }
            }
        }

        LogCacheRemoveByTag(_logger, tag);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        // HybridCache doesn't have an Exists method, so we try to get
        var result = await GetAsync<object>(key, cancellationToken).ConfigureAwait(false);
        return result is not null;
    }

    /// <inheritdoc/>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _options.DefaultExpiration,
            LocalCacheExpiration = _options.LocalCacheExpiration ?? expiration ?? _options.DefaultExpiration
        };

        // HybridCache provides built-in stampede protection
        var result = await _cache.GetOrCreateAsync(
            key,
            async ct => await factory(ct).ConfigureAwait(false),
            entryOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Gets a value from the cache, or creates and caches it if not found, with tag support.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the value if not cached.</param>
    /// <param name="expiration">Expiration time. If null, uses the default expiration.</param>
    /// <param name="tags">Tags to associate with this cache entry for bulk invalidation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cached or newly created value.</returns>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _options.DefaultExpiration,
            LocalCacheExpiration = _options.LocalCacheExpiration ?? expiration ?? _options.DefaultExpiration
        };

        var result = await _cache.GetOrCreateAsync(
            key,
            async ct => await factory(ct).ConfigureAwait(false),
            entryOptions,
            tags,
            cancellationToken).ConfigureAwait(false);

        // Track tags locally for pattern matching
        if (tags is { Count: > 0 })
        {
            TrackKeyTags(key, tags);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task SetWithSlidingExpirationAsync<T>(
        string key,
        T value,
        TimeSpan slidingExpiration,
        TimeSpan? absoluteExpiration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        // HybridCache doesn't support sliding expiration directly
        // Use the shorter of sliding or absolute as the expiration
        var effectiveExpiration = absoluteExpiration.HasValue
            ? TimeSpan.FromTicks(Math.Min(slidingExpiration.Ticks, absoluteExpiration.Value.Ticks))
            : slidingExpiration;

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = effectiveExpiration,
            LocalCacheExpiration = effectiveExpiration
        };

        await _cache.SetAsync(key, value, entryOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

        LogCacheSetSliding(_logger, key, slidingExpiration, absoluteExpiration);
    }

    /// <inheritdoc/>
    public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        // HybridCache doesn't support refresh directly
        // Return false to indicate refresh is not supported
        return Task.FromResult(false);
    }

    private void TrackKeyTags(string key, IReadOnlyCollection<string> tags)
    {
        var keyTags = _keyToTags.GetOrAdd(key, _ => []);
        lock (keyTags)
        {
            foreach (var tag in tags)
            {
                keyTags.Add(tag);

                var tagKeys = _tagToKeys.GetOrAdd(tag, _ => []);
                lock (tagKeys)
                {
                    tagKeys.Add(key);
                }
            }
        }
    }

    private static bool IsTagPattern(string pattern, out string tag)
    {
        // Check if pattern is a simple tag pattern like "tag:products" or "#products"
        if (pattern.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
        {
            tag = pattern[4..];
            return true;
        }

        if (pattern.StartsWith('#'))
        {
            tag = pattern[1..];
            return true;
        }

        tag = string.Empty;
        return false;
    }

    private static Regex GlobToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".")
            .Replace("\\[", "[")
            .Replace("\\]", "]") + "$";

        return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "HybridCache hit for key: {Key}")]
    private static partial void LogCacheHit(ILogger logger, string key);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "HybridCache miss for key: {Key}")]
    private static partial void LogCacheMiss(ILogger logger, string key);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "HybridCache set for key: {Key} with expiration: {Expiration}")]
    private static partial void LogCacheSet(ILogger logger, string key, TimeSpan expiration);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "HybridCache set for key: {Key} with sliding expiration: {SlidingExpiration}, absolute: {AbsoluteExpiration}")]
    private static partial void LogCacheSetSliding(ILogger logger, string key, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "HybridCache remove for key: {Key}")]
    private static partial void LogCacheRemove(ILogger logger, string key);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "HybridCache remove by pattern: {Pattern}, removed {Count} keys")]
    private static partial void LogCacheRemovePattern(ILogger logger, string pattern, int count);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "HybridCache remove by tag: {Tag}")]
    private static partial void LogCacheRemoveByTag(ILogger logger, string tag);
}

/// <summary>
/// Options for the hybrid cache provider.
/// </summary>
public sealed class HybridCacheProviderOptions
{
    /// <summary>
    /// Gets or sets the default expiration time for cached items in both L1 and L2.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the expiration time for L1 (local memory) cache.
    /// If null, uses the same expiration as the distributed cache.
    /// </summary>
    /// <remarks>
    /// Setting a shorter L1 expiration can help reduce memory pressure while
    /// still benefiting from the distributed cache for less frequently accessed items.
    /// </remarks>
    public TimeSpan? LocalCacheExpiration { get; set; }
}
