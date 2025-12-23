using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.Oracle.Inbox;

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
    public InboxStoreDapper(IDbConnection connection, string tableName = "InboxMessages")
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

        return await _connection.QuerySingleOrDefaultAsync<InboxMessage>(sql, new { MessageId = messageId });
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
            SET ProcessedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                Response = :Response,
                ErrorMessage = NULL
            WHERE MessageId = :MessageId";

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
            SET ErrorMessage = :ErrorMessage,
                RetryCount = RetryCount + 1,
                NextRetryAtUtc = :NextRetryAtUtc
            WHERE MessageId = :MessageId";

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
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE ExpiresAtUtc < SYS_EXTRACT_UTC(SYSTIMESTAMP)
              AND ProcessedAtUtc IS NOT NULL
            ORDER BY ExpiresAtUtc
            FETCH FIRST :BatchSize ROWS ONLY";

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
            WHERE MessageId IN :MessageIds";

        await _connection.ExecuteAsync(sql, new { MessageIds = messageIds });
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
