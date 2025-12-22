using System.Data;
using Dapper;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        _connection = connection;
        _tableName = tableName;
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

        await _connection.ExecuteAsync(sql, message);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(
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
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        var sql = $@"
            UPDATE {_tableName}
            SET ProcessedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                LastExecutedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                ErrorMessage = NULL
            WHERE Id = :MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId });
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
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
                MessageId = messageId,
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
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        if (nextScheduledAtUtc < DateTime.UtcNow)
            throw new ArgumentException("Next scheduled date cannot be in the past.", nameof(nextScheduledAtUtc));
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
                MessageId = messageId,
                NextScheduledAtUtc = nextScheduledAtUtc
            });
    }

    /// <inheritdoc />
    public async Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        var sql = $@"
            DELETE FROM {_tableName}
            WHERE Id = :MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId });
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
