using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

using static LanguageExt.Prelude;

namespace Encina.MongoDB.DataResidency;

/// <summary>
/// MongoDB implementation of <see cref="IResidencyAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for data residency enforcement decisions as required by
/// GDPR Article 5(2) (accountability principle). Each <see cref="RecordAsync"/> call
/// immediately persists the audit entry to ensure it is never lost.
/// </para>
/// <para>
/// Per GDPR Article 30, controllers must maintain records of processing activities including
/// transfers of personal data to a third country. Per Articles 44-49 (Chapter V), all
/// cross-border transfers must be documented with the applicable legal basis.
/// </para>
/// <para>
/// Uses <see cref="ResidencyAuditEntryEntity"/> as the MongoDB document type and
/// <see cref="ResidencyAuditEntryMapper"/> for domain-entity conversion. Documents are stored
/// using default MongoDB serialization conventions.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of data residency compliance and may be required during regulatory audits or
/// supervisory authority inquiries (Article 58).
/// </para>
/// </remarks>
public sealed class ResidencyAuditStoreMongoDB : IResidencyAuditStore
{
    private readonly IMongoCollection<ResidencyAuditEntryEntity> _collection;
    private readonly ILogger<ResidencyAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public ResidencyAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ResidencyAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ResidencyAuditEntryEntity>(config.Collections.ResidencyAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ResidencyAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ResidencyAuditEntryMapper.ToEntity(entry);
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Recorded residency audit entry '{EntryId}': {Action} with outcome {Outcome} for category '{DataCategory}'",
                entry.Id, entry.Action, entry.Outcome, entry.DataCategory);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(DataResidencyErrors.StoreError("RecordAudit", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var filter = Builders<ResidencyAuditEntryEntity>.Filter.Eq(d => d.EntityId, entityId);
            var sort = Builders<ResidencyAuditEntryEntity>.Sort.Descending(d => d.TimestampUtc);
            var documents = await _collection
                .Find(filter)
                .Sort(sort)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents
                .Select(ResidencyAuditEntryMapper.ToDomain)
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(
                DataResidencyErrors.StoreError("GetAuditByEntity", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ResidencyAuditEntryEntity>.Filter.Gte(d => d.TimestampUtc, fromUtc) &
                         Builders<ResidencyAuditEntryEntity>.Filter.Lte(d => d.TimestampUtc, toUtc);
            var sort = Builders<ResidencyAuditEntryEntity>.Sort.Ascending(d => d.TimestampUtc);
            var documents = await _collection
                .Find(filter)
                .Sort(sort)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents
                .Select(ResidencyAuditEntryMapper.ToDomain)
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(
                DataResidencyErrors.StoreError("GetAuditByDateRange", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ResidencyAuditEntryEntity>.Filter.Eq(
                d => d.OutcomeValue, (int)ResidencyOutcome.Blocked);
            var sort = Builders<ResidencyAuditEntryEntity>.Sort.Descending(d => d.TimestampUtc);
            var documents = await _collection
                .Find(filter)
                .Sort(sort)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents
                .Select(ResidencyAuditEntryMapper.ToDomain)
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(
                DataResidencyErrors.StoreError("GetViolations", ex.Message, ex));
        }
    }
}
