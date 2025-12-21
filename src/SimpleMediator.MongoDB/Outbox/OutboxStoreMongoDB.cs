using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimpleMediator.Messaging.Outbox;

namespace SimpleMediator.MongoDB.Outbox;

/// <summary>
/// MongoDB implementation of <see cref="IOutboxStore"/>.
/// </summary>
public sealed class OutboxStoreMongoDB : IOutboxStore
{
    private readonly IMongoCollection<OutboxMessage> _collection;
    private readonly ILogger<OutboxStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public OutboxStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<SimpleMediatorMongoDbOptions> options,
        ILogger<OutboxStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<OutboxMessage>(config.Collections.Outbox);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

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
        _logger.LogDebug("Added outbox message {MessageId} of type {NotificationType}", message.Id, message.NotificationType);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

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

        _logger.LogDebug("Retrieved {Count} pending outbox messages", messages.Count);
        return messages;
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(m => m.Id, messageId);
        var update = Builders<OutboxMessage>.Update
            .Set(m => m.ProcessedAtUtc, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount == 0)
        {
            _logger.LogWarning("Outbox message {MessageId} not found for marking as processed", messageId);
        }
        else
        {
            _logger.LogDebug("Marked outbox message {MessageId} as processed", messageId);
        }
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(m => m.Id, messageId);
        var update = Builders<OutboxMessage>.Update
            .Set(m => m.ErrorMessage, errorMessage)
            .Set(m => m.NextRetryAtUtc, nextRetryAt)
            .Inc(m => m.RetryCount, 1);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount == 0)
        {
            _logger.LogWarning("Outbox message {MessageId} not found for marking as failed", messageId);
        }
        else
        {
            _logger.LogDebug("Marked outbox message {MessageId} as failed: {ErrorMessage}", messageId, errorMessage);
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.CompletedTask;
    }
}
