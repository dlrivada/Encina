using System.Data;
using System.Data.Common;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using LanguageExt;

namespace Encina.Dapper.MySQL.Outbox;

/// <summary>
/// Dapper implementation of <see cref="IOutboxStore"/> for reliable event publishing.
/// Uses raw SQL queries for maximum performance and control.
/// </summary>
public sealed class OutboxStoreDapper : IOutboxStore
{
    /// <summary>
    /// Maximum number of message identifiers sent in one requeue statement, which keeps
    /// each statement's parameter list small.
    /// </summary>
    private const int RequeueIdBatchSize = 1000;

    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: OutboxMessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public OutboxStoreDapper(
        IDbConnection connection,
        string tableName = "OutboxMessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
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

            await _connection.ExecuteAsync(new CommandDefinition(sql, message, cancellationToken: cancellationToken));
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

        return await EitherHelpers.TryAsync<IEnumerable<IOutboxMessage>>(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE ProcessedAtUtc IS NULL
                  AND RetryCount < @MaxRetries
                  AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @NowUtc)
                ORDER BY CreatedAtUtc
                LIMIT @BatchSize";

            var messages = await _connection.QueryAsync<OutboxMessage>(
                new CommandDefinition(
                    sql,
                    new { BatchSize = batchSize, MaxRetries = maxRetries, NowUtc = nowUtc },
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

            await _connection.ExecuteAsync(
                new CommandDefinition(sql, new { MessageId = messageId, NowUtc = nowUtc }, cancellationToken: cancellationToken));
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

            await _connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        MessageId = messageId,
                        ErrorMessage = errorMessage,
                        NextRetryAtUtc = nextRetryAtUtc
                    },
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

            return (int)await _connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, new { MaxRetries = maxRetries }, cancellationToken: cancellationToken));
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

            return (int)await _connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, new { MaxRetries = maxRetries }, cancellationToken: cancellationToken));
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

            if (messageIds is null)
            {
                return await _connection.ExecuteAsync(
                    new CommandDefinition(baseSql, new { MaxRetries = maxRetries }, cancellationToken: cancellationToken));
            }

            // The identifiers are sent in chunks; one transaction makes the whole requeue all-or-nothing.
            using var transaction = await BeginTransactionAsync(cancellationToken);

            var requeued = 0;
            foreach (var chunk in messageIds.Distinct().Chunk(RequeueIdBatchSize))
            {
                requeued += await _connection.ExecuteAsync(
                    new CommandDefinition(
                        $"{baseSql} AND Id IN @Ids",
                        new { MaxRetries = maxRetries, Ids = chunk },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            await CommitAsync(transaction, cancellationToken);

            return requeued;
        }, "outbox.requeue_exhausted_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not DbConnection dbConnection)
        {
            return _connection.BeginTransaction();
        }

        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        return await dbConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task CommitAsync(IDbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        transaction.Commit();
    }
}
