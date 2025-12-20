using System.Data;
using Dapper;
using SimpleMediator.Messaging.Inbox;

namespace SimpleMediator.Dapper.Sqlite.Inbox;

/// <summary>
/// Dapper implementation of <see cref="IInboxStore"/> for idempotent message processing.
/// Provides exactly-once semantics by tracking processed messages.
/// </summary>
public sealed class InboxStoreDapper : IInboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The inbox table name (default: InboxMessages).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is null or whitespace.</exception>
    public InboxStoreDapper(IDbConnection connection, string tableName = "InboxMessages")
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        _connection = connection;
        _tableName = tableName;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> is null or whitespace.</exception>
    public async Task<IInboxMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE MessageId = @MessageId";

        return await _connection.QuerySingleOrDefaultAsync<InboxMessage>(sql, new { MessageId = messageId });
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public async Task AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            (MessageId, RequestType, ReceivedAtUtc, ProcessedAtUtc, ExpiresAtUtc, Response, ErrorMessage, RetryCount, NextRetryAtUtc, Metadata)
            VALUES
            (@MessageId, @RequestType, @ReceivedAtUtc, @ProcessedAtUtc, @ExpiresAtUtc, @Response, @ErrorMessage, @RetryCount, @NextRetryAtUtc, @Metadata)";

        await _connection.ExecuteAsync(sql, message);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> is null or whitespace.</exception>
    public async Task MarkAsProcessedAsync(
        string messageId,
        string? response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var sql = $@"
            UPDATE {_tableName}
            SET ProcessedAtUtc = datetime('now'),
                Response = @Response,
                ErrorMessage = NULL
            WHERE MessageId = @MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId, Response = response });
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> or <paramref name="errorMessage"/> is null or whitespace.</exception>
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
            SET ErrorMessage = @ErrorMessage,
                RetryCount = RetryCount + 1,
                NextRetryAtUtc = @NextRetryAtUtc
            WHERE MessageId = @MessageId";

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
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize"/> is less than 1.</exception>
    public async Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE ExpiresAtUtc < datetime('now')
              AND ProcessedAtUtc IS NOT NULL
            ORDER BY ExpiresAtUtc
            LIMIT @BatchSize";

        var messages = await _connection.QueryAsync<InboxMessage>(sql, new { BatchSize = batchSize });
        return messages.Cast<IInboxMessage>();
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messageIds"/> is null.</exception>
    public async Task RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        var sql = $@"
            DELETE FROM {_tableName}
            WHERE MessageId IN @MessageIds";

        await _connection.ExecuteAsync(sql, new { MessageIds = messageIds });
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
