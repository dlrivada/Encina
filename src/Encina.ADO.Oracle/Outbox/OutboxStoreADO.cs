using System.Data;
using Encina.Messaging.Outbox;
using Oracle.ManagedDataAccess.Client;

namespace Encina.ADO.Oracle.Outbox;

/// <summary>
/// ADO.NET implementation of <see cref="IOutboxStore"/> for reliable event publishing.
/// Uses raw OracleCommand and OracleDataReader for maximum performance and zero overhead.
/// </summary>
public sealed class OutboxStoreADO : IOutboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: OutboxMessages).</param>
    public OutboxStoreADO(IDbConnection connection, string tableName = "OutboxMessages")
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        _connection = connection;
        _tableName = tableName;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        if (maxRetries < 0)
            throw new ArgumentException("Max retries cannot be negative.", nameof(maxRetries));
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE ProcessedAtUtc IS NULL
              AND RetryCount < :MaxRetries
              AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= SYS_EXTRACT_UTC(SYSTIMESTAMP))
            ORDER BY CreatedAtUtc
            FETCH FIRST :BatchSize ROWS ONLY";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":BatchSize", batchSize);
        AddParameter(command, ":MaxRetries", maxRetries);

        var messages = new List<OutboxMessage>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

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

        return messages;
    }

    /// <inheritdoc />
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            (Id, NotificationType, Content, CreatedAtUtc, ProcessedAtUtc, ErrorMessage, RetryCount, NextRetryAtUtc)
            VALUES
            (:Id, :NotificationType, :Content, :CreatedAtUtc, :ProcessedAtUtc, :ErrorMessage, :RetryCount, :NextRetryAtUtc)";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":Id", message.Id);
        AddParameter(command, ":NotificationType", message.NotificationType);
        AddParameter(command, ":Content", message.Content);
        AddParameter(command, ":CreatedAtUtc", message.CreatedAtUtc);
        AddParameter(command, ":ProcessedAtUtc", message.ProcessedAtUtc);
        AddParameter(command, ":ErrorMessage", message.ErrorMessage);
        AddParameter(command, ":RetryCount", message.RetryCount);
        AddParameter(command, ":NextRetryAtUtc", message.NextRetryAtUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        var sql = $@"
            UPDATE {_tableName}
            SET ProcessedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                ErrorMessage = NULL
            WHERE Id = :Id";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":Id", messageId);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var sql = $@"
            UPDATE {_tableName}
            SET ErrorMessage = :ErrorMessage,
                RetryCount = RetryCount + 1,
                NextRetryAtUtc = :NextRetryAtUtc
            WHERE Id = :Id";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ":Id", messageId);
        AddParameter(command, ":ErrorMessage", errorMessage);
        AddParameter(command, ":NextRetryAtUtc", nextRetryAt);

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
        // For OracleConnection, use OpenAsync
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
