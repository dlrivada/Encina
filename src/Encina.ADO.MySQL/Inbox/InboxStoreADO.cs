using System.Data;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using LanguageExt;
using MySqlConnector;

namespace Encina.ADO.MySQL.Inbox;

/// <summary>
/// ADO.NET implementation of <see cref="IInboxStore"/> for idempotent message processing.
/// Provides exactly-once semantics by tracking processed messages.
/// </summary>
public sealed class InboxStoreADO : IInboxStore
{
    // Column name constants
    private const string ColumnMessageId = "MessageId";
    private const string ColumnRequestType = "RequestType";
    private const string ColumnReceivedAtUtc = "ReceivedAtUtc";
    private const string ColumnProcessedAtUtc = "ProcessedAtUtc";
    private const string ColumnExpiresAtUtc = "ExpiresAtUtc";
    private const string ColumnResponse = "Response";
    private const string ColumnErrorMessage = "ErrorMessage";
    private const string ColumnRetryCount = "RetryCount";
    private const string ColumnNextRetryAtUtc = "NextRetryAtUtc";
    private const string ColumnMetadata = "Metadata";

    // Parameter name constants
    private const string ParamMessageId = "@MessageId";
    private const string ParamRequestType = "@RequestType";
    private const string ParamReceivedAtUtc = "@ReceivedAtUtc";
    private const string ParamProcessedAtUtc = "@ProcessedAtUtc";
    private const string ParamExpiresAtUtc = "@ExpiresAtUtc";
    private const string ParamResponse = "@Response";
    private const string ParamErrorMessage = "@ErrorMessage";
    private const string ParamRetryCount = "@RetryCount";
    private const string ParamNextRetryAtUtc = "@NextRetryAtUtc";
    private const string ParamMetadata = "@Metadata";
    private const string ParamBatchSize = "@BatchSize";

    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;
    private readonly IDbTransactionAccessor? _transactionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The inbox table name (default: InboxMessages).</param>
    /// <param name="timeProvider">Optional time provider for UTC time generation (default: <see cref="TimeProvider.System"/>).</param>
    /// <param name="transactionAccessor">Optional accessor of the business transaction on the shared connection; when a transaction is active the store enlists or leaves it as documented in ADR-048.</param>
    public InboxStoreADO(
        IDbConnection connection,
        string tableName = "InboxMessages",
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
                SELECT *
                FROM {_tableName}
                WHERE {ColumnMessageId} = {ParamMessageId}";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, ParamMessageId, messageId);

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
                ({ColumnMessageId}, {ColumnRequestType}, {ColumnReceivedAtUtc}, {ColumnProcessedAtUtc}, {ColumnExpiresAtUtc}, {ColumnResponse}, {ColumnErrorMessage}, {ColumnRetryCount}, {ColumnNextRetryAtUtc}, {ColumnMetadata})
                VALUES
                ({ParamMessageId}, {ParamRequestType}, {ParamReceivedAtUtc}, {ParamProcessedAtUtc}, {ParamExpiresAtUtc}, {ParamResponse}, {ParamErrorMessage}, {ParamRetryCount}, {ParamNextRetryAtUtc}, {ParamMetadata})";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, ParamMessageId, message.MessageId);
            AddParameter(command, ParamRequestType, message.RequestType);
            AddParameter(command, ParamReceivedAtUtc, message.ReceivedAtUtc);
            AddParameter(command, ParamProcessedAtUtc, message.ProcessedAtUtc);
            AddParameter(command, ParamExpiresAtUtc, message.ExpiresAtUtc);
            AddParameter(command, ParamResponse, message.Response);
            AddParameter(command, ParamErrorMessage, message.ErrorMessage);
            AddParameter(command, ParamRetryCount, message.RetryCount);
            AddParameter(command, ParamNextRetryAtUtc, message.NextRetryAtUtc);
            AddParameter(command, ParamMetadata, (message as InboxMessage)?.Metadata);

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
        ArgumentNullException.ThrowIfNull(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET {ColumnProcessedAtUtc} = @NowUtc,
                    {ColumnResponse} = {ParamResponse},
                    {ColumnErrorMessage} = NULL
                WHERE {ColumnMessageId} = {ParamMessageId}";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, ParamMessageId, messageId);
            AddParameter(command, ParamResponse, response);
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
        ArgumentNullException.ThrowIfNull(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET {ColumnProcessedAtUtc} = @NowUtc,
                    {ColumnResponse} = {ParamResponse},
                    {ColumnErrorMessage} = NULL
                WHERE {ColumnMessageId} = {ParamMessageId}";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, ParamMessageId, messageId);
            AddParameter(command, ParamResponse, response);
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
                SET {ColumnErrorMessage} = {ParamErrorMessage},
                    {ColumnRetryCount} = {ColumnRetryCount} + 1,
                    {ColumnNextRetryAtUtc} = {ParamNextRetryAtUtc}
                WHERE {ColumnMessageId} = {ParamMessageId}";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, ParamMessageId, messageId);
            AddParameter(command, ParamErrorMessage, errorMessage);
            AddParameter(command, ParamNextRetryAtUtc, nextRetryAtUtc);

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
                SELECT *
                FROM {_tableName}
                WHERE {ColumnExpiresAtUtc} < @NowUtc
                  AND {ColumnProcessedAtUtc} IS NOT NULL
                ORDER BY {ColumnExpiresAtUtc}
                LIMIT {ParamBatchSize}";

            using var command = lease.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, ParamBatchSize, batchSize);
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
                WHERE {ColumnMessageId} IN ({idList})";

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
            MessageId = reader.GetString(reader.GetOrdinal(ColumnMessageId)),
            RequestType = reader.GetString(reader.GetOrdinal(ColumnRequestType)),
            ReceivedAtUtc = reader.GetDateTime(reader.GetOrdinal(ColumnReceivedAtUtc)),
            ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal(ColumnProcessedAtUtc))
                ? null
                : reader.GetDateTime(reader.GetOrdinal(ColumnProcessedAtUtc)),
            ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal(ColumnExpiresAtUtc)),
            Response = reader.IsDBNull(reader.GetOrdinal(ColumnResponse))
                ? null
                : reader.GetString(reader.GetOrdinal(ColumnResponse)),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal(ColumnErrorMessage))
                ? null
                : reader.GetString(reader.GetOrdinal(ColumnErrorMessage)),
            RetryCount = reader.GetInt32(reader.GetOrdinal(ColumnRetryCount)),
            NextRetryAtUtc = reader.IsDBNull(reader.GetOrdinal(ColumnNextRetryAtUtc))
                ? null
                : reader.GetDateTime(reader.GetOrdinal(ColumnNextRetryAtUtc)),
            Metadata = reader.IsDBNull(reader.GetOrdinal(ColumnMetadata))
                ? null
                : reader.GetString(reader.GetOrdinal(ColumnMetadata))
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

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
