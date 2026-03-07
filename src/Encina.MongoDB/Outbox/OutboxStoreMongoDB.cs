using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    public OutboxStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<OutboxStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<OutboxMessage>(config.Collections.Outbox);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

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

            await _collection.InsertOneAsync(mongoMessage, cancellationToken: cancellationToken).ConfigureAwait(false);
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
                .Find(filter)
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

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

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

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

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
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
