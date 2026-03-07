using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using LanguageExt;

namespace Encina.Dapper.MySQL.Inbox;

/// <summary>
/// Dapper implementation of <see cref="IInboxStore"/> for idempotent message processing.
/// Provides exactly-once semantics by tracking processed messages.
/// </summary>
public sealed class InboxStoreDapper : IInboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The inbox table name (default: InboxMessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public InboxStoreDapper(
        IDbConnection connection,
        string tableName = "InboxMessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await EitherHelpers.TryAsync<Option<IInboxMessage>>(async () =>
        {
            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE MessageId = @MessageId";

            var result = await _connection.QuerySingleOrDefaultAsync<InboxMessage>(sql, new { MessageId = messageId });
            return result is not null
                ? Option<IInboxMessage>.Some(result)
                : Option<IInboxMessage>.None;
        }, "inbox.get_message_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (MessageId, RequestType, ReceivedAtUtc, ProcessedAtUtc, ExpiresAtUtc, Response, ErrorMessage, RetryCount, NextRetryAtUtc, Metadata)
                VALUES
                (@MessageId, @RequestType, @ReceivedAtUtc, @ProcessedAtUtc, @ExpiresAtUtc, @Response, @ErrorMessage, @RetryCount, @NextRetryAtUtc, @Metadata)";

            await _connection.ExecuteAsync(sql, message);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(
        string messageId,
        string response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET ProcessedAtUtc = @NowUtc,
                    Response = @Response,
                    ErrorMessage = NULL
                WHERE MessageId = @MessageId";

            await _connection.ExecuteAsync(sql, new { MessageId = messageId, Response = response, NowUtc = nowUtc });
        }, "inbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return await EitherHelpers.TryAsync(async () =>
        {
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
        }, "inbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        return await EitherHelpers.TryAsync<IEnumerable<IInboxMessage>>(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE ExpiresAtUtc < @NowUtc
                  AND ProcessedAtUtc IS NOT NULL
                ORDER BY ExpiresAtUtc
                LIMIT @BatchSize";

            var messages = await _connection.QueryAsync<InboxMessage>(sql, new { BatchSize = batchSize, NowUtc = nowUtc });
            return messages.Cast<IInboxMessage>();
        }, "inbox.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        return await EitherHelpers.TryAsync(async () =>
        {
            if (!messageIds.Any())
                return;

            var sql = $@"
                DELETE FROM {_tableName}
                WHERE MessageId IN @MessageIds";

            await _connection.ExecuteAsync(sql, new { MessageIds = messageIds });
        }, "inbox.remove_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                UPDATE {_tableName}
                SET RetryCount = RetryCount + 1
                WHERE MessageId = @MessageId";

            await _connection.ExecuteAsync(sql, new { MessageId = messageId });
        }, "inbox.increment_retry_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
