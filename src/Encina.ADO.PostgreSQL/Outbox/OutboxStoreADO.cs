using System.Data;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Outbox;

/// <summary>
/// ADO.NET implementation of <see cref="IOutboxStore"/> for reliable event publishing.
/// Uses raw NpgsqlCommand and NpgsqlDataReader for maximum performance and zero overhead.
/// </summary>
public sealed class OutboxStoreADO : IOutboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: outboxmessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public OutboxStoreADO(
        IDbConnection connection,
        string tableName = "outboxmessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
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

        var sql = $@"
            SELECT id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc
            FROM {_tableName}
            WHERE processedatutc IS NULL
              AND retrycount < @MaxRetries
              AND (nextretryatutc IS NULL OR nextretryatutc <= @NowUtc)
            ORDER BY createdatutc
            LIMIT @BatchSize";

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@BatchSize", batchSize);
        AddParameter(command, "@MaxRetries", maxRetries);
        AddParameter(command, "@NowUtc", nowUtc);

        var messages = new List<OutboxMessage>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

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

        return messages;
    }

    /// <inheritdoc />
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            (id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc)
            VALUES
            (@Id, @NotificationType, @Content, @CreatedAtUtc, @ProcessedAtUtc, @ErrorMessage, @RetryCount, @NextRetryAtUtc)";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Id", message.Id);
        AddParameter(command, "@NotificationType", message.NotificationType);
        AddParameter(command, "@Content", message.Content);
        AddParameter(command, "@CreatedAtUtc", message.CreatedAtUtc);
        AddParameter(command, "@ProcessedAtUtc", message.ProcessedAtUtc);
        AddParameter(command, "@ErrorMessage", message.ErrorMessage);
        AddParameter(command, "@RetryCount", message.RetryCount);
        AddParameter(command, "@NextRetryAtUtc", message.NextRetryAtUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmpty, nameof(messageId));
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            UPDATE {_tableName}
            SET processedatutc = @NowUtc,
                errormessage = NULL
            WHERE id = @Id";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Id", messageId);
        AddParameter(command, "@NowUtc", nowUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmpty, nameof(messageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var sql = $@"
            UPDATE {_tableName}
            SET errormessage = @ErrorMessage,
                retrycount = retrycount + 1,
                nextretryatutc = @NextRetryAtUtc
            WHERE id = @Id";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Id", messageId);
        AddParameter(command, "@ErrorMessage", errorMessage);
        AddParameter(command, "@NextRetryAtUtc", nextRetryAtUtc);

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
        // For NpgsqlConnection, use OpenAsync
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
