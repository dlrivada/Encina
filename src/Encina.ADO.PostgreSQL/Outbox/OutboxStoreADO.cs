using System.Data;
using System.Globalization;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using LanguageExt;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Outbox;

/// <summary>
/// ADO.NET implementation of <see cref="IOutboxStore"/> for reliable event publishing.
/// Uses raw NpgsqlCommand and NpgsqlDataReader for maximum performance and zero overhead.
/// </summary>
/// <remarks>
/// Every command enlists in the business transaction of the scope when one is active (see
/// <see cref="DbLease.Enlisted"/>), so the rows the transactional outbox behavior adds commit or roll back with
/// the request; without an accessor, or without an active transaction, commands run in autocommit.
/// </remarks>
public sealed class OutboxStoreADO : IOutboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;
    private readonly IDbTransactionAccessor? _transactionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: outboxmessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <param name="transactionAccessor">
    /// Optional accessor of the scope's shared transaction; when a transaction is active the store's commands
    /// join it, otherwise they run in autocommit.
    /// </param>
    public OutboxStoreADO(
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
    public async Task<Either<EncinaError, IEnumerable<IOutboxMessage>>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));
        if (maxRetries < 0)
            throw new ArgumentException(StoreValidationMessages.MaxRetriesCannotBeNegative, nameof(maxRetries));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                SELECT id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc
                FROM {_tableName}
                WHERE processedatutc IS NULL
                  AND retrycount < @MaxRetries
                  AND (nextretryatutc IS NULL OR nextretryatutc <= @NowUtc)
                ORDER BY createdatutc
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
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    NotificationType = reader.GetString(reader.GetOrdinal("notificationtype")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("createdatutc")),
                    ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal("processedatutc"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("processedatutc")),
                    ErrorMessage = reader.IsDBNull(reader.GetOrdinal("errormessage"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("errormessage")),
                    RetryCount = reader.GetInt32(reader.GetOrdinal("retrycount")),
                    NextRetryAtUtc = reader.IsDBNull(reader.GetOrdinal("nextretryatutc"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("nextretryatutc"))
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
                (id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc)
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
                SET processedatutc = @NowUtc,
                    errormessage = NULL
                WHERE id = @Id";

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
                SET errormessage = @ErrorMessage,
                    retrycount = retrycount + 1,
                    nextretryatutc = @NextRetryAtUtc
                WHERE id = @Id";

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
                WHERE processedatutc IS NULL
                  AND retrycount >= @MaxRetries";

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
            var sql = $@"
                UPDATE {_tableName}
                SET retrycount = 0,
                    nextretryatutc = NULL,
                    errormessage = NULL
                WHERE processedatutc IS NULL
                  AND retrycount >= @MaxRetries";

            if (messageIds is not null)
                sql += " AND id = ANY(@Ids)";

            // A single statement is atomic on its own; it joins the scope's transaction when one is active.
            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MaxRetries", maxRetries);

            if (messageIds is not null)
                AddParameter(command, "@Ids", messageIds.Distinct().ToArray());

            await lease.EnsureOpenAsync(cancellationToken);

            return await ExecuteNonQueryAsync(command, cancellationToken);
        }, "outbox.requeue_exhausted_failed").ConfigureAwait(false);
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

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteScalarAsync(cancellationToken);

        return await Task.Run(command.ExecuteScalar, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
