using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.BreachNotification;

/// <summary>
/// MongoDB implementation of <see cref="IBreachAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for breach notification actions as required by
/// GDPR Article 33(5) (documentation) and Article 5(2) (accountability principle).
/// Each <see cref="RecordAsync"/> call immediately persists the audit entry to ensure
/// it is never lost.
/// </para>
/// <para>
/// Uses MongoDB-specific features including filter builders for type-safe queries
/// and sort operations for ascending chronological order when retrieving audit trails.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the notification measures applied and may be required during supervisory authority
/// inquiries (Article 58) or compliance audits.
/// </para>
/// </remarks>
public sealed class BreachAuditStoreMongoDB : IBreachAuditStore
{
    private readonly IMongoCollection<BreachAuditEntryDocument> _collection;
    private readonly ILogger<BreachAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public BreachAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<BreachAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<BreachAuditEntryDocument>(config.Collections.BreachAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        BreachAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = BreachAuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Recorded breach audit entry '{EntryId}': {Action} for breach '{BreachId}'",
                entry.Id, entry.Action, entry.BreachId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create("breachnotification.audit_store_error",
                $"Failed to record breach audit entry: {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachAuditEntry>>> GetAuditTrailAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        try
        {
            var filter = Builders<BreachAuditEntryDocument>.Filter.Eq(d => d.BreachId, breachId);
            var sort = Builders<BreachAuditEntryDocument>.Sort.Ascending(d => d.OccurredAtUtc);
            var documents = await _collection
                .Find(filter)
                .Sort(sort)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<BreachAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachAuditEntry>>(
                EncinaErrors.Create("breachnotification.audit_store_error",
                    $"Failed to get audit trail for breach '{breachId}': {ex.Message}", ex));
        }
    }
}
