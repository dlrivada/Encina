using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;

namespace Encina.MongoDB;

/// <summary>
/// Background service that creates MongoDB indexes on startup.
/// </summary>
internal sealed class MongoDbIndexCreator : IHostedService
{
    private readonly IMongoClient _mongoClient;
    private readonly EncinaMongoDbOptions _options;
    private readonly ILogger<MongoDbIndexCreator> _logger;

    public MongoDbIndexCreator(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<MongoDbIndexCreator> logger)
    {
        _mongoClient = mongoClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var database = _mongoClient.GetDatabase(_options.DatabaseName);

        try
        {
            if (_options.UseOutbox)
            {
                await CreateOutboxIndexesAsync(database, cancellationToken).ConfigureAwait(false);
            }

            if (_options.UseInbox)
            {
                await CreateInboxIndexesAsync(database, cancellationToken).ConfigureAwait(false);
            }

            if (_options.UseSagas)
            {
                await CreateSagaIndexesAsync(database, cancellationToken).ConfigureAwait(false);
            }

            if (_options.UseScheduling)
            {
                await CreateSchedulingIndexesAsync(database, cancellationToken).ConfigureAwait(false);
            }

            Log.IndexesCreatedSuccessfully(_logger);
        }
        catch (Exception ex)
        {
            Log.FailedToCreateIndexes(_logger, ex);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateOutboxIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<OutboxMessage>(_options.Collections.Outbox);

        var indexModels = new List<CreateIndexModel<OutboxMessage>>
        {
            // Index for GetPendingMessagesAsync query
            new(
                Builders<OutboxMessage>.IndexKeys
                    .Ascending(m => m.ProcessedAtUtc)
                    .Ascending(m => m.RetryCount)
                    .Ascending(m => m.NextRetryAtUtc)
                    .Ascending(m => m.CreatedAtUtc),
                new CreateIndexOptions { Name = "IX_Outbox_Pending" }
            ),
            // Index for finding by notification type
            new(
                Builders<OutboxMessage>.IndexKeys.Ascending(m => m.NotificationType),
                new CreateIndexOptions { Name = "IX_Outbox_NotificationType" }
            )
        };

        await collection.Indexes.CreateManyAsync(indexModels, cancellationToken).ConfigureAwait(false);
        Log.CreatedOutboxIndexes(_logger);
    }

    private async Task CreateInboxIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<InboxMessage>(_options.Collections.Inbox);

        var indexModels = new List<CreateIndexModel<InboxMessage>>
        {
            // TTL index for automatic cleanup of expired messages
            new(
                Builders<InboxMessage>.IndexKeys.Ascending(m => m.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "IX_Inbox_Expires_TTL",
                    ExpireAfter = TimeSpan.Zero // Documents expire at ExpiresAtUtc
                }
            ),
            // Index for finding by request type
            new(
                Builders<InboxMessage>.IndexKeys.Ascending(m => m.RequestType),
                new CreateIndexOptions { Name = "IX_Inbox_RequestType" }
            )
        };

        await collection.Indexes.CreateManyAsync(indexModels, cancellationToken).ConfigureAwait(false);
        Log.CreatedInboxIndexes(_logger);
    }

    private async Task CreateSagaIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<SagaState>(_options.Collections.Sagas);

        var indexModels = new List<CreateIndexModel<SagaState>>
        {
            // Index for GetStuckSagasAsync query
            new(
                Builders<SagaState>.IndexKeys
                    .Ascending(s => s.CompletedAtUtc)
                    .Ascending(s => s.LastUpdatedAtUtc),
                new CreateIndexOptions { Name = "IX_Saga_Stuck" }
            ),
            // Index for finding by saga type
            new(
                Builders<SagaState>.IndexKeys.Ascending(s => s.SagaType),
                new CreateIndexOptions { Name = "IX_Saga_Type" }
            ),
            // Index for finding by status
            new(
                Builders<SagaState>.IndexKeys.Ascending(s => s.Status),
                new CreateIndexOptions { Name = "IX_Saga_Status" }
            )
        };

        await collection.Indexes.CreateManyAsync(indexModels, cancellationToken).ConfigureAwait(false);
        Log.CreatedSagaIndexes(_logger);
    }

    private async Task CreateSchedulingIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<ScheduledMessage>(_options.Collections.ScheduledMessages);

        var indexModels = new List<CreateIndexModel<ScheduledMessage>>
        {
            // Index for GetDueMessagesAsync query
            new(
                Builders<ScheduledMessage>.IndexKeys
                    .Ascending(m => m.ProcessedAtUtc)
                    .Ascending(m => m.ScheduledAtUtc)
                    .Ascending(m => m.RetryCount)
                    .Ascending(m => m.NextRetryAtUtc),
                new CreateIndexOptions { Name = "IX_Scheduled_Due" }
            ),
            // Index for finding by request type
            new(
                Builders<ScheduledMessage>.IndexKeys.Ascending(m => m.RequestType),
                new CreateIndexOptions { Name = "IX_Scheduled_RequestType" }
            ),
            // Index for recurring messages
            new(
                Builders<ScheduledMessage>.IndexKeys.Ascending(m => m.IsRecurring),
                new CreateIndexOptions { Name = "IX_Scheduled_Recurring" }
            )
        };

        await collection.Indexes.CreateManyAsync(indexModels, cancellationToken).ConfigureAwait(false);
        Log.CreatedSchedulingIndexes(_logger);
    }
}
