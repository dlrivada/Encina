using Encina.Messaging.Inbox;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public InboxStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<InboxStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<InboxMessage>(config.Collections.Inbox);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IInboxMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        var message = await _collection
            .Find(m => m.MessageId == messageId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (message is not null)
        {
            Log.FoundInboxMessage(_logger, messageId);
        }

        return message;
    }

    /// <inheritdoc />
    public async Task AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

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
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
        var update = Builders<InboxMessage>.Update
            .Set(m => m.ProcessedAtUtc, DateTime.UtcNow)
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
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
        var update = Builders<InboxMessage>.Update
            .Set(m => m.ErrorMessage, errorMessage)
            .Set(m => m.NextRetryAtUtc, nextRetryAt)
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
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var filter = Builders<InboxMessage>.Filter.Lt(m => m.ExpiresAtUtc, now);

        var messages = await _collection
            .Find(filter)
            .Limit(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Log.RetrievedExpiredInboxMessages(_logger, messages.Count);
        return messages;
    }

    /// <inheritdoc />
    public async Task RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        var idList = messageIds.ToList();
        if (idList.Count == 0)
        {
            return;
        }

        var filter = Builders<InboxMessage>.Filter.In(m => m.MessageId, idList);
        var result = await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);

        Log.RemovedExpiredInboxMessages(_logger, result.DeletedCount);
    }

    /// <inheritdoc />
    public async Task IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
        var update = Builders<InboxMessage>.Update.Inc(m => m.RetryCount, 1);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.CompletedTask;
    }
}
