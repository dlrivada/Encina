using System.Data;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Oracle.ManagedDataAccess.Client;

namespace Encina.ADO.Oracle.Inbox;

/// <summary>
/// ADO.NET implementation of <see cref="IInboxStore"/> for idempotent message processing.
/// Provides exactly-once semantics by tracking processed messages.
/// </summary>
public sealed class InboxStoreADO : IInboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The inbox table name (default: InboxMessages).</param>
    public InboxStoreADO(IDbConnection connection, string tableName = "InboxMessages")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async Task<IInboxMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE MessageId = :MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":MessageId", messageId);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        if (await ReadAsync(reader, cancellationToken))
        {
            return new InboxMessage
            {
                MessageId = reader.GetString(reader.GetOrdinal("MessageId")),
                RequestType = reader.GetString(reader.GetOrdinal("RequestType")),
                ReceivedAtUtc = reader.GetDateTime(reader.GetOrdinal("ReceivedAtUtc")),
                ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal("ProcessedAtUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ProcessedAtUtc")),
                ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc")),
                Response = reader.IsDBNull(reader.GetOrdinal("Response"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Response")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                RetryCount = reader.GetInt32(reader.GetOrdinal("RetryCount")),
                NextRetryAtUtc = reader.IsDBNull(reader.GetOrdinal("NextRetryAtUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("NextRetryAtUtc")),
                Metadata = reader.IsDBNull(reader.GetOrdinal("Metadata"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Metadata"))
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
            (MessageId, RequestType, ReceivedAtUtc, ProcessedAtUtc, ExpiresAtUtc, Response, ErrorMessage, RetryCount, NextRetryAtUtc, Metadata)
            VALUES
            (:MessageId, :RequestType, :ReceivedAtUtc, :ProcessedAtUtc, :ExpiresAtUtc, :Response, :ErrorMessage, :RetryCount, :NextRetryAtUtc, :Metadata)";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":MessageId", message.MessageId);
        AddParameter(command, ":RequestType", message.RequestType);
        AddParameter(command, ":ReceivedAtUtc", message.ReceivedAtUtc);
        AddParameter(command, ":ProcessedAtUtc", message.ProcessedAtUtc);
        AddParameter(command, ":ExpiresAtUtc", message.ExpiresAtUtc);
        AddParameter(command, ":Response", message.Response);
        AddParameter(command, ":ErrorMessage", message.ErrorMessage);
        AddParameter(command, ":RetryCount", message.RetryCount);
        AddParameter(command, ":NextRetryAtUtc", message.NextRetryAtUtc);
        AddParameter(command, ":Metadata", (message as InboxMessage)?.Metadata);

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
        ArgumentNullException.ThrowIfNull(messageId);

        var sql = $@"
            UPDATE {_tableName}
            SET ProcessedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                Response = :Response,
                ErrorMessage = NULL
            WHERE MessageId = :MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":MessageId", messageId);
        AddParameter(command, ":Response", response);

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
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var sql = $@"
            UPDATE {_tableName}
            SET ErrorMessage = :ErrorMessage,
                RetryCount = RetryCount + 1,
                NextRetryAtUtc = :NextRetryAtUtc
            WHERE MessageId = :MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":MessageId", messageId);
        AddParameter(command, ":ErrorMessage", errorMessage);
        AddParameter(command, ":NextRetryAtUtc", nextRetryAtUtc);

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
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE ExpiresAtUtc < SYS_EXTRACT_UTC(SYSTIMESTAMP)
              AND ProcessedAtUtc IS NOT NULL
            ORDER BY ExpiresAtUtc
            FETCH FIRST :BatchSize ROWS ONLY";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":BatchSize", batchSize);

        var messages = new List<InboxMessage>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            messages.Add(new InboxMessage
            {
                MessageId = reader.GetString(reader.GetOrdinal("MessageId")),
                RequestType = reader.GetString(reader.GetOrdinal("RequestType")),
                ReceivedAtUtc = reader.GetDateTime(reader.GetOrdinal("ReceivedAtUtc")),
                ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal("ProcessedAtUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ProcessedAtUtc")),
                ExpiresAtUtc = reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc")),
                Response = reader.IsDBNull(reader.GetOrdinal("Response"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Response")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                RetryCount = reader.GetInt32(reader.GetOrdinal("RetryCount")),
                NextRetryAtUtc = reader.IsDBNull(reader.GetOrdinal("NextRetryAtUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("NextRetryAtUtc")),
                Metadata = reader.IsDBNull(reader.GetOrdinal("Metadata"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Metadata"))
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
        ArgumentNullException.ThrowIfNull(messageIds);

        var idList = string.Join(",", messageIds.Select(id => $"'{id.Replace("'", "''", StringComparison.Ordinal)}'"));
        var sql = $@"
            DELETE FROM {_tableName}
            WHERE MessageId IN ({idList})";

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
            SET RetryCount = RetryCount + 1
            WHERE MessageId = :MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":MessageId", messageId);

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

    private static async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.CanBeCanceled)
        {
            await Task.Run(() => { }, cancellationToken);
        }
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is OracleCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is OracleCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is OracleDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
