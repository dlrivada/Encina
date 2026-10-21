using System.Data;
using System.Data.Common;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using LanguageExt;

namespace Encina.Dapper.SqlServer.Outbox;

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
    /// <summary>
    /// Maximum number of message identifiers sent in one requeue statement, well below the
    /// 2,100-parameter limit of SQL Server.
    /// </summary>
    private const int RequeueIdBatchSize = 1000;

    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;
    private readonly IDbTransactionAccessor? _transactionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: OutboxMessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <param name="transactionAccessor">
    /// Optional accessor of the scope's shared transaction; when a transaction is active the store's commands
    /// join it, otherwise they run in autocommit.
    /// </param>
    public OutboxStoreDapper(
        IDbConnection connection,
        string tableName = "OutboxMessages",
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
                (Id, NotificationType, Content, CreatedAtUtc, ProcessedAtUtc, ErrorMessage, RetryCount, NextRetryAtUtc)
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
                SELECT TOP (@BatchSize) *
                FROM {_tableName}
                WHERE ProcessedAtUtc IS NULL
                  AND RetryCount < @MaxRetries
                  AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @NowUtc)
                ORDER BY CreatedAtUtc";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var messages = await lease.Connection.QueryAsync<OutboxMessage>(
                new CommandDefinition(
                    sql,
                    new { BatchSize = batchSize, MaxRetries = maxRetries, NowUtc = nowUtc },
                    lease.Transaction,
                    cancellationToken: cancellationToken));

            return messages.Cast<IOutboxMessage>();
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
                SET ProcessedAtUtc = @NowUtc,
                    ErrorMessage = NULL
                WHERE Id = @MessageId";

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
                SET ErrorMessage = @ErrorMessage,
                    RetryCount = RetryCount + 1,
                    NextRetryAtUtc = @NextRetryAtUtc
                WHERE Id = @MessageId";

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
                WHERE ProcessedAtUtc IS NULL
                  AND RetryCount < @MaxRetries";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            return await lease.Connection.ExecuteScalarAsync<int>(
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
                WHERE ProcessedAtUtc IS NULL
                  AND RetryCount >= @MaxRetries";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            return await lease.Connection.ExecuteScalarAsync<int>(
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
            var baseSql = $@"
                UPDATE {_tableName}
                SET RetryCount = 0,
                    NextRetryAtUtc = NULL,
                    ErrorMessage = NULL
                WHERE ProcessedAtUtc IS NULL
                  AND RetryCount >= @MaxRetries";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            await lease.EnsureOpenAsync(cancellationToken);

            return messageIds is null
                ? await lease.Connection.ExecuteAsync(
                    new CommandDefinition(baseSql, new { MaxRetries = maxRetries }, lease.Transaction, cancellationToken: cancellationToken))
                : await RequeueByIdsAsync(lease, baseSql, messageIds, maxRetries, cancellationToken);
        }, "outbox.requeue_exhausted_failed").ConfigureAwait(false);
    }

    // The identifiers are sent in chunks; one transaction makes the whole requeue all-or-nothing. An active
    // transaction of the scope is joined (its owner commits); otherwise the store begins its own.
    private static async Task<int> RequeueByIdsAsync(
        DbLease lease,
        string baseSql,
        IReadOnlyCollection<Guid> messageIds,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        await using var ownTransaction = lease.Transaction is null
            ? await BeginOwnTransactionAsync(lease.Connection, cancellationToken)
            : null;
        var transaction = lease.Transaction ?? ownTransaction;

        var requeued = 0;
        foreach (var chunk in messageIds.Distinct().Chunk(RequeueIdBatchSize))
        {
            requeued += await lease.Connection.ExecuteAsync(
                new CommandDefinition(
                    $"{baseSql} AND Id IN @Ids",
                    new { MaxRetries = maxRetries, Ids = chunk },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        if (ownTransaction is not null)
        {
            await ownTransaction.CommitAsync(cancellationToken);
        }

        return requeued;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    // No synchronous BeginTransaction fallback (AGENTS.md section 3): a connection that is not a DbConnection
    // cannot requeue a chunked set atomically, and the failure surfaces as the store's Left.
    private static async Task<DbTransaction> BeginOwnTransactionAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not DbConnection dbConnection)
        {
            throw new InvalidOperationException(
                $"{connection.GetType().Name} is not a System.Data.Common.DbConnection, so it cannot begin a transaction asynchronously.");
        }

        return await dbConnection.BeginTransactionAsync(cancellationToken);
    }
}
