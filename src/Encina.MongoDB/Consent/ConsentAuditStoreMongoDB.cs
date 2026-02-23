using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Consent;

/// <summary>
/// MongoDB implementation of <see cref="IConsentAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for consent-related actions as required by
/// GDPR Article 7(1). Each <see cref="RecordAsync"/> call immediately persists
/// the audit entry to ensure it is never lost.
/// </para>
/// <para>
/// Uses MongoDB-specific features including filter builders for type-safe queries
/// and sort operations for descending chronological order.
/// </para>
/// </remarks>
public sealed class ConsentAuditStoreMongoDB : IConsentAuditStore
{
    private readonly IMongoCollection<ConsentAuditEntryDocument> _collection;
    private readonly ILogger<ConsentAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public ConsentAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ConsentAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ConsentAuditEntryDocument>(config.Collections.ConsentAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ConsentAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = ConsentAuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            Log.RecordedConsentAuditEntry(_logger, entry.Id, entry.SubjectId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "ConsentAuditStore.RecordAsync");
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to record consent audit entry: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentAuditEntry>>> GetAuditTrailAsync(
        string subjectId,
        string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var filterBuilder = Builders<ConsentAuditEntryDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.SubjectId, subjectId);

            if (purpose is not null)
            {
                filter &= filterBuilder.Eq(d => d.Purpose, purpose);
            }

            var documents = await _collection
                .Find(filter)
                .SortByDescending(d => d.OccurredAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();

            Log.RetrievedConsentAuditTrail(_logger, entries.Count, subjectId);
            return Right<EncinaError, IReadOnlyList<ConsentAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "ConsentAuditStore.GetAuditTrailAsync");
            return Left<EncinaError, IReadOnlyList<ConsentAuditEntry>>(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to retrieve consent audit trail: {ex.Message}"));
        }
    }
}
