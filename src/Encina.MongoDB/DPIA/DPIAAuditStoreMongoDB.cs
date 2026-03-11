using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.DPIA;

/// <summary>
/// MongoDB implementation of <see cref="IDPIAAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for DPIA assessment actions as required by
/// GDPR Article 5(2) (accountability principle). Uses MongoDB's native document
/// model for efficient append-only audit storage.
/// </para>
/// <para>
/// Uses <see cref="DPIAAuditEntryDocument"/> for MongoDB-native BSON serialization
/// and <c>InsertOneAsync</c> for atomic, append-only audit entry persistence.
/// </para>
/// </remarks>
public sealed class DPIAAuditStoreMongoDB : IDPIAAuditStore
{
    private readonly IMongoCollection<DPIAAuditEntryDocument> _collection;
    private readonly ILogger<DPIAAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public DPIAAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<DPIAAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<DPIAAuditEntryDocument>(config.Collections.DPIAAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAuditEntryAsync(
        DPIAAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = DPIAAuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Recorded DPIA audit entry '{EntryId}': {Action} for assessment '{AssessmentId}'",
                entry.Id, entry.Action, entry.AssessmentId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to record DPIA audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = entry.AssessmentId, ["action"] = entry.Action }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>> GetAuditTrailAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<DPIAAuditEntryDocument>.Filter.Eq(d => d.AssessmentId, assessmentId.ToString("D"));
            var sort = Builders<DPIAAuditEntryDocument>.Sort.Ascending(d => d.OccurredAtUtc);
            var documents = await _collection.Find(filter).Sort(sort)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = documents
                .Select(d => d.ToEntry())
                .Where(e => e is not null)
                .Cast<DPIAAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DPIAAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DPIAAuditEntry>>(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to retrieve DPIA audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }
}
