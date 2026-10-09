using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Inbox;

/// <summary>
/// Dapper implementation of <see cref="IInboxStore"/> for idempotent message processing.
/// Provides exactly-once semantics by tracking processed messages.
/// </summary>
public sealed class InboxStoreDapper : IInboxStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;
    private readonly IDbTransactionAccessor? _transactionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The inbox table name (default: InboxMessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <param name="transactionAccessor">Optional accessor of the business transaction on the shared connection; when a transaction is active the store enlists or leaves it as documented in ADR-048.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is null or whitespace.</exception>
    public InboxStoreDapper(
        IDbConnection connection,
        string tableName = "InboxMessages",
        TimeProvider? timeProvider = null,
        IDbTransactionAccessor? transactionAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _transactionAccessor = transactionAccessor;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> is null or whitespace.</exception>
    public async Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE MessageId = @MessageId";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var result = await lease.Connection.QuerySingleOrDefaultAsync<InboxMessage>(sql, new { MessageId = messageId }, lease.Transaction);

            return result is not null
                ? Option<IInboxMessage>.Some(result)
                : Option<IInboxMessage>.None;
        }, "inbox.get_message_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
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

            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken);
            await lease.Connection.ExecuteAsync(sql, message, lease.Transaction);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> is null or whitespace.</exception>
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

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            await lease.Connection.ExecuteAsync(sql, new { MessageId = messageId, Response = response, NowUtc = nowUtc }, lease.Transaction);
        }, "inbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> CacheHandlerErrorAsync(
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

            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken);
            await lease.Connection.ExecuteAsync(sql, new { MessageId = messageId, Response = response, NowUtc = nowUtc }, lease.Transaction);
        }, "inbox.cache_handler_error_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="messageId"/> or <paramref name="errorMessage"/> is null or whitespace.</exception>
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

            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken);
            await lease.Connection.ExecuteAsync(
                sql,
                new
                {
                    MessageId = messageId,
                    ErrorMessage = errorMessage,
                    NextRetryAtUtc = nextRetryAtUtc
                },
                lease.Transaction);
        }, "inbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize"/> is less than 1.</exception>
    public async Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                SELECT TOP (@BatchSize) *
                FROM {_tableName}
                WHERE ExpiresAtUtc < @NowUtc
                  AND ProcessedAtUtc IS NOT NULL
                ORDER BY ExpiresAtUtc";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            var messages = await lease.Connection.QueryAsync<InboxMessage>(sql, new { BatchSize = batchSize, NowUtc = nowUtc }, lease.Transaction);
            return messages.Cast<IInboxMessage>();
        }, "inbox.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messageIds"/> is null.</exception>
    public async Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        // Early return for empty collections - no-op is valid behavior
        if (!messageIds.Any())
            return Unit.Default;

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                DELETE FROM {_tableName}
                WHERE MessageId IN @MessageIds";

            using var lease = DbLease.Enlisted(_connection, _transactionAccessor);
            await lease.Connection.ExecuteAsync(sql, new { MessageIds = messageIds }, lease.Transaction);
        }, "inbox.remove_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
