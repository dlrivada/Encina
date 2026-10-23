using System.Data;
using System.Data.Common;
using System.Globalization;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using LanguageExt;
using MySqlConnector;

namespace Encina.ADO.MySQL.Outbox;

/// <summary>
/// ADO.NET implementation of <see cref="IOutboxStore"/> for reliable event publishing.
/// Uses raw MySqlCommand and MySqlDataReader for maximum performance and zero overhead.
/// </summary>
/// <remarks>
/// Every command enlists in the business transaction of the scope when one is active (see
/// <see cref="DbLease.Enlisted"/>), so the rows the transactional outbox behavior adds commit or roll back with
/// the request; without an accessor, or without an active transaction, commands run in autocommit.
/// </remarks>
public sealed class OutboxStoreADO : IOutboxStore
{
    /// <summary>
    /// Maximum number of message identifiers sent in one requeue statement, which keeps
    /// each statement's parameter list small.
    /// </summary>
    private const int RequeueIdBatchSize = 1000;

    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;
    private readonly IDbTransactionAccessor? _transactionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: OutboxMessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <param name="transactionAccessor">
    /// Optional accessor of the scope's shared transaction; when a transaction is active the store's commands
    /// join it, otherwise they run in autocommit.
    /// </param>
    public OutboxStoreADO(
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
    public async Task<Either<EncinaError, IEnumerable<IOutboxMessage>>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));
        if (maxRetries < 0)
            throw new ArgumentException(StoreValidationMessages.MaxRetriesCannotBeNegative, nameof(maxRetries));

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE ProcessedAtUtc IS NULL
                  AND RetryCount < @MaxRetries
                  AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @NowUtc)
                ORDER BY CreatedAtUtc
                LIMIT @BatchSize";

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@BatchSize", batchSize);
            AddParameter(command, "@MaxRetries", maxRetries);
            AddParameter(command, "@NowUtc", nowUtc);

            var messages = new List<OutboxMessage>();

            await lease.EnsureOpenAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                messages.Add(new OutboxMessage
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    NotificationType = reader.GetString(reader.GetOrdinal("NotificationType")),
                    Content = reader.GetString(reader.GetOrdinal("Content")),
                    CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                    ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal("ProcessedAtUtc"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ProcessedAtUtc")),
                    ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                    RetryCount = reader.GetInt32(reader.GetOrdinal("RetryCount")),
                    NextRetryAtUtc = reader.IsDBNull(reader.GetOrdinal("NextRetryAtUtc"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("NextRetryAtUtc"))
                });
            }

            return (IEnumerable<IOutboxMessage>)messages;
        }, "outbox.get_pending_failed").ConfigureAwait(false);
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
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", message.Id);
            AddParameter(command, "@NotificationType", message.NotificationType);
            AddParameter(command, "@Content", message.Content);
            AddParameter(command, "@CreatedAtUtc", message.CreatedAtUtc);
            AddParameter(command, "@ProcessedAtUtc", message.ProcessedAtUtc);
            AddParameter(command, "@ErrorMessage", message.ErrorMessage);
            AddParameter(command, "@RetryCount", message.RetryCount);
            AddParameter(command, "@NextRetryAtUtc", message.NextRetryAtUtc);

            await lease.EnsureOpenAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "outbox.add_failed").ConfigureAwait(false);
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
                WHERE Id = @Id";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", messageId);
            AddParameter(command, "@NowUtc", nowUtc);

            await lease.EnsureOpenAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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
                WHERE Id = @Id";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", messageId);
            AddParameter(command, "@ErrorMessage", errorMessage);
            AddParameter(command, "@NextRetryAtUtc", nextRetryAtUtc);

            await lease.EnsureOpenAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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
                SET ErrorMessage = @ErrorMessage,
                    RetryCount = GREATEST(RetryCount + 1, @MaxRetries),
                    NextRetryAtUtc = NULL
                WHERE Id = @Id";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", messageId);
            AddParameter(command, "@ErrorMessage", errorMessage);
            AddParameter(command, "@MaxRetries", maxRetries);

            await lease.EnsureOpenAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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
                WHERE ProcessedAtUtc IS NULL
                  AND RetryCount < @MaxRetries";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MaxRetries", maxRetries);

            await lease.EnsureOpenAsync(cancellationToken);

            return Convert.ToInt32(await ExecuteScalarAsync(command, cancellationToken), CultureInfo.InvariantCulture);
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
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MaxRetries", maxRetries);

            await lease.EnsureOpenAsync(cancellationToken);

            return Convert.ToInt32(await ExecuteScalarAsync(command, cancellationToken), CultureInfo.InvariantCulture);
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
                ? await RequeueAllAsync(lease, baseSql, maxRetries, cancellationToken)
                : await RequeueByIdsAsync(lease, baseSql, messageIds, maxRetries, cancellationToken);
        }, "outbox.requeue_exhausted_failed").ConfigureAwait(false);
    }

    private static async Task<int> RequeueAllAsync(DbLease lease, string baseSql, int maxRetries, CancellationToken cancellationToken)
    {
        using var command = lease.CreateCommand();
        command.CommandText = baseSql;
        AddParameter(command, "@MaxRetries", maxRetries);
        return await ExecuteNonQueryAsync(command, cancellationToken);
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
            requeued += await RequeueChunkAsync(lease.Connection, transaction, baseSql, chunk, maxRetries, cancellationToken);
        }

        if (ownTransaction is not null)
        {
            await ownTransaction.CommitAsync(cancellationToken);
        }

        return requeued;
    }

    private static async Task<int> RequeueChunkAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string baseSql,
        Guid[] chunk,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var idParameters = new string[chunk.Length];
        for (var i = 0; i < chunk.Length; i++)
        {
            idParameters[i] = $"@Id{i}";
            AddParameter(command, idParameters[i], chunk[i]);
        }

        command.CommandText = $"{baseSql} AND Id IN ({string.Join(", ", idParameters)})";
        AddParameter(command, "@MaxRetries", maxRetries);
        return await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ADO.NET executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
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

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand sqlCommand)
            return await sqlCommand.ExecuteScalarAsync(cancellationToken);

        return await Task.Run(command.ExecuteScalar, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
