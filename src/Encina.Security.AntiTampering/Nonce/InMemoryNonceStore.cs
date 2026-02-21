using System.Collections.Concurrent;
using Encina.Security.AntiTampering.Abstractions;

namespace Encina.Security.AntiTampering.Nonce;

/// <summary>
/// In-memory implementation of <see cref="INonceStore"/> for single-instance deployments.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> to store nonces with their
/// expiration timestamps. Expired entries are cleaned up lazily during access operations
/// and periodically via a background timer.
/// </para>
/// <para>
/// <b>Not suitable for multi-instance deployments</b>: Each instance maintains its own
/// nonce store, so a nonce used against one instance is unknown to others.
/// For distributed scenarios, use <see cref="DistributedCacheNonceStore"/>.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for all operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryNonceStore(TimeProvider.System);
/// var added = await store.TryAddAsync("nonce-123", TimeSpan.FromMinutes(10));
/// // added == true (first time)
///
/// var duplicate = await store.TryAddAsync("nonce-123", TimeSpan.FromMinutes(10));
/// // duplicate == false (replay detected)
/// </code>
/// </example>
public sealed class InMemoryNonceStore : INonceStore, IDisposable
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _nonces = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Interval between periodic cleanup sweeps of expired nonces.
    /// </summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryNonceStore"/> class.
    /// </summary>
    /// <param name="timeProvider">
    /// The time provider for obtaining current UTC time.
    /// Pass <c>null</c> to use <see cref="TimeProvider.System"/>.
    /// </param>
    public InMemoryNonceStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _cleanupTimer = new Timer(
            _ => CleanupExpiredEntries(),
            state: null,
            dueTime: CleanupInterval,
            period: CleanupInterval);
    }

    /// <inheritdoc />
    public ValueTask<bool> TryAddAsync(
        string nonce,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        var expiresAt = _timeProvider.GetUtcNow().Add(expiry);
        var added = _nonces.TryAdd(nonce, expiresAt);

        if (!added)
        {
            // Key already exists — check if it has expired
            if (_nonces.TryGetValue(nonce, out var existingExpiry) &&
                existingExpiry <= _timeProvider.GetUtcNow())
            {
                // Expired entry: try to replace it
                if (_nonces.TryUpdate(nonce, expiresAt, existingExpiry))
                {
                    return ValueTask.FromResult(true);
                }
            }

            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        if (_nonces.TryGetValue(nonce, out var expiresAt))
        {
            if (expiresAt > _timeProvider.GetUtcNow())
            {
                return ValueTask.FromResult(true);
            }

            // Expired — clean up lazily
            _nonces.TryRemove(nonce, out _);
        }

        return ValueTask.FromResult(false);
    }

    /// <summary>
    /// Gets the number of nonces currently stored (including potentially expired entries).
    /// </summary>
    /// <remarks>
    /// Intended for testing and diagnostics only.
    /// </remarks>
    internal int Count => _nonces.Count;

    /// <summary>
    /// Removes all expired nonce entries.
    /// </summary>
    private void CleanupExpiredEntries()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var kvp in _nonces)
        {
            if (kvp.Value <= now)
            {
                _nonces.TryRemove(kvp.Key, out _);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}
