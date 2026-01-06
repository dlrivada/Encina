using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Encina.Caching;

namespace Encina.Testing.Fakes.Providers;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ICacheProvider"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the cache provider interface using an in-memory
/// concurrent dictionary. All operations are synchronous but return completed tasks
/// for interface compatibility.
/// </para>
/// <para>
/// This provider tracks all operations for verification in tests:
/// <list type="bullet">
/// <item><description><see cref="CachedKeys"/>: All keys that have been set</description></item>
/// <item><description><see cref="RemovedKeys"/>: All keys that have been removed</description></item>
/// <item><description><see cref="GetOperations"/>: All keys that have been requested via Get</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class FakeCacheProvider : ICacheProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentBag<string> _cachedKeys = new();
    private readonly ConcurrentBag<string> _removedKeys = new();
    private readonly ConcurrentBag<string> _getOperations = new();
    private readonly object _lock = new();
    private readonly Timer? _expirationTimer;
    private bool _disposed;

    /// <summary>
    /// Gets all keys currently in the cache.
    /// </summary>
    public IReadOnlyCollection<string> Keys => _cache.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Gets all keys that have been set (for verification).
    /// </summary>
    public IReadOnlyList<string> CachedKeys
    {
        get
        {
            lock (_lock)
            {
                return _cachedKeys.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets all keys that have been removed (for verification).
    /// </summary>
    public IReadOnlyList<string> RemovedKeys
    {
        get
        {
            lock (_lock)
            {
                return _removedKeys.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets all keys requested via Get operations (for verification).
    /// </summary>
    public IReadOnlyList<string> GetOperations
    {
        get
        {
            lock (_lock)
            {
                return _getOperations.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets the number of items currently in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets or sets whether to simulate cache errors.
    /// When true, all operations will throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public bool SimulateErrors { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeCacheProvider"/> class.
    /// </summary>
    /// <param name="enableExpirationTimer">
    /// If true, enables a background timer that checks for expired entries every second.
    /// Default is false for performance in most test scenarios.
    /// </param>
    public FakeCacheProvider(bool enableExpirationTimer = false)
    {
        if (enableExpirationTimer)
        {
            _expirationTimer = new Timer(
                _ => CleanupExpiredEntries(),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
        }
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        lock (_lock)
        {
            _getOperations.Add(key);
        }

        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult<T?>(default);
            }

            // Refresh sliding expiration if applicable
            entry.RefreshSlidingExpiration();

            return Task.FromResult((T?)entry.Value);
        }

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = new CacheEntry(value, expiration);
        _cache[key] = entry;

        lock (_lock)
        {
            _cachedKeys.Add(key);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        _cache.TryRemove(key, out _);

        lock (_lock)
        {
            _removedKeys.Add(key);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        var regex = GlobToRegex(pattern);
        var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);

            lock (_lock)
            {
                _removedKeys.Add(key);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    /// <inheritdoc />
    public Task SetWithSlidingExpirationAsync<T>(
        string key,
        T value,
        TimeSpan slidingExpiration,
        TimeSpan? absoluteExpiration,
        CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = new CacheEntry(value, absoluteExpiration, slidingExpiration);
        _cache[key] = entry;

        lock (_lock)
        {
            _cachedKeys.Add(key);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult(false);
            }

            entry.RefreshSlidingExpiration();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Checks if a key was cached (set at any point).
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was cached.</returns>
    public bool WasKeyCached(string key)
    {
        lock (_lock)
        {
            return _cachedKeys.Contains(key);
        }
    }

    /// <summary>
    /// Checks if a key was removed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was removed.</returns>
    public bool WasKeyRemoved(string key)
    {
        lock (_lock)
        {
            return _removedKeys.Contains(key);
        }
    }

    /// <summary>
    /// Checks if a key was requested via Get.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was requested.</returns>
    public bool WasKeyRequested(string key)
    {
        lock (_lock)
        {
            return _getOperations.Contains(key);
        }
    }

    /// <summary>
    /// Gets the number of times a key was requested via Get.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>The number of Get operations for the key.</returns>
    public int GetRequestCount(string key)
    {
        lock (_lock)
        {
            return _getOperations.Count(k => k == key);
        }
    }

    /// <summary>
    /// Gets all keys that match a pattern.
    /// </summary>
    /// <param name="pattern">The glob pattern to match.</param>
    /// <returns>All matching keys.</returns>
    public IReadOnlyList<string> GetKeysByPattern(string pattern)
    {
        var regex = GlobToRegex(pattern);
        return _cache.Keys.Where(k => regex.IsMatch(k)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a cached value directly for test assertions.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value if found; otherwise, default.</returns>
    public T? GetValue<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            return (T?)entry.Value;
        }

        return default;
    }

    /// <summary>
    /// Clears all cached items and resets verification state.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _cachedKeys.Clear();
            _removedKeys.Clear();
            _getOperations.Clear();
        }
    }

    /// <summary>
    /// Clears only the tracking collections while leaving cached items intact.
    /// </summary>
    public void ClearTracking()
    {
        lock (_lock)
        {
            _cachedKeys.Clear();
            _removedKeys.Clear();
            _getOperations.Clear();
        }
    }

    /// <summary>
    /// Disposes the fake cache provider.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _expirationTimer?.Dispose();
        _disposed = true;
    }

    private void ThrowIfSimulatingErrors()
    {
        if (SimulateErrors)
        {
            throw new InvalidOperationException("FakeCacheProvider is configured to simulate errors.");
        }
    }

    private void CleanupExpiredEntries()
    {
        foreach (var key in _cache.Keys.ToList())
        {
            if (_cache.TryGetValue(key, out var entry) && entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private static Regex GlobToRegex(string pattern)
    {
        // Convert glob pattern to regex
        // * -> .*
        // ? -> .
        // [abc] -> [abc]
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".")
            .Replace("\\[", "[")
            .Replace("\\]", "]") + "$";

        return new Regex(regexPattern, RegexOptions.Compiled);
    }

    private sealed class CacheEntry
    {
        private readonly DateTime _createdAt;
        private readonly TimeSpan? _absoluteExpiration;
        private readonly TimeSpan? _slidingExpiration;
        private DateTime _lastAccessedAt;

        public object? Value { get; }

        public bool IsExpired
        {
            get
            {
                var now = DateTime.UtcNow;

                // Check absolute expiration
                if (_absoluteExpiration.HasValue && now >= _createdAt + _absoluteExpiration.Value)
                {
                    return true;
                }

                // Check sliding expiration
                if (_slidingExpiration.HasValue && now >= _lastAccessedAt + _slidingExpiration.Value)
                {
                    return true;
                }

                return false;
            }
        }

        public CacheEntry(object? value, TimeSpan? expiration)
        {
            Value = value;
            _createdAt = DateTime.UtcNow;
            _lastAccessedAt = _createdAt;
            _absoluteExpiration = expiration;
        }

        public CacheEntry(object? value, TimeSpan? absoluteExpiration, TimeSpan? slidingExpiration)
        {
            Value = value;
            _createdAt = DateTime.UtcNow;
            _lastAccessedAt = _createdAt;
            _absoluteExpiration = absoluteExpiration;
            _slidingExpiration = slidingExpiration;
        }

        public void RefreshSlidingExpiration()
        {
            if (_slidingExpiration.HasValue)
            {
                _lastAccessedAt = DateTime.UtcNow;
            }
        }
    }
}
