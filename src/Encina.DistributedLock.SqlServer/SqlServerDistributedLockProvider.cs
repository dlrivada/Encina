using System.Data;

namespace Encina.DistributedLock.SqlServer;

/// <summary>
/// SQL Server implementation of <see cref="IDistributedLockProvider"/> using sp_getapplock.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses SQL Server's sp_getapplock and sp_releaseapplock stored procedures
/// for distributed locking with database-level consistency.
/// </para>
/// <para>
/// Locks are scoped to the current database and are automatically released when the
/// connection or transaction is closed.
/// </para>
/// </remarks>
public sealed partial class SqlServerDistributedLockProvider : IDistributedLockProvider
{
    private readonly string _connectionString;
    private readonly SqlServerLockOptions _options;
    private readonly ILogger<SqlServerDistributedLockProvider> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDistributedLockProvider"/> class.
    /// </summary>
    /// <param name="options">The lock options containing the connection string.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public SqlServerDistributedLockProvider(
        IOptions<SqlServerLockOptions> options,
        ILogger<SqlServerDistributedLockProvider> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _connectionString = _options.ConnectionString
            ?? throw new ArgumentException("ConnectionString is required", nameof(options));
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
        var lockId = Guid.NewGuid().ToString();
        var timeoutMs = (int)wait.TotalMilliseconds;

        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await TryAcquireLockAsync(connection, lockKey, timeoutMs, cancellationToken)
                .ConfigureAwait(false);

            if (result >= 0) // 0 or positive means lock acquired
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                LogLockAcquired(_logger, resource, lockId);
                return new LockHandle(connection, lockKey, lockId, resource, now, now.Add(expiry), _logger, _timeProvider);
            }

            // Lock not acquired, clean up connection
            await connection.DisposeAsync().ConfigureAwait(false);
            LogLockFailed(_logger, resource);
            return null;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
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
        var lockId = Guid.NewGuid().ToString();

        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Use -1 for infinite wait
            var result = await TryAcquireLockAsync(connection, lockKey, -1, cancellationToken)
                .ConfigureAwait(false);

            if (result >= 0)
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                LogLockAcquired(_logger, resource, lockId);
                return new LockHandle(connection, lockKey, lockId, resource, now, now.Add(expiry), _logger, _timeProvider);
            }

            // This shouldn't happen with infinite wait, but handle it anyway
            await connection.DisposeAsync().ConfigureAwait(false);
            throw new LockAcquisitionException(resource);
        }
        catch (Exception ex) when (ex is not LockAcquisitionException)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw new LockAcquisitionException(resource, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsLockedAsync(string resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();

        var lockKey = GetLockKey(resource);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Try to acquire with 0 timeout - if it fails, the lock is held
        var result = await TryAcquireLockAsync(connection, lockKey, 0, cancellationToken)
            .ConfigureAwait(false);

        if (result >= 0)
        {
            // We got the lock, release it immediately
            await ReleaseLockAsync(connection, lockKey, cancellationToken).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public Task<bool> ExtendAsync(string resource, TimeSpan extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);

        // SQL Server app locks don't support extension - they're held until released
        // The lock is automatically extended by keeping the connection open
        LogLockExtended(_logger, resource, extension);
        return Task.FromResult(true);
    }

    private static async Task<int> TryAcquireLockAsync(
        SqlConnection connection,
        string lockKey,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "sp_getapplock";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@Resource", lockKey);
        command.Parameters.AddWithValue("@LockMode", "Exclusive");
        command.Parameters.AddWithValue("@LockOwner", "Session");
        command.Parameters.AddWithValue("@LockTimeout", timeoutMs);

        var returnValue = new SqlParameter("@ReturnValue", SqlDbType.Int)
        {
            Direction = ParameterDirection.ReturnValue
        };
        command.Parameters.Add(returnValue);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return (int)returnValue.Value!;
    }

    private static async Task ReleaseLockAsync(
        SqlConnection connection,
        string lockKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "sp_releaseapplock";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@Resource", lockKey);
        command.Parameters.AddWithValue("@LockOwner", "Session");

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

    private sealed class LockHandle : ILockHandle
    {
        private readonly SqlConnection _connection;
        private readonly string _lockKey;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private bool _disposed;

        public LockHandle(
            SqlConnection connection,
            string lockKey,
            string lockId,
            string resource,
            DateTime acquiredAtUtc,
            DateTime expiresAtUtc,
            ILogger logger,
            TimeProvider timeProvider)
        {
            _connection = connection;
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

            // SQL Server locks are held until released - just update the expected expiry
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(extension);
            return Task.FromResult(true);
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
                if (_connection.State == ConnectionState.Open)
                {
                    await using var command = _connection.CreateCommand();
                    command.CommandText = "sp_releaseapplock";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Resource", _lockKey);
                    command.Parameters.AddWithValue("@LockOwner", "Session");

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                LogLockReleased(_logger, Resource, LockId);
            }
            catch
            {
                // Log but don't throw - closing connection will release the lock anyway
            }
            finally
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
