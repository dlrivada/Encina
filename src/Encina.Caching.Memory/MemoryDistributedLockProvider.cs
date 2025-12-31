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

        while (_timeProvider.GetUtcNow().UtcDateTime < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Try to acquire the lock
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var expiresAt = now.Add(expiry);

            var newEntry = new LockEntry(lockId, expiresAt);

            // Check if there's an existing lock
            if (_locks.TryGetValue(resource, out var existingEntry))
            {
                // Check if the existing lock has expired
                if (existingEntry.ExpiresAt <= now)
                {
                    // Try to replace the expired lock
                    if (_locks.TryUpdate(resource, newEntry, existingEntry))
                    {
                        LogLockAcquired(_logger, resource, lockId);
                        return new LockHandle(this, resource, lockId);
                    }
                }
            }
            else
            {
                // No existing lock, try to add
                if (_locks.TryAdd(resource, newEntry))
                {
                    LogLockAcquired(_logger, resource, lockId);
                    return new LockHandle(this, resource, lockId);
                }
            }

            // Wait before retrying
            await Task.Delay(retry, cancellationToken).ConfigureAwait(false);
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

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var expiresAt = now.Add(expiry);
            var newEntry = new LockEntry(lockId, expiresAt);

            // Check if there's an existing lock
            if (_locks.TryGetValue(resource, out var existingEntry))
            {
                // Check if the existing lock has expired
                if (existingEntry.ExpiresAt <= now)
                {
                    // Try to replace the expired lock
                    if (_locks.TryUpdate(resource, newEntry, existingEntry))
                    {
                        LogLockAcquired(_logger, resource, lockId);
                        return new LockHandle(this, resource, lockId);
                    }
                }
            }
            else
            {
                // No existing lock, try to add
                if (_locks.TryAdd(resource, newEntry))
                {
                    LogLockAcquired(_logger, resource, lockId);
                    return new LockHandle(this, resource, lockId);
                }
            }

            await Task.Delay(retryInterval, cancellationToken).ConfigureAwait(false);
        }
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
        if (_locks.TryGetValue(resource, out var entry) && entry.LockId == lockId)
        {
            _locks.TryRemove(resource, out _);
            LogLockReleased(_logger, resource, lockId);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Lock acquired on resource: {Resource} with lockId: {LockId}")]
    private static partial void LogLockAcquired(ILogger logger, string resource, string lockId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Lock released on resource: {Resource} with lockId: {LockId}")]
    private static partial void LogLockReleased(ILogger logger, string resource, string lockId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Lock extended on resource: {Resource} by {Extension}")]
    private static partial void LogLockExtended(ILogger logger, string resource, TimeSpan extension);

    [LoggerMessage(
        EventId = 4,
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
