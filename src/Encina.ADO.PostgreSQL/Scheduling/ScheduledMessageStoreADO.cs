using System.Data;
using Encina.Messaging;
using Encina.Messaging.Scheduling;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Scheduling;

/// <summary>
/// ADO.NET implementation of <see cref="IScheduledMessageStore"/> for delayed message execution.
/// Uses raw NpgsqlCommand and NpgsqlDataReader for maximum performance and zero overhead.
/// </summary>
public sealed class ScheduledMessageStoreADO : IScheduledMessageStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The scheduled messages table name (default: scheduledmessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is null or whitespace.</exception>
    public ScheduledMessageStoreADO(
        IDbConnection connection,
        string tableName = "scheduledmessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            (id, requesttype, content, scheduledatutc, createdatutc, processedatutc, lastexecutedatutc,
             errormessage, retrycount, nextretryatutc, isrecurring, cronexpression)
            VALUES
            (@Id, @RequestType, @Content, @ScheduledAtUtc, @CreatedAtUtc, @ProcessedAtUtc, @LastExecutedAtUtc,
             @ErrorMessage, @RetryCount, @NextRetryAtUtc, @IsRecurring, @CronExpression)";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Id", message.Id);
        AddParameter(command, "@RequestType", message.RequestType);
        AddParameter(command, "@Content", message.Content);
        AddParameter(command, "@ScheduledAtUtc", message.ScheduledAtUtc);
        AddParameter(command, "@CreatedAtUtc", message.CreatedAtUtc);
        AddParameter(command, "@ProcessedAtUtc", message.ProcessedAtUtc);
        AddParameter(command, "@LastExecutedAtUtc", message.LastExecutedAtUtc);
        AddParameter(command, "@ErrorMessage", message.ErrorMessage);
        AddParameter(command, "@RetryCount", message.RetryCount);
        AddParameter(command, "@NextRetryAtUtc", message.NextRetryAtUtc);
        AddParameter(command, "@IsRecurring", message.IsRecurring);
        AddParameter(command, "@CronExpression", message.CronExpression);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));
        if (maxRetries < 0)
            throw new ArgumentException(StoreValidationMessages.MaxRetriesCannotBeNegative, nameof(maxRetries));

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            SELECT id, requesttype, content, scheduledatutc, createdatutc, processedatutc, lastexecutedatutc,
                   errormessage, retrycount, nextretryatutc, isrecurring, cronexpression
            FROM {_tableName}
            WHERE (processedatutc IS NULL OR isrecurring = true)
              AND retrycount < @MaxRetries
              AND (
                  (nextretryatutc IS NOT NULL AND nextretryatutc <= @NowUtc)
                  OR (nextretryatutc IS NULL AND scheduledatutc <= @NowUtc)
              )
            ORDER BY scheduledatutc
            LIMIT @BatchSize";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@BatchSize", batchSize);
        AddParameter(command, "@MaxRetries", maxRetries);
        AddParameter(command, "@NowUtc", nowUtc);

        var messages = new List<IScheduledMessage>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            messages.Add(MapToScheduledMessage(reader));
        }

        return messages;
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            UPDATE {_tableName}
            SET processedatutc = @NowUtc,
                lastexecutedatutc = @NowUtc,
                errormessage = NULL
            WHERE id = @MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@MessageId", messageId);
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
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            UPDATE {_tableName}
            SET errormessage = @ErrorMessage,
                retrycount = retrycount + 1,
                nextretryatutc = @NextRetryAtUtc,
                lastexecutedatutc = @NowUtc
            WHERE id = @MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@MessageId", messageId);
        AddParameter(command, "@ErrorMessage", errorMessage);
        AddParameter(command, "@NextRetryAtUtc", nextRetryAtUtc);
        AddParameter(command, "@NowUtc", nowUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));
        if (nextScheduledAtUtc < _timeProvider.GetUtcNow().UtcDateTime)
            throw new ArgumentException(StoreValidationMessages.NextScheduledDateCannotBeInPast, nameof(nextScheduledAtUtc));

        var sql = $@"
            UPDATE {_tableName}
            SET scheduledatutc = @NextScheduledAtUtc,
                processedatutc = NULL,
                errormessage = NULL,
                retrycount = 0,
                nextretryatutc = NULL
            WHERE id = @MessageId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@MessageId", messageId);
        AddParameter(command, "@NextScheduledAtUtc", nextScheduledAtUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));

        var sql = $@"
            DELETE FROM {_tableName}
            WHERE id = @MessageId";

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

    private static ScheduledMessage MapToScheduledMessage(IDataReader reader)
    {
        return new ScheduledMessage
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            RequestType = reader.GetString(reader.GetOrdinal("requesttype")),
            Content = reader.GetString(reader.GetOrdinal("content")),
            ScheduledAtUtc = reader.GetDateTime(reader.GetOrdinal("scheduledatutc")),
            CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("createdatutc")),
            ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal("processedatutc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("processedatutc")),
            LastExecutedAtUtc = reader.IsDBNull(reader.GetOrdinal("lastexecutedatutc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("lastexecutedatutc")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("errormessage"))
                ? null
                : reader.GetString(reader.GetOrdinal("errormessage")),
            RetryCount = reader.GetInt32(reader.GetOrdinal("retrycount")),
            NextRetryAtUtc = reader.IsDBNull(reader.GetOrdinal("nextretryatutc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("nextretryatutc")),
            IsRecurring = reader.GetBoolean(reader.GetOrdinal("isrecurring")),
            CronExpression = reader.IsDBNull(reader.GetOrdinal("cronexpression"))
                ? null
                : reader.GetString(reader.GetOrdinal("cronexpression"))
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
