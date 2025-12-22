using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Encina.Messaging.Scheduling;

namespace Encina.MongoDB.Scheduling;

/// <summary>
/// MongoDB implementation of <see cref="IScheduledMessageStore"/>.
/// </summary>
public sealed class ScheduledMessageStoreMongoDB : IScheduledMessageStore
{
    private readonly IMongoCollection<ScheduledMessage> _collection;
    private readonly ILogger<ScheduledMessageStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public ScheduledMessageStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ScheduledMessageStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ScheduledMessage>(config.Collections.ScheduledMessages);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

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

        await _collection.InsertOneAsync(mongoMessage, cancellationToken: cancellationToken).ConfigureAwait(false);
        Log.AddedScheduledMessage(_logger, message.Id, message.ScheduledAtUtc);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

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
            .Find(filter)
            .SortBy(m => m.ScheduledAtUtc)
            .Limit(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Log.RetrievedDueScheduledMessages(_logger, messages.Count);
        return messages;
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
        var update = Builders<ScheduledMessage>.Update
            .Set(m => m.ProcessedAtUtc, DateTime.UtcNow)
            .Set(m => m.LastExecutedAtUtc, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount == 0)
        {
            Log.ScheduledMessageNotFoundForProcessed(_logger, messageId);
        }
        else
        {
            Log.MarkedScheduledMessageAsProcessed(_logger, messageId);
        }
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
        var update = Builders<ScheduledMessage>.Update
            .Set(m => m.ErrorMessage, errorMessage)
            .Set(m => m.NextRetryAtUtc, nextRetryAt)
            .Inc(m => m.RetryCount, 1);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount == 0)
        {
            Log.ScheduledMessageNotFoundForFailed(_logger, messageId);
        }
        else
        {
            Log.MarkedScheduledMessageAsFailed(_logger, messageId, errorMessage);
        }
    }

    /// <inheritdoc />
    public async Task RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
        var update = Builders<ScheduledMessage>.Update
            .Set(m => m.ScheduledAtUtc, nextScheduledAt)
            .Set(m => m.ProcessedAtUtc, null)
            .Set(m => m.ErrorMessage, null)
            .Set(m => m.RetryCount, 0)
            .Set(m => m.NextRetryAtUtc, null);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount == 0)
        {
            Log.ScheduledMessageNotFoundForRescheduling(_logger, messageId);
        }
        else
        {
            Log.RescheduledMessage(_logger, messageId, nextScheduledAt);
        }
    }

    /// <inheritdoc />
    public async Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScheduledMessage>.Filter.Eq(m => m.Id, messageId);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);

        if (result.DeletedCount == 0)
        {
            Log.ScheduledMessageNotFoundForCancellation(_logger, messageId);
        }
        else
        {
            Log.CancelledScheduledMessage(_logger, messageId);
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.CompletedTask;
    }
}
