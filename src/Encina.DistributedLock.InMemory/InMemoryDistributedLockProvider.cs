using System.Collections.Concurrent;

namespace Encina.DistributedLock.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IDistributedLockProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// This provider is designed for testing and single-instance scenarios.
/// It is NOT suitable for production multi-instance deployments.
/// </para>
/// <para>
/// Locks are stored in memory and will be lost if the process restarts.
/// For production use, consider Redis or SQL Server providers.
/// </para>
/// </remarks>
public sealed partial class InMemoryDistributedLockProvider : IDistributedLockProvider
{
    private readonly ConcurrentDictionary<string, LockEntry> _locks = new();
    private readonly InMemoryLockOptions _options;
    private readonly ILogger<InMemoryDistributedLockProvider> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDistributedLockProvider"/> class.
    /// </summary>
    /// <param name="options">The lock options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public InMemoryDistributedLockProvider(
        IOptions<InMemoryLockOptions> options,
        ILogger<InMemoryDistributedLockProvider> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
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

        var lockKey = GetLockKey(resource);
        var lockValue = Guid.NewGuid().ToString();
        var deadline = _timeProvider.GetUtcNow().Add(wait);

        while (_timeProvider.GetUtcNow() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Clean up expired locks
            CleanupExpiredLocks();

            var now = _timeProvider.GetUtcNow();
            var entry = new LockEntry(lockValue, now.Add(expiry));

            if (_locks.TryAdd(lockKey, entry))
            {
                LogLockAcquired(_logger, resource, lockValue);
                return new LockHandle(this, lockKey, lockValue, resource, now.DateTime, entry.ExpiresAtUtc.DateTime, _logger, _timeProvider);
            }

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

        var lockKey = GetLockKey(resource);
        var lockValue = Guid.NewGuid().ToString();
        var retryInterval = TimeSpan.FromMilliseconds(100);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Clean up expired locks
            CleanupExpiredLocks();

            var now = _timeProvider.GetUtcNow();
            var entry = new LockEntry(lockValue, now.Add(expiry));

            if (_locks.TryAdd(lockKey, entry))
            {
                LogLockAcquired(_logger, resource, lockValue);
                return new LockHandle(this, lockKey, lockValue, resource, now.DateTime, entry.ExpiresAtUtc.DateTime, _logger, _timeProvider);
            }

            await Task.Delay(retryInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsLockedAsync(string resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        var lockKey = GetLockKey(resource);

        // Clean up expired locks first
        CleanupExpiredLocks();

        return Task.FromResult(_locks.ContainsKey(lockKey));
    }

    /// <inheritdoc/>
    public Task<bool> ExtendAsync(string resource, TimeSpan extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        var lockKey = GetLockKey(resource);

        if (_locks.TryGetValue(lockKey, out var existingEntry))
        {
            var newExpiry = _timeProvider.GetUtcNow().Add(extension);
            var newEntry = existingEntry with { ExpiresAtUtc = newExpiry };

            if (_locks.TryUpdate(lockKey, newEntry, existingEntry))
            {
                LogLockExtended(_logger, resource, extension);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    private string GetLockKey(string resource)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? $"lock:{resource}"
            : $"{_options.KeyPrefix}:lock:{resource}";
    }

    private void CleanupExpiredLocks()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var kvp in _locks)
        {
            if (kvp.Value.ExpiresAtUtc <= now)
            {
                _locks.TryRemove(kvp.Key, out _);
            }
        }
    }

    internal bool TryReleaseLock(string lockKey, string lockValue)
    {
        if (_locks.TryGetValue(lockKey, out var entry) && entry.LockValue == lockValue)
        {
            return _locks.TryRemove(lockKey, out _);
        }

        return false;
    }

    internal bool TryExtendLock(string lockKey, string lockValue, TimeSpan extension)
    {
        if (_locks.TryGetValue(lockKey, out var existingEntry) && existingEntry.LockValue == lockValue)
        {
            var newExpiry = _timeProvider.GetUtcNow().Add(extension);
            var newEntry = existingEntry with { ExpiresAtUtc = newExpiry };

            return _locks.TryUpdate(lockKey, newEntry, existingEntry);
        }

        return false;
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
    internal static partial void LogLockReleased(ILogger logger, string resource, string lockId);

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

    private sealed record LockEntry(string LockValue, DateTimeOffset ExpiresAtUtc);

    private sealed class LockHandle : ILockHandle
    {
        private readonly InMemoryDistributedLockProvider _provider;
        private readonly string _lockKey;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private bool _disposed;

        public LockHandle(
            InMemoryDistributedLockProvider provider,
            string lockKey,
            string lockId,
            string resource,
            DateTime acquiredAtUtc,
            DateTime expiresAtUtc,
            ILogger logger,
            TimeProvider timeProvider)
        {
            _provider = provider;
            _lockKey = lockKey;
            LockId = lockId;
            Resource = resource;
            AcquiredAtUtc = acquiredAtUtc;
            ExpiresAtUtc = expiresAtUtc;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public string Resource { get; }
        public string LockId { get; }
        public DateTime AcquiredAtUtc { get; }
        public DateTime ExpiresAtUtc { get; private set; }
        public bool IsReleased => _disposed;

        public Task<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return Task.FromResult(false);
            }

            var result = _provider.TryExtendLock(_lockKey, LockId, extension);
            if (result)
            {
                ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(extension);
            }

            return Task.FromResult(result);
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            _provider.TryReleaseLock(_lockKey, LockId);
            LogLockReleased(_logger, Resource, LockId);

            return ValueTask.CompletedTask;
        }
    }
}
