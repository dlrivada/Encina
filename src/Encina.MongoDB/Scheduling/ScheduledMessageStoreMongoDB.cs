using Encina.Messaging.Scheduling;
using Encina.MongoDB.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Scheduling;

/// <summary>
/// MongoDB implementation of <see cref="IScheduledMessageStore"/>.
/// </summary>
public sealed class ScheduledMessageStoreMongoDB : IScheduledMessageStore
{
    private readonly IMongoCollection<ScheduledMessage> _collection;
    private readonly ILogger<ScheduledMessageStoreMongoDB> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IMongoSessionAccessor? _sessionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    /// <param name="sessionAccessor">
    /// The scope's transaction accessor. When it has an active transaction every operation of the store runs in its
    /// client session; without one (or when null) the store runs without a session.
    /// </param>
    public ScheduledMessageStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ScheduledMessageStoreMongoDB> logger,
        TimeProvider? timeProvider = null,
        IMongoSessionAccessor? sessionAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ScheduledMessage>(config.Collections.ScheduledMessages);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sessionAccessor = sessionAccessor;
    }

    private IClientSessionHandle? Session => _sessionAccessor?.CurrentSession;

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var mongoMessage = message as ScheduledMessage ?? new ScheduledMessage
            {
                Id = message.Id,
                RequestType = message.RequestType,
                Content = message.Content,
                ScheduledAtUtc = message.ScheduledAtUtc,
                CreatedAtUtc = message.CreatedAtUtc,
                ProcessedAtUtc = message.ProcessedAtUtc,
                ErrorMessage = message.ErrorMessage,
                RetryCount = message.RetryCount,
                NextRetryAtUtc = message.NextRetryAtUtc,
                IsRecurring = message.IsRecurring,
                CronExpression = message.CronExpression,
                LastExecutedAtUtc = message.LastExecutedAtUtc
            };

            await _collection.InsertOneInSessionAsync(Session, mongoMessage, cancellationToken).ConfigureAwait(false);
            Log.AddedScheduledMessage(_logger, message.Id, message.ScheduledAtUtc);
        }, "scheduling.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IScheduledMessage>>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var filter = Builders<ScheduledMessage>.Filter.And(
                Builders<ScheduledMessage>.Filter.Eq(m => m.ProcessedAtUtc, null),
                Builders<ScheduledMessage>.Filter.Lte(m => m.ScheduledAtUtc, now),
                Builders<ScheduledMessage>.Filter.Lt(m => m.RetryCount, maxRetries),
                Builders<ScheduledMessage>.Filter.Or(
                    Builders<ScheduledMessage>.Filter.Eq(m => m.NextRetryAtUtc, null),
                    Builders<ScheduledMessage>.Filter.Lte(m => m.NextRetryAtUtc, now)
                )
            );

            var messages = await _collection
                .FindInSession(Session, filter)
                .SortBy(m => m.ScheduledAtUtc)
                .Limit(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            Log.RetrievedDueScheduledMessages(_logger, messages.Count);
            return (IEnumerable<IScheduledMessage>)messages;
        }, "scheduling.get_due_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
            var update = Builders<ScheduledMessage>.Update
                .Set(m => m.ProcessedAtUtc, _timeProvider.GetUtcNow().UtcDateTime)
                .Set(m => m.LastExecutedAtUtc, _timeProvider.GetUtcNow().UtcDateTime);

            var result = await _collection.UpdateOneInSessionAsync(Session, filter, update, cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.ScheduledMessageNotFoundForProcessed(_logger, messageId);
            }
            else
            {
                Log.MarkedScheduledMessageAsProcessed(_logger, messageId);
            }
        }, "scheduling.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
            var update = Builders<ScheduledMessage>.Update
                .Set(m => m.ErrorMessage, errorMessage)
                .Set(m => m.NextRetryAtUtc, nextRetryAtUtc)
                .Inc(m => m.RetryCount, 1);

            var result = await _collection.UpdateOneInSessionAsync(Session, filter, update, cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.ScheduledMessageNotFoundForFailed(_logger, messageId);
            }
            else
            {
                Log.MarkedScheduledMessageAsFailed(_logger, messageId, errorMessage);
            }
        }, "scheduling.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
            var update = Builders<ScheduledMessage>.Update
                .Set(m => m.ScheduledAtUtc, nextScheduledAtUtc)
                .Set(m => m.ProcessedAtUtc, null)
                .Set(m => m.ErrorMessage, null)
                .Set(m => m.RetryCount, 0)
                .Set(m => m.NextRetryAtUtc, null);

            var result = await _collection.UpdateOneInSessionAsync(Session, filter, update, cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.ScheduledMessageNotFoundForRescheduling(_logger, messageId);
            }
            else
            {
                Log.RescheduledMessage(_logger, messageId, nextScheduledAtUtc);
            }
        }, "scheduling.reschedule_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
            var result = await _collection.DeleteOneInSessionAsync(Session, filter, cancellationToken).ConfigureAwait(false);

            if (result.DeletedCount == 0)
            {
                Log.ScheduledMessageNotFoundForCancellation(_logger, messageId);
            }
            else
            {
                Log.CancelledScheduledMessage(_logger, messageId);
            }
        }, "scheduling.cancel_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
