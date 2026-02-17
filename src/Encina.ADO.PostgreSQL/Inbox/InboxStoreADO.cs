using System.Data;
using Encina.Messaging;
using Encina.Messaging.Inbox;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The inbox table name (default: inboxmessages).</param>
    /// <param name="timeProvider">Optional time provider for UTC time generation (default: <see cref="TimeProvider.System"/>).</param>
    public InboxStoreADO(
        IDbConnection connection,
        string tableName = "inboxmessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IInboxMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var sql = $@"
            SELECT messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata
            FROM {_tableName}
            WHERE messageid = @MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@MessageId", messageId);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        if (await ReadAsync(reader, cancellationToken))
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

        return null;
    }

    /// <inheritdoc />
    public async Task AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            (messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata)
            VALUES
            (@MessageId, @RequestType, @ReceivedAtUtc, @ProcessedAtUtc, @ExpiresAtUtc, @Response, @ErrorMessage, @RetryCount, @NextRetryAtUtc, @Metadata)";

        using var command = _connection.CreateCommand();
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

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(
        string messageId,
        string? response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            UPDATE {_tableName}
            SET processedatutc = @NowUtc,
                response = @Response,
                errormessage = NULL
            WHERE messageid = @MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@MessageId", messageId);
        AddParameter(command, "@Response", response);
        AddParameter(command, "@NowUtc", nowUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var sql = $@"
            UPDATE {_tableName}
            SET errormessage = @ErrorMessage,
                retrycount = retrycount + 1,
                nextretryatutc = @NextRetryAtUtc
            WHERE messageid = @MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@MessageId", messageId);
        AddParameter(command, "@ErrorMessage", errorMessage);
        AddParameter(command, "@NextRetryAtUtc", nextRetryAtUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            SELECT messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata
            FROM {_tableName}
            WHERE expiresatutc < @NowUtc
              AND processedatutc IS NOT NULL
            ORDER BY expiresatutc
            LIMIT @BatchSize";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@BatchSize", batchSize);
        AddParameter(command, "@NowUtc", nowUtc);

        var messages = new List<InboxMessage>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            messages.Add(new InboxMessage
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
            });
        }

        return messages;
    }

    /// <inheritdoc />
    public async Task RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);
        if (!messageIds.Any())
            return;

        var idList = string.Join(",", messageIds.Select(id => $"'{id.Replace("'", "''", StringComparison.Ordinal)}'"));
        var sql = $@"
            DELETE FROM {_tableName}
            WHERE messageid IN ({idList})";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var sql = $@"
            UPDATE {_tableName}
            SET retrycount = retrycount + 1
            WHERE messageid = @MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@MessageId", messageId);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ADO.NET executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
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
