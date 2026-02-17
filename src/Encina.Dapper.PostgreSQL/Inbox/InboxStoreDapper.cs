using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.PostgreSQL.Inbox;

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
    /// <param name="tableName">The inbox table name (default: inboxmessages).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="tableName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is empty or whitespace.</exception>
    public InboxStoreDapper(IDbConnection connection, string tableName = "inboxmessages")
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
            SELECT messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata
            FROM {_tableName}
            WHERE messageid = @MessageId";

        return await _connection.QuerySingleOrDefaultAsync<InboxMessage>(sql, new { MessageId = messageId });
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

        await _connection.ExecuteAsync(sql, message);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(
        string messageId,
        string? response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var sql = $@"
            UPDATE {_tableName}
            SET processedatutc = NOW() AT TIME ZONE 'UTC',
                response = @Response,
                errormessage = NULL
            WHERE messageid = @MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId, Response = response });
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
    public async Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var sql = $@"
            SELECT messageid, requesttype, receivedatutc, processedatutc, expiresatutc, response, errormessage, retrycount, nextretryatutc, metadata
            FROM {_tableName}
            WHERE expiresatutc < NOW() AT TIME ZONE 'UTC'
              AND processedatutc IS NOT NULL
            ORDER BY expiresatutc
            LIMIT @BatchSize";

        var messages = await _connection.QueryAsync<InboxMessage>(sql, new { BatchSize = batchSize });
        return messages.Cast<IInboxMessage>();
    }

    /// <inheritdoc />
    public async Task RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);
        if (!messageIds.Any())
            return;

        var sql = $@"
            DELETE FROM {_tableName}
            WHERE messageid = ANY(@MessageIds)";

        await _connection.ExecuteAsync(sql, new { MessageIds = messageIds });
    }

    /// <inheritdoc />
    public async Task IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var sql = $@"
            UPDATE {_tableName}
            SET retrycount = retrycount + 1
            WHERE messageid = @MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId });
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
