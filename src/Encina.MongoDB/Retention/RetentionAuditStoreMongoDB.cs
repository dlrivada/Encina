using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB implementation of <see cref="IRetentionAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for retention-related actions as required by
/// GDPR Article 5(2) (accountability principle). Each <see cref="RecordAsync"/> call
/// immediately persists the audit entry to ensure it is never lost.
/// </para>
/// <para>
/// Uses MongoDB-specific features including filter builders for type-safe queries
/// and sort operations for ascending chronological order when retrieving audit trails.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the data retention measures applied and may be required during regulatory
/// audits or DPIA reviews (Article 35).
/// </para>
/// </remarks>
public sealed class RetentionAuditStoreMongoDB : IRetentionAuditStore
{
    private readonly IMongoCollection<RetentionAuditEntryDocument> _collection;
    private readonly ILogger<RetentionAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public RetentionAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<RetentionAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<RetentionAuditEntryDocument>(config.Collections.RetentionAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        RetentionAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = RetentionAuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Recorded retention audit entry '{EntryId}': {Action} for entity '{EntityId}'",
                entry.Id, entry.Action, entry.EntityId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("RecordAudit", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var filter = Builders<RetentionAuditEntryDocument>.Filter.Eq(d => d.EntityId, entityId);
            var sort = Builders<RetentionAuditEntryDocument>.Sort.Ascending(d => d.OccurredAtUtc);
            var documents = await _collection
                .Find(filter)
                .Sort(sort)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionAuditEntry>>(
                RetentionErrors.StoreError("GetAuditByEntityId", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sort = Builders<RetentionAuditEntryDocument>.Sort.Ascending(d => d.OccurredAtUtc);
            var documents = await _collection
                .Find(FilterDefinition<RetentionAuditEntryDocument>.Empty)
                .Sort(sort)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionAuditEntry>>(
                RetentionErrors.StoreError("GetAllAuditEntries", ex.Message, ex));
        }
    }
}
