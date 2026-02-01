using Encina.DomainModeling.Auditing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Auditing;

/// <summary>
/// MongoDB implementation of <see cref="IAuditLogStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses MongoDB-specific features:
/// <list type="bullet">
/// <item><description>BSON document serialization</description></item>
/// <item><description>Filter builders for type-safe queries</description></item>
/// <item><description>Indexes on (EntityType, EntityId) for efficient history lookups</description></item>
/// </list>
/// </para>
/// <para>
/// Each call to <see cref="LogAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class AuditLogStoreMongoDB : IAuditLogStore
{
    private readonly IMongoCollection<AuditLogDocument> _collection;
    private readonly ILogger<AuditLogStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public AuditLogStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<AuditLogStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<AuditLogDocument>(config.Collections.AuditLogs);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var document = AuditLogDocument.FromEntry(entry);

        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
        Log.AddedAuditLogEntry(_logger, entry.Id, entry.EntityType, entry.EntityId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(entityId);

        var filter = Builders<AuditLogDocument>.Filter.And(
            Builders<AuditLogDocument>.Filter.Eq(d => d.EntityType, entityType),
            Builders<AuditLogDocument>.Filter.Eq(d => d.EntityId, entityId));

        var documents = await _collection
            .Find(filter)
            .SortByDescending(d => d.TimestampUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Log.RetrievedAuditLogHistory(_logger, documents.Count, entityType, entityId);
        return documents.Select(d => d.ToEntry());
    }

    /// <inheritdoc />
#pragma warning disable CA1822 // Mark members as static - interface implementation
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
#pragma warning restore CA1822
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        _ = cancellationToken; // Unused but required by interface
        return Task.CompletedTask;
    }
}
