using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Inbox;

/// <summary>
/// MongoDB implementation of <see cref="IInboxStore"/>.
/// </summary>
public sealed class InboxStoreMongoDB : IInboxStore
{
    private readonly IMongoCollection<InboxMessage> _collection;
    private readonly ILogger<InboxStoreMongoDB> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    public InboxStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<InboxStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<InboxMessage>(config.Collections.Inbox);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _collection
                .Find(m => m.MessageId == messageId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (message is not null)
            {
                Log.FoundInboxMessage(_logger, messageId);
                return Option<IInboxMessage>.Some(message);
            }

            return Option<IInboxMessage>.None;
        }, "inbox.get_message_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var mongoMessage = message as InboxMessage ?? new InboxMessage
            {
                MessageId = message.MessageId,
                RequestType = message.RequestType,
                Response = message.Response,
                ErrorMessage = message.ErrorMessage,
                ReceivedAtUtc = message.ReceivedAtUtc,
                ProcessedAtUtc = message.ProcessedAtUtc,
                ExpiresAtUtc = message.ExpiresAtUtc,
                RetryCount = message.RetryCount,
                NextRetryAtUtc = message.NextRetryAtUtc
            };

            await _collection.InsertOneAsync(mongoMessage, cancellationToken: cancellationToken).ConfigureAwait(false);
            Log.AddedInboxMessage(_logger, message.MessageId);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
            var update = Builders<InboxMessage>.Update
                .Set(m => m.ProcessedAtUtc, _timeProvider.GetUtcNow().UtcDateTime)
                .Set(m => m.Response, response);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.InboxMessageNotFoundForProcessed(_logger, messageId);
            }
            else
            {
                Log.MarkedInboxMessageAsProcessed(_logger, messageId);
            }
        }, "inbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
            var update = Builders<InboxMessage>.Update
                .Set(m => m.ErrorMessage, errorMessage)
                .Set(m => m.NextRetryAtUtc, nextRetryAtUtc)
                .Inc(m => m.RetryCount, 1);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.InboxMessageNotFoundForFailed(_logger, messageId);
            }
            else
            {
                Log.MarkedInboxMessageAsFailed(_logger, messageId, errorMessage);
            }
        }, "inbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var filter = Builders<InboxMessage>.Filter.Lt(m => m.ExpiresAtUtc, now);

            var messages = await _collection
                .Find(filter)
                .Limit(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            Log.RetrievedExpiredInboxMessages(_logger, messages.Count);
            return (IEnumerable<IInboxMessage>)messages;
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
            var idList = messageIds.ToList();
            if (idList.Count == 0)
            {
                return;
            }

            var filter = Builders<InboxMessage>.Filter.In(m => m.MessageId, idList);
            var result = await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);

            Log.RemovedExpiredInboxMessages(_logger, result.DeletedCount);
        }, "inbox.remove_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
            var update = Builders<InboxMessage>.Update.Inc(m => m.RetryCount, 1);

            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
        }, "inbox.increment_retry_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
