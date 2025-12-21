using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimpleMediator.Messaging.Inbox;

namespace SimpleMediator.MongoDB.Inbox;

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
        IOptions<SimpleMediatorMongoDbOptions> options,
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
            _logger.LogDebug("Found inbox message {MessageId}", messageId);
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
        _logger.LogDebug("Added inbox message {MessageId} of type {RequestType}", message.MessageId, message.RequestType);
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
            _logger.LogWarning("Inbox message {MessageId} not found for marking as processed", messageId);
        }
        else
        {
            _logger.LogDebug("Marked inbox message {MessageId} as processed", messageId);
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
            _logger.LogWarning("Inbox message {MessageId} not found for marking as failed", messageId);
        }
        else
        {
            _logger.LogDebug("Marked inbox message {MessageId} as failed: {ErrorMessage}", messageId, errorMessage);
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

        _logger.LogDebug("Retrieved {Count} expired inbox messages", messages.Count);
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

        _logger.LogDebug("Removed {Count} expired inbox messages", result.DeletedCount);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.CompletedTask;
    }
}
