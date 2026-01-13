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
    private const string ParamMessageId = ":MessageId";
    private const string ParamRequestType = ":RequestType";
    private const string ParamReceivedAtUtc = ":ReceivedAtUtc";
    private const string ParamProcessedAtUtc = ":ProcessedAtUtc";
    private const string ParamExpiresAtUtc = ":ExpiresAtUtc";
    private const string ParamResponse = ":Response";
    private const string ParamErrorMessage = ":ErrorMessage";
    private const string ParamRetryCount = ":RetryCount";
    private const string ParamNextRetryAtUtc = ":NextRetryAtUtc";
    private const string ParamMetadata = ":Metadata";
    private const string ParamBatchSize = ":BatchSize";

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
            WHERE {ColumnMessageId} = {ParamMessageId}";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ParamMessageId, messageId);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        if (await ReadAsync(reader, cancellationToken))
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

        return null;
    }

    /// <inheritdoc />
    public async Task AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            ({ColumnMessageId}, {ColumnRequestType}, {ColumnReceivedAtUtc}, {ColumnProcessedAtUtc}, {ColumnExpiresAtUtc}, {ColumnResponse}, {ColumnErrorMessage}, {ColumnRetryCount}, {ColumnNextRetryAtUtc}, {ColumnMetadata})
            VALUES
            ({ParamMessageId}, {ParamRequestType}, {ParamReceivedAtUtc}, {ParamProcessedAtUtc}, {ParamExpiresAtUtc}, {ParamResponse}, {ParamErrorMessage}, {ParamRetryCount}, {ParamNextRetryAtUtc}, {ParamMetadata})";

        using var command = _connection.CreateCommand();
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
            SET {ColumnProcessedAtUtc} = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                {ColumnResponse} = {ParamResponse},
                {ColumnErrorMessage} = NULL
            WHERE {ColumnMessageId} = {ParamMessageId}";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ParamMessageId, messageId);
        AddParameter(command, ParamResponse, response);

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
            SET {ColumnErrorMessage} = {ParamErrorMessage},
                {ColumnRetryCount} = {ColumnRetryCount} + 1,
                {ColumnNextRetryAtUtc} = {ParamNextRetryAtUtc}
            WHERE {ColumnMessageId} = {ParamMessageId}";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ParamMessageId, messageId);
        AddParameter(command, ParamErrorMessage, errorMessage);
        AddParameter(command, ParamNextRetryAtUtc, nextRetryAtUtc);

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
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE {ColumnExpiresAtUtc} < SYS_EXTRACT_UTC(SYSTIMESTAMP)
              AND {ColumnProcessedAtUtc} IS NOT NULL
            ORDER BY {ColumnExpiresAtUtc}
            FETCH FIRST {ParamBatchSize} ROWS ONLY";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ParamBatchSize, batchSize);

        var messages = new List<InboxMessage>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            messages.Add(new InboxMessage
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
            WHERE {ColumnMessageId} IN ({idList})";

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
            SET {ColumnRetryCount} = {ColumnRetryCount} + 1
            WHERE {ColumnMessageId} = {ParamMessageId}";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ParamMessageId, messageId);

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
