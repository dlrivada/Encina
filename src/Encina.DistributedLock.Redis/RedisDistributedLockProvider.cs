namespace Encina.DistributedLock.Redis;

/// <summary>
/// Redis implementation of <see cref="IDistributedLockProvider"/> using StackExchange.Redis.
/// </summary>
/// <remarks>
/// <para>
/// This provider implements distributed locking using Redis SET NX with expiration.
/// For production use with multiple Redis instances, consider implementing the Redlock algorithm.
/// </para>
/// <para>
/// This provider is wire-compatible with Redis, Garnet, Valkey, Dragonfly, and KeyDB.
/// </para>
/// </remarks>
public sealed partial class RedisDistributedLockProvider : IDistributedLockProvider
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisLockOptions _options;
    private readonly ILogger<RedisDistributedLockProvider> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisDistributedLockProvider"/> class.
    /// </summary>
    /// <param name="connection">The Redis connection multiplexer.</param>
    /// <param name="options">The lock options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public RedisDistributedLockProvider(
        IConnectionMultiplexer connection,
        IOptions<RedisLockOptions> options,
        ILogger<RedisDistributedLockProvider> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private IDatabase Database => _connection.GetDatabase(_options.Database);

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
        var deadline = _timeProvider.GetUtcNow().UtcDateTime.Add(wait);

        while (_timeProvider.GetUtcNow().UtcDateTime < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = await Database.StringSetAsync(
                lockKey,
                lockValue,
                expiry,
                When.NotExists).ConfigureAwait(false);

            if (acquired)
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                LogLockAcquired(_logger, resource, lockValue);
                var context = new LockHandleContext(Database, lockKey, lockValue, resource, now, now.Add(expiry), _logger, _timeProvider);
                return new LockHandle(context);
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

            var acquired = await Database.StringSetAsync(
                lockKey,
                lockValue,
                expiry,
                When.NotExists).ConfigureAwait(false);

            if (acquired)
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                LogLockAcquired(_logger, resource, lockValue);
                var context = new LockHandleContext(Database, lockKey, lockValue, resource, now, now.Add(expiry), _logger, _timeProvider);
                return new LockHandle(context);
            }

            await Task.Delay(retryInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsLockedAsync(string resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        var lockKey = GetLockKey(resource);
        return await Database.KeyExistsAsync(lockKey).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> ExtendAsync(string resource, TimeSpan extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        var lockKey = GetLockKey(resource);

        // Extend the TTL
        var extended = await Database.KeyExpireAsync(lockKey, extension).ConfigureAwait(false);

        if (extended)
        {
            LogLockExtended(_logger, resource, extension);
        }

        return extended;
    }

    private string GetLockKey(string resource)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? $"lock:{resource}"
            : $"{_options.KeyPrefix}:lock:{resource}";
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

    /// <summary>
    /// Context information for a lock handle.
    /// </summary>
    private sealed record LockHandleContext(
        IDatabase Database,
        string LockKey,
        string LockValue,
        string Resource,
        DateTime AcquiredAtUtc,
        DateTime ExpiresAtUtc,
        ILogger Logger,
        TimeProvider TimeProvider);

    private sealed class LockHandle : ILockHandle
    {
        private readonly LockHandleContext _context;
        private bool _disposed;
        private DateTime _expiresAtUtc;

        // Lua script to safely release the lock only if we own it
        private const string ReleaseLockScript = """
            if redis.call("get", KEYS[1]) == ARGV[1] then
                return redis.call("del", KEYS[1])
            else
                return 0
            end
            """;

        // Lua script to safely extend the lock only if we own it
        private const string ExtendLockScript = """
            if redis.call("get", KEYS[1]) == ARGV[1] then
                return redis.call("pexpire", KEYS[1], ARGV[2])
            else
                return 0
            end
            """;

        public LockHandle(LockHandleContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _expiresAtUtc = context.ExpiresAtUtc;
        }

        public string Resource => _context.Resource;
        public string LockId => _context.LockValue;
        public DateTime AcquiredAtUtc => _context.AcquiredAtUtc;
        public DateTime ExpiresAtUtc => _expiresAtUtc;
        public bool IsReleased => _disposed;

        public async Task<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return false;
            }

            var extensionMs = (long)extension.TotalMilliseconds;

            var result = await _context.Database.ScriptEvaluateAsync(
                ExtendLockScript,
                [_context.LockKey],
                [_context.LockValue, extensionMs]).ConfigureAwait(false);

            if ((int)result == 1)
            {
                _expiresAtUtc = _context.TimeProvider.GetUtcNow().UtcDateTime.Add(extension);
                return true;
            }

            return false;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                await _context.Database.ScriptEvaluateAsync(
                    ReleaseLockScript,
                    [_context.LockKey],
                    [_context.LockValue]).ConfigureAwait(false);

                LogLockReleased(_context.Logger, Resource, LockId);
            }
            catch
            {
                // Log but don't throw - the lock will expire anyway
            }
        }
    }
}
