using Encina.Diagnostics;
using Encina.MongoDB.Auditing;
using Encina.MongoDB.DeadLetter;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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
            foreach (var (enabled, createIndexes) in FeatureIndexCreators())
            {
                if (enabled)
                {
                    await createIndexes(database, cancellationToken).ConfigureAwait(false);
                }
            }

            Log.IndexesCreatedSuccessfully(_logger);
        }
        catch (Exception ex)
        {
            Log.FailedToCreateIndexes(_logger, ex.ForLogging());
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // One entry per feature flag, in creation order.
    private (bool Enabled, Func<IMongoDatabase, CancellationToken, Task> CreateIndexes)[] FeatureIndexCreators() =>
    [
        (_options.UseOutbox, CreateOutboxIndexesAsync),
        (_options.UseInbox, CreateInboxIndexesAsync),
        (_options.UseSagas, CreateSagaIndexesAsync),
        (_options.UseScheduling, CreateSchedulingIndexesAsync),
        (_options.UseDeadLetterQueue, CreateDeadLetterIndexesAsync),
        (_options.UseAuditLogStore, CreateAuditLogIndexesAsync),
        (_options.UseOperationAuditStore, CreateOperationAuditIndexesAsync),
    ];

    // Mirrors the six indexes the relational providers ship in 029_CreateDeadLetterMessagesTable.sql.
    // No TTL index: expired rows are deleted by the cleanup loop on every provider (EnableAutomaticCleanup
    // and the metrics stay honest), so ExpiresAtUtc gets an ordinary index.
    private async Task CreateDeadLetterIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<DeadLetterMessage>(_options.Collections.DeadLetterMessages);

        await collection.Indexes.CreateManyAsync(BuildDeadLetterIndexModels(), cancellationToken).ConfigureAwait(false);
        Log.CreatedDeadLetterIndexes(_logger);
    }

    private static List<CreateIndexModel<DeadLetterMessage>> BuildDeadLetterIndexModels()
    {
        var keys = Builders<DeadLetterMessage>.IndexKeys;

        return
        [
            // Idempotent capture: SourceMessageId is never null, so a plain unique key is enough
            new(
                keys.Ascending(m => m.SourcePattern).Ascending(m => m.SourceMessageId),
                new CreateIndexOptions { Name = "UX_DeadLetterMessages_Source", Unique = true }),
            // Paging order
            new(
                keys.Ascending(m => m.DeadLetteredAtUtc).Ascending(m => m.Id),
                new CreateIndexOptions { Name = "IX_DeadLetterMessages_DeadLetteredAt" }),
            // Health check, statistics and FromSource
            new(
                keys.Ascending(m => m.ReplayedAtUtc).Ascending(m => m.SourcePattern).Ascending(m => m.DeadLetteredAtUtc),
                new CreateIndexOptions { Name = "IX_DeadLetterMessages_Pending" }),
            // Cleanup
            new(
                keys.Ascending(m => m.ExpiresAtUtc),
                new CreateIndexOptions { Name = "IX_DeadLetterMessages_ExpiresAt" }),
            new(
                keys.Ascending(m => m.CorrelationId),
                new CreateIndexOptions { Name = "IX_DeadLetterMessages_CorrelationId" }),
            new(
                keys.Ascending(m => m.TenantId).Ascending(m => m.DeadLetteredAtUtc),
                new CreateIndexOptions { Name = "IX_DeadLetterMessages_Tenant" })
        ];
    }

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

    private async Task CreateAuditLogIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<AuditLogDocument>(_options.Collections.AuditLogs);

        var indexModels = new List<CreateIndexModel<AuditLogDocument>>
        {
            // Composite index for efficient history lookups by entity
            new(
                Builders<AuditLogDocument>.IndexKeys
                    .Ascending(d => d.EntityType)
                    .Ascending(d => d.EntityId),
                new CreateIndexOptions { Name = "IX_AuditLogs_Entity" }
            ),
            // Index for time-based queries
            new(
                Builders<AuditLogDocument>.IndexKeys.Ascending(d => d.TimestampUtc),
                new CreateIndexOptions { Name = "IX_AuditLogs_Timestamp" }
            ),
            // Sparse index on UserId for user activity tracking (only non-null values)
            new(
                Builders<AuditLogDocument>.IndexKeys.Ascending(d => d.UserId),
                new CreateIndexOptions
                {
                    Name = "IX_AuditLogs_UserId",
                    Sparse = true
                }
            ),
            // Sparse index on CorrelationId for request correlation tracking (only non-null values)
            new(
                Builders<AuditLogDocument>.IndexKeys.Ascending(d => d.CorrelationId),
                new CreateIndexOptions
                {
                    Name = "IX_AuditLogs_CorrelationId",
                    Sparse = true
                }
            )
        };

        await collection.Indexes.CreateManyAsync(indexModels, cancellationToken).ConfigureAwait(false);
        Log.CreatedAuditLogIndexes(_logger);
    }

    // Mirrors the seven indexes the relational providers ship in 028_CreateOperationAuditEntriesTable.sql.
    private async Task CreateOperationAuditIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<OperationAuditEntryDocument>(_options.Collections.OperationAuditEntries);

        await collection.Indexes.CreateManyAsync(BuildOperationAuditIndexModels(), cancellationToken).ConfigureAwait(false);
        Log.CreatedOperationAuditIndexes(_logger);
    }

    private static List<CreateIndexModel<OperationAuditEntryDocument>> BuildOperationAuditIndexModels()
    {
        var keys = Builders<OperationAuditEntryDocument>.IndexKeys;

        return
        [
            new(
                keys.Ascending(d => d.EntityType).Ascending(d => d.EntityId),
                new CreateIndexOptions { Name = "IX_OperationAuditEntries_Entity" }),
            // Time-based queries and the retention purge
            new(
                keys.Ascending(d => d.TimestampUtc),
                new CreateIndexOptions { Name = "IX_OperationAuditEntries_Timestamp" }),
            new(
                keys.Ascending(d => d.Outcome),
                new CreateIndexOptions { Name = "IX_OperationAuditEntries_Outcome" }),
            // Sparse: only documents that have a value are indexed
            new(
                keys.Ascending(d => d.UserId),
                new CreateIndexOptions { Name = "IX_OperationAuditEntries_UserId", Sparse = true }),
            new(
                keys.Ascending(d => d.TenantId),
                new CreateIndexOptions { Name = "IX_OperationAuditEntries_TenantId", Sparse = true }),
            new(
                keys.Ascending(d => d.CorrelationId),
                new CreateIndexOptions { Name = "IX_OperationAuditEntries_CorrelationId" }),
            new(
                keys.Ascending(d => d.Action),
                new CreateIndexOptions { Name = "IX_OperationAuditEntries_Action" })
        ];
    }

}
