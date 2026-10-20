using System.Data;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using LanguageExt;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Inbox;

/// <summary>
/// ADO.NET implementation of <see cref="IInboxStore"/> for idempotent message processing.
/// Provides exactly-once semantics by tracking processed messages.
/// </summary>
public sealed class InboxStoreADO : IInboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;
    private readonly IDbTransactionAccessor? _transactionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The inbox table name (default: inboxmessages).</param>
    /// <param name="timeProvider">Optional time provider for UTC time generation (default: <see cref="TimeProvider.System"/>).</param>
    /// <param name="transactionAccessor">Optional accessor of the business transaction on the shared connection; when a transaction is active the store enlists or leaves it as documented in ADR-048.</param>
    public InboxStoreADO(
        IDbConnection connection,
        string tableName = "inboxmessages",
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
    public async Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var sql = $@"
                SELECT messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata
                FROM {_tableName}
                WHERE messageid = @MessageId";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MessageId", messageId);

            if (lease.Connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                return Option<IInboxMessage>.Some(ReadMessage(reader));
            }

            return Option<IInboxMessage>.None;
        }, "inbox.get_message_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken);
            var sql = $@"
                INSERT INTO {_tableName}
                (messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata)
                VALUES
                (@MessageId, @RequestType, @ReceivedAtUtc, @ProcessedAtUtc, @ExpiresAtUtc, @Response, @ErrorMessage, @RetryCount, @NextRetryAtUtc, @Metadata)";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MessageId", message.MessageId);
            AddParameter(command, "@RequestType", message.RequestType);
            AddParameter(command, "@ReceivedAtUtc", message.ReceivedAtUtc);
            AddParameter(command, "@ProcessedAtUtc", message.ProcessedAtUtc);
            AddParameter(command, "@ExpiresAtUtc", message.ExpiresAtUtc);
            AddParameter(command, "@Response", message.Response);
            AddParameter(command, "@ErrorMessage", message.ErrorMessage);
            AddParameter(command, "@RetryCount", message.RetryCount);
            AddParameter(command, "@NextRetryAtUtc", message.NextRetryAtUtc);
            AddParameter(command, "@Metadata", (message as InboxMessage)?.Metadata);

            if (lease.Connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(
        string messageId,
        string response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET processedatutc = @NowUtc,
                    response = @Response,
                    errormessage = NULL
                WHERE messageid = @MessageId";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MessageId", messageId);
            AddParameter(command, "@Response", response);
            AddParameter(command, "@NowUtc", nowUtc);

            if (lease.Connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "inbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> CacheHandlerErrorAsync(
        string messageId,
        string response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET processedatutc = @NowUtc,
                    response = @Response,
                    errormessage = NULL
                WHERE messageid = @MessageId";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MessageId", messageId);
            AddParameter(command, "@Response", response);
            AddParameter(command, "@NowUtc", nowUtc);

            if (lease.Connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "inbox.cache_handler_error_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken);
            var sql = $@"
                UPDATE {_tableName}
                SET errormessage = @ErrorMessage,
                    retrycount = retrycount + 1,
                    nextretryatutc = @NextRetryAtUtc
                WHERE messageid = @MessageId";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@MessageId", messageId);
            AddParameter(command, "@ErrorMessage", errorMessage);
            AddParameter(command, "@NextRetryAtUtc", nextRetryAtUtc);

            if (lease.Connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "inbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                SELECT messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata
                FROM {_tableName}
                WHERE expiresatutc < @NowUtc
                  AND processedatutc IS NOT NULL
                ORDER BY expiresatutc
                LIMIT @BatchSize";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@BatchSize", batchSize);
            AddParameter(command, "@NowUtc", nowUtc);

            var messages = new List<InboxMessage>();

            if (lease.Connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                messages.Add(ReadMessage(reader));
            }

            return (IEnumerable<IInboxMessage>)messages;
        }, "inbox.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);
        if (!messageIds.Any())
            return Unit.Default;

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var idList = string.Join(",", messageIds.Select(id => $"'{id.Replace("'", "''", StringComparison.Ordinal)}'"));
            var sql = $@"
                DELETE FROM {_tableName}
                WHERE messageid IN ({idList})";

            using var command = lease.CreateCommand();
            command.CommandText = sql;

            if (lease.Connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "inbox.remove_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ADO.NET executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private static InboxMessage ReadMessage(IDataReader reader)
    {
        return new InboxMessage
        {
            MessageId = reader.GetString(reader.GetOrdinal("messageid")),
            RequestType = reader.GetString(reader.GetOrdinal("requesttype")),
            ReceivedAtUtc = reader.GetDateTime(reader.GetOrdinal("receivedatutc")),
            ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal("processedatutc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("processedatutc")),
            ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal("expiresatutc")),
            Response = reader.IsDBNull(reader.GetOrdinal("response"))
                ? null
                : reader.GetString(reader.GetOrdinal("response")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("errormessage"))
                ? null
                : reader.GetString(reader.GetOrdinal("errormessage")),
            RetryCount = reader.GetInt32(reader.GetOrdinal("retrycount")),
            NextRetryAtUtc = reader.IsDBNull(reader.GetOrdinal("nextretryatutc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("nextretryatutc")),
            Metadata = reader.IsDBNull(reader.GetOrdinal("metadata"))
                ? null
                : reader.GetString(reader.GetOrdinal("metadata"))
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
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

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
