using System.Collections.Concurrent;
using Encina.Diagnostics;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Security.AntiTampering.Nonce;

/// <summary>
/// In-memory implementation of <see cref="INonceStore"/> for single-instance deployments.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> to store nonces with their
/// expiration timestamps. Expired entries are cleaned up lazily during access operations
/// and periodically by a cleanup timer created through the injected <see cref="TimeProvider"/>.
/// A failed cleanup sweep is logged (exception type and stack trace, never its message) and the
/// timer keeps running; the next sweep retries.
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
    private readonly ILogger _logger;
    private readonly ITimer _cleanupTimer;

    /// <summary>
    /// Interval between periodic cleanup sweeps of expired nonces.
    /// </summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryNonceStore"/> class.
    /// </summary>
    /// <param name="timeProvider">
    /// The time provider for obtaining current UTC time.
    /// Pass <c>null</c> to use <see cref="TimeProvider.System"/>. It also drives the cleanup timer.
    /// </param>
    /// <param name="logger">
    /// The logger for failed cleanup sweeps. Pass <c>null</c> to discard them.
    /// </param>
    public InMemoryNonceStore(TimeProvider? timeProvider = null, ILogger<InMemoryNonceStore>? logger = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? (ILogger)NullLogger<InMemoryNonceStore>.Instance;
        _cleanupTimer = _timeProvider.CreateTimer(
            _ => RunCleanupSweep(),
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
            var now = _timeProvider.GetUtcNow();

            if (expiresAt > now)
            {
                return ValueTask.FromResult(true);
            }

            // Expired — clean up lazily
            TryRemoveExpired(new KeyValuePair<string, DateTimeOffset>(nonce, expiresAt), now);
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
    /// The cleanup timer callback. It never throws: an exception escaping a timer callback on the
    /// system clock terminates the process, so a failed sweep is logged and the next one retries.
    /// </summary>
    private void RunCleanupSweep()
    {
        try
        {
            CleanupExpiredEntries();
        }
        catch (Exception ex)
        {
            AntiTamperingLogMessages.NonceCleanupFailed(_logger, ex.ForLogging());
        }
    }

    /// <summary>
    /// Removes all expired nonce entries.
    /// </summary>
    private void CleanupExpiredEntries()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var kvp in _nonces)
        {
            TryRemoveExpired(kvp, now);
        }
    }

    /// <summary>
    /// Removes an expired entry only if it still holds the expiry that was read, so a nonce that
    /// <see cref="TryAddAsync"/> replaced in the meantime is kept and its replay still rejected (#3836).
    /// </summary>
    /// <param name="observed">The entry as it was read.</param>
    /// <param name="now">The current time.</param>
    /// <returns><see langword="true"/> if the entry was removed.</returns>
    internal bool TryRemoveExpired(KeyValuePair<string, DateTimeOffset> observed, DateTimeOffset now) =>
        observed.Value <= now && _nonces.TryRemove(observed);

    /// <inheritdoc />
    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}
