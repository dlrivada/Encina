using Encina.MongoDB.Auditing;
using Encina.MongoDB.Consent;
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

            if (_options.UseAuditLogStore)
            {
                await CreateAuditLogIndexesAsync(database, cancellationToken).ConfigureAwait(false);
            }

            if (_options.UseConsent)
            {
                await CreateConsentIndexesAsync(database, cancellationToken).ConfigureAwait(false);
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

    private async Task CreateConsentIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        // Consent records indexes
        var consentsCollection = database.GetCollection<ConsentRecordDocument>(_options.Collections.Consents);

        var consentIndexModels = new List<CreateIndexModel<ConsentRecordDocument>>
        {
            // Unique compound index for one consent per subject-purpose pair
            new(
                Builders<ConsentRecordDocument>.IndexKeys
                    .Ascending(d => d.SubjectId)
                    .Ascending(d => d.Purpose),
                new CreateIndexOptions
                {
                    Name = "IX_Consent_SubjectId_Purpose",
                    Unique = true
                }
            ),
            // Index for querying by subject and status
            new(
                Builders<ConsentRecordDocument>.IndexKeys
                    .Ascending(d => d.SubjectId)
                    .Ascending(d => d.Status),
                new CreateIndexOptions { Name = "IX_Consent_SubjectId_Status" }
            ),
            // Index for status-based queries (e.g., finding all active consents)
            new(
                Builders<ConsentRecordDocument>.IndexKeys.Ascending(d => d.Status),
                new CreateIndexOptions { Name = "IX_Consent_Status" }
            )
        };

        await consentsCollection.Indexes.CreateManyAsync(consentIndexModels, cancellationToken).ConfigureAwait(false);

        // Consent audit entries indexes
        var auditCollection = database.GetCollection<ConsentAuditEntryDocument>(_options.Collections.ConsentAuditEntries);

        var auditIndexModels = new List<CreateIndexModel<ConsentAuditEntryDocument>>
        {
            // Index for querying audit trail by subject
            new(
                Builders<ConsentAuditEntryDocument>.IndexKeys.Ascending(d => d.SubjectId),
                new CreateIndexOptions { Name = "IX_ConsentAudit_SubjectId" }
            ),
            // Compound index for querying audit trail by subject and purpose
            new(
                Builders<ConsentAuditEntryDocument>.IndexKeys
                    .Ascending(d => d.SubjectId)
                    .Ascending(d => d.Purpose),
                new CreateIndexOptions { Name = "IX_ConsentAudit_SubjectId_Purpose" }
            ),
            // Index for time-based queries
            new(
                Builders<ConsentAuditEntryDocument>.IndexKeys.Ascending(d => d.OccurredAtUtc),
                new CreateIndexOptions { Name = "IX_ConsentAudit_OccurredAtUtc" }
            )
        };

        await auditCollection.Indexes.CreateManyAsync(auditIndexModels, cancellationToken).ConfigureAwait(false);

        // Consent versions indexes
        var versionsCollection = database.GetCollection<ConsentVersionDocument>(_options.Collections.ConsentVersions);

        var versionIndexModels = new List<CreateIndexModel<ConsentVersionDocument>>
        {
            // Index for querying versions by purpose
            new(
                Builders<ConsentVersionDocument>.IndexKeys.Ascending(d => d.Purpose),
                new CreateIndexOptions { Name = "IX_ConsentVersions_Purpose" }
            ),
            // Compound index for finding the latest version per purpose
            new(
                Builders<ConsentVersionDocument>.IndexKeys
                    .Ascending(d => d.Purpose)
                    .Descending(d => d.EffectiveFromUtc),
                new CreateIndexOptions { Name = "IX_ConsentVersions_Purpose_EffectiveFromUtc" }
            )
        };

        await versionsCollection.Indexes.CreateManyAsync(versionIndexModels, cancellationToken).ConfigureAwait(false);

        Log.CreatedConsentIndexes(_logger);
    }
}
