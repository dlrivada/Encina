using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Outbox;

namespace Encina.Dapper.PostgreSQL.Outbox;

/// <summary>
/// Dapper implementation of <see cref="IOutboxStore"/> for reliable event publishing.
/// Uses raw SQL queries for maximum performance and control.
/// </summary>
public sealed class OutboxStoreDapper : IOutboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The outbox table name (default: outboxmessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="tableName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is empty or whitespace.</exception>
    public OutboxStoreDapper(
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
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            (id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc)
            VALUES
            (@Id, @NotificationType, @Content, @CreatedAtUtc, @ProcessedAtUtc, @ErrorMessage, @RetryCount, @NextRetryAtUtc)";

        await _connection.ExecuteAsync(sql, message);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            SELECT id, notificationtype, content, createdatutc, processedatutc, errormessage, retrycount, nextretryatutc
            FROM {_tableName}
            WHERE processedatutc IS NULL
              AND retrycount < @MaxRetries
              AND (nextretryatutc IS NULL OR nextretryatutc <= @NowUtc)
            ORDER BY createdatutc
            LIMIT @BatchSize";

        var messages = await _connection.QueryAsync<OutboxMessage>(
            sql,
            new { BatchSize = batchSize, MaxRetries = maxRetries, NowUtc = nowUtc });

        return messages.Cast<IOutboxMessage>();
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
            WHERE id = @MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId, NowUtc = nowUtc });
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
            WHERE id = @MessageId";

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
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
