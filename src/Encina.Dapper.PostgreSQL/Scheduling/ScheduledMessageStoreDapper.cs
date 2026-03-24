using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Scheduling;

/// <summary>
/// Dapper implementation of <see cref="IScheduledMessageStore"/> for delayed message execution.
/// Provides persistence and retrieval of scheduled messages with support for recurring schedules.
/// </summary>
public sealed class ScheduledMessageStoreDapper : IScheduledMessageStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The scheduled messages table name (default: scheduledmessages).</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is null or whitespace.</exception>
    public ScheduledMessageStoreDapper(
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
    public async Task<Either<EncinaError, Unit>> AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (id, requesttype, content, scheduledatutc, createdatutc, processedatutc, lastexecutedatutc,
                 errormessage, retrycount, nextretryatutc, isrecurring, cronexpression)
                VALUES
                (@Id, @RequestType, @Content, @ScheduledAtUtc, @CreatedAtUtc, @ProcessedAtUtc, @LastExecutedAtUtc,
                 @ErrorMessage, @RetryCount, @NextRetryAtUtc, @IsRecurring, @CronExpression)";

            await _connection.ExecuteAsync(sql, message);
        }, "scheduling.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IScheduledMessage>>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));
        if (maxRetries < 0)
            throw new ArgumentException(StoreValidationMessages.MaxRetriesCannotBeNegative, nameof(maxRetries));

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                SELECT id, requesttype, content, scheduledatutc, createdatutc, processedatutc, lastexecutedatutc,
                       errormessage, retrycount, nextretryatutc, isrecurring, cronexpression, correlationid, metadata
                FROM {_tableName}
                WHERE (processedatutc IS NULL OR isrecurring = true)
                  AND retrycount < @MaxRetries
                  AND (
                      (nextretryatutc IS NOT NULL AND nextretryatutc <= @NowUtc)
                      OR (nextretryatutc IS NULL AND scheduledatutc <= @NowUtc)
                  )
                ORDER BY scheduledatutc
                LIMIT @BatchSize";

            var messages = await _connection.QueryAsync<ScheduledMessage>(
                sql,
                new { BatchSize = batchSize, MaxRetries = maxRetries, NowUtc = nowUtc });

            return messages.Cast<IScheduledMessage>();
        }, "scheduling.get_due_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET processedatutc = @NowUtc,
                    lastexecutedatutc = @NowUtc,
                    errormessage = NULL
                WHERE id = @MessageId";

            await _connection.ExecuteAsync(sql, new { MessageId = messageId, NowUtc = nowUtc });
        }, "scheduling.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET errormessage = @ErrorMessage,
                    retrycount = retrycount + 1,
                    nextretryatutc = @NextRetryAtUtc,
                    lastexecutedatutc = @NowUtc
                WHERE id = @MessageId";

            await _connection.ExecuteAsync(
                sql,
                new
                {
                    MessageId = messageId,
                    ErrorMessage = errorMessage,
                    NextRetryAtUtc = nextRetryAtUtc,
                    NowUtc = nowUtc
                });
        }, "scheduling.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));
        if (nextScheduledAtUtc < _timeProvider.GetUtcNow().UtcDateTime)
            throw new ArgumentException(StoreValidationMessages.NextScheduledDateCannotBeInPast, nameof(nextScheduledAtUtc));

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                UPDATE {_tableName}
                SET scheduledatutc = @NextScheduledAtUtc,
                    processedatutc = NULL,
                    errormessage = NULL,
                    retrycount = 0,
                    nextretryatutc = NULL
                WHERE id = @MessageId";

            await _connection.ExecuteAsync(
                sql,
                new
                {
                    MessageId = messageId,
                    NextScheduledAtUtc = nextScheduledAtUtc
                });
        }, "scheduling.reschedule_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.MessageIdCannotBeEmptyGuid, nameof(messageId));

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                DELETE FROM {_tableName}
                WHERE id = @MessageId";

            await _connection.ExecuteAsync(sql, new { MessageId = messageId });
        }, "scheduling.cancel_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
