using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Encina.Caching.Memory;

/// <summary>
/// In-memory implementation of <see cref="ICacheProvider"/> using <see cref="IMemoryCache"/>.
/// </summary>
/// <remarks>
/// <para>
/// This provider is ideal for:
/// </para>
/// <list type="bullet">
/// <item><description>Single-instance applications</description></item>
/// <item><description>Development and testing scenarios</description></item>
/// <item><description>Caching data that doesn't need to be shared across instances</description></item>
/// </list>
/// <para>
/// For distributed caching across multiple application instances, use Redis, Garnet, or NCache providers.
/// </para>
/// </remarks>
public sealed partial class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheOptions _options;
    private readonly ILogger<MemoryCacheProvider> _logger;

    // Track all keys for pattern-based invalidation
    private readonly ConcurrentDictionary<string, byte> _keyTracker = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheProvider"/> class.
    /// </summary>
    /// <param name="cache">The underlying memory cache.</param>
    /// <param name="options">The caching options.</param>
    /// <param name="logger">The logger.</param>
    public MemoryCacheProvider(
        IMemoryCache cache,
        IOptions<MemoryCacheOptions> options,
        ILogger<MemoryCacheProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue(key, out var value))
        {
            LogCacheHit(_logger, key);
            return Task.FromResult((T?)value);
        }

        LogCacheMiss(_logger, key);
        return Task.FromResult(default(T?));
    }

    /// <inheritdoc/>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration
        };

        // Track the key for pattern-based invalidation
        _keyTracker.TryAdd(key, 0);

        // Register a callback to remove from tracker when evicted
        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            if (k is string keyString)
            {
                _keyTracker.TryRemove(keyString, out _);
            }
        });

        _cache.Set(key, value, options);
        LogCacheSet(_logger, key, expiration ?? _options.DefaultExpiration);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        _cache.Remove(key);
        _keyTracker.TryRemove(key, out _);
        LogCacheRemove(_logger, key);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        var regex = GlobToRegex(pattern);
        var keysToRemove = _keyTracker.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keyTracker.TryRemove(key, out _);
        }

        LogCacheRemovePattern(_logger, pattern, keysToRemove.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_cache.TryGetValue(key, out _));
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

        if (_cache.TryGetValue(key, out var existingValue))
        {
            LogCacheHit(_logger, key);
            return (T)existingValue!;
        }

        // Use GetOrCreate for atomic operation
        var result = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration;

            // Track the key
            _keyTracker.TryAdd(key, 0);

            entry.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                if (k is string keyString)
                {
                    _keyTracker.TryRemove(keyString, out _);
                }
            });

            return await factory(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        LogCacheSet(_logger, key, expiration ?? _options.DefaultExpiration);

        return result!;
    }

    /// <inheritdoc/>
    public Task SetWithSlidingExpirationAsync<T>(
        string key,
        T value,
        TimeSpan slidingExpiration,
        TimeSpan? absoluteExpiration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration
        };

        if (absoluteExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }

        // Track the key
        _keyTracker.TryAdd(key, 0);

        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            if (k is string keyString)
            {
                _keyTracker.TryRemove(keyString, out _);
            }
        });

        _cache.Set(key, value, options);
        LogCacheSetSliding(_logger, key, slidingExpiration, absoluteExpiration);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        // For memory cache, simply accessing the value refreshes the sliding expiration
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    private static Regex GlobToRegex(string pattern)
    {
        // Convert glob pattern to regex
        // * -> .* (match any sequence)
        // ? -> . (match single character)
        // [abc] -> [abc] (character class)
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
        Message = "Cache hit for key: {Key}")]
    private static partial void LogCacheHit(ILogger logger, string key);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Cache miss for key: {Key}")]
    private static partial void LogCacheMiss(ILogger logger, string key);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Cache set for key: {Key} with expiration: {Expiration}")]
    private static partial void LogCacheSet(ILogger logger, string key, TimeSpan expiration);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Cache set for key: {Key} with sliding expiration: {SlidingExpiration}, absolute: {AbsoluteExpiration}")]
    private static partial void LogCacheSetSliding(ILogger logger, string key, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Cache remove for key: {Key}")]
    private static partial void LogCacheRemove(ILogger logger, string key);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Cache remove by pattern: {Pattern}, removed {Count} keys")]
    private static partial void LogCacheRemovePattern(ILogger logger, string pattern, int count);
}

/// <summary>
/// Options for the memory cache provider.
/// </summary>
public sealed class MemoryCacheOptions
{
    /// <summary>
    /// Gets or sets the default expiration time for cached items.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
}
