using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Outbox;

namespace Encina.Dapper.Sqlite.Outbox;

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
    /// <param name="tableName">The outbox table name (default: OutboxMessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when connection or tableName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tableName is empty or whitespace.</exception>
    public OutboxStoreDapper(
        IDbConnection connection,
        string tableName = "OutboxMessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sql = $@"
            INSERT INTO {_tableName}
            (Id, NotificationType, Content, CreatedAtUtc, ProcessedAtUtc, ErrorMessage, RetryCount, NextRetryAtUtc)
            VALUES
            (@Id, @NotificationType, @Content, @CreatedAtUtc, @ProcessedAtUtc, @ErrorMessage, @RetryCount, @NextRetryAtUtc)";

        await _connection.ExecuteAsync(sql, message);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1 or maxRetries is negative.</exception>
    public async Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE ProcessedAtUtc IS NULL
              AND RetryCount < @MaxRetries
              AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @NowUtc)
            ORDER BY CreatedAtUtc
            LIMIT @BatchSize";

        var messages = await _connection.QueryAsync<OutboxMessage>(
            sql,
            new { BatchSize = batchSize, MaxRetries = maxRetries, NowUtc = nowUtc });

        return messages.Cast<IOutboxMessage>();
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when messageId is empty Guid.</exception>
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message ID cannot be empty GUID.", nameof(messageId));
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            UPDATE {_tableName}
            SET ProcessedAtUtc = @NowUtc,
                ErrorMessage = NULL
            WHERE Id = @MessageId";

        await _connection.ExecuteAsync(sql, new { MessageId = messageId, NowUtc = nowUtc });
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when messageId is empty Guid or errorMessage is empty/whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when errorMessage is null.</exception>
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message ID cannot be empty GUID.", nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var sql = $@"
            UPDATE {_tableName}
            SET ErrorMessage = @ErrorMessage,
                RetryCount = RetryCount + 1,
                NextRetryAtUtc = @NextRetryAtUtc
            WHERE Id = @MessageId";

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
