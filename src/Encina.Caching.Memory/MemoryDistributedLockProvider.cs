using System.Collections.Concurrent;

namespace Encina.Caching.Memory;

/// <summary>
/// In-memory implementation of <see cref="IDistributedLockProvider"/> for single-instance scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This provider is useful for:
/// </para>
/// <list type="bullet">
/// <item><description>Development and testing without external dependencies</description></item>
/// <item><description>Single-instance applications that need resource coordination</description></item>
/// <item><description>Unit testing lock-protected flows</description></item>
/// </list>
/// <para>
/// For distributed locking across multiple instances, use Redis (Redlock) or database-based providers.
/// </para>
/// </remarks>
public sealed partial class MemoryDistributedLockProvider : IDistributedLockProvider
{
    private readonly ConcurrentDictionary<string, LockEntry> _locks = new();
    private readonly ILogger<MemoryDistributedLockProvider> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDistributedLockProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MemoryDistributedLockProvider(ILogger<MemoryDistributedLockProvider> logger)
        : this(logger, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDistributedLockProvider"/> class with a custom time provider.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for controlling time in tests.</param>
    public MemoryDistributedLockProvider(ILogger<MemoryDistributedLockProvider> logger, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiry,
        TimeSpan wait,
        TimeSpan retry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        var deadline = _timeProvider.GetUtcNow().UtcDateTime.Add(wait);
        var lockId = Guid.NewGuid().ToString();

        // Always attempt at least once, so a zero wait still takes a free lock. The retry delay
        // runs on the injected TimeProvider (as the deadline does), so a test clock drives the
        // whole wait (#531).
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handle = TryTakeLock(resource, lockId, expiry);
            if (handle is not null)
            {
                return handle;
            }

            var remaining = deadline - _timeProvider.GetUtcNow().UtcDateTime;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(NextDelay(retry, remaining), _timeProvider, cancellationToken).ConfigureAwait(false);
        }

        LogLockFailed(_logger, resource);
        return null;
    }

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        var lockId = Guid.NewGuid().ToString();
        var retryInterval = TimeSpan.FromMilliseconds(100);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handle = TryTakeLock(resource, lockId, expiry);
            if (handle is not null)
            {
                return handle;
            }

            await Task.Delay(retryInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The wait before the next attempt: the retry interval, cut to the time left before the deadline,
    /// and never under one millisecond, because a zero delay completes at once and would spin the loop
    /// without ever letting a test clock advance.
    /// </summary>
    private static TimeSpan NextDelay(TimeSpan retry, TimeSpan remaining)
        => TimeSpan.FromMilliseconds(Math.Max(1, Math.Min(retry.TotalMilliseconds, remaining.TotalMilliseconds)));

    private LockHandle? TryTakeLock(string resource, string lockId, TimeSpan expiry)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var newEntry = new LockEntry(lockId, now.Add(expiry));

        // Add when free, or replace an expired entry only if it is still the one that was read.
        var taken = _locks.TryGetValue(resource, out var existingEntry)
            ? existingEntry.ExpiresAt <= now && _locks.TryUpdate(resource, newEntry, existingEntry)
            : _locks.TryAdd(resource, newEntry);

        if (!taken)
        {
            return null;
        }

        LogLockAcquired(_logger, resource, lockId);
        return new LockHandle(this, resource, lockId);
    }

    /// <inheritdoc/>
    public Task<bool> IsLockedAsync(string resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        if (_locks.TryGetValue(resource, out var entry))
        {
            // Check if the lock has expired
            return Task.FromResult(entry.ExpiresAt > _timeProvider.GetUtcNow().UtcDateTime);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> ExtendAsync(string resource, TimeSpan extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        if (_locks.TryGetValue(resource, out var entry))
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (entry.ExpiresAt > now)
            {
                var newExpiry = now.Add(extension);
                var newEntry = entry with { ExpiresAt = newExpiry };

                if (_locks.TryUpdate(resource, newEntry, entry))
                {
                    LogLockExtended(_logger, resource, extension);
                    return Task.FromResult(true);
                }
            }
        }

        return Task.FromResult(false);
    }

    private void ReleaseLock(string resource, string lockId)
    {
        // Compare-and-remove, so a release never deletes a newer owner's entry; it reads again when
        // a concurrent extension replaced this owner's entry between the read and the remove.
        while (_locks.TryGetValue(resource, out var entry) && entry.LockId == lockId)
        {
            if (_locks.TryRemove(new KeyValuePair<string, LockEntry>(resource, entry)))
            {
                LogLockReleased(_logger, resource, lockId);
                return;
            }
        }
    }

    [LoggerMessage(
        EventId = 3556,
        Level = LogLevel.Debug,
        Message = "Lock acquired on resource: {Resource} with lockId: {LockId}")]
    private static partial void LogLockAcquired(ILogger logger, string resource, string lockId);

    [LoggerMessage(
        EventId = 3557,
        Level = LogLevel.Debug,
        Message = "Lock released on resource: {Resource} with lockId: {LockId}")]
    private static partial void LogLockReleased(ILogger logger, string resource, string lockId);

    [LoggerMessage(
        EventId = 3558,
        Level = LogLevel.Debug,
        Message = "Lock extended on resource: {Resource} by {Extension}")]
    private static partial void LogLockExtended(ILogger logger, string resource, TimeSpan extension);

    [LoggerMessage(
        EventId = 3559,
        Level = LogLevel.Debug,
        Message = "Failed to acquire lock on resource: {Resource}")]
    private static partial void LogLockFailed(ILogger logger, string resource);

    private sealed record LockEntry(string LockId, DateTime ExpiresAt);

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly MemoryDistributedLockProvider _provider;
        private readonly string _resource;
        private readonly string _lockId;
        private bool _disposed;

        public LockHandle(MemoryDistributedLockProvider provider, string resource, string lockId)
        {
            _provider = provider;
            _resource = resource;
            _lockId = lockId;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _provider.ReleaseLock(_resource, _lockId);
            }
            return ValueTask.CompletedTask;
        }
    }
}
