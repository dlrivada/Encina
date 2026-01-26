using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Scheduling;

namespace Encina.Dapper.Oracle.Scheduling;

/// <summary>
/// Dapper implementation of <see cref="IScheduledMessageStore"/> for delayed message execution.
/// Provides persistence and retrieval of scheduled messages with support for recurring schedules.
/// </summary>
public sealed class ScheduledMessageStoreDapper : IScheduledMessageStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The scheduled messages table name (default: ScheduledMessages).</param>
    public ScheduledMessageStoreDapper(IDbConnection connection, string tableName = "ScheduledMessages")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var sql = $@"
            INSERT INTO {_tableName}
            (Id, RequestType, Content, ScheduledAtUtc, CreatedAtUtc, ProcessedAtUtc, LastExecutedAtUtc,
             ErrorMessage, RetryCount, NextRetryAtUtc, IsRecurring, CronExpression)
            VALUES
            (:Id, :RequestType, :Content, :ScheduledAtUtc, :CreatedAtUtc, :ProcessedAtUtc, :LastExecutedAtUtc,
             :ErrorMessage, :RetryCount, :NextRetryAtUtc, :IsRecurring, :CronExpression)";

        // Use DynamicParameters to convert GUID to RAW(16) byte array for Oracle
        var parameters = new DynamicParameters();
        parameters.Add("Id", message.Id.ToByteArray(), DbType.Binary, size: 16);
        parameters.Add("RequestType", message.RequestType);
        parameters.Add("Content", message.Content);
        parameters.Add("ScheduledAtUtc", message.ScheduledAtUtc);
        parameters.Add("CreatedAtUtc", message.CreatedAtUtc);
        parameters.Add("ProcessedAtUtc", message.ProcessedAtUtc);
        parameters.Add("LastExecutedAtUtc", message.LastExecutedAtUtc);
        parameters.Add("ErrorMessage", message.ErrorMessage);
        parameters.Add("RetryCount", message.RetryCount);
        parameters.Add("NextRetryAtUtc", message.NextRetryAtUtc);
        parameters.Add("IsRecurring", message.IsRecurring ? 1 : 0);
        parameters.Add("CronExpression", message.CronExpression);

        await _connection.ExecuteAsync(sql, parameters);
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
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE (ProcessedAtUtc IS NULL OR IsRecurring = 1)
              AND RetryCount < :MaxRetries
              AND (
                  (NextRetryAtUtc IS NOT NULL AND NextRetryAtUtc <= SYS_EXTRACT_UTC(SYSTIMESTAMP))
                  OR (NextRetryAtUtc IS NULL AND ScheduledAtUtc <= SYS_EXTRACT_UTC(SYSTIMESTAMP))
              )
            ORDER BY ScheduledAtUtc
            FETCH FIRST :BatchSize ROWS ONLY";

        var messages = await _connection.QueryAsync<ScheduledMessage>(
            sql,
            new { BatchSize = batchSize, MaxRetries = maxRetries });

        return messages.Cast<IScheduledMessage>();
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));
        var sql = $@"
            UPDATE {_tableName}
            SET ProcessedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                LastExecutedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                ErrorMessage = NULL
            WHERE Id = :MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId.ToByteArray() });
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
        var sql = $@"
            UPDATE {_tableName}
            SET ErrorMessage = :ErrorMessage,
                RetryCount = RetryCount + 1,
                NextRetryAtUtc = :NextRetryAtUtc,
                LastExecutedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP)
            WHERE Id = :MessageId";

        await _connection.ExecuteAsync(
            sql,
            new
            {
                MessageId = messageId.ToByteArray(),
                ErrorMessage = errorMessage,
                NextRetryAtUtc = nextRetryAtUtc
            });
    }

    /// <inheritdoc />
    public async Task RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));
        if (nextScheduledAtUtc < DateTime.UtcNow)
            throw new ArgumentException(StoreValidationMessages.NextScheduledDateCannotBeInPast, nameof(nextScheduledAtUtc));
        var sql = $@"
            UPDATE {_tableName}
            SET ScheduledAtUtc = :NextScheduledAtUtc,
                ProcessedAtUtc = NULL,
                ErrorMessage = NULL,
                RetryCount = 0,
                NextRetryAtUtc = NULL
            WHERE Id = :MessageId";

        await _connection.ExecuteAsync(
            sql,
            new
            {
                MessageId = messageId.ToByteArray(),
                NextScheduledAtUtc = nextScheduledAtUtc
            });
    }

    /// <inheritdoc />
    public async Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));
        var sql = $@"
            DELETE FROM {_tableName}
            WHERE Id = :MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId.ToByteArray() });
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
