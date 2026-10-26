using Encina.Messaging.Outbox;
using Encina.MongoDB.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Outbox;

/// <summary>
/// MongoDB implementation of <see cref="IOutboxStore"/>.
/// </summary>
public sealed class OutboxStoreMongoDB : IOutboxStore
{
    private readonly IMongoCollection<OutboxMessage> _collection;
    private readonly ILogger<OutboxStoreMongoDB> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IMongoSessionAccessor? _sessionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    /// <param name="sessionAccessor">
    /// The scope's transaction accessor. When it has an active transaction every operation of the store runs in its
    /// client session; without one (or when null) the store runs without a session, as the background processor does.
    /// </param>
    public OutboxStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<OutboxStoreMongoDB> logger,
        TimeProvider? timeProvider = null,
        IMongoSessionAccessor? sessionAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<OutboxMessage>(config.Collections.Outbox);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sessionAccessor = sessionAccessor;
    }

    private IClientSessionHandle? Session => _sessionAccessor?.CurrentSession;

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var mongoMessage = message as OutboxMessage ?? new OutboxMessage
            {
                Id = message.Id,
                NotificationType = message.NotificationType,
                Content = message.Content,
                CreatedAtUtc = message.CreatedAtUtc,
                ProcessedAtUtc = message.ProcessedAtUtc,
                ErrorMessage = message.ErrorMessage,
                RetryCount = message.RetryCount,
                NextRetryAtUtc = message.NextRetryAtUtc
            };

            await _collection.InsertOneInSessionAsync(Session, mongoMessage, cancellationToken).ConfigureAwait(false);
            Log.AddedOutboxMessage(_logger, message.Id);
        }, "outbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IOutboxMessage>>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var filter = Builders<OutboxMessage>.Filter.And(
                Builders<OutboxMessage>.Filter.Eq(m => m.ProcessedAtUtc, null),
                Builders<OutboxMessage>.Filter.Lt(m => m.RetryCount, maxRetries),
                Builders<OutboxMessage>.Filter.Or(
                    Builders<OutboxMessage>.Filter.Eq(m => m.NextRetryAtUtc, null),
                    Builders<OutboxMessage>.Filter.Lte(m => m.NextRetryAtUtc, now)
                )
            );

            var messages = await _collection
                .FindInSession(Session, filter)
                .SortBy(m => m.CreatedAtUtc)
                .Limit(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            Log.RetrievedPendingOutboxMessages(_logger, messages.Count);
            return (IEnumerable<IOutboxMessage>)messages;
        }, "outbox.get_pending_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<OutboxMessage>.Filter.Eq(m => m.Id, messageId);
            var update = Builders<OutboxMessage>.Update
                .Set(m => m.ProcessedAtUtc, _timeProvider.GetUtcNow().UtcDateTime);

            var result = await _collection.UpdateOneInSessionAsync(Session, filter, update, cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.OutboxMessageNotFoundForProcessed(_logger, messageId);
            }
            else
            {
                Log.MarkedOutboxMessageAsProcessed(_logger, messageId);
            }
        }, "outbox.mark_processed_failed").ConfigureAwait(false);
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
            var filter = Builders<OutboxMessage>.Filter.Eq(m => m.Id, messageId);
            var update = Builders<OutboxMessage>.Update
                .Set(m => m.ErrorMessage, errorMessage)
                .Set(m => m.NextRetryAtUtc, nextRetryAtUtc)
                .Inc(m => m.RetryCount, 1);

            var result = await _collection.UpdateOneInSessionAsync(Session, filter, update, cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.OutboxMessageNotFoundForFailed(_logger, messageId);
            }
            else
            {
                Log.MarkedOutboxMessageAsFailed(_logger, messageId, errorMessage);
            }
        }, "outbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsExhaustedAsync(
        Guid messageId,
        string errorMessage,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            // One atomic pipeline update: retryCount = max(retryCount + 1, maxRetries), no next retry.
            var retryCount = new BsonDocument("$max", new BsonArray
            {
                new BsonDocument("$add", new BsonArray { "$retryCount", 1 }),
                maxRetries
            });
            var stage = new BsonDocument("$set", new BsonDocument
            {
                { "errorMessage", errorMessage },
                { "nextRetryAtUtc", BsonNull.Value },
                { "retryCount", retryCount }
            });

            var filter = Builders<OutboxMessage>.Filter.Eq(m => m.Id, messageId);
            var update = Builders<OutboxMessage>.Update.Pipeline(
                PipelineDefinition<OutboxMessage, OutboxMessage>.Create([stage]));

            var result = await _collection.UpdateOneInSessionAsync(Session, filter, update, cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.OutboxMessageNotFoundForFailed(_logger, messageId);
            }
            else
            {
                Log.MarkedOutboxMessageAsFailed(_logger, messageId, errorMessage);
            }
        }, "outbox.mark_exhausted_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetPendingCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<OutboxMessage>.Filter.And(
                Builders<OutboxMessage>.Filter.Eq(m => m.ProcessedAtUtc, null),
                Builders<OutboxMessage>.Filter.Lt(m => m.RetryCount, maxRetries));

            var count = await _collection.CountInSessionAsync(Session, filter, cancellationToken).ConfigureAwait(false);
            return checked((int)count);
        }, "outbox.get_pending_count_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetExhaustedCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
        {
            var count = await _collection.CountInSessionAsync(Session, ExhaustedFilter(maxRetries), cancellationToken).ConfigureAwait(false);
            return checked((int)count);
        }, "outbox.get_exhausted_count_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> RequeueExhaustedAsync(
        int maxRetries,
        IReadOnlyCollection<Guid>? messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        if (messageIds is { Count: 0 })
            return 0;

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = ExhaustedFilter(maxRetries);
            if (messageIds is not null)
            {
                filter = Builders<OutboxMessage>.Filter.And(
                    filter,
                    Builders<OutboxMessage>.Filter.In(m => m.Id, messageIds.Distinct()));
            }

            var update = Builders<OutboxMessage>.Update
                .Set(m => m.RetryCount, 0)
                .Set(m => m.NextRetryAtUtc, null)
                .Set(m => m.ErrorMessage, null);

            var result = await _collection.UpdateManyInSessionAsync(Session, filter, update, cancellationToken).ConfigureAwait(false);
            return checked((int)result.ModifiedCount);
        }, "outbox.requeue_exhausted_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private static FilterDefinition<OutboxMessage> ExhaustedFilter(int maxRetries)
        => Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(m => m.ProcessedAtUtc, null),
            Builders<OutboxMessage>.Filter.Gte(m => m.RetryCount, maxRetries));
}
