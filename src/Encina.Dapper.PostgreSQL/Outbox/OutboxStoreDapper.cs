using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using LanguageExt;

namespace Encina.Dapper.PostgreSQL.Outbox;

/// <summary>
/// Dapper implementation of <see cref="IOutboxStore"/> for reliable event publishing.
/// Uses raw SQL queries for maximum performance and control.
/// </summary>
/// <remarks>
/// Every command enlists in the business transaction of the scope when one is active (see
/// <see cref="DbLease.Enlisted"/>), so the rows the transactional outbox behavior adds commit or roll back with
/// the request; without an accessor, or without an active transaction, commands run in autocommit.
/// </remarks>
public sealed class OutboxStoreDapper : IOutboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;
    private readonly IDbTransactionAccessor? _transactionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: outboxmessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <param name="transactionAccessor">
    /// Optional accessor of the scope's shared transaction; when a transaction is active the store's commands
    /// join it, otherwise they run in autocommit.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="tableName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is empty or whitespace.</exception>
    public OutboxStoreDapper(
        IDbConnection connection,
        string tableName = "outboxmessages",
        TimeProvider? timeProvider = null,
        IDbTransactionAccessor? transactionAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _transactionAccessor = transactionAccessor;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc)
                VALUES
                (@Id, @NotificationType, @Content, @CreatedAtUtc, @ProcessedAtUtc, @ErrorMessage, @RetryCount, @NextRetryAtUtc)";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            await lease.Connection.ExecuteAsync(new CommandDefinition(sql, message, lease.Transaction, cancellationToken: cancellationToken));
        }, "outbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IOutboxMessage>>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                SELECT id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc
                FROM {_tableName}
                WHERE processedatutc IS NULL
                  AND retrycount < @MaxRetries
                  AND (nextretryatutc IS NULL OR nextretryatutc <= @NowUtc)
                ORDER BY createdatutc
                LIMIT @BatchSize";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var messages = await lease.Connection.QueryAsync<OutboxMessage>(
                new CommandDefinition(
                    sql,
                    new { BatchSize = batchSize, MaxRetries = maxRetries, NowUtc = nowUtc },
                    lease.Transaction,
                    cancellationToken: cancellationToken));

            return (IEnumerable<IOutboxMessage>)messages.Cast<IOutboxMessage>().ToList();
        }, "outbox.get_pending_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmpty, nameof(messageId));

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET processedatutc = @NowUtc,
                    errormessage = NULL
                WHERE id = @MessageId";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            await lease.Connection.ExecuteAsync(
                new CommandDefinition(sql, new { MessageId = messageId, NowUtc = nowUtc }, lease.Transaction, cancellationToken: cancellationToken));
        }, "outbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmpty, nameof(messageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                UPDATE {_tableName}
                SET errormessage = @ErrorMessage,
                    retrycount = retrycount + 1,
                    nextretryatutc = @NextRetryAtUtc
                WHERE id = @MessageId";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            await lease.Connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        MessageId = messageId,
                        ErrorMessage = errorMessage,
                        NextRetryAtUtc = nextRetryAtUtc
                    },
                    lease.Transaction,
                    cancellationToken: cancellationToken));
        }, "outbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsExhaustedAsync(
        Guid messageId,
        string errorMessage,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmpty, nameof(messageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                UPDATE {_tableName}
                SET errormessage = @ErrorMessage,
                    retrycount = GREATEST(retrycount + 1, @MaxRetries),
                    nextretryatutc = NULL
                WHERE id = @MessageId";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            await lease.Connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        MessageId = messageId,
                        ErrorMessage = errorMessage,
                        MaxRetries = maxRetries
                    },
                    lease.Transaction,
                    cancellationToken: cancellationToken));
        }, "outbox.mark_exhausted_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetPendingCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                SELECT COUNT(*)
                FROM {_tableName}
                WHERE processedatutc IS NULL
                  AND retrycount < @MaxRetries";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            return (int)await lease.Connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, new { MaxRetries = maxRetries }, lease.Transaction, cancellationToken: cancellationToken));
        }, "outbox.get_pending_count_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetExhaustedCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                SELECT COUNT(*)
                FROM {_tableName}
                WHERE processedatutc IS NULL
                  AND retrycount >= @MaxRetries";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            return (int)await lease.Connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, new { MaxRetries = maxRetries }, lease.Transaction, cancellationToken: cancellationToken));
        }, "outbox.get_exhausted_count_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> RequeueExhaustedAsync(
        int maxRetries,
        IReadOnlyCollection<Guid>? messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        if (messageIds is { Count: 0 })
            return 0;

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                UPDATE {_tableName}
                SET retrycount = 0,
                    nextretryatutc = NULL,
                    errormessage = NULL
                WHERE processedatutc IS NULL
                  AND retrycount >= @MaxRetries";

            if (messageIds is not null)
                sql += " AND id = ANY(@Ids)";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            return await lease.Connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { MaxRetries = maxRetries, Ids = messageIds?.Distinct().ToArray() },
                    lease.Transaction,
                    cancellationToken: cancellationToken));
        }, "outbox.requeue_exhausted_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
